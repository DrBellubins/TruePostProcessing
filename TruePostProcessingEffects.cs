using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.Compositing;
using Stride.Rendering.Materials;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// A custom bundle of <see cref="ImageEffect"/>.
    /// </summary>
    [DataContract("TruePostProcessingEffects")]
    [Display("True Post-processing effects")]
    public sealed class TruePostProcessingEffects : ImageEffect, IImageEffectRenderer, IPostProcessingEffects
    {
        /// <inheritdoc/>
        [DataMember(-101), Display(Browsable = false)]
        [NonOverridable]
        public Guid Id { get; set; } = Guid.NewGuid();
		
		[DataMemberIgnore] public ColorspaceConversion ColorspaceConversion { get; private set; }

        /// <summary>
        /// Gets the TrueBloom effect.
        /// </summary>
        [DataMember("True Bloom"), Category] public TrueBloom TrueBloom { get; private set; }

        public bool RequiresVelocityBuffer => false;
        public bool RequiresNormalBuffer => false;
        public bool RequiresSpecularRoughnessBuffer => false;

        /// <summary>
        /// Initializes a new instance of the <see cref="TruePostProcessingEffects" /> class.
        /// </summary>
        /// <param name="services">The services.</param>
        public TruePostProcessingEffects(IServiceRegistry services)
            : this(RenderContext.GetShared(services))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TruePostProcessingEffects"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public TruePostProcessingEffects(RenderContext context)
            : this()
        {
            Initialize(context);
        }
		
		/// <summary>
        /// Initializes a new instance of the <see cref="TruePostProcessingEffects"/> class.
        /// </summary>
        public TruePostProcessingEffects()
        {
            ColorspaceConversion = new ColorspaceConversion();
            TrueBloom = new TrueBloom();
        }

        /// <summary>
        /// Disables all post processing effects.
        /// </summary>
        public void DisableAll()
        {
            ColorspaceConversion.Enabled = false;
            TrueBloom.Enabled = false;
        }

        public override void Reset()
        {
            // TODO: Check how to reset other effects too

            base.Reset();
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            ColorspaceConversion = ToLoadAndUnload(ColorspaceConversion);
            TrueBloom = ToLoadAndUnload(TrueBloom);
        }

        public void Collect(RenderContext context)
        {
        }

        public void Draw(RenderDrawContext drawContext, RenderOutputValidator outputValidator, Texture[] inputs, Texture inputDepthStencil, Texture outputTarget)
        {
            var colorIndex = outputValidator.Find<ColorTargetSemantic>();
            if (colorIndex < 0)
                return;

            SetInput(0, inputs[colorIndex]);
            SetInput(1, inputDepthStencil);

            var normalsIndex = outputValidator.Find<NormalTargetSemantic>();
            if (normalsIndex >= 0)
            {
                SetInput(2, inputs[normalsIndex]);
            }

            var specularRoughnessIndex = outputValidator.Find<SpecularColorRoughnessTargetSemantic>();
            if (specularRoughnessIndex >= 0)
            {
                SetInput(3, inputs[specularRoughnessIndex]);
            }

            var reflectionIndex0 = outputValidator.Find<OctahedronNormalSpecularColorTargetSemantic>();
            var reflectionIndex1 = outputValidator.Find<EnvironmentLightRoughnessTargetSemantic>();
            if (reflectionIndex0 >= 0 && reflectionIndex1 >= 0)
            {
                SetInput(4, inputs[reflectionIndex0]);
                SetInput(5, inputs[reflectionIndex1]);
            }

            var velocityIndex = outputValidator.Find<VelocityTargetSemantic>();
            if (velocityIndex != -1)
            {
                SetInput(6, inputs[velocityIndex]);
            }

            SetOutput(outputTarget);
            Draw(drawContext);
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var input = GetInput(0);
            var output = GetOutput(0);

            if (input == null || output == null)
            {
                return;
            }

            var inputDepthTexture = GetInput(1); // Depth

            // If input == output, than copy the input to a temporary texture
            if (input == output)
            {
                var newInput = NewScopedRenderTarget2D(input.Width, input.Height, input.Format);
                context.CommandList.Copy(input, newInput);
                input = newInput;
            }

            var currentInput = input;

            // Update the parameters for this post effect
            /*if (!Enabled)
            {
                if (input != output)
                {
                    Scaler.SetInput(input);
                    Scaler.SetOutput(output);
                    Scaler.Draw(context);
                }

                return;
            }*/

            // sRGB -> AP1 conversion
            var colorSpaceRT = NewScopedRenderTarget2D(input.Width, input.Height, input.Format);
            ColorspaceConversion.SetInput(currentInput);
            ColorspaceConversion.SetOutput(colorSpaceRT);

            ColorspaceConversion.IsFirstStage = true;
            ColorspaceConversion.Draw(context);

            // TrueBloom pass
            if (TrueBloom.Enabled)
            {
                TrueBloom.SetInput(colorSpaceRT);
                TrueBloom.SetOutput(currentInput);
                TrueBloom.Draw(context);
            }

            // ACES Tonemap && AP1 -> sRGB conversion
            ColorspaceConversion.SetInput(currentInput);
            ColorspaceConversion.SetOutput(output);

            ColorspaceConversion.IsFirstStage = false;
            ColorspaceConversion.Draw(context);

            // Draw to screen
            /*Scaler.SetInput(colorSpaceRT);
            Scaler.SetOutput(output);
            Scaler.Draw(context);*/
        }
    }
}
