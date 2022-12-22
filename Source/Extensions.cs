#region Emik.MPL

// <copyright file="Extensions.cs" company="Emik">
// Copyright (c) Emik. This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// </copyright>

#endregion

#pragma warning disable SA1600
namespace Emik.Kebnekaise.Gramophones;

static class Extensions
{
    internal static void Apply(this IEnumerable<ParameterInstance> parameters, EventInstance? instance) =>
        parameters.For(
            (p, i) =>
            {
                if (p.getValue(out var val) is RESULT.OK)
                    instance?.setParameterValueByIndex(i, val);
            }
        );

    internal static void Shut(this TextMenu? menu)
    {
        const string Back = "event:/ui/main/button_back";
        Audio.Play(Back);
        menu?.Close();
    }

    internal static string? Index(this int index) => Searcher.Song[index];

    internal static IEnumerable<ParameterInstance> Parameters(this EventInstance? instance)
    {
        if (instance is null)
            yield break;

        for (var i = 0; i < int.MaxValue; i++)
        {
            instance.getParameterByIndex(i, out var next);

            if (next is null)
                yield break;

            yield return next;
        }
    }
}
