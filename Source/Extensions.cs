// SPDX-License-Identifier: MPL-2.0
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

    internal static string Name(this ParameterInstance parameter) => parameter.Description().name;

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

    internal static PARAMETER_DESCRIPTION Description(this ParameterInstance parameter)
    {
        parameter.getDescription(out var description);
        return description;
    }
}
