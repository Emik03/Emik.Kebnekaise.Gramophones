// SPDX-License-Identifier: MPL-2.0
#pragma warning disable CS1591, SA1600
namespace Emik.Kebnekaise.Gramophones;

[CLSCompliant(false)]
public sealed class GramophoneModule : EverestModule
{
    public GramophoneModule() => Instance = this;

    public static GramophoneModule Instance { get; private set; } = new();

    public static GramophoneSettings Settings => (GramophoneSettings)Instance._Settings;

    public override Type SettingsType => typeof(GramophoneSettings);

    [Command("gramophone_ambience", "[Gramophone] Mutes the ambience")]
    public static void Ambience() => Gramophone.MuteAmbience();

    [Command("gramophone_play", "[Gramophone] Play a song")]
    public static void Play(string? song) => Gramophone.Play(song);

    [Command("gramophone_change", "[Gramophone] Change a parameter in the song")]
    public static void Change(string? param, string? value) => Gramophone.SetParam(param, value);

    [Command("gramophone_trigger", "[Gramophone] Toggles if the map is allowed to change song parameter values")]
    public static void Inhibit() => Gramophone.Inhibit();

    [Command("gramophone_stop", "[Gramophone] Stop a song")]
    public static void Stop() => Gramophone.Stop();

    [Command("gramophone_alternate", "[Gramophone] Toggles whether to play through the main or cassette channel.")]
    public static void UseAlt() => Gramophone.UseAlt();

    public override void CreateModMenuSection(TextMenu? menu, bool inGame, EventInstance? snapshot)
    {
        base.CreateModMenuSection(menu, inGame, snapshot);

        new[]
        {
            (Item)Gramophone.Fallback,
            new OnOff(Localized.Enable, Settings.Enabled).Change(x => Settings.Enabled = x),
            new OnOff(Localized.Menu, Settings.Menu).Change(x => Settings.Menu = x),
            new OnOff(Localized.Alt, Settings.Alt).Change(x => Settings.Alt = x),
            new OnOff(Localized.Params, Settings.Inhibit).Change(Gramophone.Inhibit),
        }.For(x => menu?.Add(x));
    }

    public override void Load()
    {
        AudioState.Apply += Gramophone.Apply;
        OnAudio.SetMusic += Gramophone.SetMusic;
        OnAudio.SetAltMusic += Gramophone.SetAltMusic;
        OnAudio.SetParameter += Gramophone.SetParameter;
        OnAudio.SetMusicParam += Gramophone.SetMusicParam;
        Everest.Events.Level.OnCreatePauseMenuButtons += Gramophone.Pause;
    }

    public override void Unload()
    {
        Gramophone.Stop();

        AudioState.Apply -= Gramophone.Apply;
        OnAudio.SetMusic -= Gramophone.SetMusic;
        OnAudio.SetAltMusic -= Gramophone.SetAltMusic;
        OnAudio.SetParameter -= Gramophone.SetParameter;
        OnAudio.SetMusicParam -= Gramophone.SetMusicParam;
        Everest.Events.Level.OnCreatePauseMenuButtons -= Gramophone.Pause;
    }
}
