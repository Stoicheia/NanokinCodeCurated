using Anjin.EventSystemNS.Actions;

namespace Anjin.Nanokin.Actions
{
	[EventActionMetadata("Override Camera Config","Overworld Cameras")]
	public class OverrideCameraConfigAction : EventAction
	{
		public bool RetainStack;
		//public CameraConfig_OLD ConfigOld;

		public override void InitSockets() { }
		public override void Init()
		{
			//config = new CameraConfig();
			RetainStack = true;
		}
	}

	[EventActionImplementation(typeof(OverrideCameraConfigAction), "Nanokin")]
	public class OverrideCameraConfigActionImpl : NanokinActionImpl<OverrideCameraConfigAction>
	{
		//public override void OnEnter() { CameraController.Live.OverrideConfig(Handler.ConfigOld, Handler.RetainStack); }
	}

	[EventActionMetadata("Camera Config Return", "Overworld Cameras")]
	public class CameraConfigReturnAction : EventAction
	{
		public enum Mode{Previous, Base}
		public Mode mode;
		public override void InitSockets() { }
		public override void Init()
		{
			mode = Mode.Previous;
		}

		public static CameraConfigReturnAction Create(Mode _mode)
		{
			var a = Create<CameraConfigReturnAction>();
			a.mode = _mode;
			return a;
		}
	}

	[EventActionImplementation(typeof(CameraConfigReturnAction), "Nanokin")]
	public class CameraConfigReturnActionImpl : NanokinActionImpl<CameraConfigReturnAction>
	{
		public override void OnEnter()
		{
			/*if(Handler.mode == CameraConfigReturnAction.Mode.Previous)
				CameraController.Live.ReturnToPreviousConfig();
			else if(Handler.mode == CameraConfigReturnAction.Mode.Base)
				CameraController.Live.ReturnToBaseConfig();*/
		}
	}

	[EventActionMetadata("Wait for cinemachine blend", "Cinemachine")]
	public class WaitForCinemachineBlendAction : EventAction
	{
		public float WaitUntilPercentage;

		public override void InitSockets() { }
		public override void Init()
		{
			WaitUntilPercentage = 1;
		}
	}

	[EventActionImplementation(typeof(WaitForCinemachineBlendAction), "Nanokin")]
	public class WaitForCinemachineBlendActionImpl : NanokinActionImpl<WaitForCinemachineBlendAction>
	{
		public override bool Blocking => true;
		public override bool Done => _done;
		private bool _done = false;

		/// <summary>
		/// We need to make sure to give time for cinemachine to detect the change in
		/// camera priority, so we need to wait for a couple of frames before checking.
		/// </summary>
		public const int UPDATES_UNTIL_CHECK = 2;

		public int beforeTimer = UPDATES_UNTIL_CHECK;

		public override void OnEnter() { beforeTimer = UPDATES_UNTIL_CHECK; }

		public override void OnUpdate()
		{
			if (beforeTimer > 0) beforeTimer--;
			else
			{
				_done = true;
				/*if (CameraController.Live.Brain.IsBlending && CameraController.Live.Brain.ActiveBlend != null)
				{
					if (CameraController.Live.Brain.ActiveBlend.TimeInBlend /
					    CameraController.Live.Brain.ActiveBlend.Duration >= Mathf.Clamp01(Handler.WaitUntilPercentage))
					{
						_done = true;
					}
				}
				else
				{
					_done = true;
				}*/
			}
		}
	}
}