// <copyright file="GramophoneModule.cs" company="Emik">
// Copyright (c) Emik. This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// </copyright>
#pragma warning disable CS1591, SA1600
namespace Emik.Kebnekaise.Gramophones;

[CLSCompliant(false)]
public sealed class GramophoneModule : EverestModule
{
    public GramophoneModule() => Instance = this;

    public static GramophoneModule Instance { get; private set; } = new();

    public static GramophoneSettings Settings => (GramophoneSettings)Instance._Settings;

    public override Type SettingsType => typeof(GramophoneSettings);

    [Command("gramophone_play", "[Gramophone] Play a song")]
    public static void Play(string? song) => Gramophone.Play(song);

    [Command("gramophone_change", "[Gramophone] Change a parameter in the song")]
    public static void Change(string? param, string? value) => Gramophone.SetParam(param, value);

    [Command("gramophone_stop", "[Gramophone] Stop a song")]
    public static void Stop() => Gramophone.Stop();

    public override void CreateModMenuSection(TextMenu? menu, bool inGame, EventInstance? snapshot)
    {
        base.CreateModMenuSection(menu, inGame, snapshot);

        new Item[]
        {
            new OnOff(Localized.Enable, Settings.Enabled).Change(x => Settings.Enabled = x),
            new OnOff(Localized.Menu, Settings.Menu).Change(x => Settings.Menu = x),
        }.For(x => menu?.Add(x));
    }

    public override void Load()
    {
        Gramophone.Previous = Audio.CurrentMusic;

        AudioState.Apply += Gramophone.Apply;
        Everest.Events.Level.OnCreatePauseMenuButtons += Gramophone.Pause;
        OnAudio.SetMusic += Gramophone.SetMusic;
        OnAudio.SetMusicParam += Gramophone.SetMusicParam;
        OnAudio.SetParameter += Gramophone.SetParameter;
    }

    public override void Unload()
    {
        Gramophone.Stop();

        AudioState.Apply -= Gramophone.Apply;
        Everest.Events.Level.OnCreatePauseMenuButtons -= Gramophone.Pause;
        OnAudio.SetMusic -= Gramophone.SetMusic;
        OnAudio.SetMusicParam -= Gramophone.SetMusicParam;
        OnAudio.SetParameter -= Gramophone.SetParameter;
    }
}
