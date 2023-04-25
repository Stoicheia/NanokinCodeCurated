using System;
using Anjin.Nanokin.Park;
using Anjin.Util;
using API.Spritesheet.Indexing.Runtime;
using Assets.Scripts.Utils;
using Knife.DeferredDecals;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util;
using Util.Odin.Attributes;
using Util.UniTween.Value;

namespace Anjin.Actors
{
	/// <summary>
	/// An actor that can
	/// - Roam slowly (move)
	/// - Rush fast (move + run held)
	/// - Dive underground and out (dive)
	/// - Emerge out fully (jump while underground)
	/// </summary>
	public class LandsharkActor : ActorKCC, IRecyclable, IEncounterActor
	{
// @formatter:off
		[Title("Design")]
		[SerializeField] private float _emergeCollisionDelay = 0.15f;
		[SerializeField] private LandsharkActorSettings Settings;
		[SerializeField] private ManualTimer            _emergeEnterTimer;

		[Title("References")]
		[SerializeField] private SpriteRenderer      _spriteRenderer;
		[SerializeField] private SpriteAnim _spriteAnimator;
		[SerializeField] private Transform           _view;
		[SerializeField] private Decal               _shadowDecal;

		[Title("FX")]
		[SerializeField] private float _distanceUnderground;
		[SerializeField] private float _distanceUndergroundPreEmerge;
		[SerializeField] private float _afterImageCooldownDuration;

		[FormerlySerializedAs("_burrowParticles"), SerializeField]
		private ParticleRef _fxBurrow;

		[SerializeField] private ParticleRef _fxEmerging;
		[SerializeField] private ParticleRef _fxEmergeBurst;
		[SerializeField] private ParticleRef _fxTrail;

		[Title("Tweens")]
		[SerializeField, FormerlySerializedAs("_diveIn")] private TweenerTo _diveInProgress;
		[SerializeField] private TweenerTo _diveInOpacity;

		[Title("Sounds")]
		[SerializeField] private AudioDef SFX_Burrow;
		[SerializeField] private AudioDef SFX_Emerging;
		[SerializeField] private AudioDef SFX_HeadBounce;

		[SerializeField, FormerlySerializedAs("_diveOut")]   private TweenerTo _diveOutProgress;
		[SerializeField]                                     private TweenerTo    _diveOutOpacity;
		[SerializeField, FormerlySerializedAs("_emergeOut")] private TweenerTo    _emergeOutProgress;
		[SerializeField]                                     private Vector3Tween _emergeOutScale;
// @formatter:on

		private Stunnable _stunnable;
		private Vector3   _shadowBaseScale;

		[ShowInPlay] private TankMoveState _moveState;
		[ShowInPlay] private GroundState   _preEmergeState;
		[ShowInPlay] private GroundState   _emergeState;

		private TweenableFloat   _burrowProgress; // value in range 0 - 1 where 1 is fully underground, used for rendering.
		private TweenableFloat   _opacity;
		private TweenableVector3 _scale;

		private bool  _isUnderground = true;
		private bool  _isHeadSprite;
		private bool  _hasJustEmerged;
		private float _afterImageCooldown;

		public override StateKCC GetDefaultState() => _moveState;

		public override float MaxJumpHeight => 0;

		protected override void Awake()
		{
			base.Awake();

			_stunnable = gameObject.GetOrAddComponent<Stunnable>();
			_stunnable.onStunned += () =>
			{
				// Force out of the ground if we're stunned
				_scale.Set(_emergeOutScale);
			};

			_shadowBaseScale = _shadowDecal.transform.localScale;
		}

		protected override void RegisterStates()
		{
			_burrowProgress = 0;
			_opacity        = 1;
			_scale          = Vector3.one;

			RegisterState(_moveState      = new TankMoveState(Settings.Move));
			RegisterState(_emergeState    = new GroundState());
			RegisterState(_preEmergeState = new GroundState());
		}

		public void Recycle()
		{
			_isHeadSprite              = false;
			_isUnderground             = true;
			_hasJustEmerged            = false;
			_afterImageCooldown        = 0;
			_moveState.accumulatedTime = 0;
			_burrowProgress.value      = 0;
			_opacity.value             = 1;
			_scale.value               = Vector3.one;
		}

		protected override void Update()
		{
			base.Update();

			bool collides = _emergeState && elapsedStateTime > _emergeCollisionDelay;
			Motor.Capsule.isTrigger = !collides;

			// gameObject.layer = _emergeState && elapsedStateTime > _emergeCollisionDelay
			// 	? Layers.Enemy
			// 	: Layers.Player;

			if (_stunnable.Stunned)
				inputs = CharacterInputs.DefaultInputs;
		}

		protected override StateKCC GetNextState(ref Vector3 currentVelocity, float deltaTime)
		{
			_emergeEnterTimer.Update(deltaTime);

			if (_stunnable.Stunned)
				return _emergeState;

			if (_moveState)
			{
				if (inputs.jumpPressed)
				{
					_emergeEnterTimer.Restart();
					return _preEmergeState;
				}
				else if (inputs.diveHeld)
				{
					if (!_isUnderground)
					{
						// Go underground smoothly. (invisible)
						_isUnderground = true;
						_burrowProgress.To(1, _diveInProgress);
						return _moveState;
					}
				}
				else
				{
					if (_isUnderground)
					{
						// Back out smoothly. (fin)
						_isUnderground = false;
						_burrowProgress.To(0, _diveOutProgress);
						return _moveState;
					}
				}
			}

			if (_preEmergeState && _emergeEnterTimer.JustEnded)
			{
				// WE EMERGE! rawrrr
				_hasJustEmerged = true;
				_isHeadSprite   = true;
				_isUnderground  = false;

				// Tweens
				_burrowProgress.To(0, _emergeOutProgress);
				_scale.To(_emergeOutScale);
				_opacity.To(1, _diveInOpacity);

				return _emergeState;
			}

			if (_emergeState && inputs.diveHeld)
			{
				// Return to movement.
				_isUnderground = false;
				_burrowProgress.To(0, _diveOutProgress);
				return _moveState;
			}

			return null;
		}

		protected override void UpdateFX()
		{
			base.UpdateFX();

			if (!_emergeState && _burrowProgress.value >= 1 - Mathf.Epsilon)
			{
				// We can only swap the sprite while fully underground, otherwise it makes no sense and looks jarring.
				_isHeadSprite = false;
			}

			// Particles
			_fxBurrow?.SetPlaying(_burrowProgress.IsTweenActive);
			_fxEmerging?.SetPlaying(_preEmergeState);
			_fxEmergeBurst?.SetPlaying(_hasJustEmerged);
			_fxTrail?.SetPlaying(_moveState && _burrowProgress.value >= 1 - Mathf.Epsilon);

			// After-images
			if (_moveState && _moveState.MaxSpeedProgress > 0.25f)
			{
				_afterImageCooldown -= Time.deltaTime;
				if (_afterImageCooldown <= 0)
				{
					_afterImageCooldown = _afterImageCooldownDuration;
					GameEffects.Live.SpawnAfterImage(_spriteRenderer.transform.position + _spriteRenderer.transform.rotation * Vector3.forward * 0.1f, _spriteAnimator.player.Sprite);
				}
			}

			// Update the projector to scale with burrowing
			_shadowDecal.transform.localScale = _shadowBaseScale * (1 - _burrowProgress.value);
		}

		public override void AfterCharacterUpdate(float deltaTime)
		{
			base.AfterCharacterUpdate(deltaTime);

			_hasJustEmerged = false;

			if (!IsMotorStable)
			{
				// Snap to ground
				int hits = Physics.RaycastNonAlloc(position, Vector3.down, TempArrays.RaycastHits8, 20, Layers.Walkable.mask);
				if (hits > 0)
				{
					Motor.SetPosition(TempArrays.RaycastHits8[0].point);
				}
			}
		}

		public override void UpdateRenderState(ref RenderState state)
		{
			if (stateChanged)
			{
				state = new RenderState(AnimID.Roam);
			}

			if (_spriteAnimator)
			{
				// TODO use vfx
				// _spriteAnimator.opacity = (1 - _burrowProgress).Clamp01();
			}

			state.animID = _emergeState || _isHeadSprite ? AnimID.Emerge : AnimID.Roam;

			if (_preEmergeState)
				state.offset = Vector3.down * _distanceUndergroundPreEmerge;
			else
				state.offset = Vector3.down * _burrowProgress * _distanceUnderground;

			if (_view)
				_view.localScale = _scale;
		}

		public bool IsAggroed {
			get {
				if (activeBrain is LandsharkBrain lbrain) {
					switch (lbrain.State) {
						case LandsharkBrain.States.Chase:
						case LandsharkBrain.States.Teleporting:
						case LandsharkBrain.States.Emerge:
							return true;

						default: return false;
					}
				}

				return false;
			}
		}
	}
}