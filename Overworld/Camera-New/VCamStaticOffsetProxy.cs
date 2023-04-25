using Cinemachine;
using Sirenix.OdinInspector;

namespace Anjin.Cameras
{
	public class VCamStaticOffsetProxy : VCamProxyNormal
	{
		public ComposerSettings ComposerSettings;

		[TitleGroup("Runtime")] public CinemachineComposer          Composer;

		public override void Start()
		{
			base.Start();

			if (Cam == null) return;
			Composer        = Cam.GetCinemachineComponent<CinemachineComposer>();
		}

		public override void Update()
		{
			base.Update();
			ComposerSettings.Apply(Composer);
		}

		public override void SetFromConfig(CamConfig config)
		{
			base.SetFromConfig(config);
			ComposerSettings   = config.Settings_Composer;
		}
	}
}