// SPDX-License-Identifier: MPL-2.0
#pragma warning disable SA1600
namespace Emik.Kebnekaise.Gramophones;

static class Gramophone
{
    public const int MaxLength = 30;

    static readonly Dictionary<string, string> s_friendly = new(StringComparer.OrdinalIgnoreCase);

    // Do not inline. This exists purely for lifetime reasons. (to prevent GC from collecting)
    static IList<Item>? s_items;

    static IList<Item>? s_old;

    static IList<ParameterInstance>? s_parameters;

    internal static bool IsPlaying { get; set; }

    internal static string? Previous { get; set; }

    internal static string? Current { get; set; }

    static Celeste.AudioState? AudioSession => (Engine.Scene as Level)?.Session.Audio;

    static Localized.LocalString Label => Searcher.IsSorted ? Localized.Shuffle : Localized.Sort;

    internal static void Apply(AudioState.orig_Apply? orig, Celeste.AudioState? self) =>
        (!IsPlaying).Then(orig)?.Invoke(self);

    internal static void Inhibit() => Inhibit(!GramophoneModule.Settings.Inhibit);

    internal static void Inhibit(bool x) => GramophoneModule.Settings.Inhibit = x;

    internal static void MuteAmbience() => Audio.CurrentAmbienceEventInstance?.stop(STOP_MODE.ALLOWFADEOUT);

    internal static void Pause(Level? level, TextMenu? menu, bool minimal)
    {
        Button button = new(Localized.Gramo);
        button.Pressed(Press);

        void Press()
        {
            if (level is null || menu is null)
                return;

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

    internal static void SetMusicParam(OnAudio.orig_SetMusicParam? orig, string? path, float value) =>
        (GramophoneModule.Settings.Inhibit || !IsPlaying || path.IsBanned()).Then(orig)?.Invoke(path, value);

    internal static void SetParameter(
        OnAudio.orig_SetParameter orig,
        EventInstance instance,
        string param,
        float value
    ) =>
        (GramophoneModule.Settings.Inhibit || !IsPlaying || instance.IsBanned()).Then(orig)
      ?.Invoke(instance, param, value);

    internal static void SetParam(string? param, string? value)
    {
        if (param is null)
            return;

        _ = float.TryParse(value, out var v);

        s_parameters
          ?.Where(
                x =>
                {
                    x.getDescription(out var d);
                    return param.Equals(d.name);
                }
            )
           .For(x => x.setValue(v));
    }

    internal static void Stop() => Set(Previous, false);

    internal static bool SetMusic(OnAudio.orig_SetMusic? orig, string? path, bool startPlaying, bool allowFadeOut)
    {
        if (!IsPlaying)
            return orig?.Invoke(path, startPlaying, allowFadeOut) ?? false;

        Previous = path;

        return false;
    }

    static void AddItems(this TextMenu menu, Item song, Item description, Action onChange)
    {
        Button shuffle = new(Label);

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
                shuffle.Label = Label;
                shuffle.Update();
                onChange();
            }
        );

        s_items = new List<Item>
        {
            new Header(Localized.Gramo),
            new SubHeader(Localized.Which),
            new Button(Localized.Stop).Pressed(Stop),
            new Button(Localized.Ambience).Pressed(MuteAmbience),
            shuffle,
            new OnOff(Localized.Params, GramophoneModule.Settings.Inhibit).Change(Inhibit),
            step,
            song,
            description,
        };

        s_old?.For(s_items.Add);
        s_items.Select(menu.Add).Enumerate();
    }

    static void Set(string? path, bool isPlaying)
    {
        if (AudioSession is not { } audio)
            return;

        Audio.SetMusic(path);
        IsPlaying = isPlaying;
        audio.Music.Event = path;
        audio.Apply();
    }

    static void Screen(this Level? level, int returnIndex, bool minimal)
    {
        const string PauseSnapshot = nameof(PauseSnapshot);

        using var data = new DynData<Level?>(level);

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
        if (song is null || description is null)
            return;

        song.Index = Searcher.IsSearching ? 0 : Searcher.Song.IndexOf(Current);
        song.OnValueChange(song.Index);
        song.Values.Count.For(i => song.Values[i] = new(Friendly(i), i)).Enumerate();
        description.Title = DescriptionTitle().Replace(Localized.SearchTemplate, Searcher.Query);

        Logger.Log(LogLevel.Error, nameof(Gramophone), Searcher.Query);

        var container = description.Container;
        var i = container.IndexOf(description);

        container.Remove(description);
        container.Insert(i, description);
    }

    static bool IsBanned(this EventInstance? instance) =>
        instance?.getDescription(out var a) is RESULT.OK && a.getPath(out var b) is RESULT.OK && b.IsBanned();

    static string DescriptionTitle() => Searcher.IsSearching ? Localized.Searching : Localized.Enter;

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
           .Conjoin()
           .Split('\n')
           .Select(x => x.Length <= MaxLength ? x : x.Reverse().Take(MaxLength).Append('\u2026').Reverse().Conjoin())
           .Conjoin('\n');
    }

    static Slider MakeSlider()
    {
        int index = Searcher.Song.IndexOf(Current),
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
