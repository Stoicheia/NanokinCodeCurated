using System;
using Anjin.Cameras;
using Anjin.Nanokin;
using Cinemachine;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	public class TransitionBrain : ActorBrain<PlayerActor>, ICharacterActorBrain<PlayerActor>, ICamController
	{
		public const int MAX_MOVE_PREVENTION_FRAMES = 60;
		public override int Priority => 5;

		public CinemachineBlendDefinition DefaultBlendOutgoing = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseIn, 1);
		public CinemachineBlendDefinition DefaultBlendIncoming = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseIn, 1);

		[NonSerialized, ShowInPlay] public PlayerActor           controlling;
		[NonSerialized, ShowInPlay] public Vector3               startPoint;
		[NonSerialized, ShowInPlay] public Vector3               targetWalkPoint;
		[NonSerialized, ShowInPlay] public TransitionOrientation orientation;
		[NonSerialized, ShowInPlay] public bool                  isWarpOut = false;
		[NonSerialized, ShowInPlay] public TransitionVolume      Volume;
		private PlayerControlBrain _triggerRepository;

		private void Start()
		{
			_triggerRepository = GetComponent<PlayerControlBrain>();
			GameController.OnMidWarp += () =>
			{
				//InsureVolumeCamsInactive();
				GameCams.SetBlendOverride(GameCams.Cut, this);
				isWarpOut = false;
			};

			GameController.OnEndMidWarp += () =>
			{
				GameCams.ReleaseBlendOverride();
			};
		}

		private int moveTimer = 0;
		public void PollInputs(PlayerActor plr, ref CharacterInputs Inputs)
		{
			var charPos = plr.transform.position;

			if (!plr.IsMotorStable && moveTimer > 0)
			{
				moveTimer--;
				Inputs.NoMovement();
			}
			else
			{
				moveTimer = MAX_MOVE_PREVENTION_FRAMES;
				Inputs.look = null;
				var move = (targetWalkPoint - charPos);
				Inputs.move = new Vector3(move.x, 0, move.z).normalized;
			}

			if (Volume == null || Vector2.Distance(charPos.xz(), targetWalkPoint.xz()) < 1f) {
				DebugLogger.Log("no inputs??", LogContext.Overworld, LogPriority.Temp);
				Inputs.NoMovement();
				if (!isWarpOut) {
					plr.PopOutsideBrain(this);
				}
			}
		}

		private void Update()
		{
			if (actor == null)
			{
				// Debug.LogWarning("Transition Brain Actor has not been assigned. Attempting to assign. FIX THIS BUG"); // oxy: this is not a bug!
				actor       = (PlayerActor)_triggerRepository.actor;
				return;
			}

			if(controlling != null)
				_triggerRepository.CheckInteractions(actor);
		}

		public void ResetInputs(PlayerActor character, ref CharacterInputs Inputs) { }

		public override void OnBeginControl(PlayerActor actor)
		{
			GameCams.SetController(this);
			controlling = actor;
			//actor.LeaveArea(1, 0);
		}

		public void StartUsing(PlayerActor to_control, TransitionVolume volume)
		{
			if (to_control == null || volume == null) return;

			InsureVolumeCamsInactive();

			controlling = to_control;
			Volume      = volume;

			targetWalkPoint = Volume.GetTargetPositionFromHit(controlling.transform.position);
			orientation     = Volume.GetOrientationFromPos(controlling.transform.position);

			if (orientation == TransitionOrientation.Negative)
				Blend = Volume.OverrideBlendIncoming ? Volume.BlendIncoming : DefaultBlendIncoming;
			else
				Blend = Volume.OverrideBlendOutgoing ? Volume.BlendOutgoing : DefaultBlendOutgoing;

			if (to_control.activeBrain != this)
			{
				to_control.PushOutsideBrain(this);
			}

			DebugDraw.DrawMarker(targetWalkPoint, 1, Color.blue, 4, true);
		}

		public override void OnTick(PlayerActor actor, float dt) { }

		public override void OnEndControl(PlayerActor actor)
		{
			if (GameController.Live.StateGame != GameController.GameState.WarpOut && GameCams.Live._controller == this) {
				GameCams.ReleaseController();
			} else {
				InsureVolumeCamsInactive();
			}

			Volume      = null;
			controlling = null;
		}

		public void PollInputs(Actor  character, ref CharacterInputs Inputs) => PollInputs(character as PlayerActor, ref Inputs);
		public void ResetInputs(Actor character, ref CharacterInputs Inputs) => PollInputs(character as PlayerActor, ref Inputs);

		// Cameras
		//---------------------------------------------------------------------------

		public void InsureVolumeCamsInactive()
		{
			if (!Volume) return;
			DebugLogger.Log("Ensure Deactivated " + Volume, LogContext.Overworld, LogPriority.Low);
			if (Volume.SideACam) Volume.SideACam.Priority = GameCams.PRIORITY_INACTIVE;
			if (Volume.SideBCam) Volume.SideBCam.Priority = GameCams.PRIORITY_INACTIVE;
		}

		public void OnActivate() { }



		public void OnRelease(ref CinemachineBlendDefinition? blend)
		{
			if (!Volume) return;
			InsureVolumeCamsInactive();

			blend = Blend;
		}

		public void ActiveUpdate()
		{
			if (!Volume) return;

			if (Volume.SideACam)
				Volume.SideACam.Priority = (orientation == TransitionOrientation.Positive) ? GameCams.PRIORITY_ACTIVE : GameCams.PRIORITY_INACTIVE;

			if (Volume.SideBCam)
				Volume.SideBCam.Priority = (orientation == TransitionOrientation.Negative) ? GameCams.PRIORITY_ACTIVE : GameCams.PRIORITY_INACTIVE;
		}

		[NonSerialized, ShowInPlay] public CinemachineBlendDefinition Blend;

		public void GetBlends(ref CinemachineBlendDefinition? blend, ref CinemachineBlenderSettings settings) => blend = Blend;
	}
}