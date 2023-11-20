// SPDX-License-Identifier: MPL-2.0
#pragma warning disable S2971, SA1600
namespace Emik.Kebnekaise.Gramophones;

// ReSharper disable once NullableWarningSuppressionIsUsed
static class Searcher
{
#if !NETCOREAPP
    static readonly FMOD.Studio.System s_system = (FMOD.Studio.System)typeof(Audio)
       .GetField("system", BindingFlags.NonPublic | BindingFlags.Static)!
       .GetValue(null);
#endif
    static readonly string[] s_banned = ["char", "env", "game", "menu", "sound", "sfx", "ui"];

    static string? s_previous;

    static IList<string?>? s_songs;

    internal static bool IsSearching { get; private set; }

    internal static bool IsSorted { get; private set; } = true;

    internal static IList<string?> Song => s_songs ??= Songs().ToGuardedLazily();

    internal static string Query { get; set; } = "";

    internal static void Process(char c, Action reload)
    {
        if (!TryInsert(c))
        {
            // Update description to reflect that we're no longer searching.
            reload();
            return;
        }

        if (s_songs is null)
            return;

        s_songs
           .OrderByDescending(x => Gramophone.MakeFriendly(x).Then(Score))
           .ThenBy(Self, StringComparer.OrdinalIgnoreCase)
           .ToList()
           .For((x, i) => s_songs[i] = x);

        reload();
    }

    // ReSharper disable once AssignmentInConditionalExpression
    internal static void Rearrange()
    {
        if (s_songs is null)
            return;

        var sorted = (IsSorted = !IsSorted)
            ? s_songs.OrderBy(Self, StringComparer.OrdinalIgnoreCase)
            : s_songs.Shuffle() as IEnumerable<string?>;

        sorted.ToList().For((x, i) => s_songs[i] = x);
    }

    internal static bool IsBanned(this string? path) => path is not null && Array.Exists(s_banned, path.Contains);

    internal static double Score(string? s) => s.JaroEmik(Query, CharComparer.Default);

    static void Play(string path)
    {
        if (!IsSearching)
            return;

        Audio.Play(path);
    }

    static bool HasSongGuids(ZipEntry x) => HasSongGuids(x.FileName);

    static bool HasSongGuids(string x) => x.EndsWith(".guids.txt", StringComparison.InvariantCultureIgnoreCase);

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

                if (IsSearching)
                    Query = "";

                Audio.Play(IsSearching ? In : Out);
                break;
            case Backspace:
                (Query.Length is not 0).Then(Play)?.Invoke(Delete);
                Query = Query.Backspace();
                break;
            case var _ when IsSearching &&
                !c.IsControl() &&
                !(c.IsWhitespace() && Query.Length is 0) &&
                Query.Length < Gramophone.MaxLength:
                Query += c;
                Play(c.IsWhitespace() ? Whitespace : Character);
                break;
        }

        return IsSearching;
    }

    static string Backspace(this string x) => x is "" ? x : x[..^1];

    static string? Self(string? x) => x;

    static EventDescription? GetEventDescription(string? path)
    {
        // Reimplementation of [Celeste]GetEventDescription, except it doesn't log when the event doesn't exist.
        const string Prefix = "guid://";
        EventDescription? ret = null;

        if (path is null || Audio.cachedEventDescriptions.TryGetValue(path, out ret))
            return ret;
#if NETCOREAPP
        var result = Audio.cachedModEvents.TryGetValue(path, out ret) ? RESULT.OK :
            path.StartsWith(Prefix) ? Audio.System.getEventByID(new(path[Prefix.Length..]), out ret) :
            Audio.System.getEvent(path, out ret);
#else
        var result = path.StartsWith(Prefix)
            ? s_system.getEventByID(new(path[Prefix.Length..]), out ret)
            : s_system.getEvent(path, out ret);
#endif
        if (result is not RESULT.OK)
            return ret;

        _ = ret.loadSampleData(); // I have no idea if this method is pure or not, but I call it anyway.
        Audio.cachedEventDescriptions.Add(path, ret);
        return ret;
    }
#pragma warning disable CA1859
    static IEnumerable<string?> Songs()
#pragma warning restore CA1859
    {
        static void Finish(List<string> list)
        {
            list.ForEach(x => Logger.Log(nameof(Gramophone), x));

            if (Audio.CurrentMusic != s_previous)
                Audio.SetMusic(s_previous);
        }

        static bool Desired(string x) => x.StartsWith("event:/") && !Array.Exists(s_banned, x.Contains);

        static bool HasParams(string x)
        {
            const int MinimumAudioLength = 15 * 1000;

            s_previous ??= Audio.CurrentMusic;

            if (GetEventDescription(x) is not { } description)
                return false;

            if (description.getLength(out var milliseconds) is RESULT.OK && milliseconds >= MinimumAudioLength)
                return true;

            return description.getParameterCount(out var count) is RESULT.OK && count > 0;
        }

        static IEnumerable<string> Read(ZipEntry x)
        {
            using var stream = x.OpenReader();
            using StreamReader reader = new(stream);
            return reader.ReadToEnd().Split();
        }

        static IEnumerable<string> Local() =>
            Please
               .Try(Directory.GetFiles, PathMods, "*.txt", SearchOption.AllDirectories)
               .SelectMany(Enumerable.AsEnumerable)
               .Where(HasSongGuids)
               .SelectMany(x => Please.Try(File.ReadAllText, x))
               .SelectMany(x => x.Split());

        static IEnumerable<ZipFile> AllFiles(string? x) => Please.Try(ZipFile.Read, x);

        return Please
           .Try(Directory.GetFiles, PathMods, "*.zip", SearchOption.AllDirectories)
           .SelectMany(Enumerable.AsEnumerable)
           .Select(AllFiles)
           .SelectMany(Enumerable.AsEnumerable)
           .SelectMany(Enumerable.AsEnumerable)
           .Where(HasSongGuids)
           .SelectMany(Read)
           .Concat(Local())
           .Where(Desired)
           .ToSet(StringComparer.OrdinalIgnoreCase)
           .Where(HasParams)
           .OrderBy(Self, StringComparer.OrdinalIgnoreCase)
           .ToList()
           .Peek(Finish);
    }

    sealed class CharComparer : IEqualityComparer<char>
    {
        /// <summary>Gets the comparer.</summary>
        public static CharComparer Default { get; } = new();

        /// <inheritdoc />
        public bool Equals(char x, char y) => x.IsWhitespace() && y.IsWhitespace() || x.ToUpper() == y.ToUpper();

        /// <inheritdoc />
        public int GetHashCode(char obj) => obj.IsWhitespace() ? '\t' : obj.ToUpper();
    }
}
