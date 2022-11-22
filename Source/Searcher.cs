// <copyright file="Searcher.cs" company="Emik">
// Copyright (c) Emik. This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// </copyright>
#pragma warning disable SA1600
namespace Emik.Kebnekaise.Gramophones;

static class Searcher
{
    static readonly string[] s_banned = { "char", "env", "game", "sound", "sfx", "ui" };

    static IList<string?>? s_songs;

    internal static bool IsSorted { get; private set; } = true;

    internal static IList<string?> Song => s_songs ??= Songs().ToGuardedLazily();

    internal static void Rearrange() => // ReSharper disable once AssignmentInConditionalExpression
        s_songs = ((IsSorted = !IsSorted) ? s_songs?.OrderBy(Sorter) : s_songs?.Shuffle() as IEnumerable<string?>)
          ?.ToGuardedLazily();

    static IEnumerable<string?> Songs()
    {
        static void Log(string? str) => Logger.Log(nameof(Gramophone), str);

        static bool Desired(string x) => x.StartsWith("event:/") && !s_banned.Any(x.Contains);

        static bool HasParams(string x) => Please.Try(() => Audio.CreateInstance(x)).Ok.Parameters().Any();

        static bool HasSongGuids(ZipEntry x) => x.FileName.EndsWith(".guids.txt");

        static IEnumerable<string> Read(ZipEntry x)
        {
            using var stream = x.OpenReader();
            using StreamReader reader = new(stream);
            return reader.ReadToEnd().Split();
        }

        static IEnumerable<ZipFile> AllFiles(string? x) => Please.Try(ZipFile.Read, x);

        const string Zip = "*.zip";

        var location = Everest.Loader.PathMods;

#pragma warning disable MA0029
        return Please
#pragma warning restore MA0029
           .Try(Directory.GetFiles, location, Zip)
           .SelectMany(Enumerable.AsEnumerable)
           .Select(AllFiles)
           .SelectMany(Enumerable.AsEnumerable)
           .SelectMany(Enumerable.AsEnumerable)
           .Where(HasSongGuids)
           .SelectMany(Read)
           .Where(Desired)
           .Where(HasParams)
           .Distinct(StringComparer.OrdinalIgnoreCase)
           .OrderBy(Sorter)
           .ToList()
           .For(Log);
    }

    static string? Sorter(string? x) => x;
}
