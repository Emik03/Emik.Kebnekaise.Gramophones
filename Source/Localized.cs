#region Emik.MPL

// <copyright file="Localized.cs" company="Emik">
// Copyright (c) Emik. This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// </copyright>

#endregion

#pragma warning disable SA1600
namespace Emik.Kebnekaise.Gramophones;

static class Localized
{
    internal static LocalString Ambience { get; } = new(nameof(Ambience));

    internal static LocalString Enable { get; } = new(nameof(Enable));

    internal static LocalString Gramo { get; } = new(nameof(Gramo));

    internal static LocalString Menu { get; } = new(nameof(Menu));

    internal static LocalString None { get; } = new(nameof(None));

    internal static LocalString Params { get; } = new(nameof(Params));

    internal static LocalString Shuffle { get; } = new(nameof(Shuffle));

    internal static LocalString Song { get; } = new(nameof(Song));

    internal static LocalString Sort { get; } = new(nameof(Sort));

    internal static LocalString Step { get; } = new(nameof(Step));

    internal static LocalString Stop { get; } = new(nameof(Stop));

    internal static LocalString Which { get; } = new(nameof(Which));

    internal readonly record struct LocalString(string Key)
    {
        public static implicit operator string(LocalString localString)
        {
            var name = $"{nameof(Gramophone)}_{localString.Key}";
            return Dialog.Clean(name);
        }
    }
}
