// SPDX-License-Identifier: MPL-2.0
#pragma warning disable SA1600
namespace Emik.Kebnekaise.Gramophones;

static class Gramophone
{
    const int MaxLength = 31;

    static bool s_inhibit;

    // Do not inline. This exists purely for lifetime reasons. (to prevent GC from collecting)
    static IList<Item>? s_items;

    static IList<Item>? s_old;

    static IList<ParameterInstance>? s_parameters;

    internal static bool IsPlaying { get; set; }

    internal static string? Previous { get; set; }

    internal static string? Current { get; set; }

    static Celeste.AudioState? AudioSession => (Engine.Scene as Level)?.Session.Audio;

    static Localized.LocalString Label => Searcher.IsSorted ? Localized.Shuffle : Localized.Sort;

    internal static void Apply(AudioState.orig_Apply? orig, Celeste.AudioState? self)
    {
        if (!IsPlaying)
            orig?.Invoke(self);
    }

    internal static void Inhibit() => s_inhibit = !s_inhibit;

    internal static void MuteAmbience() => Audio.CurrentAmbienceEventInstance?.stop(STOP_MODE.ALLOWFADEOUT);

    internal static void Pause(Level? level, TextMenu? menu, bool minimal)
    {
        Item? item = null;

        void Press()
        {
            menu.RemoveSelf();

            if (level is null)
                return;

            var i = menu.IndexOf(item);
            level.PauseMainMenuOpen = false;
            level.Screen(i, minimal);
        }

        _ = GramophoneModule.Settings.Menu.Then(() => menu?.Add(new Button(Localized.Gramo).Pressed(Press)));
    }

    internal static void Play(string? song)
    {
        _ = IsPlaying.NotThen(() => Previous = Audio.CurrentMusic);

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
        (s_inhibit || !IsPlaying).Then(orig)?.Invoke(path, value);

    internal static void SetParameter(
        OnAudio.orig_SetParameter orig,
        EventInstance instance,
        string param,
        float value
    ) =>
        (s_inhibit || !IsPlaying).Then(orig)?.Invoke(instance, param, value);

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

    static void AddItems(this TextMenu menu, Item song, Action onChange)
    {
        var shuffle = new Button(Label);

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

        s_items = new[]
        {
            new Header(Localized.Gramo),
            new SubHeader(Localized.Which),
            new Button(Localized.Stop).Pressed(Stop),
            new Button(Localized.Ambience).Pressed(MuteAmbience),
            shuffle,
            new OnOff(Localized.Params, s_inhibit).Change(x => s_inhibit = x),
            step,
            song,
        };

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

    static string Friendly(int i) =>
        i.Index()?.Replace("music:/", "").Replace("event:/", "") is { } wide
            ? (wide.Reverse().Take(MaxLength) is var thin &&
                wide.Length > MaxLength
                    ? thin.Concat(new[] { '\u2026' })
                    : thin)
           .Reverse()
           .Conjoin()
            : Localized.None;

    static string Name(this ParameterInstance parameter)
    {
        parameter.getDescription(out var description);
        return description.name;
    }

    static Slider MakeSlider()
    {
        int index = Searcher.Song.IndexOf(Current),
            upper = Searcher.Song.Count - 1;

        return new(Localized.Song, Friendly, 0, upper, index);
    }

    static TextMenu AddMenus(this TextMenu menu, EventInstance? pause)
    {
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

        void Leave() => Audio.ResumeSnapshot(pause);

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

        var song = MakeSlider();
        _ = song.Change(Change).Enter(Enter).Leave(Leave);

        menu.AddItems(
            song,
            () =>
            {
                song.OnValueChange(Searcher.Song.IndexOf(Current));

                song.Values.Clear();
                Searcher.Song.Count.For(x => song.Add(Friendly(x), x, x is 0)).Enumerate();
            }
        );

        Refresh();

        return menu;
    }
}
