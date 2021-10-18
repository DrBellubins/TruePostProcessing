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
using System.Collections.Generic;

namespace Stride.Rendering.Images
{
    [DataContract(nameof(TrueBloom))]
    public class TrueBloom : ImageEffect
    {
        [DataMember]
        [DefaultValue(1)]
        [DataMemberRange(0.1, 10.0, 0.01, 1.0, 1)]
        public float Intensity { get; set; }

        [DataMember]
        [DefaultValue(1)]
        [DataMemberRange(0.1, 10.0, 0.01, 1.0, 1)]
        public float Radius { get; set; }

        [DataMemberIgnore] public List<Texture> bloomRenderTargets = new(5);

        private const int sampleCount = 4;

        private const float _bloomStrength1 = 0.5f;
        private const float _bloomStrength2 = 1;
        private const float _bloomStrength3 = 2;
        private const float _bloomStrength4 = 1;
        private const float _bloomStrength5 = 2;

        private const float _bloomRadius1 = 1.0f;
        private const float _bloomRadius2 = 2.0f;
        private const float _bloomRadius3 = 2.0f;
        private const float _bloomRadius4 = 4.0f;
        private const float _bloomRadius5 = 4.0f;

        private readonly ImageEffectShader trueBloomFilter;
        private Vector2 BloomInverseResolution;

        public TrueBloom() : this("TrueBloom_shader")
		{
			
		}
		
		public TrueBloom(string shaderName) : base(shaderName) 
		{
			if (shaderName == null) throw new ArgumentNullException("trueBloomFilterName");
				trueBloomFilter = new ImageEffectShader(shaderName);
		}

        protected override void InitializeCore()
        {
            base.InitializeCore();

            ToLoadAndUnload(trueBloomFilter);
        }

        protected override void DrawCore(RenderDrawContext context)
        {
            var input = GetInput(0);
            var output = GetOutput(0) ?? input;
			
			var width = input.Width;
			var height = input.Height;

            if (input == null)
                return;

            BloomInverseResolution = new Vector2(1.0f / input.Width, 1.0f / input.Height);

            var bloomMip0 = NewScopedRenderTarget2D(input.Width, input.Height, input.Format);

            Scaler.SetInput(input);
            Scaler.SetOutput(bloomMip0);
            Scaler.Draw(context, "Bloom Mip0");

            // Downsample
            var bloomInput = bloomMip0;

            bloomRenderTargets.Add(bloomInput);
            trueBloomFilter.Parameters.Set(TrueBloomKeys.IsDownsample, true);

            for (int i = 0; i < sampleCount; ++i)
            {
				width /= 2;
				height /= 2;
				BloomInverseResolution *= 2;
				
                var downsampleRT = NewScopedRenderTarget2D(width, height, input.Format);
                bloomRenderTargets.Add(downsampleRT);

                trueBloomFilter.Parameters.Set(TrueBloomKeys.InverseResolution, BloomInverseResolution);

                Scaler.SetInput(bloomInput);
                Scaler.SetOutput(downsampleRT);
                Scaler.Draw(context, $"Bloom downsample {i}");

                bloomInput = downsampleRT;
            }

            // Upsample
            trueBloomFilter.Parameters.Set(TrueBloomKeys.IsDownsample, false);

            var intensityIterator = Intensity;
            var radiusIterator = Radius;

            for (int i = sampleCount - 1; i >= 0; i--)
            {
				BloomInverseResolution /= 2;
                intensityIterator -= Intensity / 2;
                radiusIterator -= Radius / 2;
				
                var upsampleRT = bloomRenderTargets[i];
				
                trueBloomFilter.Parameters.Set(TrueBloomKeys.InverseResolution, BloomInverseResolution);
                trueBloomFilter.Parameters.Set(TrueBloomKeys.Strength, intensityIterator);
                trueBloomFilter.Parameters.Set(TrueBloomKeys.Radius, radiusIterator);

                Scaler.SetInput(bloomInput);
                Scaler.SetOutput(upsampleRT);
                Scaler.Draw(context, $"Bloom upsample {i}");

                bloomInput = upsampleRT;
            }

            bloomRenderTargets.Clear();

            Scaler.BlendState = BlendStates.Additive;

            Scaler.SetInput(bloomInput);
            Scaler.SetOutput(output);
            Scaler.Draw(context);

            Scaler.BlendState = BlendStates.Default;
            //Scaler.Reset();


            /*
            // DOWNSAMPLE TO MIP1
            var _bloomRenderTarget2DMip1 = NewScopedRenderTarget2D(input.Width / 2, input.Height / 2, input.Format);

            trueBloomFilter.Parameters.Set(TrueBloomKeys.IsDownsample, true);

            Scaler.SetInput(_bloomRenderTarget2DMip0);
            Scaler.SetOutput(_bloomRenderTarget2DMip1);
            Scaler.Draw(context);

            // DOWNSAMPLE TO MIP2
            trueBloomFilter.Parameters.Set(TrueBloomKeys.InverseResolution, BloomInverseResolution * 2);

            var _bloomRenderTarget2DMip2 = NewScopedRenderTarget2D(input.Width / 4, input.Height / 4, input.Format);

            Scaler.SetInput(_bloomRenderTarget2DMip1);
            Scaler.SetOutput(_bloomRenderTarget2DMip2);
            Scaler.Draw(context);

            // DOWNSAMPLE TO MIP3
            trueBloomFilter.Parameters.Set(TrueBloomKeys.InverseResolution, BloomInverseResolution * 2);

            var _bloomRenderTarget2DMip3 = NewScopedRenderTarget2D(input.Width / 8, input.Height / 8, input.Format);

            Scaler.SetInput(_bloomRenderTarget2DMip2);
            Scaler.SetOutput(_bloomRenderTarget2DMip3);
            Scaler.Draw(context);

            // DOWNSAMPLE TO MIP4
            trueBloomFilter.Parameters.Set(TrueBloomKeys.InverseResolution, BloomInverseResolution * 2);

            var _bloomRenderTarget2DMip4 = NewScopedRenderTarget2D(input.Width / 16, input.Height / 16, input.Format);

            Scaler.SetInput(_bloomRenderTarget2DMip3);
            Scaler.SetOutput(_bloomRenderTarget2DMip4);
            Scaler.Draw(context);

            // DOWNSAMPLE TO MIP5
            trueBloomFilter.Parameters.Set(TrueBloomKeys.InverseResolution, BloomInverseResolution * 2);

            var _bloomRenderTarget2DMip5 = NewScopedRenderTarget2D(input.Width / 32, input.Height / 32, input.Format);

            Scaler.SetInput(_bloomRenderTarget2DMip4);
            Scaler.SetOutput(_bloomRenderTarget2DMip5);
            Scaler.Draw(context);

            // UPSAMPLE TO MIP4
            trueBloomFilter.Parameters.Set(TrueBloomKeys.InverseResolution, BloomInverseResolution / 2);

            trueBloomFilter.Parameters.Set(TrueBloomKeys.IsDownsample, false);
            trueBloomFilter.Parameters.Set(TrueBloomKeys.Radius, _bloomRadius5 * Radius);
            trueBloomFilter.Parameters.Set(TrueBloomKeys.Strength, _bloomStrength5 * Intensity);

            Scaler.SetInput(_bloomRenderTarget2DMip5);
            Scaler.SetOutput(_bloomRenderTarget2DMip4);
            Scaler.Draw(context);

            // UPSAMPLE TO MIP3
            trueBloomFilter.Parameters.Set(TrueBloomKeys.InverseResolution, BloomInverseResolution / 2);

            trueBloomFilter.Parameters.Set(TrueBloomKeys.Radius, _bloomRadius4 * Radius);
            trueBloomFilter.Parameters.Set(TrueBloomKeys.Strength, _bloomStrength4 * Intensity);

            Scaler.SetInput(_bloomRenderTarget2DMip4);
            Scaler.SetOutput(_bloomRenderTarget2DMip3);
            Scaler.Draw(context);

            // UPSAMPLE TO MIP2
            trueBloomFilter.Parameters.Set(TrueBloomKeys.InverseResolution, BloomInverseResolution / 2);

            trueBloomFilter.Parameters.Set(TrueBloomKeys.Radius, _bloomRadius3 * Radius);
            trueBloomFilter.Parameters.Set(TrueBloomKeys.Strength, _bloomStrength3 * Intensity);

            Scaler.SetInput(_bloomRenderTarget2DMip3);
            Scaler.SetOutput(_bloomRenderTarget2DMip2);
            Scaler.Draw(context);

            // UPSAMPLE TO MIP1
            trueBloomFilter.Parameters.Set(TrueBloomKeys.InverseResolution, BloomInverseResolution / 2);

            trueBloomFilter.Parameters.Set(TrueBloomKeys.Radius, _bloomRadius2 * Radius);
            trueBloomFilter.Parameters.Set(TrueBloomKeys.Strength, _bloomStrength2 * Intensity);

            Scaler.SetInput(_bloomRenderTarget2DMip2);
            Scaler.SetOutput(_bloomRenderTarget2DMip1);
            Scaler.Draw(context);

            // UPSAMPLE TO MIP0
            trueBloomFilter.Parameters.Set(TrueBloomKeys.InverseResolution, BloomInverseResolution / 2);

            Scaler.BlendState = BlendStates.Additive;
            trueBloomFilter.Parameters.Set(TrueBloomKeys.Radius, _bloomRadius1 * Radius);
            trueBloomFilter.Parameters.Set(TrueBloomKeys.Strength, _bloomStrength1 * Intensity);

            Scaler.SetInput(_bloomRenderTarget2DMip1);
            Scaler.SetOutput(_bloomRenderTarget2DMip0);
            Scaler.Draw(context);
			
			// Finish
			Scaler.SetInput(_bloomRenderTarget2DMip0);
            Scaler.SetOutput(output);
            Scaler.Draw(context);
            Scaler.Reset();

            Scaler.BlendState = BlendStates.Default;
            */
        }
    }
}
