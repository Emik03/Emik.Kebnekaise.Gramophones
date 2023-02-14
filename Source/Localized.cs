// SPDX-License-Identifier: MPL-2.0
#pragma warning disable SA1600
namespace Emik.Kebnekaise.Gramophones;

static class Localized
{
    public const string
        PercentTemplate = "(P)",
        SearchTemplate = "(S)",
        UpperTemplate = "(U)";

    internal static LocalString Ambience { get; } = new(nameof(Ambience));

    internal static LocalString Enable { get; } = new(nameof(Enable));

    internal static LocalString Enter { get; } = new(nameof(Enter));

    internal static LocalString Gramo { get; } = new(nameof(Gramo));

    internal static LocalString Loading { get; } = new(nameof(Loading));

    internal static LocalString Menu { get; } = new(nameof(Menu));

    internal static LocalString None { get; } = new(nameof(None));

    internal static LocalString Params { get; } = new(nameof(Params));

    internal static LocalString Searching { get; } = new(nameof(Searching));

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
