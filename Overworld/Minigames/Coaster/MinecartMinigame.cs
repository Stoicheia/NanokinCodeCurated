using System;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Nanokin;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine.Playables;
using Util.Odin.Attributes;

namespace Anjin.Minigames
{
	public class MinecartMinigame : Minigame, ICamController, ICoasterController
	{
		public RollerCoaster            Coaster;
		public CoasterCarActor          Cart;
		public CinemachineVirtualCamera Camera;

		public CoasterTrack       StartingTrack;
		public float              StartingDistance;

		[NonSerialized, ShowInPlay] private SplineComputer   _currentTrack;
		[NonSerialized, ShowInPlay] private PlayableDirector _currentDirector;

		public override void Start()
		{
			base.Start();

			Cart.Controller = this;
			Cart.Coaster    = Coaster;

			Cart.gameObject.SetActive(false);
		}

		public override async UniTask<bool> Setup(IMinigameSettings settings = null)
		{
			Cart.SetTrack(StartingTrack, StartingDistance);

			await Script_Setup(settings);

			return true;
		}

		public override async UniTask<bool> Begin(MinigamePlayOptions options = MinigamePlayOptions.Default)
		{
			if (state != MinigameState.Off || ControlsGame && !GameController.Live.BeginMinigame(this))
				return false;

			_playOptions = options;

			Boot();
			state = MinigameState.Intro;

			// Spawn/Enable cart
			Cart.gameObject.SetActive(true);


			// Set player to cart
			ActorController.SetPartyActive(false);
			ActorController.SetPlayer(Cart);

			GameCams.Push(this);

			if (Script.Script != null)
			{
				await Script_OnStart();

				if ((_playOptions & MinigamePlayOptions.PlayIntro) != 0)
					await Script_Intro();

				await Script_OnRun();
			}

			state = MinigameState.Running;

			return true;
		}

		public override async UniTask Finish(MinigameFinish finish = MinigameFinish.Normal)
		{
			if (state != MinigameState.Running) return;

			Script._state.table["was_quit"] = finish == MinigameFinish.UserQuit;

			state = MinigameState.Outro;
			await Script_OnFinish();

			if ((_playOptions & MinigamePlayOptions.PlayIntro) != 0)
				await Script_Outro();

			await Script_OnEnd();

			GameCams.Pop(this);

			foreach (CoasterTrack track in GetComponentsInChildren<CoasterTrack>()) {
				foreach(TriggerGroup group in track.Spline.triggerGroups) {
					foreach (SplineTrigger trigger in group.triggers) {
						trigger.Reset();
					}
				}
			}

			// Despawn/Disable cart
			Cart.gameObject.SetActive(false);

			// Return player to party head
			ActorController.SetPartyActive(true);
			ActorController.SetPlayerToDefault();

			AfterFinish();
		}

		[Button, ShowInPlay]
		public void Restart()
		{
			Cart.SetTrack(StartingTrack, StartingDistance);

			// TODO (C.L. 01-25-2023): Refactor this out
			foreach (var obstacle in GetComponentsInChildren<CoasterObstacle>())
				obstacle.Reset();

			// TODO (C.L. 01-25-2023): Move these to Minigame
			foreach (IMinigameResettable resettable in _resettables)
				resettable.OnMinigameReset();

		}

		protected override void Update()
		{
			base.Update();

			if (!IsRunning)
				return;
		}

		public void OnActivate()
		{
			Camera.Priority = GameCams.PRIORITY_ACTIVE;
		}

		public void OnRelease(ref CinemachineBlendDefinition? blend)
		{
			blend           = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0);
			Camera.Priority = GameCams.PRIORITY_INACTIVE;
		}

		public void ActiveUpdate(){ }

		public void GetBlends(ref CinemachineBlendDefinition? blend, ref CinemachineBlenderSettings settings) { }

		public async void OnCarVoided(CoasterCarActor car)
		{
			// TODO (C.L. 01-23-2023): Checkpoints
			await UniTask2.Seconds(1.5f);
			Cart.ToRide();
			Restart();
		}

		public void OnTrackEnd(CoasterTrack.EndBehaviors behavior, CoasterTrack track, CoasterCarActor actor)
		{
			switch (behavior)
			{
				case CoasterTrack.EndBehaviors.Voided: OnCarVoided(actor); break;

				case CoasterTrack.EndBehaviors.None:   break;
				case CoasterTrack.EndBehaviors.JumpTo: break;
				case CoasterTrack.EndBehaviors.Eject:    break;
			}
		}

		public void OnCoasterTrigger(string trigger, SplineUser user) {
			if(trigger == RollerCoaster.TRIGGER_FINISH)
				Finish();
		}

		public void OnActorTryEnter(Actor actor) { }
		public void OnActorExit(Actor     actor) { }

		public bool CoasterActive => IsRunning;
	}
}