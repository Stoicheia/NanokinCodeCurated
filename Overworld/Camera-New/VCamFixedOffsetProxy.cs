using Cinemachine;
using Sirenix.OdinInspector;

namespace Anjin.Cameras
{
	public class VCamFixedOffsetProxy : VCamProxyNormal
	{
		public TransposerSettings        TransposerSettings;
		public ComposerSettings          ComposerSettings;

		[TitleGroup("Runtime")] public CinemachineTransposer Transposer;
		[TitleGroup("Runtime")] public CinemachineComposer   Composer;

		public override void Start()
		{
			base.Start();

			if (Cam == null) return;

			Transposer = Cam.GetCinemachineComponent<CinemachineTransposer>();
			Composer   = Cam.GetCinemachineComponent<CinemachineComposer>();
		}

		public override void Update()
		{
			base.Update();

			TransposerSettings.Apply(Transposer);
			ComposerSettings.Apply(Composer);
		}

		public override void SetFromConfig(CamConfig config)
		{
			base.SetFromConfig(config);
			TransposerSettings = config.Settings_Transposer;
			ComposerSettings   = config.Settings_Composer;
		}
	}
}