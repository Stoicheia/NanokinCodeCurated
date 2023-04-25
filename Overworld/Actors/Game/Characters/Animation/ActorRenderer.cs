using System;
using System.Collections.Generic;
using Anjin.Cameras;
using Anjin.Util;
using API.Spritesheet.Indexing.Runtime;
using Combat.Data.VFXs;
using JetBrains.Annotations;
using Knife.DeferredDecals;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityUtilities;
using Util;
using Util.Animation;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;

namespace Anjin.Actors
{
	public class ActorRenderer : MonoBehaviour
	{
		/// <summary>
		/// Directionality of the actor sprite.
		/// </summary>
		public Modes Mode = Modes.Auto;

		/// <summary>
		/// Required for animation features.
		/// </summary>
		[Title("Features")]
		[Required]
		[FormerlySerializedAs("SpriteAnimator")]
		[DisableInPlayMode]
		public SpriteAnim Animable;

		/// <summary>
		/// Required for directional animation.
		/// It's best to link it for improved startup performances.
		/// </summary>
		[Optional]
		[DisableInPlayMode]
		[CanBeNull]
		public ActorBase Actor; // Fetched automatically if unset

		/// <summary>
		/// Required to adjust drop-shadow with actor opacity.
		/// </summary>
		[DisableInPlayMode]
		[Optional]
		public Decal DropShadow;

		/// <summary>
		/// Can be set to use it when the actor becomes transparent.
		/// </summary>
		[DisableInPlayMode]
		[Optional]
		public Material TransparencyMaterial;

		// ------------------------------------------------------------

		/// <summary>
		/// Root of the actor.
		/// </summary>
		[Title("Roots")]
		[SerializeField]
		private Transform Root;

		[DisableInPlayMode, Optional]
		[HideInInspector]
		public Transform BillboardRoot;

		/// <summary>
		/// The transform that will receive sprite tilting.
		/// </summary>
		[FormerlySerializedAs("RotatableObject")]
		[DisableInPlayMode]
		[Optional]
		[Tooltip("The transform that will receive sprite tilting.")]
		public Transform TiltRoot;

		/// <summary>
		/// The transform that will receive position offsets.
		/// </summary>
		[FormerlySerializedAs("SpriteRoot"), FormerlySerializedAs("SpriteObj")]
		[DisableInPlayMode]
		[Required]
		[Tooltip("The transform that will receive position offsets.")]
		public Transform PositionRoot;

		[NonSerialized] public IRenderStateModifier modifier;

		[NonSerialized] public AnimID lastAnim;
		[NonSerialized] public float  animProgress;
		[NonSerialized] public int    animRepeats;

		[NonSerialized, ShowInPlay]
		public RenderState state = new RenderState(AnimID.Stand);

		private VFXManager       _vfxManager;
		private SpriteRenderer[] _spriteRenderers;
		private int              _lastVfxCount;
		private Vector3          _basePositionRoot;
		private Vector3          _baseScale;
		private Material         _baseMaterial;
		private bool             _wasOverriden;
		private Modes            _activeMode;


		private RenderFlags _lastFlags;
		private float       _lastPitch;
		private float       _lastRoll;

		private bool _valid;
		private bool _hasActor,        _hasVfx;
		private bool _hasPositionRoot, _hasTiltRoot;
		private bool _hasTransparencyMat;
		private bool _hasWalk, _hasRun, _hasJump, _hasRise, _hasFall, _hasLand;
		private bool _hasIdle1;
		private bool _hasDropShadow;

#if UNITY_EDITOR
		private HashSet<string> _printedUnknownAnimations;
#endif

		private static Dictionary<(string, Direction8), string> _cachedAnimationNames = new Dictionary<(string, Direction8), string>();


		private void Awake()
		{
#if UNITY_EDITOR
			_printedUnknownAnimations = new HashSet<string>();
#endif
			_vfxManager      = GetComponentInParent<VFXManager>();
			_spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

			_hasVfx       = _vfxManager != null;
			_baseMaterial = _spriteRenderers[0].sharedMaterial;
		}

		private void PlayerOnCompleted()
		{
			if (_hasActor)
			{
				ActorBrain brain = Actor.activeBrain;
				if (brain != null && brain.OverridesAnim(Actor))
					brain.OnAnimEndReached(Actor);
			}
		}

		private void Start()
		{
			if (Actor == null && !TryGetComponent(out Actor))
			{
				Actor = GetComponentInChildren<ActorBase>();
				if (Actor == null)
					Actor = GetComponentInParent<ActorBase>();
			}

			if (PositionRoot != null) {
				_basePositionRoot = PositionRoot.transform.localPosition;
				_baseScale        = PositionRoot.transform.localScale;
			}

			if (Animable == null)
			{
				this.LogError("Couldn't obtain an AnimableSpritesheet for this renderer.");
			}
			else if (Animable.IsProxy)
			{
				this.LogError("Cannot use a proxied AnimableSpritesheet with CharacteRenderer.");
			}
			else
			{
				_valid               =  true;
				Animable.onCompleted += PlayerOnCompleted;
			}

			_hasActor           = Actor != null;
			_hasDropShadow      = DropShadow != null;
			_hasPositionRoot    = PositionRoot != null;
			_hasTiltRoot        = TiltRoot != null;
			_hasTransparencyMat = TransparencyMaterial != null;

			if (Root == null && _hasActor) Root = Actor.transform;
			if (Root == null) Root              = transform;

			// Determine the type of character directions
			// ----------------------------------------
			OnSpriteChange();
		}

		public void OnSpriteChange()
		{
			if (Mode == Modes.Auto)
			{
				bool d  = TestDir(Direction8.Down);
				bool u  = TestDir(Direction8.Up);
				bool l  = TestDir(Direction8.Left);
				bool r  = TestDir(Direction8.Right);
				bool ul = TestDir(Direction8.UpLeft);
				bool dl = TestDir(Direction8.DownLeft);
				bool ur = TestDir(Direction8.UpRight);
				bool dr = TestDir(Direction8.DownRight);

				if (u && d && l)
				{
					if (r)
						_activeMode = ul && dl && ur && dr
							? Modes.Ordinal
							: Modes.Cardinal;
					else
						_activeMode = ul && dl
							? Modes.OrdinalMirrored
							: Modes.CardinalMirrored;
				}
			}
			else
			{
				_activeMode = Mode;
			}

			_hasRun   = TestDir(Direction8.Down, AnimID.Run);
			_hasWalk  = TestDir(Direction8.Down, AnimID.Walk);
			_hasRun   = TestDir(Direction8.Down, AnimID.Run);
			_hasJump  = TestDir(Direction8.Down, AnimID.Jump);
			_hasRise  = TestDir(Direction8.Down, AnimID.Rise);
			_hasFall  = TestDir(Direction8.Down, AnimID.Fall);
			_hasLand  = TestDir(Direction8.Down, AnimID.Land);
			_hasIdle1 = TestDir(Direction8.Down, AnimID.Idle1);
		}

		private void LateUpdate()
		{

			if (!_valid) return;

			float azimuthBlend = MathUtil.ToWorldAzimuthBlendable(_hasActor ? Actor.facing : Vector3.forward); // TODO optimize this

			Direction8 ordinal  = MathUtil.ToWorldAzimuthOrdinal(azimuthBlend);
			Direction8 cardinal = MathUtil.ToWorldAzimuthCardinal(azimuthBlend);

			if (_hasActor && Actor.hasBrain)
			{
				bool overriden = Actor.activeBrain.OverridesAnim(Actor);
				if (overriden != _wasOverriden)
				{
					// Make sure to reset the state, in case the brain did something weird with it..
					state         = new RenderState(AnimID.Stand);
					_wasOverriden = overriden;
				}

				if (overriden)
				{
					state = Actor.activeBrain.GetAnimOverride(Actor);
				}
			}

			if (modifier != null)
				modifier.ModifyRenderState(this, Actor, ref state);

			Vector3    position = _basePositionRoot + state.offset;
			Vector3    scale    = _baseScale;
			Quaternion roll     = Quaternion.identity;

			if (_hasDropShadow) {
				DropShadow.enabled = !state.dropShadowDisable;
			}

			// VFX
			// ----------------------------------------
			if (_hasVfx) // || _vfxManager.Count != _lastVfxCount)
			{
				_lastVfxCount = _vfxManager.all.Count;

				VFXState vfxstate = _vfxManager.state;

				Color tint     = vfxstate.tint.Alpha(vfxstate.opacity);
				Color fill     = vfxstate.fill;
				float opacity  = vfxstate.opacity;
				float emission = vfxstate.emissionPower;

				bool needAlpha    = opacity < 1 - 0.01;
				bool needInstance = Math.Abs(emission - 1) > Mathf.Epsilon || fill.a > Mathf.Epsilon;

				RenderFlags flags = RenderFlags.None;

				if (needInstance) flags |= RenderFlags.Instance;
				if (needAlpha) flags    |= RenderFlags.Alpha;

				position += vfxstate.offset;

				for (var i = 0; i < _spriteRenderers.Length; i++)
				{
					SpriteRenderer sr = _spriteRenderers[i];

					sr.color = tint;

					// Transparency requires transparent render queue, but we may want to stick with
					// geometry queue for most of the time until we actually need transparency
					// Why? transparency doesn't play nice with grab passes (reflections, water distortion, etc.)
					// It will still look wrong in certain overlapping scenarios, but at least we have
					// a bit of wiggle room to accomodate this limitation.

					if (flags != _lastFlags)
					{
						sr.material = needAlpha && _hasTransparencyMat
							? TransparencyMaterial
							: _baseMaterial;
					}

					if (needInstance)
					{
						// These two functions cause the material to instancify
						sr.ColorFill(fill);
						sr.EmissionPower(emission);
					}

					if (_hasDropShadow)
						DropShadow.Fade = vfxstate.opacity;

					state.animName = _vfxManager.state.animSet ?? state.animName;
				}


				_lastFlags = flags;
			}


			// ANIMATION
			// ----------------------------------------

			if (state.animMode == RenderState.AnimMode.Named)
			{
				// Try playing the animation directly by its exact name.

				if (state.animName != null)
				{
					// Animation without the direction (e.g. sword, idle, jump)
					string anim = state.animName;

					bool success = Play(anim, PlayOptions.Continue, ordinal, cardinal);
					if (!success)
						Play("stand", PlayOptions.Continue, ordinal, cardinal);

					lastAnim = AnimID.None;
				}
				else
				{
					AnimID anim = ApplyAlias(state.animID);

					PlayOptions option = PlayOptions.Continue;
					if (lastAnim == anim || anim == AnimID.Walk && lastAnim == AnimID.Run || anim == AnimID.Run && lastAnim == AnimID.Walk)
						option = PlayOptions.KeepPosition;

					bool success = Play(anim, ordinal, cardinal, option);
					if (!success)
						Play(AnimID.Stand, ordinal, cardinal, option);

					lastAnim = anim;
				}
			}
			else if (state.animMode == RenderState.AnimMode.CustomFrames)
			{
				Animable.Play(state.animCustom);
			}

			if (state.animPercent > -1 + Mathf.Epsilon)
				Animable.player.SetPlayPercent(state.animPercent); // TODO investigate why this is getting called so much

			Animable.playSpeed             = state.animSpeed;
			Animable.player.repeats        = state.animRepeats;
			Animable.player.looping        = state.loops;
			Animable.player.frameModifiers = state.frameModifiers;
			Animable.player.speedCurve     = state.animSpeedCurve;

			if (state.xFlip) {
				scale.x *= -1;
			}

			// POSITION
			// ----------------------------------------
			if (_hasPositionRoot) {
				PositionRoot.localPosition = position;
				PositionRoot.localScale    = scale;
			}


			// TILTING
			// ----------------------------------------
			if (_hasTiltRoot && GameOptions.current.sprite_tilting)
			{
				// float dx = Mathf.Abs(state.pitch - _lastPitch);
				// float dz = Mathf.Abs(state.roll - _lastRoll);

				float dot = Vector3.Dot(Vector3.Cross(Actor.facing.Horizontal(), Actor.Up), GameCams.Live.UnityCam.transform.forward.Horizontal());

				Quaternion qCamera = Quaternion.Euler(0, -GameCams.Live.UnityCam.transform.eulerAngles.y, 0);

				Quaternion qRoll  = Quaternion.AngleAxis(state.roll, qCamera * Root.forward);
				Quaternion qPitch = Quaternion.AngleAxis(state.pitch, qCamera * Root.right);

				TiltRoot.localRotation = qPitch * qRoll;
			}

			_lastPitch   = state.pitch;
			_lastRoll    = state.roll;
			animProgress = Animable.player.elapsedPercent;
			animRepeats  = Animable.player.elapsedRepeats;
			// lastState  = state;
		}

		private AnimID ApplyAlias(AnimID ani)
		{
			switch (ani)
			{
				case AnimID.Idle1 when !_hasIdle1: return AnimID.Stand;
				case AnimID.Walk when !_hasWalk:   return AnimID.Run;
				case AnimID.Run when !_hasRun:     return AnimID.Walk;

				case AnimID.Jump when !_hasJump:
				case AnimID.Rise when !_hasRise:
				case AnimID.Fall when !_hasFall:
					return AnimID.Air;

				case AnimID.Land when !_hasLand:
					if (!Actor.HasVelocity)
						return AnimID.Stand;
					else if (_hasWalk)
						return AnimID.Walk;
					else
						return AnimID.Run;

				default:
					return ani;
			}
		}

		private bool Play(AnimID anim, Direction8 ordinal, Direction8 cardinal, PlayOptions option)
		{
			var ok = false;

			switch (_activeMode)
			{
				case Modes.Cardinal:
					ok = Play(cardinal, anim, option).IsPlaying(); // Cardinal (4 frames)
					break;

				case Modes.Ordinal:
					ok = Play(ordinal, anim, option).IsPlaying();
					break;

				case Modes.OrdinalMirrored:
					ok = Play(ordinal.ForceLeft(), anim, option).IsPlaying();
					if (ok && ordinal.IsRight())
					{
						// TODO flip the sprite horizontally
					}

					break;

				case Modes.CardinalMirrored:
					ok = Play(cardinal.ForceLeft(), anim, option).IsPlaying();
					if (ok && ordinal.IsRight())
					{
						// TODO flip the sprite horizontally
					}

					break;
			}

			if (!ok)
				ok = Animable.Play(anim, Direction8.None, option).IsPlaying();

			return ok;
		}

		private bool Play(string anim, PlayOptions option, Direction8 ordinal, Direction8 cardinal)
		{
			bool ok = false;

			switch (_activeMode)
			{
				case Modes.Cardinal:
					ok = Play(cardinal, anim, option).IsPlaying(); // Cardinal (4 frames)
					break;

				case Modes.Ordinal:
					ok = Play(ordinal, anim, option).IsPlaying();
					break;

				case Modes.OrdinalMirrored:
					ok = Play(ordinal.ForceLeft(), anim, option).IsPlaying(); // Ordinal (5 frames + mirroring on right)
					if (ok && ordinal.IsRight())
					{
						// TODO flip the sprite horizontally
					}

					break;

				case Modes.CardinalMirrored:
					ok = Play(cardinal.ForceLeft(), anim, option).IsPlaying(); // Cardinal (3 frames + mirroring on right)
					if (ok && ordinal.IsRight())
					{
						// TODO flip the sprite horizontally
					}

					break;
			}

			if (!ok)
				ok = Animable.Play(anim, option).IsPlaying();

#if UNITY_EDITOR
			if (!ok && !_printedUnknownAnimations.Contains(anim))
			{
				_printedUnknownAnimations.Add(anim);
				this.LogWarn($"CharacterAnimator: could not find an animation for '{anim}'.");
			}
#endif

			return ok;
		}

		private AnimationPlayResult Play(Direction8 direction, string anim, PlayOptions option)
		{
			string animDir = GetAnimDir(direction, anim);
			return Animable.Play(animDir, option);
		}

		private AnimationPlayResult Play(Direction8 direction, AnimID anim, PlayOptions option) => Animable.Play(anim, direction, option);

		private void OnValidate()
		{
			if (Actor == null)
				Actor = GetComponent<Actor>();
		}


		private bool TestDir(Direction8 dir, AnimID anim = AnimID.Stand)
		{
			return Animable.indexing.GetAnimation(anim, dir, out var _);
		}

		private static string GetAnimDir(Direction8 direction, string animation)
		{
			(string animName, Direction8 dir) pair = (animation, direction);

			if (!_cachedAnimationNames.TryGetValue(pair, out string dirstate))
			{
				// Cache the combination of name and direction.
				_cachedAnimationNames[pair] = dirstate = $"{animation}_{DirUtil.ToString(direction)}";
			}

			return dirstate;
		}

		public enum Modes
		{
			/// <summary>
			/// Automatically figure out from the available animation columns.
			/// </summary>
			Auto,

			/// <summary>
			/// Up, down, left and right.
			/// </summary>
			Cardinal,

			/// <summary>
			/// Diagonals + orthogonals
			/// </summary>
			Ordinal,

			/// <summary>
			/// All left and vertical directions, right is mirrored from left.
			/// </summary>
			CardinalMirrored,

			/// <summary>
			/// Up, left and down, right is mirrored from left.
			/// </summary>
			OrdinalMirrored,

			None,
		}

		[Flags]
		public enum RenderFlags
		{
			None     = 0,
			Instance = 1 << 0,
			Alpha    = 1 << 1
		}
	}
}