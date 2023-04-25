using Cinemachine;
using Sirenix.OdinInspector;

namespace Anjin.Cameras
{
	public class VCamHOrbitProxy : VCamProxyNormal {
		
		public TransposerSettings 	     TransposerSettings;
		public OrbitalTransposerSettings OrbitSettings = OrbitalTransposerSettings.Default;
		public ComposerSettings          ComposerSettings;

		[TitleGroup("Runtime")] public CinemachineOrbitalTransposer OrbitTransposer;
		[TitleGroup("Runtime")] public CinemachineComposer          Composer;

		public override void Start()
		{
			base.Start();

			if (Cam == null) return;

			OrbitTransposer = Cam.GetCinemachineComponent<CinemachineOrbitalTransposer>();
			Composer        = Cam.GetCinemachineComponent<CinemachineComposer>();
		}

		public override void Update()
		{
			base.Update();

			TransposerSettings.Apply(OrbitTransposer);
			OrbitSettings.Apply(OrbitTransposer);
			ComposerSettings.Apply(Composer);
		}

		public override void SetFromConfig(CamConfig config)
		{
			base.SetFromConfig(config);
			TransposerSettings = config.Settings_Transposer;
			OrbitSettings = config.Settings_OrbitalTransposer;
			ComposerSettings = config.Settings_Composer;
		}
	}
}