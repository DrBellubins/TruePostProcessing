using Stride.Graphics;
using Stride.Rendering;
using Stride.Core.Mathematics;

namespace Stride.Rendering
{
    static partial class TrueBloomKeys
    {
        public static readonly ValueParameterKey<bool> IsDownsample = ParameterKeys.NewValue<bool>();
        public static readonly ValueParameterKey<Vector2> InverseResolution = ParameterKeys.NewValue<Vector2>();
        public static readonly ValueParameterKey<float> Radius = ParameterKeys.NewValue<float>();
        public static readonly ValueParameterKey<float> Strength = ParameterKeys.NewValue<float>();
    }
}
