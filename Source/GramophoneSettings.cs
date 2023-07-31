// SPDX-License-Identifier: MPL-2.0
namespace Emik.Kebnekaise.Gramophones;

/// <summary>Contains settings for this mod.</summary>
[CLSCompliant(false)]
public sealed class GramophoneSettings : EverestModuleSettings
{
    /// <summary>Gets or sets a value indicating whether Cassette music is to be preserved.</summary>
    [SettingIgnore]
    public bool Alt { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether this mod is enabled.</summary>
    [SettingIgnore]
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether <see cref="Gramophone" /> is shown on the menu.</summary>
    [SettingIgnore]
    public bool Menu { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the game is able to mutate FMOD Parameters.</summary>
    [SettingIgnore]
    public bool Inhibit { get; set; } = true;

    /// <summary>Gets or sets a value how many steps are equivalent to 1 in FMOD.</summary>
    [SettingIgnore, ValueRange(1, int.MaxValue)]
    public int Step { get; set; } = 1;
}
