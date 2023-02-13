// SPDX-License-Identifier: MPL-2.0
#pragma warning disable SA1600
namespace Emik.Kebnekaise.Gramophones;

static class Gramophone
{
    const int MaxLength = 30;

    // Do not inline. This exists purely for lifetime reasons. (to prevent GC from collecting)
    static IList<Item>? s_items, s_old;

    static IList<ParameterInstance>? s_parameters;

    internal static bool IsPlaying { get; set; }

    internal static string? Previous { get; set; }

    internal static string? Current { get; set; }

    internal static void Apply(AudioState.orig_Apply? orig, Celeste.AudioState? self)
    {
        if (!IsPlaying)
            orig?.Invoke(self);
    }

    internal static void MuteAmbience() => NewAudioState(ambience: "").Apply();

    internal static void Pause(Level? level, TextMenu? menu, bool minimal)
    {
        var button = new Button(Localized.Gramo);
        button.Pressed(Press);

        void Press()
        {
            if (level is null || menu is null)
                return;

            menu.RemoveSelf();
            var self = menu.IndexOf(button);

            level.PauseMainMenuOpen = false;
            level.Screen(self, minimal);
        }

        if (GramophoneModule.Settings.Visible)
            menu?.Add(button);
    }

    internal static void Play(string? song)
    {
        if (!IsPlaying)
            Previous = Audio.CurrentMusic;

        // Temporarily assign to false to allow the song to be played.
        IsPlaying = false;
        NewAudioState(Previous).Apply();
        IsPlaying = true;

        Current = song;

        s_parameters = Audio
           .CurrentMusicEventInstance
           .Parameters()
           .OrderBy(Name, StringComparer.OrdinalIgnoreCase)
           .ToList();
    }

    internal static void SetMusicParam(OnAudio.orig_SetMusicParam? orig, string? path, float value) =>
        (GramophoneModule.Settings.Inhibit || !IsPlaying).Then(orig)?.Invoke(path, value);

    internal static void SetParameter(
        OnAudio.orig_SetParameter orig,
        EventInstance instance,
        string param,
        float value
    ) =>
        (GramophoneModule.Settings.Inhibit || !IsPlaying).Then(orig)?.Invoke(instance, param, value);

    internal static void SetParam(string? param, string? value)
    {
        if (param is null)
            return;

        _ = float.TryParse(value, out var v);

        s_parameters
          ?.Where(x => x.getDescription(out var d) is RESULT.OK && d.name == param)
           .For(x => x.setValue(v));
    }

    internal static void Stop()
    {
        IsPlaying = false;
        NewAudioState(Previous).Apply();
    }

    internal static bool SetMusic(OnAudio.orig_SetMusic? orig, string? path, bool startPlaying, bool allowFadeOut)
    {
        _ = Searcher.Song;

        if (!IsPlaying)
            return orig?.Invoke(path, startPlaying, allowFadeOut) ?? false;

        Previous = path;

        return false;
    }

    internal static void AddItems(this TextMenu menu, Item song, Action onChange)
    {
        Button shuffle = new(Localized.DynamicShuffle);

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
                shuffle.Label = Localized.DynamicShuffle;
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
            new OnOff(Localized.Inhibit, GramophoneModule.Settings.Inhibit).Change(GramophoneModule.ChangeInhibit),
            step,
            song,
        };

        s_items.Select(menu.Add).Enumerate();
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

    static string CurrentAmbience() =>
        Audio.CurrentAmbienceEventInstance?.getDescription(out var a) is RESULT.OK &&
        a?.getPath(out var path) is RESULT.OK
            ? path
            : "";

    static string Friendly(int i) => MakeFriendly(Searcher.Song[i]);

    static string MakeFriendly(string? s) => s?.Split(":/").LastOrDefault()?.StringHell() ?? Localized.None;

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

    static string Name(this ParameterInstance parameter)
    {
        parameter.getDescription(out var description);
        return description.name;
    }

    static Celeste.AudioState NewAudioState(string? music = null, string? ambience = null) =>
        new(music ?? Audio.CurrentMusic, ambience ?? CurrentAmbience());

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
