using System;
using System.Collections.Generic;
using Anjin.Cameras;
using Anjin.Nanokin;
using Anjin.Nanokin.Map;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using Util;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;

namespace Anjin.Actors
{
	public class PlayerControlBrain : ActorBrain,
		ICharacterActorBrain,
		IFirstPersonFlightBrain,
		ICharacterInputProvider<MinecartInputs>,
		ICameraBrain
	{
		private const int MAX_INTERACTABLES      = 6;
		private const int MAX_ACTOR_COLLISIONS   = 12;
		private const int MAX_TRIGGER_COLLISIONS = 12;
		private const int MAX_COLLECTABLES       = 256;

		//This is const because we need to know the maximum we can set the collectable magnetic range to.
		public const float COLLECTABLE_DETECT_RADIUS = 20;

		const float SPLICER_TRANSITION_SECONDS = 0.2f;

		public bool  InteractCanDetect;
		public float InteractDetectRadius;

		public LayerMask InteractDetectLayer;
		public LayerMask ActorCollideLayer;
		public LayerMask TriggerCollideLayer;
		public LayerMask TransitionCollideLayer;
		public LayerMask CollectableColliderLayer;

		[Title("Debug")]
		[NonSerialized, ShowInPlay] public TransitionVolume transitionToggle;
		[NonSerialized, ShowInPlay] public PlayerCollisionChecker<TransitionVolume> nearbyTransitionVolumes;
		[NonSerialized, ShowInPlay] public ActorCollisionChecker                    nearbyActors;
		[NonSerialized, ShowInPlay] public PlayerCollisionChecker<Trigger>          nearbyTriggers;
		[NonSerialized, ShowInPlay] public List<Trigger>                            triggerToggles;

		[NonSerialized, ShowInPlay] public InteractableCollisionChecker        nearbyInteractables;
		[NonSerialized, ShowInPlay] public List<Collectable> seenCollectables;
		[NonSerialized, ShowInPlay] public PlayerCollisionChecker<Collectable> nearbyCollectables;

		private List<Collectable> attractedCollectables;

		private const int MAX_TRANSITION_COLLISIONS = 1;

		private bool _wasMenuing;

		public override int Priority => 1;

		private void Awake()
		{
			nearbyInteractables     = new InteractableCollisionChecker(MAX_INTERACTABLES);
			nearbyActors            = new ActorCollisionChecker(MAX_ACTOR_COLLISIONS);
			nearbyTriggers          = new PlayerCollisionChecker<Trigger>(MAX_TRIGGER_COLLISIONS);
			nearbyTransitionVolumes = new PlayerCollisionChecker<TransitionVolume>(MAX_TRANSITION_COLLISIONS);
			seenCollectables        = new List<Collectable>();
			nearbyCollectables      = new PlayerCollisionChecker<Collectable>(MAX_COLLECTABLES);
			attractedCollectables = new List<Collectable>();

			triggerToggles   = new List<Trigger>();
			transitionToggle = null;
		}


		private async UniTask OpenSplicerMenu()
		{
			//OverworldHUD.HideStats(false);
			//OverworldHUD.HideCompass();

			if (GameOptions.current.splicer_menu_transitions)
				await GameEffects.FadeOut(SPLICER_TRANSITION_SECONDS / 2f);

			Overworld.Controllers.LayerController.Activate("pause_object_off", false);

			await MenuManager.SetMenu(Menus.SplicerHub);

			if (GameOptions.current.splicer_menu_transitions)
				await GameEffects.FadeIn(SPLICER_TRANSITION_SECONDS / 2f);

			SplicerHub.exitHandler = ExitSplicerHub;

			_wasMenuing = true;
		}

		private static void ExitSplicerHub(SplicerHub hub)
		{
			ExitSplicerHubAsync(hub).Forget();
		}

		private static async UniTask ExitSplicerHubAsync(SplicerHub hub)
		{
			await GameEffects.FadeOut(SPLICER_TRANSITION_SECONDS / 2f);
			await MenuManager.SetMenu(Menus.SplicerHub, false);
			await GameEffects.FadeIn(SPLICER_TRANSITION_SECONDS / 2f);
			Overworld.Controllers.LayerController.Activate("pause_object_off", true);
		}

		public void CheckInteractions(Actor actor)
		{
			nearbyInteractables.Clear();
			// Interactables
			//-------------------------------------------------------------------
			if (GameController.Live.CanControlPlayer() && InteractCanDetect)
			{
				int interactables = Physics.OverlapSphereNonAlloc(actor.transform.position + actor.facing * InteractDetectRadius * 0.5f, InteractDetectRadius, nearbyInteractables.colliders, InteractDetectLayer, QueryTriggerInteraction.Collide);
				nearbyInteractables.FilterColliders(interactables);

				if (nearbyInteractables.count > 0 && GameInputs.interact.IsPressed && nearbyInteractables.objs[0].CanInteractWith(actor))
					nearbyInteractables.objs[0].Interact(actor);
			}

			if (actor is ActorKCC kccActor)
			{
				bool controlled = actor.activeBrain == this;

				// Triggers
				int triggers = CheckKCCCapsule(kccActor, nearbyTriggers.colliders, TriggerCollideLayer);
				nearbyTriggers.FilterColliders(triggers);

				// Collisions with other actors
				int actors = CheckKCCCapsule(kccActor, nearbyActors.colliders, ActorCollideLayer);
				nearbyActors.FilterColliders(actors);

				// Transitions
				int transitions = CheckKCCCapsule(kccActor, nearbyTransitionVolumes.colliders, Layers.TriggerVolume);
				nearbyTransitionVolumes.FilterColliders(transitions);

				int collectables = Physics.OverlapSphereNonAlloc(kccActor.transform.position, COLLECTABLE_DETECT_RADIUS, nearbyCollectables.colliders, CollectableColliderLayer, QueryTriggerInteraction.Collide);
				nearbyCollectables.FilterColliders(collectables);


				// Transitions. NOTE: MUST go before triggers!
				//-----------------------------------------------------
				if (nearbyTransitionVolumes.any)
				{
					// Go into transition mode.
					TransitionVolume trans = nearbyTransitionVolumes.objs[0];

					if (trans != transitionToggle)
					{
						transitionToggle = trans;

						if (trans.Detectable)
						{
							ActorController.Live.TransitionBrain.StartUsing(ActorController.playerActor as PlayerActor, trans);
							/*ActorController.Live.TransitionBrain.UpdateVolume(trans);
							actor.AddBrain(ActorController.Live.TransitionBrain);*/
							//ActorController.Live.TransitionBrain.isWarpOut = true;

							//Debug.Log($"Trigger transition volume {trans}");
							/*for (int i = 0; i < nearbyTriggers.count; i++) {
								if (trans.TryGetComponent(out Trigger trigger))
									triggerToggles.Add(trigger);
							}*/
							return;
						}
					}
				}
				else
				{
					transitionToggle = null;
				}


				// Triggers NOTE: This code runs even if the player has another brain.
				//-----------------------------------------------------

				for (int i = 0; i < triggerToggles.Count; i++)
				{
					if (!nearbyTriggers.objs.ContainsValue(triggerToggles[i])) {
						triggerToggles[i].OnPlayerLeave();
						triggerToggles.RemoveAt(i--);
					}
				}
				for (int i = 0; i < nearbyTriggers.count; i++)
				{
					if (!triggerToggles.Contains(nearbyTriggers.objs[i]))
					{
						if (controlled || !nearbyTriggers.objs[i].RequiresControlledPlayer)
						{
							if(nearbyTriggers.objs[i].OnTriggerBase(kccActor))
								triggerToggles.Add(nearbyTriggers.objs[i]);
						}
					}
				}

				for (int i = 0; i < nearbyCollectables.count; i++)
				{
					Collectable collectable = nearbyCollectables.objs[i];
					if (collectable == null) continue;

					if (collectable.PlayerAttracts && collectable.spawned)
					{
						if (Vector3.Distance(kccActor.transform.position, collectable.transform.position) <= collectable.PlayerAttractRange)
						{
							if(!attractedCollectables.Contains(collectable))
								attractedCollectables.Add(collectable);
							if (!seenCollectables.Contains(collectable))
							{
								seenCollectables.Add(collectable);
								collectable.OnSuccessfulCollect += (() => attractedCollectables.Remove(collectable));
							}
						}

						if (attractedCollectables.Contains(collectable))
						{
							if (!collectable.spawned)
								attractedCollectables.Remove(collectable);
							var colTransform = collectable.transform;
							var position = colTransform.position;
							position += (kccActor.transform.position - position).normalized * 6 * Time.deltaTime;
							colTransform.position = position;
						}
					}
				}
			}

			// Menuing
			// ----------------------------------------

			if (GameController.Live.CanControlPlayer())
			{
				if (GameInputs.showOverworldHUD.IsPressed)
				{
					OverworldHUD.Live.NextMode();
					//OverworldHUD.Live.ToggleUIState();
				}
				else if (!SplicerHub.Exists || !SplicerHub.menuActive)
				{
					PlayerActor playerActor = actor as PlayerActor;

					if ((playerActor != null))
					{
						if (GameInputs.splicer.AbsorbPress(0.1f))
						{
							if (playerActor.currentState.IsGround) {
								if (!GameController.IsMinigame) {
									OpenSplicerMenu().ForgetWithErrors();
								}
								else {
									UI.MinigameQuitPrompt.Live.Show();
								}
							}
						}
					}
				}
			}
		}

		private void UnassignAttractedCollectible(Collectable c)
		{
			if(attractedCollectables.Contains(c))
				attractedCollectables.Remove(c);
		}
		private void Update()
		{
			nearbyInteractables.Clear();

			if (actor == null)
			{
				return;
			}

			CheckInteractions(actor);
		}

		public int CheckKCCCapsule(ActorKCC actor, Collider[] colliders, LayerMask mask) =>
			Physics.OverlapCapsuleNonAlloc(actor.transform.position + actor.Motor.CharacterTransformToCapsuleBottom,
				actor.transform.position + actor.Motor.CharacterTransformToCapsuleTop,
				actor.Motor.Capsule.radius,
				colliders,
				mask,
				QueryTriggerInteraction.Collide);

		public override void OnTick(float dt) { }

		public void ResetInputs(Actor character, ref CharacterInputs inputs)
		{
			inputs = CharacterInputs.DefaultInputs;
		}

		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			if (!GameController.Live.CanControlPlayer())
			{
				inputs = CharacterInputs.DefaultInputs;
				return;
			}

			if (_wasMenuing)
			{
				_wasMenuing = false;
				inputs      = CharacterInputs.DefaultInputs;
				return;
			}

			if (GameInputs.ActiveDevice == InputDevices.None)
			{
				inputs = CharacterInputs.DefaultInputs;
				return;
			}

			inputs.look = null;

			Vector3 joystick          = new Vector3(GameInputs.move.Horizontal, 0, GameInputs.move.Vertical);
			float   joystickMagnitude = joystick.magnitude;
			if (joystickMagnitude < 0.02f)
				// A bit of deadzone, stops the player from moving at 0.01mm/s and lookin weird
				joystickMagnitude = 0;

			// Joystick simulation on keyboard using the run key.
			if (GameInputs.ActiveDevice == InputDevices.KeyboardAndMouse)
				if (GameOptions.current.run_by_default)
				{
					if (GameInputs.run.IsDown) joystick *= 0.45f;
				}
				else
				{
					if (!GameInputs.run.IsDown) joystick *= 0.6f;
				}


			Vector3    camDir = MathUtil.GetPlanarCameraDirection(GameCams.Live.UnityCam.gameObject.transform.rotation, character.Up);
			Quaternion camRot = Quaternion.LookRotation(camDir, character.Up);

			inputs.move    = (camRot * joystick).normalized * joystickMagnitude;
			inputs.hasMove = joystickMagnitude > Mathf.Epsilon;

			inputs.jumpPressed  |= GameInputs.jump.IsPressed;
			inputs.divePressed  |= GameInputs.dive.IsPressed;
			inputs.glidePressed |= GameInputs.jump.IsPressed;
			inputs.doubleJumpPressed |= GameInputs.jump.IsPressed;
			inputs.pogoPressed  |= GameInputs.pogo.IsPressed;

			inputs.jumpHeld  |= GameInputs.jump.IsDown;
			inputs.diveHeld  |= GameInputs.dive.IsDown;
			inputs.glideHeld |= GameInputs.jump.IsDown;
			inputs.doubleJumpHeld |= GameInputs.jump.IsDown;
			inputs.pogoHeld  |= GameInputs.pogo.IsDown;

			if (nearbyInteractables.count == 0)
			{
				inputs.swordPressed |= GameInputs.sword.IsPressed;
				inputs.swordHeld |= GameInputs.sword.IsDown;
			}

			inputs.runHeld |= GameInputs.run.IsDown;
		}

		public void PollInputs(ref FirstPersonFlightInputs inputs)
		{
			inputs.MoveDirection = new Vector3(GameInputs.DebugCamMoveHor.Horizontal, GameInputs.DebugCamMoveVer.Value, GameInputs.DebugCamMoveHor.Vertical);
			inputs.ZoomDelta     = GameInputs.DebugCamMoveZoom.Value;
			inputs.FastMode      = GameInputs.run.IsDown;

			if (GameInputs.ActiveDevice != InputDevices.KeyboardAndMouse ||
			    GameController.DebugMode && !Mouse.current.rightButton.isPressed)
			{
				inputs.RotationDelta = new Vector2(GameInputs.look.Vertical, GameInputs.look.Horizontal);
			}
			else
			{
				inputs.RotationDelta.x = GameInputs.GetCapturedMouseDelta().x / 60 / GameOptions.current.CameraSensitivity * (GameOptions.current.CameraMouseInvertX ? -1 : 1);
				inputs.RotationDelta.y = GameInputs.GetCapturedMouseDelta().y / 60 / GameOptions.current.CameraSensitivity * (GameOptions.current.CameraMouseInvertY ? -1 : 1);
			}
		}

		public void PollInputs(ref MinecartInputs inputs)
		{
			inputs.leanLeft  = GameInputs.move.left.IsDown;
			inputs.leanRight = GameInputs.move.right.IsDown;

			inputs.jumpPressed  |= GameInputs.jump.IsPressed;
			inputs.swordPressed |= GameInputs.sword.IsPressed;
		}

		public bool ActiveOverride => true;

		public class InteractableCollisionChecker : PlayerCollisionChecker<Interactable>
		{
			public InteractableCollisionChecker(int max) : base(max) { }

			public override bool BelongsInList(Interactable obj) => !obj.Disabled;
		}

		public class ActorCollisionChecker : PlayerCollisionChecker<Actor>
		{
			public ActorCollisionChecker(int max) : base(max) { }
		}

		public class PlayerCollisionChecker<T>
			where T : Component
		{
			public T[]        objs;
			public int        count;
			public Collider[] colliders;
			public bool       any;

			public PlayerCollisionChecker(int max)
			{
				objs      = new T[max];
				colliders = new Collider[max];
				count     = 0;
			}

			public void FilterColliders(int collisions)
			{
				Clear();

				for (int i = 0; count < colliders.Length && i < collisions; i++)
				{
					if (!colliders[i]) continue;
					if (colliders[i].gameObject.TryGetComponent(out T obj))
					{
						if (BelongsInList(obj))
							objs[count++] = obj;
					}
				}

				any = count > 0;
			}

			public virtual bool BelongsInList(T obj) => true;

			public void Clear()
			{
				count = 0;
				any   = false;

				for (int i = 0; i < objs.Length; i++)
				{
					objs[i] = null;
				}
			}
		}

	}
}