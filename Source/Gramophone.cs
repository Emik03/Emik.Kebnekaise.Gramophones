// SPDX-License-Identifier: MPL-2.0
#pragma warning disable SA1600
namespace Emik.Kebnekaise.Gramophones;

static class Gramophone
{
    public const int MaxLength = 25;

    static readonly Action s_endSnapshot = (Action)Delegate.CreateDelegate(
        typeof(Action), // ReSharper disable once NullableWarningSuppressionIsUsed
        typeof(Audio).GetMethod("EndMainDownSnapshot", BindingFlags.Static | BindingFlags.NonPublic)!
    );

    static readonly Dictionary<string, string> s_friendly = new(StringComparer.OrdinalIgnoreCase);

    static bool s_hasInit;

    // Do not inline. This exists purely for lifetime reasons. (to prevent GC from collecting)
    static IList<Item>? s_items;

    static IList<Item>? s_old;

    static IList<ParameterInstance>? s_parameters;

    internal static SubHeader Fallback =>
        new(((string)Localized.Previous).Replace(Localized.SearchTemplate, MakeFriendly(Previous)), false);

    internal static bool IsPlaying { get; set; }

    internal static bool? IsPaused => Audio.CurrentAmbienceEventInstance.getPaused(out var x) is RESULT.OK ? x : null;

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
           .OrderBy(Name, StringComparer.OrdinalIgnoreCase)
           .ToList();
    }

    internal static void SetAltMusic(OnAudio.orig_SetAltMusic? orig, string? path) =>
        (GramophoneModule.Settings.Alt || !IsPlaying).Then(orig)?.Invoke(path);

    internal static void SetMusicParam(OnAudio.orig_SetMusicParam? orig, string? path, float value)
    {
        if (!IsPlaying || path.IsBanned())
            orig?.Invoke(path, value);

        if (GramophoneModule.Settings.Inhibit)
            SetParam(path, value);
    }

    internal static void SetParam(string? param, string? value)
    {
        _ = float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v);
        SetParam(param, v);
    }

    internal static void SetParam(string? param, float value, EventInstance? instance = null)
    {
        static double Score(string? param, ParameterInstance x)
        {
            x.getDescription(out var d);
            return param.JaroEmik(d.name);
        }

        (instance ?? Audio.CurrentMusicEventInstance)
           .Parameters()
           .Where(x => x.Name() is not "fade")
           .MaxBy(x => Score(param, x))
          ?.setValue(value);
    }

    internal static void SetParameter(OnAudio.orig_SetParameter orig, EventInstance instance, string param, float value)
    {
        if (!IsPlaying || instance.IsBanned())
            orig(instance, param, value);

        if (GramophoneModule.Settings.Inhibit)
            SetParam(param, value, instance);
    }

    internal static bool SetMusic(OnAudio.orig_SetMusic? orig, string? path, bool startPlaying, bool allowFadeOut)
    {
        SetPrevious(path);
        return !IsPlaying && (orig?.Invoke(path, startPlaying, allowFadeOut) ?? false);
    }

    internal static void Stop() => Set(Previous, false);

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

    static void OverrideCassette()
    {
        Audio.SetAltMusic("");
        s_endSnapshot();
    }

    static void Set(string? path, bool isPlaying)
    {
        if (AudioSession is not { } audio)
            return;

        Audio.SetMusic(path);

        if (!GramophoneModule.Settings.Alt)
            OverrideCassette();

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

        // ReSharper disable once NullableWarningSuppressionIsUsed
#pragma warning disable RCS1249
        using var data = new DynData<Level>(level!);
#pragma warning restore RCS1249
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
        int NewValue() => (int)(Searcher.Score(song.Values.FirstOrDefault()?.Item1) * 100);

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

    static string Name(this ParameterInstance parameter)
    {
        parameter.getDescription(out var description);
        return description.name;
    }

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

    static Slider MakeSlider()
    {
        int index = Searcher.Song.IndexOf(Current) is var i && i is -1
                ? Searcher.Song.IndexOf(Audio.CurrentMusic) is var j && j is -1 ? 1 : j
                : i,
            upper = Searcher.Song.Count - 1;

        return new(Localized.Song, Friendly, 0, upper, index);
    }

    static TextMenu AddMenus(this TextMenu menu, EventInstance? pause)
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

        void Update() => UpdateDisplay(song, description);

        void Input(char c) => Searcher.Process(c, Update);

        Item Item(ParameterInstance p)
        {
            const int MaxValue = 1000;

            p.getValue(out var cur);

            var step = (float)GramophoneModule.Settings.Step;

            return new Slider(p.Name(), i => Math.Round(i / step, 2).Stringify(), 0, MaxValue, (int)(cur * step))
               .Change(i => p.setValue(i / step))
               .Enter(Enter)
               .Leave(Leave);
        }

        menu.AddItems(song.Change(Change).Enter(EnterSong).Leave(LeaveSong), description, Update);
        return menu;
    }
}
