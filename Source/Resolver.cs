#region Emik.MPL

// <copyright file="Resolver.cs" company="Emik">
// Copyright (c) Emik. This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// </copyright>

#endregion

namespace Emik.Kebnekaise.Gramophones;

/// <summary>Resolves dependencies used by this library.</summary>
static class Resolver
{
    static string Loader => $"{Please.Try(LoadDependencies)}";

    /// <summary>Loads missing dependencies.</summary>
    [ModuleInitializer]
    internal static void Init() => Logger.Log(nameof(Gramophone), Loader);

    static void LoadDependencies() =>
        Directory
           .GetFiles(PathMods, "Emik.Results.dll")
           .Select(Assembly.LoadFile)
           .Enumerate();
}
