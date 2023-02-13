// SPDX-License-Identifier: MPL-2.0
namespace Emik.Kebnekaise.Gramophones;

/// <summary>Contains settings for this mod.</summary>
[CLSCompliant(false)]
public sealed class GramophoneSettings : EverestModuleSettings
{
    /// <summary>Gets or sets a value indicating whether this mod is enabled.</summary>
    [SettingIgnore]
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether to allow the game to mutate the parameters.</summary>
    [SettingIgnore]
    public bool Inhibit { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether <see cref="Gramophone" /> is shown on the menu.</summary>
    [SettingIgnore]
    public bool Visible { get; set; } = true;

    /// <summary>Gets or sets a value how many steps are equivalent to one unit in FMOD.</summary>
    [SettingIgnore, ValueRange(1, int.MaxValue)]
    public int Step { get; set; } = 1;
}
