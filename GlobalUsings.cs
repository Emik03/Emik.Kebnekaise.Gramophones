// SPDX-License-Identifier: MPL-2.0
global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.Diagnostics.CodeAnalysis;
global using System.IO;
global using System.Linq;
global using System.Runtime;
global using System.Runtime.CompilerServices;
global using System.Runtime.InteropServices;
global using System.Runtime.Serialization;
global using System.Threading;
global using Celeste;
global using Celeste.Mod;
global using Celeste.Mod.UI;
global using Emik.Morsels;
global using Emik.Results;
global using FMOD;
global using FMOD.Studio;
global using InlineMethod;
global using Ionic.Zip;
global using JetBrains.Annotations;
global using Monocle;
global using MonoMod.Utils;
global using On.Celeste.Mod.UI;
global using static Celeste.Mod.Everest.Loader;
global using static Celeste.TextMenu;
global using Audio = Celeste.Audio;
global using AudioState = On.Celeste.AudioState;
global using Dialog = Celeste.Dialog;
global using EventInstance = FMOD.Studio.EventInstance;
global using Level = Celeste.Level;
global using OnAudio = On.Celeste.Audio;
global using Slider = Celeste.TextMenu.Slider;
global using TextMenu = Celeste.TextMenu;
global using ZipFile = Ionic.Zip.ZipFile;
