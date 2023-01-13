#region Emik.MPL

// <copyright file="Resolver.cs" company="Emik">
// Copyright (c) Emik. This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// </copyright>

#endregion

namespace Emik.Kebnekaise.Gramophones;

/// <summary>Resolves dependencies used by this library.</summary>
static class Resolver
{
    /// <summary>Loads missing dependencies.</summary>
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Init() =>
        Directory
           .GetFiles(PathMods, "Emik.Results.dll")
           .Lazily(x => Logger.Log(nameof(Gramophone), $"Loading assembly: {x}"))
           .Select(Assembly.LoadFile)
           .Lazily(x => Logger.Log(nameof(Gramophone), $"Loaded library: {x}"))
           .Enumerate();
}
