using Cinemachine;

namespace Anjin.Cameras
{
	public class VCamProxyNormal : VCamProxy<CinemachineVirtualCamera>
	{

		public LensSettings Lens = DefaultLens;

		public static LensSettings DefaultLens = new LensSettings(70, 1, 0.1f, 20000f, 0);

		public override void Update()
		{
			base.Update();
			if (Cam == null) return;
			Cam.m_Lens = Lens;
		}

		public override void SetFromConfig(CamConfig config)
		{
			base.SetFromConfig(config);
			Lens = config.Lens;
		}
	}
}