// SPDX-License-Identifier: MPL-2.0
#pragma warning disable CS1591, SA1600
namespace Emik.Kebnekaise.Gramophones;

// ReSharper disable NullableWarningSuppressionIsUsed
[CLSCompliant(false)]
public sealed class GramophoneModule : EverestModule
{
    public GramophoneModule() => Instance = this;

    public static GramophoneModule Instance { get; private set; } = new();

    public static GramophoneSettings Settings => (GramophoneSettings?)Instance._Settings ?? new();

    public override Type SettingsType => typeof(GramophoneSettings);

    [Command("gramophone_ambience", "[Gramophone] Toggles the ambience")]
    public static void Ambience() => Gramophone.Ambience();

    [Command("gramophone_play", "[Gramophone] Play a song")]
    public static void Play(string song) => Gramophone.Play(song);

    [Command("gramophone_change", "[Gramophone] Change a parameter in the song")]
    public static void Change(string param, float value) =>
        Audio.CurrentMusicEventInstance?.setParameterValue(param, value);

    [Command("gramophone_trigger", "[Gramophone] Toggles if the map is allowed to change song parameter values")]
    public static void Inhibit() => Gramophone.Inhibit();

    [Command("gramophone_stop", "[Gramophone] Stop a song")]
    public static void Stop() => Gramophone.Stop();

    [Command("gramophone_cassette", "[Gramophone] Toggles whether cassette music can override Gramophone.")]
    public static void UseAlt() => Gramophone.UseAlt();

    public override void CreateModMenuSection(TextMenu? menu, bool inGame, EventInstance? snapshot)
    {
        base.CreateModMenuSection(menu!, inGame, snapshot!);
        menu?.AddMenus(snapshot);
    }

    public override void Load()
    {
        AudioState.Apply += Gramophone.Apply;
        OnAudio.SetMusic += Gramophone.SetMusic;
        OnCassetteBlockManager.Update += Gramophone.Update;
        OnParameterInstance.setValue += Gramophone.SetValue;
        Everest.Events.Level.OnCreatePauseMenuButtons += Gramophone.Pause;
        OnEventInstance.setParameterValue += Gramophone.SetParameterValue;
    }

    public override void Unload()
    {
        Gramophone.Stop();

        AudioState.Apply -= Gramophone.Apply;
        OnAudio.SetMusic -= Gramophone.SetMusic;
        OnCassetteBlockManager.Update -= Gramophone.Update;
        OnParameterInstance.setValue -= Gramophone.SetValue;
        Everest.Events.Level.OnCreatePauseMenuButtons -= Gramophone.Pause;
        OnEventInstance.setParameterValue -= Gramophone.SetParameterValue;
    }
}
