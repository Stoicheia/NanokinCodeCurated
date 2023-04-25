using System;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Util;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Addressable;
using Util.Extensions;
using Util.Odin.Attributes;

namespace Anjin.Minigames
{
	// TODO (C.L. 02-6-2023): Save file persistence

	public class MapMinecartController : SerializedMonoBehaviour, ICamController, ICoasterController
	{
		public enum States {Off, Intro, Outro, Running}

		public Spline.Direction Direction = Spline.Direction.Forward;

		public Transform EjectForwards;
		public Transform EjectBackwards;

		public CinemachineVirtualCamera Camera;

		public RollerCoaster Coaster;

		public ComponentAsset<CoasterCarActor> CarAsset;

		[NonSerialized, ShowInPlay] public CoasterCarActor Car;

		[NonSerialized, ShowInPlay] public  States State;



		[NonSerialized, ShowInPlay] public  bool   PlayerRiding;
		[NonSerialized, ShowInPlay] public  bool   ReachedEnd;
		[NonSerialized, ShowInPlay] private bool   _ready;

		[ShowInPlay] private PlayerActor Player;

		//[NonSerialized, ShowInPlay] public bool Active;

		private void Awake()
		{
			State = States.Off;
			//Active = false;

			_ready = false;

			if (Coaster == null)
				Coaster = GetComponent<RollerCoaster>();

			Load().ForgetWithErrors();
		}

		private async UniTask Load()
		{
			CoasterCarActor result = await CarAsset.Load();

			Car = result.InstantiateNew(transform, true).GetComponent<CoasterCarActor>();

			Car.Controller = this;
			Car.Coaster    = Coaster;
			Car.SetTrack(Coaster.AllTracks[0], 0);

			if(Camera)
			{
				Camera.Priority = GameCams.PRIORITY_INACTIVE;
				Camera.Follow   = Car.transform;
				Camera.LookAt   = Car.transform;
			}

			_ready = true;
		}

		[Button]
		public void Enter() => Enter(ActorController.playerActor as PlayerActor);

		public void Activate()
		{
			if (State != States.Off) return;
			/*if (Active) return;
			Active = true;*/

			State = States.Intro;
			_intro().ForgetWithErrors();

			Car.State.Change(Car.Ride);
			Car.Follower.direction = Direction;
			Car.Follower.Evaluate(Car.Follower.GetPercent());
		}

		public void Deactivate()
		{
			if (State == States.Off) return;
			State = States.Off;

			//_outro().ForgetWithErrors();

			Car.State.Change(Car.Idle);
		}

		public async UniTask _intro()
		{
			await UniTask2.Frames(5);
			await UniTask.WaitUntil(() => !GameCams.IsBlending);
			State = States.Running;
		}

		public async UniTask _outro()
		{
			await UniTask2.Seconds(0.5f);
			State = States.Off;
		}

		public void DoOutro() {}

		public void Enter(PlayerActor actor)
		{
			if (actor == null)	return;

			Activate();

			Player       = actor;
			PlayerRiding = true;

			Car.AddRider(actor);

			ActorController.SetPlayer(Car, false);

			// TODO: Transition somehow
			actor.ChangeState(actor._coasterRide.EnterCar(Car));

			if(Camera)
			{
				GameCams.Push(this);
				Camera.ReorientHorizontal(Car.transform.forward);
			}

		}

		// COASTER
		// ========================================================================

		public void OnActorExit(Actor actor) => EjectPlayerIfRiding();

		public void EjectPlayerIfRiding()
		{
			if (!PlayerRiding) return;
			PlayerRiding = false;

			Car.RemoveRider(Player);

			if(Camera)
				GameCams.Pop(this);

			if(Player._coasterRide)
				Player._coasterRide.Eject(Direction == Spline.Direction.Forward ? EjectForwards : EjectBackwards);

			ActorController.SetPlayer(Player, false);
		}

		public bool CoasterActive => State == States.Running && _ready;

		public void OnCoasterTrigger(string trigger, SplineUser user) { }

		public void OnTrackEnd(CoasterTrack.EndBehaviors behavior, CoasterTrack track, CoasterCarActor actor)
		{
			switch (behavior)
			{
				case CoasterTrack.EndBehaviors.Voided:
					OnCarVoided(actor);
					break;

				case CoasterTrack.EndBehaviors.Eject:
					Deactivate();
					EjectPlayerIfRiding();
					break;
			}

			if (Direction == Spline.Direction.Forward)
				Direction = Spline.Direction.Backward;
			else
				Direction = Spline.Direction.Forward;
		}

		public async void OnCarVoided(CoasterCarActor actor)
		{
			await UniTask2.Seconds(1.5f);
			actor.ToRide();
			actor.SetTrack(actor.Track, 0);
		}


		public void OnActorTryEnter(Actor actor)
		{
			Enter(actor as PlayerActor);
		}

		public void OnActivate() => Camera.Priority = GameCams.PRIORITY_ACTIVE;

		public void OnRelease(ref CinemachineBlendDefinition? blend)
		{
			blend           = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 0.25f);
			Camera.Priority = GameCams.PRIORITY_INACTIVE;
		}

		public void ActiveUpdate() { }

		public void GetBlends(ref CinemachineBlendDefinition? blend, ref CinemachineBlenderSettings settings)
		{
			blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 0.5f);
		}
	}
}