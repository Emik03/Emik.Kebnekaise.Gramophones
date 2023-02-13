// SPDX-License-Identifier: MPL-2.0
#pragma warning disable SA1600
namespace Emik.Kebnekaise.Gramophones;

static class Searcher
{
    static readonly string[] s_banned = { "char", "env", "game", "menu", "sound", "sfx", "ui" };

    static IList<string?>? s_songs;

    internal static bool IsSorted { get; private set; } = true;

    internal static IList<string?> Song => s_songs = Songs().ToGuardedLazily();

    internal static void Rearrange() =>
        s_songs = ((IsSorted = !IsSorted)
            ? s_songs?.OrderBy(Self, StringComparer.OrdinalIgnoreCase)
            : s_songs?.Shuffle().AsEnumerable())?.ToGuardedLazily();

    static void Finish(List<string> list)
    {
        list.For(x => Logger.Log(nameof(Gramophone), x));
        Gramophone.Stop();
    }

    static string? Self(string? x) => x;

    static IEnumerable<string?> Songs()
    {
        static bool Desired(string x) => x.StartsWith("event:/") && !s_banned.Any(x.Contains);

        static bool HasParams(string x) => Please.Try(Audio.GetEventDescription, x).Ok is not null;

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
           .Peek(Finish);
    }
}
