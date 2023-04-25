using System;
using Anjin.Cameras;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Util.Odin.Attributes;

namespace Anjin.Nanokin.Core.Options {
	public class Quality : StaticBoy<Quality> {



		//[NonSerialized, ShowInPlay]
		public Settings Default;

		[NonSerialized]
		private Settings _current;

		[HideInEditorMode, ShowInPlay]
		public static Settings Current {
			get {
				if (Live == null) return null;
				return Live._current;
			}
			set {
				if (Live == null) return;
				if (Live._current != null && value != null) {
					Live._current = value;
					Live.UpdateUsingSettings(Live._current);
				}
				else {
					Live._current = Live.Default;
					Live.UpdateUsingSettings(Live.Default);
				}
			}
		}

		protected override void OnAwake()
		{
			base.OnAwake();
			Current = Default;
		}

		private void OnDestroy()
		{
			ResizeDynamicResolution(1);
		}

		[Button]
		public void UpdateUsingCurrent() => UpdateUsingSettings(Current);

		[Button]
		private void SwitchToInbuiltSetting(int index)
		{

		}

		[Button]
		public void ResizeDynamicResolution(float scale)
		{
			ScalableBufferManager.ResizeBuffers(scale, scale);
		}

		private void UpdateUsingSettings(Settings settings)
		{
			// Standard
			QualitySettings.masterTextureLimit = (int)settings.TextureQuality;
			QualitySettings.antiAliasing       = 0;
			QualitySettings.softParticles      = settings.SoftParticles;

			QualitySettings.shadows          = settings.ShadowQuality;
			QualitySettings.shadowResolution = settings.ShadowResolution;
			QualitySettings.shadowProjection = settings.ShadowProjection;
			QualitySettings.shadowDistance   = Mathf.Clamp(settings.ShadowDistance, MIN_SHADOW_DISTANCE , MAX_SHADOW_DISTANCE);

			ResizeDynamicResolution(Mathf.Clamp(settings.ResolutionScale, MIN_RESOLUTION_SCALING, MAX_RESOLUTION_SCALING));

			// Antialiasing
			switch (settings.AntialiasMode) {
				case AntialiasMode.Off:
					GameCams.Live.PostProcess.antialiasingMode = PostProcessLayer.Antialiasing.None;
					break;

				case AntialiasMode.FXAA:
					GameCams.Live.PostProcess.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
					break;

				case AntialiasMode.SMAA:
					GameCams.Live.PostProcess.antialiasingMode                          = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
					GameCams.Live.PostProcess.subpixelMorphologicalAntialiasing.quality = settings.SMAAQualityLevel;
					break;
			}

			PostProcessVolumeEnabler.UpdateAll();

		}

		public static void SetTextureQuality(TextureQuality                         quality)     { Current.TextureQuality   = quality;	Live.UpdateUsingCurrent(); }
		public static void SetAntialiasMode(AntialiasMode                           mode)        { Current.AntialiasMode    = mode;	Live.UpdateUsingCurrent(); }
		public static void SetSMAAQuality(SubpixelMorphologicalAntialiasing.Quality quality )    { Current.SMAAQualityLevel = quality;	Live.UpdateUsingCurrent(); }
		public static void SetResolutionScaling(float                               scaling )    { Current.ResolutionScale  = scaling;	Live.UpdateUsingCurrent(); }

		public static void SetShadowQuality(ShadowQuality                           quality )    { Current.ShadowQuality    = quality;	Live.UpdateUsingCurrent(); }
		public static void SetShadowResolution(ShadowResolution                     resolution ) { Current.ShadowResolution = resolution;	Live.UpdateUsingCurrent(); }
		public static void SetShadowProjection(ShadowProjection                     projection ) { Current.ShadowProjection = projection;	Live.UpdateUsingCurrent(); }
		public static void SetShadowDistance(float                                  distance )   { Current.ShadowDistance   = distance;	Live.UpdateUsingCurrent(); }

		public static void SetSoftParticles(bool	enabled)    { Current.SoftParticles  = enabled;	Live.UpdateUsingCurrent(); }
		public static void SetBloom(bool			enabled)    { Current.BloomEnabled   = enabled;	Live.UpdateUsingCurrent(); }
		public static void SetSSAO(bool				enabled)    { Current.SSAOEnabled    = enabled;	Live.UpdateUsingCurrent(); }

		private void Update()
		{
			Current.FOV = Mathf.Clamp(Current.FOV, FOV_MIN, FOV_MAX);
		}

		public const float MIN_RESOLUTION_SCALING = 0.5f;
		public const float MAX_RESOLUTION_SCALING = 1f;

		public const float MIN_SHADOW_DISTANCE		= 25;
		public const float MAX_SHADOW_DISTANCE		= 250;
		public const float DEFAULT_SHADOW_DISTANCE	= 100;

		public const float FOV_MIN		= 50;
		public const float FOV_MAX		= 120;
		public const float FOV_DEFAULT	= 70;

		// Unity doesn't have an enum for this
		public enum TextureQuality {
			Full	= 0,
			Half	= 1,
			Quarter = 2,
			Eight	= 3,
		}

		public enum AntialiasMode {
			Off = 0,
			FXAA = 1,
			SMAA = 2,
		}


		public class Settings {

			public float FOV = FOV_DEFAULT;

			public TextureQuality TextureQuality = TextureQuality.Full;

			// 0.5 - 1
			// Anything lower than 0.5 doesn't make sense because the HUD isn't even visible
			public float ResolutionScale = 1;

			// Antialiasing
			public AntialiasMode                             AntialiasMode    = AntialiasMode.SMAA;
			public SubpixelMorphologicalAntialiasing.Quality SMAAQualityLevel = SubpixelMorphologicalAntialiasing.Quality.High;

			// Shadows
			public ShadowQuality    ShadowQuality    = ShadowQuality.All;
			public ShadowResolution ShadowResolution = ShadowResolution.VeryHigh;
			public ShadowProjection ShadowProjection = ShadowProjection.StableFit;
			public float            ShadowDistance   = DEFAULT_SHADOW_DISTANCE;

			public bool SoftParticles = true;
			public bool BloomEnabled  = true;
			public bool SSAOEnabled   = true;
		}

	}
}