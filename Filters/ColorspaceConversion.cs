using System;
using Stride.Core;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Core.Mathematics;
using Stride.Rendering.Images;
using System.ComponentModel;
using Stride.Core.Annotations;
using Stride.Rendering.Compositing;
using Stride.Rendering.Materials;

namespace Stride.Rendering.Images
{
    [DataContract(nameof(ColorspaceConversion))]
    public class ColorspaceConversion : ImageEffect
    {
        [DataMemberIgnore] public bool IsFirstStage;

        private ImageEffectShader colorSpaceConversionShader;

        public ColorspaceConversion() : base(null, false) { }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            colorSpaceConversionShader = ToLoadAndUnload(new ImageEffectShader("ColorSpaceConversion_shader"));
        }

        protected override void DrawCore(RenderDrawContext context)
        {
			var input = GetInput(0);
            var output = GetOutput(0) ?? input;

            if (input == null)
                return;
			
            colorSpaceConversionShader.Parameters.Set(ColorSpaceConversionKeys.IsFirstStage, IsFirstStage);
			
            Scaler.SetInput(input);
            Scaler.SetOutput(output);
            Scaler.Draw(context);
        }
    }
}
