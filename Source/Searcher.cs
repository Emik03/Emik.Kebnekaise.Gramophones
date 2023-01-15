#region Emik.MPL

// <copyright file="Searcher.cs" company="Emik">
// Copyright (c) Emik. This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// </copyright>

#endregion

#pragma warning disable SA1600
namespace Emik.Kebnekaise.Gramophones;

static class Searcher
{
    static readonly string[] s_banned = { "char", "env", "game", "menu", "sound", "sfx", "ui" };

    static IList<string?>? s_songs;

    internal static bool IsSorted { get; private set; } = true;

    internal static IList<string?> Song => s_songs ??= Songs().ToGuardedLazily();

    internal static void Rearrange() => // ReSharper disable once AssignmentInConditionalExpression
        s_songs = ((IsSorted = !IsSorted)
                ? s_songs?.OrderBy(Self, StringComparer.OrdinalIgnoreCase)
                : s_songs?.Shuffle() as IEnumerable<string?>)
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

        return Please
           .Try(Directory.GetFiles, PathMods, "*.zip")
           .SelectMany(Enumerable.AsEnumerable)
           .Select(AllFiles)
           .SelectMany(Enumerable.AsEnumerable)
           .SelectMany(Enumerable.AsEnumerable)
           .Where(HasSongGuids)
           .SelectMany(Read)
           .Where(Desired)
           .Where(HasParams)
           .Distinct(StringComparer.OrdinalIgnoreCase)
           .OrderBy(Self, StringComparer.OrdinalIgnoreCase)
           .ToList()
           .For(Log);
    }

    static string? Self(string? x) => x;
}
