// SPDX-License-Identifier: MPL-2.0
#pragma warning disable S1450, SA1600, RCS1249
namespace Emik.Kebnekaise.Gramophones;

// ReSharper disable NullableWarningSuppressionIsUsed
static class Gramophone
{
    public const int MaxLength = 25;

    static readonly Dictionary<string, string> s_friendly = new(StringComparer.OrdinalIgnoreCase);

    static readonly FieldInfo s_sfx =
        typeof(CassetteBlockManager).GetField("sfx", BindingFlags.NonPublic | BindingFlags.Instance)!;

    static readonly Converter<CassetteBlockManager, EventInstance?> s_sfxGetter = Getter();

    static readonly Action<CassetteBlockManager, EventInstance?> s_sfxSetter = Setter();

    static bool s_hasInit, s_hasOverridenCassette;

    // Do not inline. This exists purely for lifetime reasons. (to prevent GC from collecting)
    static IList<Item>? s_items;

    static IList<Item>? s_old;

    static IList<ParameterInstance>? s_parameters;

    internal static SubHeader Fallback =>
        new(((string)Localized.Previous).Replace(Localized.SearchTemplate, MakeFriendly(Previous)), false);

    internal static bool IsPlaying { get; set; }

    internal static bool? IsPaused => Audio.CurrentAmbienceEventInstance?.getPaused(out var x) is RESULT.OK ? x : null;

    internal static string? Previous { get; set; }

    internal static string? Current { get; set; }

    static string DescriptionText => Searcher.IsSearching ? Localized.Searching : Localized.Enter;

    static string ShuffleText => Searcher.IsSorted ? Localized.Shuffle : Localized.Sort;

    static Celeste.AudioState? AudioSession => (Engine.Scene as Level)?.Session.Audio;

    internal static void Apply(AudioState.orig_Apply? orig, Celeste.AudioState? self) =>
        (!IsPlaying).Then(orig)?.Invoke(self);

    internal static void Ambience() => Ambience(!IsPaused ?? true);

    internal static void Ambience(bool x)
    {
        Audio.CurrentAmbienceEventInstance.setPaused(x);

        if (x)
            Audio.CurrentAmbienceEventInstance.stop(STOP_MODE.ALLOWFADEOUT);
        else
            Audio.CurrentAmbienceEventInstance.start();
    }

    internal static void Inhibit() => Inhibit(!GramophoneModule.Settings.Inhibit);

    internal static void Inhibit(bool x) => GramophoneModule.Settings.Inhibit = x;

    internal static void UseAlt() => UseAlt(!GramophoneModule.Settings.Alt);

    internal static void UseAlt(bool x) => GramophoneModule.Settings.Alt = x;

    internal static void Pause(Level? level, TextMenu? menu, bool minimal)
    {
        Button button = new(Localized.Gramo);
        button.Pressed(Press);

        void Press()
        {
            if (level is null || menu is null)
                return;

            if (!s_hasInit && (s_hasInit = true))
                Play(Audio.CurrentMusic);

            menu.RemoveSelf();
            var i = menu.IndexOf(button);
            level.PauseMainMenuOpen = false;
            level.Screen(i, minimal);
        }

        _ = GramophoneModule.Settings.Menu.Then(() => menu?.Add(button));
    }

    internal static void Play(string? song)
    {
        if (!IsPlaying)
            Previous = Audio.CurrentMusic;

        // Temporarily assign to false to allow the song to be played.
        IsPlaying = false;
        Set(song, true);
        Current = song;

        s_parameters = Audio
           .CurrentMusicEventInstance
           .Parameters()
           .OrderBy(Extensions.Name, StringComparer.OrdinalIgnoreCase)
           .ToList();
    }

    internal static bool SetMusic(OnAudio.orig_SetMusic? orig, string? path, bool startPlaying, bool allowFadeOut)
    {
        SetPrevious(path);
        return !IsPlaying && (orig?.Invoke(path, startPlaying, allowFadeOut) ?? false);
    }

    internal static void Update(OnCassetteBlockManager.orig_Update orig, CassetteBlockManager self)
    {
        if (IsPlaying && GramophoneModule.Settings.Alt)
            Replace(self, Audio.CurrentMusicEventInstance, true);
        else if (s_hasOverridenCassette && Engine.Scene is Level { Session.Area.ID: var id })
#if NETCOREAPP
            Replace(self, Audio.CreateInstance(AreaData.Areas[id].CassetteSong), false);
#else
            Replace(self, Audio.Play(AreaData.Areas[id].CassetteSong), false);
#endif
        orig(self);
    }

    internal static void Stop() => Set(Previous, false);

    internal static RESULT SetParameterValue(
        On.FMOD.Studio.EventInstance.orig_setParameterValue orig,
        EventInstance self,
        string name,
        float f
    ) =>
        !IsPlaying || self.IsBanned() ? orig(self, name, f) :
        !GramophoneModule.Settings.Inhibit ? RESULT.OK : self.Parameters()
           .Omit(x => x.Name() is "fade")
           .MaxBy(x => name.JaroEmik(x.Name()))
          ?.setValue(f) ??
        orig(self, name, f);

    internal static RESULT SetValue(
        On.FMOD.Studio.ParameterInstance.orig_setValue orig,
        ParameterInstance self,
        float f
    ) =>
        !IsPlaying || self.Description() is var description && description.name.IsBanned() ? orig(self, f) :
        GramophoneModule.Settings.Inhibit &&
        description is { maximum: var max, minimum: var min } &&
        max - min is var mod ? orig(self, ((f - min) % mod + mod) % mod + min) : RESULT.OK;

    static void AddItems(this TextMenu menu, Item song, Item description, Action onChange)
    {
        Button shuffle = new(ShuffleText);

        var step = new Slider(Localized.Step, Stringifier.Stringify, 1, 20, GramophoneModule.Settings.Step)
           .Change(
                x =>
                {
                    GramophoneModule.Settings.Step = x;
                    onChange();
                }
            );

        _ = shuffle.Pressed(
            () =>
            {
                Searcher.Rearrange();
                shuffle.Label = ShuffleText;
                shuffle.Update();
                onChange();
            }
        );

        s_items = new List<Item>
        {
            new Header(Localized.Gramo),
            new SubHeader(Localized.Which),
            Fallback,
            new Button(Localized.Stop).Pressed(Stop),
            shuffle,
            new OnOff(Localized.Ambience, IsPaused ?? false).Pressed(Ambience),
            new OnOff(Localized.Params, GramophoneModule.Settings.Inhibit).Change(Inhibit),
            new OnOff(Localized.Alt, GramophoneModule.Settings.Alt).Change(UseAlt),
            step,
            song,
            description,
        };

        s_old?.For(s_items.Add);
        s_items.Select(menu.Add).Enumerate();
    }

    static void Replace(CassetteBlockManager self, EventInstance? other, bool hasOverridenCassette)
    {
        var sfx = s_sfxGetter(self);
        s_hasOverridenCassette = hasOverridenCassette;

        if (sfx == other)
            return;

        s_sfxSetter(self, other);
        sfx?.release();
    }

    static void Set(string? path, bool isPlaying)
    {
        if (AudioSession is not { } audio)
            return;

        Audio.SetMusic(path);
        audio.Music.Event = path;
        audio.Apply();
        IsPlaying = isPlaying;
    }

    static void SetPrevious(string? path)
    {
        _ = Searcher.Song;
        Previous = path;
    }

    static void Screen(this Level? level, int returnIndex, bool minimal)
    {
        const string PauseSnapshot = nameof(PauseSnapshot);

        using var data = new DynData<Level>(level!);
        var pause = data.Get<EventInstance>(PauseSnapshot);
        var menu = new TextMenu().AddMenus(pause);

        menu.OnESC = menu.OnCancel = () =>
        {
            menu.Shut();
            level?.Pause(returnIndex, minimal);
        };

        menu.OnPause = () =>
        {
            menu.Shut();
            Engine.FreezeTimer = 0.15f;
            _ = level is not null && (level.Paused = false);
        };

        level?.Add(menu);
    }

    static void UpdateDisplay(Slider? song, SubHeader? description)
    {
        int NewValue() => (int)(Searcher.Score(song.Values.Nth(0)?.Item1) * 100);

        if (song is null || description is null)
            return;

        var currentIndex = Searcher.Song.IndexOf(Current);

        if (currentIndex is -1)
            return;

        var upper = song.Values.Count;

        song.Index = Searcher.IsSearching ? 0 : currentIndex;
        song.OnValueChange(song.Index);
        upper.For(i => song.Values[i] = new(Friendly(i), i)).Enumerate();

        description.Title = DescriptionText
           .Replace(Localized.SearchTemplate, Searcher.Query)
           .Replace(Localized.PercentTemplate, $"{NewValue()}")
           .Replace(Localized.UpperTemplate, $"{upper + 1}");

        var container = description.Container;
        var containerIndex = container.IndexOf(description);

        if (containerIndex is -1)
            return;

        container.Remove(description);
        container.Insert(containerIndex, description);
    }

    static bool IsBanned(this EventInstance? instance) =>
        instance?.getDescription(out var a) is RESULT.OK && a.getPath(out var b) is RESULT.OK && b.IsBanned();

    internal static string Friendly(int i) => MakeFriendly(Searcher.Song[i]);

    internal static string MakeFriendly(string? s) =>
        s is null ? "" :
        s_friendly.TryGetValue(s, out var val) ? val :
        s_friendly[s] = s.Split(":/").LastOrDefault()?.StringHell() ?? Localized.None;

    static string StringHell(this string wide)
    {
        var seenSlash = false;

        return wide
           .Replace('_', ' ')
           .Replace('-', ' ')
           .Reverse()
           .SelectMany(x => x is '/' ? !seenSlash && (seenSlash = true) ? "\n" : " > " : $"{x}")
           .Reverse()
           .Concat()
           .Split('\n')
           .Select(x => x.Length <= MaxLength ? x : x.Reverse().Take(MaxLength).Append('\u2026').Reverse().Concat())
           .Conjoin('\n');
    }

    static Action<CassetteBlockManager, EventInstance?> Setter()
    {
        var info = typeof(CassetteBlockManager).GetField("sfx", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var cassette = Expression.Parameter(typeof(CassetteBlockManager));
        var instance = Expression.Parameter(typeof(EventInstance));
        var field = Expression.Field(cassette, info);
        var assign = Expression.Assign(field, instance);
        return Expression.Lambda<Action<CassetteBlockManager, EventInstance?>>(assign, cassette, instance).Compile();
    }

    static Converter<CassetteBlockManager, EventInstance?> Getter()
    {
        var cassette = Expression.Parameter(typeof(CassetteBlockManager));
        var field = Expression.Field(cassette, s_sfx);
        return Expression.Lambda<Converter<CassetteBlockManager, EventInstance?>>(field, cassette).Compile();
    }

    static Slider MakeSlider()
    {
        int index = Searcher.Song.IndexOf(Current) is var i && i is -1
                ? Searcher.Song.IndexOf(Audio.CurrentMusic) is var j && j is -1 ? 1 : j
                : i,
            upper = Searcher.Song.Count - 1;

        return new(Localized.Song, Friendly, 0, upper, index);
    }
#pragma warning disable MA0051
    static TextMenu AddMenus(this TextMenu menu, EventInstance? pause)
#pragma warning restore MA0051
    {
        var song = MakeSlider();
        SubHeader description = new(Localized.Enter, false);

        void Change(int x)
        {
            Play(Searcher.Song[x]);
            Refresh();
        }

        void Refresh()
        {
            s_old?.Select(menu.Remove).Enumerate();
            s_old = s_parameters?.Select(Item).ToList();
            s_old?.Select(menu.Add).Enumerate();
        }

        void Enter() => Audio.EndSnapshot(pause);

        void EnterSong()
        {
            Enter();
            TextInput.OnInput += Input;
        }

        void Leave() => Audio.ResumeSnapshot(pause);

        void LeaveSong()
        {
            Leave();
            TextInput.OnInput -= Input;
        }

        // ReSharper disable once LocalFunctionHidesMethod
        void Update() => UpdateDisplay(song, description);

        void Input(char c) => Searcher.Process(c, Update);

        Item Item(ParameterInstance p)
        {
            p.getValue(out var cur);
            var step = (float)GramophoneModule.Settings.Step;
            var description = p.Description();
            var min = (int)Math.Floor(description.minimum);
            var max = (int)Math.Ceiling(description.maximum);

            void Change(int i)
            {
                var old = GramophoneModule.Settings.Inhibit;
                GramophoneModule.Settings.Inhibit = true;
                p.setValue(i / step);
                GramophoneModule.Settings.Inhibit = old;
            }

            return new Slider(p.Name(), i => Math.Round(i / step, 2).Stringify(), min, max, (int)(cur * step))
               .Change(Change)
               .Enter(Enter)
               .Leave(Leave);
        }

        menu.AddItems(song.Change(Change).Enter(EnterSong).Leave(LeaveSong), description, Update);
        return menu;
    }
}
