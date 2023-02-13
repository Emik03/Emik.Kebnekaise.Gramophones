// SPDX-License-Identifier: MPL-2.0
#pragma warning disable SA1600
namespace Emik.Kebnekaise.Gramophones;

static class Searcher
{
    static readonly string[] s_banned = { "char", "env", "game", "menu", "sound", "sfx", "ui" };

    static readonly CaseInsensitiveCharComparer s_comparer = new();

    static string? s_previous;

    static IList<string?>? s_songs;

    internal static bool IsSearching { get; private set; }

    internal static bool IsSorted { get; private set; } = true;

    internal static IList<string?> Song => s_songs ??= Songs().ToGuardedLazily();

    internal static string Query { get; set; } = "";

    internal static void Process(char c, Action reload)
    {
        if (!TryInsert(c))
            return;

        s_songs
          ?.ToList()
           .OrderByDescending(x => Gramophone.MakeFriendly(x).Jaro(Query, s_comparer))
           .ThenBy(Self, StringComparer.OrdinalIgnoreCase)
           .For((x, i) => s_songs[i] = x);

        reload();
    }

    internal static void Rearrange() => // ReSharper disable once AssignmentInConditionalExpression
        s_songs = ((IsSorted = !IsSorted)
            ? s_songs?.OrderBy(Self, StringComparer.OrdinalIgnoreCase)
            : s_songs?.Shuffle() as IEnumerable<string?>)?.ToGuardedLazily();

    static void Play(string path)
    {
        if (!IsSearching)
            return;

        Audio.Play(path);
    }

    static bool TryInsert(char c)
    {
        const char
            Backspace = '\b',
            Enter = '\n',
            Return = '\r';

        const string
            Character = "event:/ui/main/rename_entry_char",
            Delete = "event:/ui/main/rename_entry_backspace",
            In = "event:/ui/main/savefile_rename_start",
            Out = "event:/ui/main/rename_entry_accept",
            Whitespace = "event:/ui/main/rename_entry_space";

        switch (c)
        {
            case Enter or Return:
                MInput.Active = !(MInput.Disabled = IsSearching = !IsSearching);
                Audio.Play(IsSearching ? In : Out);
                break;
            case Backspace:
                (Query.Length is not 0).Then(Play)?.Invoke(Delete);
                Query = Query.Backspace();
                break;
            case var _ when !c.IsControl() && IsSearching && !(c.IsWhitespace() && Query.Length is 0):
                Query = c.ToString();
                Play(c.IsWhitespace() ? Whitespace : Character);
                break;
        }

        return IsSearching;
    }

    static string? Self(string? x) => x;

    // ReSharper disable once UnusedMethodReturnValue.Local
    static string Backspace(this string sb) => sb.Length is 0 ? sb : sb[..^1];

    static IEnumerable<string?> Songs()
    {
        static void Finish(List<string> list) => list.For(x => Logger.Log(nameof(Gramophone), x));

        static bool Desired(string x) => x.StartsWith("event:/") && !s_banned.Any(x.Contains);

        static bool HasParams(string x)
        {
            s_previous ??= Audio.CurrentMusic;
            return Please.Try(Audio.GetEventDescription, x).Ok is not null;
        }

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

    sealed class CaseInsensitiveCharComparer : IEqualityComparer<char>
    {
        /// <inheritdoc />
        public bool Equals(char x, char y) => x.ToUpper() == y.ToUpper();

        /// <inheritdoc />
        public int GetHashCode(char obj) => obj.ToUpper();
    }
}
