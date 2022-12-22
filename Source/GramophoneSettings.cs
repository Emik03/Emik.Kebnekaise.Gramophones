#region Emik.MPL

// <copyright file="GramophoneSettings.cs" company="Emik">
// Copyright (c) Emik. This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// </copyright>

#endregion

namespace Emik.Kebnekaise.Gramophones;

/// <summary>Contains settings for this mod.</summary>
[CLSCompliant(false)]
public sealed class GramophoneSettings : EverestModuleSettings
{
    /// <summary>Gets or sets a value indicating whether this mod is enabled.</summary>
    [SettingIgnore]
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether <see cref="Gramophone" /> is shown on the menu.</summary>
    [SettingIgnore]
    public bool Menu { get; set; } = true;

    /// <summary>Gets or sets a value how many steps are equivalent to 1 in FMOD.</summary>
    [SettingIgnore, ValueRange(1, int.MaxValue)]
    public int Step { get; set; } = 1;
}
