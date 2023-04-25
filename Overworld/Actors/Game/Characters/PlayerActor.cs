using System;
using System.Collections.Generic;
using Anjin.Actors.States;
using Anjin.Core.Flags;
using Anjin.Nanokin;
using Anjin.Nanokin.Map;
using Anjin.Nanokin.Park;
using Anjin.Util;
using Anjin.Utils;
using API.Spritesheet.Indexing.Runtime;
using Assets.Scripts.Utils;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Triggers;
using DG.Tweening;
using Drawing;
using KinematicCharacterController;
using Overworld.Terrains;
using Pathfinding.Poly2Tri;
using SaveFiles;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityUtilities;
using Util;
using Util.Components.Timers;
using Util.Odin.Attributes;
using Util.RenderingElements.Trails;

namespace Anjin.Actors
{
	/// <summary>
	/// The player's actor.
	/// Supports a rich ecosystem of states and highly sophisticated movement!
	/// (can be used by AI as well, or PlaybackBrain)
	/// </summary>
	public class PlayerActor : ActorKCC
	{
		public new static Action<PlayerActor> OnSpawn;
		public new static Action<PlayerActor> OnDespawn;
		// CONSTANTS
		// ----------------------------------------

		public const string GFLAG_POGO  = "movetool_pogo";
		public const string GFLAG_GLIDE = "movetool_glide";
		public const string GFLAG_SLASH = "movetool_sword";

		public const int STATE_WALK         = 0;
		public const int STATE_JUMPTAKEOFF  = 1;
		public const int STATE_JUMP         = 2;
		public const int STATE_FALL         = 3;
		public const int STATE_SLIDE        = 4;
		public const int STATE_SLOPE        = 5;
		public const int STATE_SLIDE_HOP    = 6;
		public const int STATE_SLIDE_EXIT   = 7;
		public const int STATE_DIVEDASH     = 8;
		public const int STATE_DIVEDOWN     = 9;
		public const int STATE_GLIDE        = 10;
		public const int STATE_POGO         = 11;
		public const int STATE_DIVEBUM      = 12;
		public const int STATE_SWIM         = 13;
		public const int STATE_SLASH        = 14;
		public const int STATE_WATER_JET    = 15;
		public const int STATE_LAUNCHED     = 16;
		public const int STATE_DOUBLE_JUMP  = 17;
		public const int STATE_WALL_SLIDE	= 18;
		public const int STATE_WALL_JUMP	= 19;
		public const int STATE_WALL_TAKEOFF = 20;
		public const int STATE_WALL_BONK	= 21;
		public const int STATE_SIT			= 22;
		public const int STATE_COASTER_RIDE = 23;
		public const int STATE_EJECT		= 24;

		/// <summary>
		/// Number of frames locked without movement before
		/// triggering the softlock prevention.
		/// </summary>
		private const int SOFTLOCK_DETECTION_FRAMES = 5;

		/// <summary>
		/// Threshold speed to be under before
		/// triggering the softlock preventation.
		/// </summary>
		private const float SOFTLOCK_DETECTION_SPEED = 0.025f;
		private const float INPUT_BUFFERING_TIME_WINDOW = 0.25f;

		// SERIALIZED SETTINGS
		// ----------------------------------------

		[FormerlySerializedAs("_settings"), Title("References")]
		[SerializeField] public PlayerActorSettings Settings;

		public bool EnablePlayerTilting;

		[Title("Timers")]
		[SerializeField] private ManualTimer GlideCooldown = 0.3f;

		[SerializeField] private float CoyoteTime = 0.05f;
		[SerializeField] private AnimationCurve LegSpeedToSlideSpeed = AnimationCurve.Linear(0, 1f, 16, 0.1f);

		// Note(C.L. 7-26-22): I had to make these public. Will need to rename them at some point later.
		[ShowInPlay, TitleGroup("States")] public WalkState          _walk;
		[ShowInPlay, TitleGroup("States")] public GroundTakeoffState _jumpTakeoff;
		[ShowInPlay, TitleGroup("States")] public JumpState          _jump;
		[ShowInPlay, TitleGroup("States")] public FallState          _fall;
		[ShowInPlay, TitleGroup("States")] public SlideState         _slide;
		[ShowInPlay, TitleGroup("States")] public SlideState         _slope;
		[ShowInPlay, TitleGroup("States")] public PenguinHopState    _slideHopIn;
		[ShowInPlay, TitleGroup("States")] public JumpState          _slideHopOut;
		[ShowInPlay, TitleGroup("States")] public AerialDashState    _diveDash;
		[ShowInPlay, TitleGroup("States")] public AerialDashState    _diveDown;
		[ShowInPlay, TitleGroup("States")] public GlideState         _glide;
		[ShowInPlay, TitleGroup("States")] public PogoState          _pogo;
		[ShowInPlay, TitleGroup("States")] public FallState          _diveBum;
		[ShowInPlay, TitleGroup("States")] public SwimState          _swim;
		[ShowInPlay, TitleGroup("States")] public WaterJetState      _waterJet;
		[ShowInPlay, TitleGroup("States")] public LaunchState        _launched;
		[ShowInPlay, TitleGroup("States")] public DoubleJumpState	 _doublejump;
		[ShowInPlay, TitleGroup("States")] public WallSlideState	 _wallSlide;
		[ShowInPlay, TitleGroup("States")] public WallJumpState		 _wallJump;
		[ShowInPlay, TitleGroup("States")] public WallTakeoffState	 _wallTakeoff;
		[ShowInPlay, TitleGroup("States")] public BonkState			 _wallBonk;
		[ShowInPlay, TitleGroup("States")] public SitState			 _sit;
		[ShowInPlay, TitleGroup("States")] public CoasterRideState   _coasterRide;
		[ShowInPlay, TitleGroup("States")] public EjectState		_eject;

		[SerializeField, ShowInInspector]
		private SlashState _slash;

		[Title("SFX")]
		public AudioDef SFX_Slash;

		[Title("FX")]
		[SerializeField] private Trail SpeedTrail;
		[SerializeField] private ParticlePrefab FX_Jump;
		[SerializeField] private ParticlePrefab FX_Land;
		[SerializeField] private ParticlePrefab FX_Dash;
		[SerializeField] private ParticlePrefab FX_Dive;
		[SerializeField] private ParticlePrefab FX_CancelHop;
		[SerializeField] private ParticlePrefab FX_SlideHop;
		[SerializeField] private ParticleRef    FX_Walking;
		[SerializeField] private ParticleRef    FX_Sliding;
		[SerializeField] private ParticleRef    FX_Gliding;

		[SerializeField] private ParticleRef    FX_SwimmingIdle;
		[SerializeField] private ParticleRef    FX_SwimmingMove;
		[SerializeField] private ParticleRef    FX_SwimmingVertical;

		[SerializeField] private ParticlePrefab FX_SwimEntryTiny;
		[SerializeField] private ParticlePrefab FX_SwimEntryLight;
		[SerializeField] private ParticlePrefab FX_SwimEntryHeavy;

		[SerializeField] private ParticlePrefab FX_WallSlide;
		[SerializeField] private ParticlePrefab FX_WallJump;
		[SerializeField] private ParticlePrefab FX_Bonk;
		[SerializeField] private ParticlePrefab FX_SwordSweepPrefab;
		[SerializeField] private ParticlePrefab FX_SwordHitPrefab;


		// RUNTIME
		// ----------------------------------------

		// Logic
		private StateKCC _previousAirState;
		private Vector3  _jumpEntryVelocity; // Velocity buffered for takeoff
		private bool     _waterJetLock;
		private Vector3	 _knockback;

		private FixedArray<Checkpoint>			_checkpoints;
		private FixedArray<ResetCheckpoint>		_resetCheckpoint;
		private FixedArray<VoidOutZone>			_voidOutZones;


		// Custom ledge handling
		private Vector3 _ledgeHit;
		private float   _ledgeFacingDot;
		private float   _ledgeDistance;
		private bool    _ledgePush;

		// Polish
		private float         _standElapsed;
		private float         _swimBobbing;
		private float         _pogoPitch;
		private bool          _hasPlayedFallSound;
		private bool          _wasAerial;
		private float         _lastDashParticles;
		private float         _lastSlideParticles;
		private float         _lastDashAnimReset;
		private bool          _speedTrailEnabled;
		private TrailSettings _speedTrailSettings;
		private float         _speedTrailCount;
		private float         _speedTrailSpacing;
		private float         _landAnimDuration;
		private ValTimer      _swimEntryCooldown;

		[NonSerialized]
		public ValTimer       SwimExitCooldown;

		// Leeway
		private BufferedInput _bufferedJump;
		private BufferedInput _bufferedDive;
		private BufferedInput _bufferedSlash;
		private bool          _wasGroundedLastState;
		private float _wallSlideReleaseTimer;
		private bool _wallSlideReleased;

		// Flags
		private bool _canSlope;
		private bool _gSword;
		private bool _gPogo;
		private bool _gGlide;
		private bool _stunned;

		// Fall Height
		[ShowInPlay] public  float LastFallHeight => _lastFallHeight;
		[ShowInPlay] private float _lastFallHeight;
		[ShowInPlay] private float _fallHeightStartY;
		[ShowInPlay] private float _fallHeightHighestY;

		// Softlock prevention
		private SoftlockState _softlockState;

		private RaycastHit[] _internalCharacterHits = new RaycastHit[KinematicCharacterMotor.MaxHitsBudget];

		private bool IsDiveState(StateKCC state = null)
		{
			int id = currentStateID;
			if (state != null)
				id = state.id;

			return id == STATE_DIVEDASH || id == STATE_DIVEDOWN;
		}

		private bool IsCandidateForFallHeight(StateKCC state = null)
		{
			int id = currentStateID;
			if (state != null)
				id = state.id;

			return id == STATE_JUMP || id == STATE_FALL || id == STATE_DOUBLE_JUMP || id == STATE_DIVEDASH || id == STATE_DIVEDOWN || id == STATE_POGO;
		}

		/// <summary>
		/// Get the next ground state depending on current character state.
		/// </summary>
		public StateKCC SmartGroundMove => _walk; // !_sprint.IsDone ? (StateKCC) _sprint : _walk;

		public override StateKCC GetDefaultState() => _walk;
		public override bool     IsRunning         => velocity.magnitude / Settings.WalkRun.MaxSpeed > Settings.ActorSpeedForRunAnim;
		public override float    MaxJumpHeight     => Settings.Jump.Height;

		public bool IsSwimming => _swim;

		#region Transition paths

		private bool HasPogoTransition
		{
			get
			{
				switch (currentStateID)
				{
					case STATE_WALK:
					case STATE_FALL:
					case STATE_JUMP:
					case STATE_DOUBLE_JUMP:
					case STATE_WALL_JUMP:
					case STATE_SLIDE_HOP:
						return true;
					default:
						return false;
				}
			}
		}

		private bool HasGlideTransition
		{
			get
			{
				switch (currentStateID)
				{
					case STATE_WALK:
					case STATE_FALL:
					case STATE_DOUBLE_JUMP:
					case STATE_WALL_JUMP:
					case STATE_SLIDE_HOP:
					case STATE_DIVEDASH:
					case STATE_SLIDE when _slide.IsAir:
						return true;

					default: return false;
				}
			}
		}

		private bool HasSlashTransition
		{
			get
			{
				switch (currentStateID)
				{
					case STATE_WALK:
					case STATE_FALL:
					case STATE_JUMP:
					case STATE_SLIDE_HOP: // :-)
					case STATE_SLIDE_EXIT:
					case STATE_DOUBLE_JUMP:
					case STATE_WALL_JUMP:
						return true;

					default: return false;
				}
			}
		}

		private bool HasDiveTransition
		{
			get
			{
				switch (currentStateID)
				{
					case STATE_WALK:
					case STATE_FALL:
					case STATE_JUMP:
					case STATE_DOUBLE_JUMP:
					case STATE_WALL_JUMP:
					case STATE_SLIDE_EXIT:
					case STATE_GLIDE:
					case STATE_WATER_JET:
						return true;

					default: return false;
				}
			}
		}

		private bool HasLaunchTransition
		{
			get
			{
				switch (currentStateID)
				{
					/*case STATE_WALK:
					case STATE_FALL:
					case STATE_JUMP:
					case STATE_SLIDE_EXIT:
					case STATE_GLIDE:
					case STATE_WATER_JET:
						return true;*/

					default: return true;
				}
			}
		}

	#endregion

	#region State Rules

		private bool IsJumpInitiable => true;

		private bool IsSlidingAllowed
		{
			get
			{
				if (_slide && IsMotorStable)
				{
					// Return to walk / too slow for sliding
					// ----------------------------------------
					float currentSpeed = velocity.Horizontal().magnitude;
					if (currentSpeed < Settings.MinSlideSpeed)
						return false;
				}

				return true;
			}
		}

	#endregion

		protected override void Awake()
		{
			base.Awake();

			_stunned = false;

			_knockback = Vector3.zero;

			_speedTrailEnabled = SpeedTrail != null
			                     && Settings.SpeedToTrailImageCount != null
			                     && Settings.SpeedToTrailImageSpacing != null;

			_checkpoints       = new FixedArray<Checkpoint>(4);
			_resetCheckpoint = new FixedArray<ResetCheckpoint>(4);
			_voidOutZones      = new FixedArray<VoidOutZone>(16);

			if (_speedTrailEnabled)
			{
				_speedTrailSettings    = Instantiate(SpeedTrail.Settings);
				SpeedTrail.Settings    = _speedTrailSettings;
				SpeedTrail.PlayOnStart = false;
				SpeedTrail.StopInstant();
			}
		}

		protected override void Start()
		{
			base.Start();

			ReadFlags();
			Flags.boolChanged += OnBoolChanged;
		}

		protected virtual void OnEnable()
		{
			OnSpawn?.Invoke(this);
		}

		protected virtual void OnDisable()
		{
			OnDespawn?.Invoke(this);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			Flags.boolChanged -= OnBoolChanged;
		}

		private void OnBoolChanged(BoolFlag obj)
		{
			switch (obj.name)
			{
				case GFLAG_POGO:
				case GFLAG_SLASH:
				case GFLAG_GLIDE:
					ReadFlags();
					break;
			}
		}

		private void ReadFlags()
		{
			_gSword = CheckFlag(GFLAG_SLASH);
			_gPogo  = CheckFlag(GFLAG_POGO);
			_gGlide = CheckFlag(GFLAG_GLIDE);
		}

		private bool CheckFlag(string name) => Settings.EnableSaveFlags && SaveManager.current != null && Flags.GetBool(name);

		protected override void RegisterStates()
		{
			RegisterState(STATE_WALK,         _walk        = new WalkState(Settings.WalkRun));
			RegisterState(STATE_JUMPTAKEOFF,  _jumpTakeoff = new GroundTakeoffState(Settings.JumpTakeoff));
			RegisterState(STATE_SLIDE,        _slide       = new SlideState(Settings.Slide));
			RegisterState(STATE_SLOPE,        _slope       = new SlideState(Settings.Slope));
			RegisterState(STATE_FALL,         _fall        = new FallState(Settings.Fall));
			RegisterState(STATE_SLIDE_EXIT,   _slideHopOut = new SlideHopOutState(Settings.SlideHopOut));
			RegisterState(STATE_SLIDE_HOP,    _slideHopIn  = new PenguinHopState(Settings.SlideHopForward));
			RegisterState(STATE_DIVEDASH,     _diveDash    = new AerialDashState(Settings.DiveDash));
			RegisterState(STATE_DIVEDOWN,     _diveDown    = new AerialDashState(Settings.DiveDown));
			RegisterState(STATE_JUMP,         _jump        = new JumpState(Settings.Jump));
			RegisterState(STATE_GLIDE,        _glide       = new GlideState(Settings.Glide));
			RegisterState(STATE_POGO,         _pogo        = new PogoState(Settings.Pogo));
			RegisterState(STATE_SWIM,         _swim        = new SwimState(Settings.Swim));
			RegisterState(STATE_DIVEBUM,      _diveBum     = new FallState(Settings.BumDive));
			RegisterState(STATE_WATER_JET,    _waterJet    = new WaterJetState(Settings.WaterJet));
			RegisterState(STATE_LAUNCHED,     _launched    = new LaunchState(Settings.Launched));
			RegisterState(STATE_SLASH,        _slash);
			RegisterState(STATE_DOUBLE_JUMP,  _doublejump  = new DoubleJumpState(Settings.DoubleJump));
			RegisterState(STATE_WALL_SLIDE,   _wallSlide   = new WallSlideState(Settings.WallSlide));
			RegisterState(STATE_WALL_JUMP,    _wallJump    = new WallJumpState(Settings.WallJump));
			RegisterState(STATE_WALL_TAKEOFF, _wallTakeoff = new WallTakeoffState(Settings.WallTakeoff));
			RegisterState(STATE_WALL_BONK,    _wallBonk    = new BonkState(Settings.WallBonk));
			RegisterState(STATE_SIT,          _sit         = new SitState());
			RegisterState(STATE_COASTER_RIDE, _coasterRide = new CoasterRideState());
			RegisterState(STATE_EJECT, _eject       = new EjectState());

			_slash.settings = Settings.Slash;

			renderer.Animable.enabled = true;
		}

		protected override void OnAfterStateDecision(ref Vector3 currentVelocity, float deltaTime, bool changedState)
		{
			base.OnAfterStateDecision(ref currentVelocity, deltaTime, changedState);

			Motor.InputMove = inputs.move; //update move input information, used for wall donk bonk shonk

			// Buffered inputs
			// ----------------------------------------
			if (changedState)
			{
				_bufferedJump  = BufferedInput.Zero;
				_bufferedDive  = BufferedInput.Zero;
				_bufferedSlash = BufferedInput.Zero;
			}

			_bufferedJump.Update(inputs.jumpPressed);
			_bufferedDive.Update(inputs.divePressed);
			_bufferedSlash.Update(inputs.swordPressed);

			// SOFTLOCK DETECTION
			// ----------------------------------------

			switch (currentStateID)
			{
				case STATE_DIVEDASH:
				case STATE_DIVEDOWN:
				case STATE_JUMP:
				case STATE_DOUBLE_JUMP:
				case STATE_FALL:
				case STATE_GLIDE:
				case STATE_SLIDE when !inputs.hasMove:
				case STATE_SLOPE when !inputs.hasMove:
					/*if (_softlockState.Update(position, facing))
						ChangeState(SoftlockFailsafe());*/


					break;

				default:
					_softlockState.Reset();
					break;
			}
		}

		protected override StateKCC GetNextState(ref Vector3 currentVelocity, float deltaTime)
		{
			UpdateOverlaps();

			for (int i = 0; i < _voidOutZones; i++) {
				VoidOutZone zone = _voidOutZones[i];
				if(GameController.Live.StateGame != GameController.GameState.OverworldDeath)
					GameController.TriggerOverworldDeath(zone.Config, zone);

				//return null;
			}

			for (int i = 0; i < _checkpoints; i++) {
				GameController.PlayerEnterCheckpoint(_checkpoints[i]);
			}

			if (_resetCheckpoint.Count > 0) {
				GameController.ResetCheckpoint();
			}

			bool anySpeedBoost = false;

			for (int i = 0; i < numPlatformingVolumes; i++) {
				PlatformingVolume vol = platformingVolumes[i];
				if (vol.SpeedBoost) {

					anySpeedBoost     = true;
					SpeedBoostContact = true;

					if(!SpeedBoost) {
						SpeedBoost           = true;
						SpeedBoostMultiplier = vol.SpeedBoostMultiplier.ValueOrDefault(2);
						SpeedBoostDuration.Set(vol.SpeedBoostDuration.ValueOrDefault(1.5f), true);
					}
				}
			}

			if (!anySpeedBoost && SpeedBoost) {
				SpeedBoostContact = false;
				if (SpeedBoostDuration.Tick(deltaTime)) {
					SpeedBoost = false;
				}
			}

			Vector3 groundNormal = Motor.GroundingStatus.GroundNormal;
			float   groundDot    = Vector3.SignedAngle(groundNormal, Motor.CharacterUp, Motor.CharacterUp); // under 0 = going against slope, above 0 = towards slope
			float   groundHem    = SphereDist(transform.position + Motor.CharacterTransformToCapsuleBottomHemi, transform.position, Motor.GroundingStatus.GroundPoint);

			// Flags for possible states
			bool bpogo   = Settings.EnablePogo     && HasPogoTransition  && _gPogo;
			bool bslash  = Settings.EnableSword    && HasSlashTransition && _gSword;
			bool bglide  = Settings.EnableGlide    && _gGlide            && HasGlideTransition && GlideCooldown.IsDone;
			bool bdive   = Settings.EnableDiveDash && HasDiveTransition  && (airMetrics.grounded || airMetrics.airTime > Settings.MinJumpTimeBeforeDive);

			bool blaunch = HasLaunchTransition && (airMetrics.grounded);

			#region Special

			if (_swim)
			{
				// EXIT
				// ----------------------------------------
				if (!hasWater)
					return _fall;

				if (inputs.jumpPressed)
				{
					// Hop out of the water.
					// TODO check if jumping towards a wall, if so jump higher
					return _jump.WithHeight(ref currentVelocity, Settings.SwimJumpHeight);
				}

				return _swim;
			}

			// Water jet entry/exit
			if (_waterJet) {

				if (inputs.jumpPressed) {
					return _jump.WithHeight(ref currentVelocity, Settings.SwimJumpHeight);
				}

				if (!hasWaterJet)
					return _fall;
			} else if (!_waterJetLock && hasWaterJet) {
				_waterJetLock = true;
				return _waterJet;
			}

			if (_waterJetLock && !hasWaterJet) {
				_waterJetLock = false;
			}

			if (hasWater)
			{
				// We can still overlap with water for a few frames when jumping out, so we
				// would oscillate between jumping and swimming for a few frames.
				// This check works as long as we don't live in a weird universe where
				// the density of air is enough to keep a block of water completely in suspension
				// as a block. Or until Kyle decides that Nanoland scientists can break physics
				if (currentVelocity.y <= 0)
				{
					airMetrics.RefreshSecondJump();
					return _swim;
				}
			}

			if (_eject && _eject.IsDone) {
				OnLanding();
				currentVelocity = _eject.ExitVelocity;
				return _fall;
			}

			if (_launched && _launched.IsDone) {

				switch (_launched.ExitBehavior) {
					case LaunchExitBehavior.RetainVelocity:
						currentVelocity = _launched.ExitDirection * _launched.LaunchPad.ExitSpeed.ValueOrDefault(_launched.settings.DefaultExitSpeed);
						break;

					case LaunchExitBehavior.Slide:
						currentVelocity = _launched.ExitDirection * _launched.LaunchPad.ExitSpeed.ValueOrDefault(_launched.settings.DefaultExitSlideSpeed);
						return _slide;
				}

				return _fall;
			}

			if (!_launched && hasLaunchPad && blaunch && launchPad.CanLaunchPlayerIfContacting) {
				_launched.LaunchPad = launchPad;
				return _launched;
			}

			if (_stunned)
			{
				_stunned = false;

				return _wallBonk.WithForce(ref currentVelocity, _knockback, true);
			}

			#endregion

			GlideCooldown.Update(deltaTime);

			// All checks for state transitions are performed in this function.

			// Note: This is executed before UpdateVelocity.
			//       Some states we might need to check after UpdateVelocity instead.
			//       If it turns out to be necessary we can add another state transition
			//       pass after velocity has applied. This seems entirely fine so far though.

			// SLOPES
			// ----------------------------------------

			if (_slope)
			{
				//if (groundDot < Settings.SlopeThreshold) return _walk;
				if (hasSurface && surface.Behavior == Surface.Behaviors.ForceStand) return _walk;
			}
			else if (IsMotorStable)
			{
				//if (_canSlope && groundDot > Settings.SlopeThreshold) return _slope; // && groundHem < 0.75f
				if (hasSurface && surface.Behavior == Surface.Behaviors.ForceSlope) return _slope;
			}

			// SLIDING
			// ----------------------------------------

		#region Sliding

			if (_slide && IsMotorStable)
			{
				bool isSlideStarted  = _slide && elapsedStateTime > Settings.SlideStartLag;
				bool isBufferedInput = _bufferedDive.IsRecent(Settings.SlideHopForceTimingBonusWindow);

				// Hop dash
				// ----------------------------------------
				bool isBufferedDive = isBufferedInput && isSlideStarted;
				bool isManualDive   = _slide && inputs.divePressed && !isBufferedInput;

				if (isManualDive || isBufferedDive)
				{
					float bonus = 0;

					if (isBufferedInput)
					{
						// Gain a bit of speed depending on how long ago we pressed the button (longer = less bonus)
						float t = (Time.time - _bufferedDive.firstPress) / Settings.SlideHopForceTimingBonusWindow; // 0 = furthest, 1 = perfect last moment press
						bonus += Settings.SlideHopForceTimingBonusCurve.Evaluate(t);
					}

					if (isManualDive && !isSlideStarted)
					{
						// Perfect timing, dive pressed during start lag
						bonus += Settings.SlideHopForceTimingBonusCurve.Evaluate(1);
					}

					// Gain a bit of speed depending on how fast we're going
					bonus += Settings.SlideHopForceBySpeed.Evaluate(currentVelocity.ChangeY(0).magnitude);

					currentVelocity += facing * bonus;
					return _slideHopIn.WithHeight(ref currentVelocity, Settings.SlideHopHeightBySpeed.Evaluate(currentVelocity.magnitude));
				}

				// Hop out
				// ----------------------------------------
				if (inputs.jumpPressed || _bufferedJump.IsRecent(Settings.SlideHopForceTimingBonusWindow) && elapsedStateTime > Settings.SlideStartLag)
				{
					// Cancel the slide with a little hop.
					return _slideHopOut.WithDefaultHeight(ref currentVelocity);
				}

			}

			if (IsDiveState() || _slide && IsMotorStable || _slideHopIn)
			{
				var wall = Motor.MovingInto;

				if (wall.collider != null && (Vector3.Dot(wall.normal, inputs.move.normalized) < -Settings.WallBonkEntryThreshold) && (currentVelocity.Horizontal().magnitude >= Settings.WallBonkMinSpeed) && !Motor.CheckForSteps(wall, currentVelocity))
				{
					//_wallSlideReleased = false;
					return _wallBonk.WithForce(ref currentVelocity, inputs.move);
				}
			}

			// Stay in sliding

			if (_slide)
			{
				if (!IsSlidingAllowed)
					return _walk;
				else if(!hasWater)
					return _slide;
			}

			// Recoil from running into a wall
			// ----------------------------------------
			if (_wallBonk)
			{
				if (_wallBonk.BonkOver)
				{
					return SmartGroundMove;
				}
				else if(!hasWater)
				{
					return _wallBonk;
				}
			}

		#endregion

		#region Walking & Jumping

			if (_walk)
			{
				if ((inputs.jumpPressed || _bufferedJump.IsRecent(0.15f)) && IsJumpInitiable)
				{
					// JUMP
					// ----------------------------------------
					_jumpEntryVelocity = currentVelocity;

					if (currentVelocity.magnitude >= Settings.JumpTakeoff.MaxSpeedForTakeoffActivation)
						return _jump.WithDefaultHeight(ref currentVelocity);
					else
						return _jumpTakeoff;
				}
			}

			if (_jumpTakeoff && _jumpTakeoff.ShouldBeginJump)
			{
				// TAKEOFF RELEASE
				// ----------------------------------------
				float range  = Settings.Jump.Height - Settings.JumpTakeoff.MinHeight;
				float height = Settings.JumpTakeoff.MinHeight + Settings.JumpTakeoff.JumpForceCurve.Evaluate(_jumpTakeoff.NormalizedElapsedTakeoff) * range;

				// TODO save velocity magnitude on start of takeoff and use for GetJumpForce

				currentVelocity = _jumpEntryVelocity;
				_jump.actor.Jump(ref currentVelocity, height);
				return _jump;
			}

			if (_wasGroundedLastState && _fall && airMetrics.airTime < 0.12f && inputs.jumpPressed)
			{
				// JUMP LEEWAY
				// ----------------------------------------
				return _jump.WithDefaultHeight(ref currentVelocity);
			}

			if (airMetrics.airborn && airMetrics.airTime >= 0.12f && inputs.doubleJumpPressed && !airMetrics.hasJumpedAgain && !_wallSlide && !_wallJump && !_wallTakeoff)
			{
				airMetrics.BeforeSecondJump();
				return _doublejump.WithHeight(ref currentVelocity, Settings.DoubleJump.Height);
			}

		#endregion

			// DIVE DASH
			// ----------------------------------------

			if ((inputs.divePressed || _bufferedDive.IsRecent()) && bdive) {
				return EvaluateDiveDash();
			}

			// SECRET TECH: Auto-slide on steep ground when dive is held. You can keep it held after a dive dash and to resume sliding in ways and places you normally couldn't. Fun toy for speedrunners
			if (_walk && inputs.diveHeld && IsSlidingAllowed && groundDot > 2.5f)
				return _slide;

		#region Glide

			if (_glide)
			{
				// if (!_characterInputs.GlideDown)
				// 	// Stop gliding when the button is released.
				// 	return _fallState;

				if (inputs.jumpPressed)
				{
					return _fall;
				}

				if (inputs.divePressed)
				{
					return EvaluateDiveDash();
				}
			}
			else if (inputs.glidePressed && bglide)
			{
				// Begin gliding
				return _glide;
			}

		#endregion

		#region Slash

			if (_slash)
			{
				// Exit before slash has ended
				// ----------------------------------------
				if (elapsedStateTime > Settings.SwordDurationBeforeJumpAllowed)
				{
					if (_slash.IsGround)
					{
						if (inputs.jumpPressed) return _jump.WithDefaultHeight(ref currentVelocity);
						if (_previousAirState == _slideHopIn) return _slide; // Go back into sliding
					}

					if (_slash.IsAir)
					{
						if (inputs.divePressed) return _diveDown;
						if (inputs.glidePressed && bglide) return _glide;
						if (inputs.doubleJumpPressed && !airMetrics.hasJumpedAgain)
						{
							airMetrics.BeforeSecondJump();
							return _doublejump.WithHeight(ref currentVelocity, Settings.DoubleJump.Height);
						}
					}
				}

				// Auto-end at end of the animation
				// ----------------------------------------
				SpritePlayer player = renderer.Animable.player;
				if (renderer.lastAnim == AnimID.Sword && player.elapsedRepeats >= 1)
				{
					// Active until the animation has played
					if (_previousAirState != null || airMetrics.airborn)
					{
						StateKCC nextState = _previousAirState;
						if (nextState == _fall || nextState == _jump || nextState == _doublejump || nextState == _slideHopIn || nextState == _slideHopOut)
							nextState = null;

						StateKCC ret = nextState ?? _fall;
						_previousAirState = null;
						return ret;
					}
					else
					{
						if (_bufferedJump.IsRecent(0.3f))
							return _jump.WithDefaultHeight(ref currentVelocity);

						return SmartGroundMove;
					}
				}

				return _slash;
			}
			else if ((inputs.swordPressed || _bufferedSlash.IsRecent(0.15f)) && bslash)
			{
				// Start slash
				// ----------------------------------------
				if (airMetrics.airborn)
				{
					_previousAirState = currentState;
				}
				airMetrics.BeforeSecondJump(); //disallow jumping after slash. too fucking strong

				if (_wallJump)
				{
					if (_wallJump.CanAct)
					{
						return _slash;
					}
				}
				else
				{
					return _slash;
				}
			}

		#endregion

		#region Pogo

			if (_pogo)
			{
				if (_pogo.GroundProgress >= 0.5f && inputs.pogoPressed)
				{
					// Stopping pogo
					if (airMetrics.airborn)
						return _slideHopOut.WithDefaultHeight(ref currentVelocity);
					else
						return _walk;
				}

				return _pogo;
			}
			else if (inputs.pogoPressed && bpogo)
			{
				_pogoPitch = 0;
				return _pogo;
			}

		#endregion

		if (_wallSlide)
		{
			if (IsMotorStable)
			{
				return SmartGroundMove;
			}

			if (inputs.jumpPressed || _bufferedJump.IsRecent(0.15f) && IsJumpInitiable)
			{
				if (Motor.MovingInto.normal != Vector3.zero)
				{
					_wallTakeoff.FromWallNormal = Motor.MovingInto.normal;
				}
				else
				{
					Vector3 facingNormal = facing.normalized;

					Motor.CharacterCollisionsRaycast(position + Motor.Radius * facing,
						facing,
						1.1f,
						out RaycastHit hit,
					_internalCharacterHits);

					_wallTakeoff.FromWallNormal = hit.normal;
				}

				_wallTakeoff.UpdateDirection();
				return _wallTakeoff;
			}

			if (_wallSlideReleased)
			{
				_wallSlideReleaseTimer += deltaTime;
			}

			else
			{
				_wallSlideReleaseTimer = 0;
			}

			if (_wallSlideReleaseTimer > Settings.WallSlide.ClingSeconds)
			{
				return _fall;
			}
		}

		//walling
		if (_wallTakeoff && _wallTakeoff.ShouldBeginJump)
		{
			// TAKEOFF RELEASE
			// ----------------------------------------
			float wallJumpHeight = Settings.WallJump.Height;
			float outward = Settings.WallJump.InitialOutwardSpeed;
			float leftward = Settings.WallJump.InitialOutwardSpeed;
			Vector3 jumpDir = Vector3.zero;

			if (Mathf.Abs(Vector3.Dot(inputs.move, _wallTakeoff.FromWallNormal)) < _wallTakeoff.settings.SideLeniency)
			{
				outward *= Settings.WallTakeoff.SideOutMultipler;
				wallJumpHeight *= Settings.WallTakeoff.SideHeightMultiplier;
				leftward *= Settings.WallTakeoff.SideThrustMultiplier;
				jumpDir = inputs.move;
			}
			else if (_wallTakeoff.SwordUse)
			{
				outward *= Settings.WallTakeoff.UpThrustMultiplier;
				wallJumpHeight *= Settings.WallTakeoff.UpHeightMultiplier;
				leftward *= 0;
				jumpDir = _wallTakeoff.FromWallNormal;
			}
			else if (Vector3.Dot(inputs.move, _wallTakeoff.FromWallNormal) > -0.1f)
			{
				outward *= Settings.WallTakeoff.BackThrustMultiplier;
				wallJumpHeight *= Settings.WallTakeoff.BackHeightMultplier;
				leftward *= 0;
				jumpDir = _wallTakeoff.FromWallNormal;
			}
			else
			{
				outward *= Settings.WallTakeoff.ForwardThrustMultiplier;
				wallJumpHeight *= Settings.WallTakeoff.ForwardHeightMultiplier;
				leftward *= 0;
				jumpDir = _wallTakeoff.FromWallNormal;
			}

			airMetrics.airTime = 0;

			//_wallJump.actor.Jump(ref currentVelocity,
			//	wallJumpHeight,
			//	outward,
			//	Motor.MovingInto.normal);

			_wallJump.actor.Jump(ref currentVelocity,
				wallJumpHeight,
				outward,
				_wallTakeoff.FromWallNormal);

			_wallJump.actor.Jump(ref currentVelocity,
				0,
				leftward,
				inputs.move, heightCalc: false);

			return _wallJump.WithDirection(jumpDir);
		}

		#region FallingIntoWall
		if (IsAirState && airMetrics.airTime > Settings.CanWallSlideAfter && !_wallTakeoff)
		{
			var wall = Motor.MovingInto;
			if (wall.collider != null && Vector3.Dot(wall.normal, inputs.move.normalized) < -Settings.WallSlideEntryThreshold)
			{
				_wallSlideReleased = false;
				return _wallSlide;
			}
		}
		#endregion

		#region WallJump
		if (_wallJump)
		{
			var wall = Motor.MovingInto;
			if (wall.collider != null && Vector3.Dot(wall.normal, inputs.move.normalized) < -Settings.WallSlideEntryThreshold
			                          && airMetrics.airTime > Settings.CanWallSlideAfter && !inputs.swordHeld)
			{
				_wallSlideReleased = false;
				return _wallSlide;
			}

			if (!IsMotorStable)
			{
				return _wallJump;
			}
		}
		#endregion

		#region WallSlide
		if (_wallSlide)
		{
			var wall = Motor.MovingInto;
			if (wall.collider == null && !_wallSlideReleased)
			{
				_wallSlideReleased = true;
				_wallSlideReleaseTimer = 0;
			}
		}
		#endregion



		// Automatic falling
			// ----------------------------------------
			if (IsGroundState && !IsMotorStable && airMetrics.airTime >= CoyoteTime && !_wallTakeoff)
				return _fall;

			// Automatic ground
			// ----------------------------------------
			if (IsAirState && IsMotorStable)
			{
				if (IsDiveState())
				{
					currentVelocity = currentVelocity.normalized * (currentVelocity.magnitude + Settings.SlideBonusFromDive);
					return _slide;
				}
				else if (_slideHopIn)
				{
					return _slide;
				}
				else
				{
					return SmartGroundMove;
				}
			}
			return null;
		}


		private StateKCC SoftlockFailsafe()
		{
			Debug.Log("Softlock detected, ejecting upwards");
			Jump(3.5f);
			return _jump;
		}


		private StateKCC EvaluateDiveDash()
		{
			if (currentStateID != STATE_WATER_JET && velocity.y < 0)
				return _diveDown;

			if (_waterJet && _waterJet.IsOnHead) {
				return _diveDash.WithAddedJumpForce(_waterJet.settings.JumpForceWhenDivingOffHead);
			}

			return _diveDash;
		}

		public override void UpdateOverlaps()
		{
			_voidOutZones.Reset();
			_checkpoints.Reset();
			_resetCheckpoint.Reset();
			base.UpdateOverlaps();
		}

		public override void OnPlatformingPhysicsOverlap(Collider collider)
		{
			if (!_voidOutZones.Full && collider.TryGetComponent(out VoidOutZone voidZone)) {
				_voidOutZones.Add(voidZone);
			}

			if (!_checkpoints.Full && collider.TryGetComponent(out Checkpoint checkpoint)) {
				_checkpoints.Add(checkpoint);
			}

			if (!_resetCheckpoint.Full && collider.TryGetComponent(out ResetCheckpoint cancel)) {
				_resetCheckpoint.Add(cancel);
			}
		}

		public void OnStun(Vector3 direction)
		{
			_stunned = true;
			_knockback = direction;
		}

		public override void OnBounce(BounceInfo info)
		{
			base.OnBounce(info);
			ChangeState(_jump);
		}

		protected override void OnLanding()
		{
			//Debug.Log("Landed");
			base.OnLanding();
			_landAnimDuration = CalcLandAnimDuration();
		}

		public void TrySit(Transform root)
		{
			if (root == null) return;
			ChangeState(_sit.WithSitRoot(root));
		}

		protected override void UpdateFX()
		{
			center = position + Vector3.up * height / 2;
			float speed  = velocity.magnitude;
			float hspeed = velocity.Horizontal().magnitude;

			bool isMovingVertical = velocity.y.Abs() > 1.5f;
			bool isMoving         = speed > 1.5f;

			if (currentStateID != STATE_SWIM) {
				_swimEntryCooldown.Tick();
			}

			if (currentStateID == STATE_SWIM)  {
				SwimExitCooldown.Tick();
			}

			if (stateChanged)
			{
				// Particle States
				// ----------------------------------------

				FX_Sliding.SetPlaying(_slope);
				FX_Gliding.SetPlaying(_glide);
				FX_Walking.SetPlaying(false);
				FX_SwimmingVertical?.SetPlaying(false);
				FX_SwimmingMove?.SetPlaying(false);
				FX_SwimmingIdle?.SetPlaying(false);

				_standElapsed = 0;
				_pogoPitch    = 0;
				_swimBobbing  = 0;

				// Burst Particles
				// ----------------------------------------
				switch (currentStateID)
				{
					case STATE_DIVEDASH:
					case STATE_DIVEDOWN:
						FX_Dive.Instantiate(transform, parent: false);
						break;

					case STATE_SLIDE_EXIT:
						FX_CancelHop.Instantiate(transform, parent: false);
						break;

					case STATE_SLIDE_HOP:
						FX_SlideHop.Instantiate(transform, parent: false);
						break;

					case STATE_WALK:
						if (_wasAerial && airMetrics.grounded)
						{
							FX_Land.Instantiate(transform, parent: false);
						}

						break;

					case STATE_SLASH:
						FX_SwordSweepPrefab.Instantiate(transform, Settings.SwordParticlesAngleLimit, velocity);
						break;

					case STATE_SLOPE:
						break;

					case STATE_GLIDE:
						break;

					case STATE_SWIM:
						break;

					case STATE_WALL_JUMP:
						FX_WallJump.Instantiate(transform, parent: false);
						break;

					case STATE_WALL_BONK:
						FX_Bonk.Instantiate(transform, parent: false);
						break;
				}

				/*switch (previousStateID) {
					case STATE_SWIM:
						break;
				}*/
			}

			// Effects
			// ----------------------------------------

			switch (currentStateID)
			{
				case STATE_WALK:
					// FX_Walking.SetEmission(speed / (Settings.WalkRun.MaxSpeed * .75f));
					FX_Walking.SetPlaying(speed > 0.1f);

					// Update step sound
					_walk.StepSound = null;
					if (terrain)
					{
						_walk.StepSound = terrain.StepSound;
					}

					// Fancy idle
					// ----------------------------------------
					if (_walk && speed < 1) { }

					// Dash particles
					// ----------------------------------------
					if (_walk.DashTime > _lastDashParticles)
					{
						FX_Dash.Instantiate(null, transform.position, Quaternion.LookRotation(-inputs.move));
						_lastDashParticles = _walk.DashTime;
					}


					break;

				case STATE_SLIDE:
					FX_Walking.SetEmission(1);
					FX_Walking.SetPlaying(_slide && _slide.IsGround);
					break;

				case STATE_WALL_BONK:
					FX_Walking.SetEmission(1.5f);
					FX_Walking.SetPlaying(_wallBonk && _wallBonk.IsGround);
					break;

				case STATE_POGO:
					if (airMetrics.grounded)
					{
						_pogoPitch *= 0.2f;
					}
					else
					{
						float targetPitch = Settings.PogoPitchVertical.Evaluate(velocity.y) * Settings.PogoPitchHorizontal.Evaluate(velocity.ChangeY(0).magnitude);
						_pogoPitch = MathUtil.LerpDamp(_pogoPitch, targetPitch, Settings.PogoPitchLerping);
					}

					break;


				case STATE_SLASH:
					_slash.animationPercent = renderer.Animable.player.elapsedPercent;

					if (_slash.newHits.Count > 0)
					{
						Centroid centroid = new Centroid();
						centroid.add(transform.position);

						foreach (Collider hit in _slash.newHits)
						{
							if (hit == null) continue;
							FX_SwordHitPrefab.Instantiate(hit.transform, hit.transform.position);

							GameObject freezeFrameObj = new GameObject("Freeze Frame Volume");
							freezeFrameObj.transform.position = hit.transform.position;

							SphereCollider sphereCollider = freezeFrameObj.AddComponent<SphereCollider>();
							sphereCollider.isTrigger = true;
							sphereCollider.radius    = 100f;

							FreezeFrameVolume freezeFrameVolume = freezeFrameObj.AddComponent<FreezeFrameVolume>();
							freezeFrameVolume.DurationFrames = 4;

							centroid.add(hit.transform.position);
						}

						_slash.newHits.Clear();
					}

					break;

				case STATE_SWIM:
					// Particles
					FX_SwimmingVertical?.SetPlaying(_swim && isMovingVertical);
					FX_SwimmingMove?.SetPlaying(_swim && !isMovingVertical && isMoving);
					FX_SwimmingIdle?.SetPlaying(_swim && !isMoving);

					// Bobbing
					float target = Mathf.Sin(elapsedStateTime * Settings.SwimBobbingSpeed) * Settings.SwimBobbingAmplitude;
					_swimBobbing = MathUtil.EasedLerp(_swimBobbing, target, Settings.SwimBobbingLerpSpeed);
					break;

				case STATE_WATER_JET:
					// Particles
					/*FX_SwimmingVertical?.SetPlaying(_swim && isMovingVertical);
					FX_SwimmingMove?.SetPlaying(_swim     && !isMovingVertical && isMoving);*/
					FX_SwimmingVertical?.SetPlaying(_waterJet);
					break;

				case STATE_WALL_SLIDE:
					if (Time.time > _lastSlideParticles + Settings.WallSlide.ParticleIntervalSeconds )
					{
						var offset = Motor.CharacterForward * Motor.Capsule.radius;
						FX_WallSlide.Instantiate(null, transform.position + offset , Quaternion.LookRotation(-inputs.move));
						_lastSlideParticles = Time.time;
					}

					break;
			}

			if (Settings.EnableSpeedTrail && _speedTrailEnabled)
			{
				if (hspeed < 1)
				{
					SpeedTrail.StopProgressive();
				}
				else if (_slide || IsGroundState || SpeedBoost)
				{
					SpeedTrail.Play();

					float t = Settings.SpeedToTrailTransitionRange.InverseLerp(deltaVelocity.Horizontal().magnitude);

					float imgcount   = Settings.SpeedToTrailImageCount.Evaluate(hspeed);
					float imgspacing = Settings.SpeedToTrailImageSpacing.Evaluate(hspeed);

					float imgcountDamping = Mathf.Lerp(Settings.SpeedToTrailImageCountDamping, Settings.SpeedToTrailImageCountDampingDecelerating, t);
					float imgcountSpacing = Mathf.Lerp(Settings.SpeedToTrailImageSpacingDamping, Settings.SpeedToTrailImageCountDampingDecelerating, t);

					_speedTrailCount   = _speedTrailCount.LerpDamp(imgcount, imgcountDamping);
					_speedTrailSpacing = _speedTrailSpacing.LerpDamp(imgspacing, imgcountSpacing);

					_speedTrailSettings.ImageCount   = (int)_speedTrailCount;
					_speedTrailSettings.ImageSpacing = (int)_speedTrailSpacing;
				}
			}

			_wasAerial = airMetrics.airborn;
		}


		public override void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport stability)
		{
			base.ProcessHitStabilityReport(hitCollider, hitNormal, hitPoint, atCharacterPosition, atCharacterRotation, ref stability);

			// Reset
			_ledgeDistance  = 1;
			_ledgeFacingDot = 0;
			_ledgePush      = false;

			// Slope
			_canSlope = !stability.ValidStepDetected &&
			            !stability.LedgeDetected &&
			            !stability.IsOnEmptySideOfLedge;

			Ray         ray      = new Ray(atCharacterPosition, -Up);
			const float RAY_DIST = INPUT_BUFFERING_TIME_WINDOW;

			Draw.editor.Ray(ray, RAY_DIST, Color.red);

			bool isSlope = Physics.Raycast(ray, out RaycastHit rh, RAY_DIST, Layers.Walkable.mask);
			if (isSlope)
			{
				// Only allow slope sliding above a threshold
				// ----------------------------------------
				if (_canSlope)
				{
					float slopeAngle = Vector3.SignedAngle(rh.normal, Motor.CharacterUp, Motor.CharacterUp);
					_canSlope = slopeAngle > 10;
				}
			}
			else
			{
				// Can't stand too far from ledge
				// ----------------------------------------
				Vector3 groundNormal = Motor.GroundingStatus.GroundNormal;
				float   groundDot    = Vector3.SignedAngle(groundNormal, Motor.CharacterUp, Motor.CharacterUp); // under 0 = going against slope, above 0 = towards slope

				_ledgeHit       = hitPoint;
				_ledgeFacingDot = Vector3.Dot(hitPoint - transform.position, facing);
				_ledgeDistance  = Vector3.Distance((atCharacterPosition + Motor.CharacterTransformToCapsuleBottomHemi).Horizontal(), transform.position.Horizontal()) / Motor.Capsule.radius;

				if (_ledgeDistance > Motor.MaxStableDistanceFromLedge)
				{
					if (_ledgeFacingDot < 0)
					{
						stability.IsStable = false;
					}
					// else
					// TODO If we're facing away from ledge we should push in instead (better staircase handling)
					// _ledgePush = true;
				}
			}
		}

		protected override void OnStateTransition(StateKCC prev, ref StateKCC next)
		{
			if (prev == null || next == null) return;

			_softlockState.Reset(); // So we can't accidentally trigger the lock prevention from a previous state
			_wasGroundedLastState = prev.IsGround && next.IsAir;

			// COOLDOWNS
			// ----------------------------------------
			if (prev == _glide)
			{
				GlideCooldown.Restart();
			}

			if (next == _swim || next.IsGround || airMetrics.grounded)
			{
				GlideCooldown.Finish();
			}


			// Particles
			// ----------------------------------------
			//if ((oldstate.IsGround || oldstate == _fall || oldstate == _jump) && (newstate == _jump || newstate == _doublejump))
			if (prev != _swim && (next == _jump || next == _doublejump))
			{
				GameSFX.Play(Settings.SFX_JumpLaunch, transform.position);
				FX_Jump.Instantiate(transform);
			}

			if (next == _doublejump) {
				GameSFX.Play(Settings.SFX_JumpLaunch, transform.position);
				FX_Jump.Instantiate(transform, transform.position + Vector3.up * 0.5f);
			}


			// Sound Effects
			// ----------------------------------------
			if (next == _glide) GameSFX.Play(Settings.SFX_GlideStart, transform.position);
			if (prev == _glide) GameSFX.Play(Settings.SFX_GlideEnd, transform.position);

			if (next == _jumpTakeoff) GameSFX.Play(Settings.SFX_JumpPrepare, transform.position);

			if (next == _diveDash || next == _diveDown) GameSFX.Play(Settings.SFX_DashDive, transform.position);
			if (next == _slide) GameSFX.Play(Settings.SFX_SlideStart, transform.position);
			if (next == _slideHopOut) GameSFX.Play(Settings.SFX_SlideHopOut, transform.position);
			if (next == _slideHopIn) GameSFX.Play(Settings.SFX_PenguinHop, transform.position);

			if (prev.IsAir && next.IsGround) GameSFX.Play(Settings.SFX_Land, transform.position);
			if (prev == _slide && next == _walk) GameSFX.Play(Settings.SFX_SlideToWalk, transform.position);

			if (!_hasPlayedFallSound && airMetrics.yDelta < 0)
			{
				GameSFX.Play(Settings.SFX_JumpFallStart, transform.position);
				_hasPlayedFallSound = true;
			}

			if (next == _slash) GameSFX.Play(SFX_Slash, transform);
			if (next == _wallBonk && _wallBonk.PlaySound) GameSFX.Play(Settings.SFX_Bonk, transform);
			if (next == _wallJump) GameSFX.Play(Settings.SFX_WallJump, transform);

			if (next.IsGround)
			{
				_hasPlayedFallSound = false;
			}

			if (next == _swim) {

				if (_swimEntryCooldown.done) {

					if (LastFallHeight > Settings.SwimEntryBigSplashHeight) {
						FX_SwimEntryHeavy.Instantiate(transform);
						GameSFX.Play(Settings.SFX_WaterEntryHeavy, transform.position);
					} else if (prev == _slide || prev == _diveDash || prev == _diveDown || LastFallHeight > Settings.SwimEntryLightSplashHeight) {
						FX_SwimEntryLight.Instantiate(transform);
						GameSFX.Play(Settings.SFX_WaterEntryLight, transform.position);
					} else {
						FX_SwimEntryTiny.Instantiate(transform);
						GameSFX.Play(Settings.SFX_WaterEntryTiny, transform.position);
					}
				}

				_swimEntryCooldown.Set(Settings.SwimEntryCooldown);

			} else if (prev == _swim && next == _jump) {
				if (SwimExitCooldown.done)
					FX_SwimEntryTiny.Instantiate(transform);
				_swimEntryCooldown.Set(Settings.SwimExitCooldown);

				GameSFX.Play(Settings.SFX_WaterExitJump, transform.position);
			}

			if (IsCandidateForFallHeight(next) && !IsCandidateForFallHeight(prev)) {
				_fallHeightStartY   = transform.position.y;
				_fallHeightHighestY = _fallHeightStartY;
				_lastFallHeight     = 0;
			}
		}

		public override void AfterCharacterUpdate(float dt)
		{
			base.AfterCharacterUpdate(dt);

			if (IsCandidateForFallHeight(currentState)) {

				if (transform.position.y > _fallHeightHighestY) {
					_fallHeightHighestY = transform.position.y;
				}

				_lastFallHeight = Mathf.Abs(transform.position.y - _fallHeightHighestY);
			}

			if (_ledgePush && slopeDir.magnitude > 0)
			{
				Vector3 p = transform.position + -slopeDir.Horizontal() * _ledgeDistance * Motor.Capsule.radius * _ledgeFacingDot;
				float   h = _ledgeHit.y - p.y;

				// Draw.ingame.CrossXY(p, 0.15f, Color.red);
				// Draw.editor.CrossXY(p, 0.15f, Color.red);

				// Debug.Log($"{_ledgeFacingDot}");

				if (Physics.Raycast(
					    new Ray(p + Vector3.up * h, -Motor.CharacterUp),
					    out RaycastHit rh,
					    h + 0.01f,
					    CollisionMask))
				{
					Motor.SetPosition(rh.point);
				}
			}

			// if (Motor.GroundingStatus.IsStableOnGround)
			// {
			// 	float groundHem = SphereDist(transform.position + Motor.CharacterTransformToCapsuleBottomHemi, transform.position, Motor.GroundingStatus.GroundPoint);
			// 	if (groundHem > 0.25f)
			// 	{
			// 		Vector3 radDir   = (Motor.GroundingStatus.GroundPoint - transform.position).normalized;
			// 		Vector3 radPoint = transform.position + Motor.CharacterTransformToCapsuleBottomHemi + radDir.Horizontal() * Motor.Capsule.radius;
			//
			// 		// Offset from groundPoint to outer bound of the hemisphere
			// 		Vector3 offset = radPoint - Motor.GroundingStatus.GroundPoint;
			//
			// 		Ray ray = new Ray(transform.position - offset, -Motor.CharacterUp);
			// 		Draw.editor.Ray(ray, Motor.GroundDetectionExtraDistance, Color.cyan);
			// 		int count = Physics.RaycastNonAlloc(ray, _raycastResults, Motor.GroundDetectionExtraDistance, ValidCollisionLayers);
			// 		for (var i = 0; i < count; i++)
			// 		{
			// 			RaycastHit hit = _raycastResults[i];
			// 			if (hit.collider.GetInstanceID() == Motor.GroundingStatus.GroundCollider.GetInstanceID())
			// 			{
			// 				// Teleport down and let KCC eject us
			// 				Motor.SetPosition(hit.point, false);
			// 				break;
			// 			}
			// 		}
			// 	}
			// }
		}

		public override void AddForce(Vector3 force, bool setY = false, bool setXZ = false)
		{
			base.AddForce(force, setY, setXZ);

			if (force.y > Mathf.Epsilon)
			{
				inertia.settings = Settings.Jump.InertialControl;
			}
		}

		public override void UpdateRenderState(ref RenderState state)
		{
			if (stateChanged)
			{
				state = new RenderState(AnimID.Stand);

				switch (currentStateID)
				{
					case STATE_JUMPTAKEOFF:
					{
						state.animSpeed = 1.5f;
						state.animID    = AnimID.Jump;
						break;
					}

					case STATE_DIVEDASH:
					case STATE_DIVEDOWN:
					case STATE_SLIDE_HOP:
					case STATE_SLIDE when airMetrics.airborn:
						state.animID = AnimID.Dive;
						state.offset = new Vector3(0, -0.175f, 0);
						break;

					case STATE_SLIDE when airMetrics.grounded:
					{
						state.animID = AnimID.Glide;
						state.offset = new Vector3(0, -0.175f, 0);
						break;
					}

					case STATE_SLIDE_EXIT:
						state.animID = AnimID.Air;
						break;

					case STATE_DOUBLE_JUMP:
						state.animID      = AnimID.DoubleJump;
						state.animRepeats = 1;
						break;

					case STATE_WALL_SLIDE:
						state.animID = AnimID.WallSlide;
						break;

					case STATE_WALL_JUMP:
						state.animID = AnimID.DoubleJump;
						break;

					case STATE_WALL_TAKEOFF:
						state.animID = AnimID.WallTakeoff;
						break;

					case STATE_WALL_BONK:
						state.animID = AnimID.WallBonk;
						//state.animRepeats = 1;
						break;

					case STATE_GLIDE:
						state.animID = AnimID.Glide;
						break;
				}
			}

			switch (currentStateID)
			{
				case STATE_SLASH:
				{
					state.animID      = AnimID.Sword;
					state.animRepeats = 1;
					break;
				}

				case STATE_WALK:
				{
					float speed = velocity.magnitude;

					if ((renderer.lastAnim == AnimID.Walk || renderer.lastAnim == AnimID.Run) && _wasGroundedLastState)
					{
						// Utmost attention to detail here at Anjin
						// the animation shall not change until the foot steps
						// on the ground
						if (renderer.Animable.player.FrameIndex % (renderer.Animable.player.FrameCount / 2) != 0)
							return;
					}

					if (airMetrics.traveledHeightFalling > 1f && airMetrics.elapsedSinceLanding < _landAnimDuration)
					{
						state.animID = AnimID.Land;
					}
					else if (speed > Settings.ActorSpeedForRunAnim) // || inputs.moveMagnitude > 0.25f) // TODO this increases the feedback, but breaks in cutscenes (since the actor will always be running)
					{
						// float distance = 1 - (Vector3.Dot(FacingDirection, CurrentState.TargetFacingDirection) / 2f + 0.5f);
						// _runRollforce.Gain(distance * 3);
						// _runRollforce.Decay();

						state.animID      = AnimID.Run;
						state.animSpeed   = Settings.RunAnimSpeed.Evaluate(velocity.xz().magnitude);
						state.animPercent = -1;

						if (Mathf.Abs(_walk.DashTime - _lastDashAnimReset) > 0.01f)
						{
							state.animPercent  = 0;
							_lastDashAnimReset = _walk.DashTime;
						}

						state.animSpeed = Settings.DashAnimSpeed.Evaluate(Time.time - _walk.DashTime);
					}
					else if (inputs.hasMove || speed > Settings.ActorSpeedForWalkAnim)
					{
						// if (!inputs.hasMove && lastAnim == AnimID.Run)
						// return Renderer.lastState;

						state.animID    = AnimID.Walk;
						state.animSpeed = Settings.WalkAnimSpeed.Evaluate(velocity.xz().magnitude);

						//Debug.Log(inputs.hasMove);
					}
					else
					{
						state.animID    = AnimID.Stand;
						state.animSpeed = 0.75f;

						this.DoFancyIdle1(ref state,
							ref _standElapsed,
							Settings.StandTimeForFancyIdle, Settings.FancyIdleRepeats);

						//Debug.Log("stand and deliver");
					}

					//Draw.Label2D(transform.position + Vector3.up * 3, $"state.animID: {state.animID.ToString()}\ninputs.hasMove: {inputs.hasMove}\nnormspeed: {normspeed}");

					break;
				}

				case STATE_JUMP:
					break;

				case STATE_DOUBLE_JUMP:
					if (!stateChanged) {
						if (state.animID == AnimID.DoubleJump && renderer.Animable.player.IsDonePlaying) {
							state.animID      = AnimID.Fall;
							state.animRepeats = 0;
						}
					}
					break;

				case STATE_DIVEDASH:
				case STATE_DIVEDOWN:
				case STATE_SLIDE when airMetrics.airborn:
				case STATE_SLIDE_HOP:
				case STATE_SLIDE_EXIT:
					state.pitch  = FloatRange.Remap(velocity.y, Settings.DolphinDiveSpritePitchVelocityRange, Settings.DolphinDiveSpritePitchRange);
					state.animID = AnimID.Dive;
					break;

				case STATE_POGO:
				{
					if (IsAirState)
					{
						if (velocity.y > 0) state.animName = "pogo_rise";
						if (velocity.y < 0) state.animName = "pogo_fall";
					}
					else if (IsGroundState)
					{
						if (_pogo.GroundProgress >= 0.5f)
						{
							state.animName = "pogo_jump";
						}
						else
						{
							state.animName = "pogo_land";
						}
					}

					state.pitch  = _pogoPitch;
					state.offset = Vector3.up * 0.1325f;
					break;
				}

				case STATE_SLOPE:
				{
					state.animID = AnimID.Sit;

					state.offset = new Vector3(0, -0.225f, 0);
					break;
				}

				case STATE_SLIDE when airMetrics.grounded:
				{
					state.animID = AnimID.Glide;
					state.offset = new Vector3(0, -0.175f, 0);
					state.pitch  = 0;
					state.roll   = 0;
					break;
				}

				case STATE_SWIM:
				{
					state.animID    = AnimID.SwimIdle;
					state.animSpeed = 1;
					state.offset    = _swimBobbing * Vector3.down;

					float hspeed = velocity.xz().magnitude;
					if (hspeed > Settings.ActorSpeedForWalkAnim * 2)
					{
						state.animID    = AnimID.SwimMove;
						state.animSpeed = Settings.Swim_HSpeed_To_AnimationSpeed.Evaluate(hspeed);
					}

					break;
				}

				case STATE_GLIDE:
				{
					state.roll = _glide.GlideTilting;
					break;
				}

				case STATE_WATER_JET:
				{
					if (_waterJet.EnteredOnHead) {
						if(_waterJet.IsPlayerMoving)
							state.animID = AnimID.SwimMove;
						else
							state.animID = AnimID.SwimIdle;
					} else {
						// TODO(C.L.): Try high frames of jump animation instead.
						state.animID = AnimID.Glide;
						/*state.animID    = AnimID.Jump;
						state.animSpeed = 0;*/
					}

					state.animSpeed = 1;

					break;
				}

				case STATE_LAUNCHED: {

					if (_launched.Time < 0.5f) {
						state.animID = AnimID.Dive;
					} else {
						state.animID = AnimID.Glide;
					}

					break;
				}

				case STATE_SIT:
				{
					state.animID            = AnimID.Sit;
					state.offset            = new Vector3(0, -0.2f, 0);
					state.dropShadowDisable = true;
					break;
				}

				case STATE_EJECT:
				{
					state.animID = AnimID.DoubleJump;
					break;
				}
			}

			switch (currentStateID)
			{
				case STATE_JUMP:
				case STATE_FALL:
				case STATE_SLIDE_EXIT:
				{
					if (velocity.y < 1.5f)
					{
						state.animID = AnimID.Fall;
					}
					else if (velocity.y > 0)
					{
						state.animID = AnimID.Rise;
					}

					break;
				}
			}

			// Match the angle to the ground's slope
			// ----------------------------------------
			switch (currentStateID)
			{
				case STATE_WALK:
					if (GameOptions.current.sprite_tilting_ground && EnablePlayerTilting)
					{
						if (slopeDir.magnitude > 0.1f)
						{
							Vector3 descent = slopeDir;
							var     ofacing = Vector3.Cross(facing, Motor.CharacterUp);

							Draw.editor.Line(transform.position, transform.position + descent, Color.green);
							Draw.editor.Line(transform.position, transform.position + facing, Color.red);

							float dotAligned = Vector3.Dot(descent.Horizontal().normalized, facing.Horizontal().normalized);
							float dotOrtho   = Vector3.Dot(descent.Horizontal().normalized, ofacing.Horizontal().normalized);

							Vector3 euler = Quaternion.LookRotation(slopeDir).eulerAngles;
							state.pitch = state.pitch.LerpDamp(euler.x * dotAligned, Settings.GroundTiltPitchDamping).Clamp(-Settings.GroundTiltPitchMax, Settings.GroundTiltPitchMax);
							state.roll  = state.roll.LerpDamp(euler.x * dotOrtho, Settings.GroundTiltRollDamping).Clamp(-Settings.GroundTiltPitchMax, Settings.GroundTiltPitchMax);
						}
						else
						{
							state.pitch = state.pitch.LerpDamp(0, Settings.GroundTiltPitchDamping);
							state.roll  = state.roll.LerpDamp(0, Settings.GroundTiltRollDamping);
						}
					}

					break;

				case STATE_JUMP:
				case STATE_FALL:
					state.pitch = state.pitch.LerpDamp(0, Settings.AirTiltPitchDamping);
					state.roll  = state.roll.LerpDamp(0, Settings.AirTiltRollDamping);
					break;
			}
		}

		private float CalcLandAnimDuration() =>
			// Landing anim feels good when letting go of the joystick, nice weight
			Settings.LandAnimDurationByHSpeed.Evaluate(velocity.Horizontal().magnitude * inputs.moveMagnitude);

		public void LeaveArea(float targetAlpha, float duration)
		{
			SpriteRenderer spriteRenderer;
			bool           success = renderer.PositionRoot.TryGetComponent(out spriteRenderer);

			if (success)
			{
				spriteRenderer.DOFade(targetAlpha, duration);
			}
		}

	#region Special States

		/// <summary>
		/// A special jump state where we are doing a little hop forward for more speed or to reach a steeper slope nearby.
		/// </summary>
		public class PenguinHopState : JumpState
		{
			public PenguinHopState(Settings jump) : base(jump) { }

			public override void UpdateFacing(ref Vector3 facing, float dt)
			{
				MathUtil.SlerpWithSharpness(ref facing, actor.JoystickOrFacing, settings.TurnSpeed, dt);
			}

			protected override void UpdateHorizontal(ref Vector3 hvel, float dt)
			{
				MathUtil.SlerpWithSharpness(ref actor.inertia.direction, actor.JoystickOrFacing, settings.TurnSpeed, dt);

				base.UpdateHorizontal(ref hvel, dt);
			}
		}

		/// <summary>
		/// A special jump state where we are jumping out of a slide jump.
		/// </summary>
		public class SlideHopOutState : JumpState
		{
			public SlideHopOutState(Settings settings) : base(settings) { }

			protected override void UpdateHorizontal(ref Vector3 hvel, float dt)
			{
				MathUtil.SlerpWithSharpness(ref actor.inertia.direction, actor.JoystickOrFacing, settings.TurnSpeed, dt);
				base.UpdateHorizontal(ref hvel, dt);
			}

			public override void UpdateFacing(ref Vector3 facing, float dt)
			{
				MathUtil.SlerpWithSharpness(ref facing, actor.JoystickOrFacing, settings.TurnSpeed, dt);
			}
		}

		[Serializable]
		public class SlashState : StateKCC
		{
			[SerializeField, ShowInInspector]
			private SphereCollider[] Hitboxes;

			private bool _useHighVersion;

			[Serializable]
			public class Settings
			{
				[FormerlySerializedAs("BaseForce"), SerializeField]
				public float BaseKnockback;

				[SerializeField] public AnimationCurve SweepCurve;

				[Space]
				[SerializeField] public float AirThrustForce = 4f;

				[SerializeField] public float AirHopHeight = 4;

				[Space]
				[SerializeField] public float GroundThrustForce = 4f;

				[SerializeField] public float GroundHopHeight = 4;

				[Space]
				[SerializeField] public InertiaForce.Settings Inertia;

				[SerializeField]
				public float TurnSpeed;
			}

			[FormerlySerializedAs("BaseForce"), SerializeField]
			private float BaseKnockback;

			[SerializeField] private AnimationCurve SweepCurve;

			[Space]
			[SerializeField] private float AirThrustForce = 4f;

			[SerializeField] private float AirHopHeight = 4;

			[Space]
			[SerializeField] private float GroundThrustForce = 4f;

			[SerializeField] private float GroundHopHeight = 4;

			[Space]
			[SerializeField] private InertiaForce.Settings Inertia;

			[NonSerialized]
			public Settings settings;

			/// <summary>
			/// The current progress of the sword animation
			/// which is handled externally.
			/// </summary>
			[NonSerialized] public float animationPercent;

			/// <summary>
			/// New hits for this frame.
			/// </summary>
			[NonSerialized] public List<Collider> newHits = new List<Collider>();

			/// <summary>
			/// The hits for this sweep. Resets once the current sword sweep ends.
			/// </summary>
			[NonSerialized] public HashSet<Collider> currentHits = new HashSet<Collider>();

			private float[]    _baseRadiuses;
			private Collider[] _overlaps = new Collider[8];
			private Vector3    _startPosition;

			private static List<IHitHandler<SwordHit>> _hithandlers = new List<IHitHandler<SwordHit>>();


			public override void UpdateFacing(ref Vector3 facing, float dt)
			{
				MathUtil.SlerpWithSharpness(ref facing, actor.JoystickOrFacing, settings.TurnSpeed, dt);
			}

			public override void OnActivate()
			{
				base.OnActivate();

				_startPosition = actor.Position;

				if (_baseRadiuses == null)
				{
					_baseRadiuses = new float[Hitboxes.Length];
					for (var i = 0; i < Hitboxes.Length; i++)
					{
						SphereCollider hitbox = Hitboxes[i];
						_baseRadiuses[i] = hitbox.radius;
					}
				}

				newHits.Clear();
				currentHits.Clear();

				// Physics
				float thrustForce = settings.GroundThrustForce;
				float hopHeight   = settings.GroundHopHeight;

				if (actor.airMetrics.airborn)
				{
					thrustForce = settings.AirThrustForce;
					hopHeight   = actor.airMetrics.yDelta > 0 ? settings.AirHopHeight : 0;
					if (!inputs.hasMove) thrustForce *= 0.75f;
				}
				else
				{
					// Only thrust if holding inputs when on the ground
					if (!inputs.hasMove && actor.velocity.magnitude < 1.5f) thrustForce *= 0f;
				}

				actor.AddForce(thrustForce * Vector3.Slerp(actor.facing, actor.JoystickOrFacing, 0.33f) + Vector3.up * actor.CalculateJumpForce(hopHeight));

				// Inertia
				actor.inertia.settings = settings.Inertia;
				actor.inertia.Reset(settings.Inertia, actor.velocity);
			}

			protected override Vector3 TurnDirection
			{
				get
				{
					if (Vector3.Distance(_startPosition, actor.Position) < Mathf.Epsilon)
						return actor.inertia.direction;

					return _startPosition.Towards(actor.Position).Horizontal();
				}
			}

			public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
			{
				UpdateAir(ref currentVelocity, deltaTime);
			}

			public override void OnUpdate(float dt)
			{
				base.OnUpdate(dt);

				if (!active)
					return;

				for (var i = 0; i < Hitboxes.Length; i++)
				{
					Hitboxes[i].radius = _baseRadiuses[i] * settings.SweepCurve.Evaluate(animationPercent);
				}

				newHits.Clear();

				foreach (SphereCollider hitbox in Hitboxes)
				{
					Hitscan(hitbox);
				}
			}

			private void Hitscan(SphereCollider hitbox)
			{
				int mask = Layers.Default.mask | Layers.Enemy.mask | Layers.Actor.mask | Layers.Interactable.mask | Layers.Collidable.mask | Layers.Projectile.mask;
				int size = Physics.OverlapSphereNonAlloc(hitbox.gameObject.transform.position, hitbox.radius, _overlaps, mask, QueryTriggerInteraction.Collide);
				for (int i = 0; i < size; i++)
				{

					Collider overlap = _overlaps[i];
					// Don't wanna hit ourselves.
					if (overlap.gameObject == actor.gameObject || overlap.attachedRigidbody != null && overlap.attachedRigidbody.gameObject == actor.gameObject)
						continue;

					// Don't wanna hit triggers.
					if (overlap.isTrigger)
						continue;

					// Already hit this collider.
					if (currentHits.Contains(overlap))
						continue;

					overlap.GetComponentsInChildren(_hithandlers);

					// Not a hit handler.
					if (_hithandlers.Count == 0)
						continue;

					SwordHit hit = new SwordHit(
						actor.Position.Towards(overlap.transform.position).ChangeY(0).normalized,
						settings.BaseKnockback
					);

					bool any_hit = false;

					foreach (IHitHandler<SwordHit> handler in _hithandlers) {
						if(handler.IsHittable(hit)) {
							any_hit = true;
							handler.OnHit(hit);
						}
					}

					if (any_hit) {
						currentHits.Add(overlap);
						newHits.Add(overlap);
					}
				}
			}
		}

	#endregion

		private static float SphereDist(Vector3 center, Vector3 p1, Vector3 p2)
		{
			Vector3 p1n = p1 - center;
			Vector3 p2n = p2 - center;
			return Mathf.Acos(Vector3.Dot(p1n.normalized, p2n.normalized));
		}

		private struct SoftlockState
		{
			private int     frames;
			private Vector3 lastpos;
			private Vector3 lastfacing;

			public bool Update(Vector3 pos, Vector3 facing)
			{
				float opos    = (lastpos - pos).magnitude;
				float ofacing = (lastfacing - facing).magnitude;

				if (opos < SOFTLOCK_DETECTION_SPEED && ofacing < SOFTLOCK_DETECTION_SPEED)
				{
					frames++;
					if (frames >= SOFTLOCK_DETECTION_FRAMES)
					{
						frames = 0;

						return true;
						// if (!inputs.hasMove && _slide || _slope)
					}
				}
				else
				{
					Reset();
				}

				lastpos    = pos;
				lastfacing = facing;

				return false;
			}

			public void Reset()
			{
				frames = 0;
			}
		}

		private struct BufferedInput
		{
			public static BufferedInput Zero => new BufferedInput
			{
				pressed    = false,
				firstPress = float.MinValue,
				lastPress  = float.MinValue
			};

			public bool  pressed;
			public float firstPress;
			public float lastPress;

			public void Update(bool pressed)
			{
				if (pressed)
				{
					if (!this.pressed)
					{
						firstPress   = Time.time;
						this.pressed = true;
					}

					lastPress = Time.time;
				}
			}

			public bool IsRecent(float threshold = INPUT_BUFFERING_TIME_WINDOW) => Time.time - lastPress < threshold;
		}

	}
}