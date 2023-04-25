using System;
using Anjin.Actors.States;
using Anjin.Nanokin.Park;
using Anjin.Util;
using Assets.Scripts.Utils;
using KinematicCharacterController;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Serialization;
using UnityUtilities;
using Util;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	[RequireComponent(typeof(Stunnable))]
	[RequireComponent(typeof(EncounterCollider))]
	public class SurpriseFishActor : ActorKCC, IEncounterActor
	{
		[Title("States")]
		[SerializeField]			   private MockState   _waitState;       // Wait for a signal to leap.
		[SerializeField]               private MockState   _leapChargeState; // Charging the leap. (visual state)
		[SerializeField]			   private JumpState   _emergeState;     // Flies into the air, then holds to aim

		[SerializeField] private HangState _hangState;
		//[OdinSerialize, NonSerialized] private TweenState  _leapState;       // Jump to the goal position!!
		[SerializeField]			   private JumpState _leapState;       // Jump to the goal position!!
		[SerializeField]               private GroundState _layState;        // Lay on the ground in a pathetic manner
		[SerializeField]               private FlopState   _flopState;       // Flop in the air.

		[Title("Settings", HorizontalLine = false)]
		[SerializeField] private ManualTimer _flopDelay;
		[SerializeField] private float AfterImageCooldownDuration = 0.35f;

		[Title("Rendering")]
		[SerializeField] private GameObject _goView;
		[SerializeField] private GameObject _goShadow;
		[SerializeField] private float      _airVerticalStillVelocityRange = 1.5f;

		[Title("Particles")]
		[InfoBox("Particles active while the fish is about to jump.")]
		[SerializeField] private ParticleRef _fxLeapCharging;
		[SerializeField] private ParticleRef _fxLeapStart;
		[SerializeField] private ParticleRef _fxLeaping;
		[SerializeField] private ParticleRef _fxFlopStart;
		[SerializeField] private ParticleRef _fxFlopping;
		[SerializeField] private ParticleRef _fxLand;

		[Title("Audio")]
		[SerializeField] private AudioDef _adLeapStart;
		[SerializeField] private AudioDef _adFlopStart;
		[SerializeField] private AudioDef _adLand;

		private Stunnable         _stunnable;
		private EncounterCollider _encounterCollider;
		private float             _afterImageCooldown;

		public override StateKCC GetDefaultState() => _waitState;

		public override float MaxJumpHeight => 0;

		public bool AtPeak => (_emergeState.Gravity == 0);

		public bool IsAggroed => (_leapState);

		//public Vector3 LeapGoal
		//{
		//set => _leapState.Goal = value;
		//}

		protected override void Awake()
		{
			base.Awake();

			_stunnable         = GetComponent<Stunnable>();
			_encounterCollider = GetComponent<EncounterCollider>();

			_waitState.airMetrics = airMetrics;
			_leapChargeState.airMetrics = airMetrics;
			_emergeState.airMetrics = airMetrics;
			_leapState.airMetrics = airMetrics;
			_layState.airMetrics = airMetrics;
			_flopState.airMetrics = airMetrics;
		}

		protected override void Start()
		{
			base.Start();
			_encounterCollider.NeutralAdvantage = EncounterAdvantages.Enemy;
		}

		public const int STATE_WAIT       = 0; // Wait for a signal to leap.
		public const int STATE_LEAPCHARGE = 1; // Charging the leap. (visual state)
		public const int STATE_EMERGE	  = 2; // Flies into the air, then holds to aim
		public const int STATE_HANG	      = 3; // Flies into the air, then holds to aim
		public const int STATE_LEAP       = 4; // Jump to the goal position!!
		public const int STATE_LAY        = 5; // Lay on the ground in a pathetic manner
		public const int STATE_FLOP       = 6; // Flop in the air.

		protected override void RegisterStates()
		{
			RegisterState(STATE_WAIT, _waitState);
			RegisterState(STATE_LEAPCHARGE, _leapChargeState);
			RegisterState(STATE_EMERGE, _emergeState);
			RegisterState(STATE_HANG, _hangState);
			RegisterState(STATE_LEAP, _leapState);
			RegisterState(STATE_LAY, _layState);
			RegisterState(STATE_FLOP, _flopState);
		}

		protected override StateKCC GetNextState(ref Vector3 currentVelocity, float deltaTime)
		{
			UpdateOverlaps();
			_flopDelay.Update(deltaTime);

			if (_waitState && inputs.swordPressed)
			{
				Dbg.Log("Current actor state: wait state");

				// Charging the laep (water dropplets and stuff indicating that something sinister is about to take place)
				return _leapChargeState;
			}

			if (_leapChargeState && inputs.jumpPressed)
			{
				//#if UNITY_EDITOR
				//				if (inputs.hasMove)
				//					// Debug feature when player is the fish
				//					_leapState.Goal = (position + inputs.move).DropToGround();
				//#endif
				Dbg.Log("Current actor state: leap charge state");

				GameSFX.Play(_adLeapStart, transform.position);
				//return _leapState;
				return _emergeState.WithDefaultHeight(ref currentVelocity);
			}

			if (_emergeState)
			{
				Dbg.Log("Current actor state: emerge state");
				if (_emergeState.CloseToApex)
				{
					return _hangState;
				}

				if (inputs.divePressed)
				{
					return _leapState.WithForceTowards(ref currentVelocity, _leapState.settings.HorzForce, inputs.move);
				}
			}

			if (_hangState)
			{
				Dbg.Log("Current actor state: hang state");
				if (inputs.divePressed)
				{
					return _leapState.WithForceTowards(ref currentVelocity, _leapState.settings.HorzForce, inputs.move);
				}
			}

			if (_leapState || _flopState)
			{
				if (Motor.GroundingStatus.IsStableOnGround)
				{
					// Landed on the ground!
					GameSFX.Play(_adLand, transform.position);

					if (hasWater)
					{
						return _waitState;
					}

					return _layState;
				}
				else
				{
					_encounterCollider.NeutralAdvantage = EncounterAdvantages.Enemy;

					return null;
				}
			}

			if (_layState)
			{
				if (hasWater)
				{
					return _waitState;
				}

				if (!_stunnable.Stunned && _flopDelay.IsDone)
				{
					// Time to flop!
					_flopDelay.Reset();
					GameSFX.Play(_adFlopStart, transform.position);
					_encounterCollider.NeutralAdvantage = EncounterAdvantages.Player;

					return _flopState;
				}

				if (inputs.jumpPressed)
				{
					// Get ready to flop.
					_flopDelay.PlayOrContinue();
					return _layState;
				}
			}

			if (_flopState)
			{
				if (hasWater)
				{
					return _waitState;
				}
			}

			return null;
		}

		public override void OnColliderHit(Collider collider,
			Vector3 hitNormal,
			Vector3 hitPoint,
			ref HitStabilityReport hitStability
		)
		{
			_encounterCollider.HandleCollision(collider);
		}

		protected override void UpdateFX()
		{
			base.UpdateFX();

			if (currentState.justActivated)
			{
				_goView.gameObject.SetActive(!_waitState);
				_goShadow.gameObject.SetActive(!_waitState);
			}

			_fxLeapCharging?.SetPlaying(_leapChargeState);
			_fxLeapStart?.SetPlaying(_leapState && airMetrics.justLifted);
			_fxLeaping?.SetPlaying(_leapState);
			_fxFlopStart?.SetPlaying(_layState && airMetrics.justLifted);
			_fxFlopping?.SetPlaying(_flopState);
			_fxLand?.SetPlaying(_layState && airMetrics.justLanded);

			if (_leapState)
			{
				_afterImageCooldown -= Time.deltaTime;
				if (_afterImageCooldown <= 0)
				{
					_afterImageCooldown = AfterImageCooldownDuration;

					SpriteRenderer spriteRenderer  = renderer.Animable.Renderer;
					Transform      spriteTransform = spriteRenderer.transform;
					Sprite         sprite          = spriteRenderer.sprite;

					GameEffects.Live.SpawnAfterImage(spriteTransform.position + spriteTransform.rotation * Vector3.forward * 0.1f, sprite);
				}
			}
		}

		public override void UpdateRenderState(ref RenderState state)
		{
			base.UpdateRenderState(ref state);

			if (stateChanged)
			{
				state             = new RenderState();
				state.animPercent = -1;
			}

			switch (currentStateID)
			{
				case STATE_EMERGE:
				case STATE_LEAP:
				case STATE_FLOP:
				case STATE_HANG:
					if (velocity.y > _airVerticalStillVelocityRange)
						state.animID = AnimID.FishRise;
					else if (velocity.y < -_airVerticalStillVelocityRange)
						state.animID = AnimID.FishFall;
					else
						state.animID = AnimID.FishStill;

					state.animPercent = -1;
					break;

				case STATE_LAY:
					state.animID      = AnimID.FishFlop;
					state.animPercent = -1;
					break;
			}

			if (_flopDelay.IsPlaying)
			{
				state.animID      = AnimID.FishFlop;
				state.animPercent = 0.75f;
			}
		}

		public override void AfterCharacterUpdate(float deltaTime)
		{
			base.AfterCharacterUpdate(deltaTime);

			inputs.jumpPressed = false;
		}

		public void OnHit(SwordHit info)
		{
			ChangeState(_flopState);
		}

		[Serializable]
		public class JumpState : AirState
		{
			private Vector3 _startPosition;
			private bool _wasFalling;

			private bool _raising;
			private Vector3 _initialVelocity;

			public Settings settings;
			public override float SpeedDamping => settings.HorizontalDamping;

			public override float Gravity
			{
				get
				{
					float gravityScale = settings.ApexHang.GetGravityScale(airMetrics);
					return base.Gravity * settings.GravityScale * (gravityScale > 0 ? gravityScale : 0);
				}
			}

			private float FallingMod => airMetrics.yDelta < 0 ? -1 : 1;
			public bool CloseToApex {
				get
				{
					return actor.velocity.magnitude / _initialVelocity.magnitude * FallingMod < settings.ApexOffset;
				}
			}

		//protected override Vector3 TurnDirection
			//{
			//	get
			//	{
			//		if (settings.UseInputLookDirection && inputs.look.HasValue && inputs.look.Value.magnitude > Mathf.Epsilon)
			//		{
			//			return inputs.look.Value;
			//		}

			//		if (Vector3.Distance(_startPosition, actor.position) < Mathf.Epsilon)
			//			return actor.inertia.direction;

			//		return _startPosition.Towards(actor.position).Horizontal();
			//	}
			//}

			public JumpState(Settings settings)
			{
				this.settings = settings;
			}

			public override void OnActivate()
			{
				_startPosition = actor.position;
				actor.inertia.Reset(settings.InertialControl, actor.velocity);

				_raising = actor.velocity.y > 0;
			}

			protected override void UpdateVertical(ref Vector3 vel, float dt)
			{
				base.UpdateVertical(ref vel, dt);

				// In case we start rising again for some reason, wind volumes etc.
				if (_wasFalling && vel.y > 0)
					_startPosition = actor.position;

				_wasFalling = vel.y < 0;

				if (!inputs.jumpHeld && _raising && vel.y > 0)
				{
					_raising = false;
				}
			}

			public JumpState WithHeight(ref Vector3 currentVelocity, float height)
			{
				actor.Jump(ref currentVelocity, height);
				_initialVelocity = currentVelocity;
				return this;
			}

			public JumpState WithForceTowards(ref Vector3 currentVelocity, float force, Vector3 direction)
			{
				actor.Jump(ref currentVelocity, 0, force, direction);
				_initialVelocity = currentVelocity;
				return this;
			}

			public JumpState WithDefaultHeight(ref Vector3 currentVelocity)
			{
				actor.Jump(ref currentVelocity, settings.Height);
				_initialVelocity = currentVelocity;
				return this;
			}

			[Serializable]
			public class Settings
			{
				public bool UseInputLookDirection = false;

				[FormerlySerializedAs("height")]
				public float Height = 5f;
				public float HorzForce = 5f;
				[FormerlySerializedAs("gravityScale")]
				public float GravityScale = 1;
				public float HorizontalDamping = 4f;
				[Range01]
				public float ShortHopDrag = 0f;
				public float ShortHopDuration = 0f;

				[FormerlySerializedAs("inertiaControl")]
				public InertiaForce.Settings InertialControl;
				[FormerlySerializedAs("apexHang")]
				public JumpHanger ApexHang;
				public float TurnSpeed;

				public float ApexOffset = -0.1f;
			}
		}

		[Serializable]
		public class HangState : AirState
		{
			public Settings settings;
			public override float Gravity => settings.Gravity;
			public override float SpeedDamping => 0;

			public HangState(Settings settings)
			{
				this.settings = settings;
			}

			public override void OnActivate()
			{
				base.OnActivate();
				actor.velocity.y = 0;
			}

			protected override void UpdateVertical(ref Vector3 vel, float dt)
			{
				base.UpdateVertical(ref vel, dt);
			}

			protected override void UpdateHorizontal(ref Vector3 hvel, float dt)
			{
				return;
			}

			[Serializable]
			public class Settings
			{
				public float Gravity = 0.02f;
			}
		}

		[Serializable]
		public class FlopState : AirState
		{
			[Title("Settings", HorizontalLine = false), FormerlySerializedAs("verticalForce")]
			public RangeOrFloat verticalHeight;
			public RangeOrFloat horizontalForce;
			public RangeOrFloat horizontalDrag;
			[Space]
			public RangeOrFloat rotationForce;
			public RangeOrFloat rotationDrag;

			[Title("Runtime", HorizontalLine = false)]
			[ShowInInspector, HideInEditorMode, Sirenix.OdinInspector.ReadOnly]
			private float _yawVelocity;
			[ShowInInspector, HideInEditorMode, Sirenix.OdinInspector.ReadOnly]
			private float _yawDrag;
			[ShowInInspector, HideInEditorMode, Sirenix.OdinInspector.ReadOnly]
			private float _hDrag;

			public override void UpdateFacing(ref Vector3 facing, float dt)
			{
				if (actor.velocity.magnitude < Mathf.Epsilon)
					return;

				facing = Quaternion.RotateTowards(
					actor.Motor.TransientRotation,
					Quaternion.LookRotation(actor.velocity.Change3(y: 0).normalized),
					_yawVelocity * dt
				) * Vector3.forward;

				_yawVelocity *= 1 - _yawDrag;
			}

			protected override void UpdateHorizontal(ref Vector3 hvel, float dt)
			{
				base.UpdateHorizontal(ref hvel, dt);

				hvel *= 1 - _hDrag;
			}

			public override void OnActivate()
			{
				base.OnActivate();

				actor.ClearVelocity();
				actor.Jump(verticalHeight.Evaluate());
				actor.AddForce(new Vector3(horizontalForce.Evaluate() * RNG.Sign, 0, horizontalForce.Evaluate() * RNG.Sign));

				_yawVelocity = rotationForce.Evaluate() * RNG.Sign;
				_yawDrag     = rotationDrag.Evaluate();
				_hDrag       = horizontalDrag.Evaluate();
			}
		}
	}
}