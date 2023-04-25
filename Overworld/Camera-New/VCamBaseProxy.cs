using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Cameras
{
	public abstract class VCamProxy : SerializedMonoBehaviour
	{
		public abstract CinemachineVirtualCameraBase Getcam();

		public Transform Follow;
		public Transform LookAt;

		public bool IgnoreFollow = false;

		public VCamTarget Target;
		//public WorldPoint TargetPoint;

		public enum TargetMode { Manual, Worldpoint}
		public TargetMode targetMode = TargetMode.Manual;

		public CamRef reference = CamRef.NullRef;
		public int Priority = -1;

		public void SpawnTarget()
		{
			targetMode = TargetMode.Worldpoint;
			var go = new GameObject("Target");
			go.transform.SetParent(GameCams.Live.TargetsRoot);
			Target = go.AddComponent<VCamTarget>();
		}

		void OnDestroy()
		{
			if(Target != null)
				Destroy(Target);
		}

		public virtual void SetFromConfig(CamConfig config)
		{
			if(Target) Target.Point = config.TargetWorldPoint;
			//TargetPoint = config.Target;
		}
	}

	public class VCamProxy<T> : VCamProxy
		where T : CinemachineVirtualCameraBase
	{
		[TitleGroup("Runtime")]
		public T Cam;

		public virtual void Start()
		{
			Cam = GetComponent<T>();
		}

		public virtual void Update()
		{
			if (Cam == null) return;

			switch (targetMode)
			{
				case TargetMode.Manual:
					Cam.Follow = IgnoreFollow ? null : Follow;
					Cam.LookAt = LookAt;
				break;

				case TargetMode.Worldpoint:
					//Target.point = TargetPoint;
					Cam.Follow = Target.transform;
					Cam.LookAt = Target.transform;
				break;
			}

			Cam.Priority = Priority;
		}

		public override CinemachineVirtualCameraBase Getcam() => Cam;
	}
}