using Sirenix.OdinInspector;
using UnityEngine;
using Util;

namespace Anjin.Actors
{
	/// <summary>
	/// This is just so we can tweak values at runtime without have to continuously copy them to the prefab.
	/// Also good for testing different settings and possibly re-using.
	/// I really wish we could move this to the same file as PlayerCharacter, but Unity requires ScriptableObjects to be in a file named the same...
	/// </summary>
	public class PlayerActorSettings : SerializedScriptableObject
	{
		[Title("Transition")]
		public float MinAirTimeBeforeGlide;

		public float MinJumpTimeBeforeDive;

		public AnimationCurve AirDashDirectionCurve = AnimationCurve.Linear(0, 0, 1, 1); // Allows to make a steeper curve, that way it's easier to aim forward

		[Tooltip("Minimum speed before slide should end.")]
		public float MinSlideSpeed = 1.2f;
		public float SlideBonusFromDive = 1.5f;
		public float SlopeThreshold     = -0.5f;
		public float SlideStartLag      = 0.15f;

		[Tooltip("The maximum speed at which the force stops applying when hopping during a slide.")]
		public AnimationCurve SlideHopForceBySpeed = AnimationCurve.Linear(0, 10, 20, 0);
		public float          SlideHopForceTimingBonusWindow = 0.25f;
		public AnimationCurve SlideHopForceTimingBonusCurve  = AnimationCurve.EaseInOut(0, 1, 1, 1.35f);
		public AnimationCurve SlideHopHeightBySpeed          = AnimationCurve.Linear(0, 0.7f, 25, 1.25f);

		public float SwimJumpHeight = 1;
		public float SwordHopHeight = 1;
		public float SwordDurationBeforeJumpAllowed;
		public float SwordParticlesAngleLimit = 0.5f;

		public float CanWallSlideAfter = 0.25f;
		public float WallSlideEntryThreshold = 0.96f;
		public float WallBonkEntryThreshold = 0.96f;
		public float WallBonkMinSpeed		= 3f;

		[Title("States")]
		[DarkBox] public WalkState.Settings WalkRun;
		[DarkBox] public SlideState.Settings         Slide;
		[DarkBox] public SlideState.Settings         Slope;
		[DarkBox] public JumpState.Settings          Jump;
		[DarkBox] public JumpState.Settings          SlideHopOut;
		[DarkBox] public JumpState.Settings          SlideHopForward;
		[DarkBox] public GroundTakeoffState.Settings JumpTakeoff;
		[DarkBox] public FallState.Settings          Fall;
		[DarkBox] public GlideState.Settings         Glide;
		[DarkBox] public PogoState.Settings          Pogo;
		[DarkBox] public AerialDashState.Settings    DiveDash;
		[DarkBox] public AerialDashState.Settings    DiveDown;
		// [DarkBox] public SlopeState.Settings Slope;
		[DarkBox] public SwimState.Settings              Swim;
		[DarkBox] public SprintState.Settings            Sprint;
		[DarkBox] public FallState.Settings              BumDive;
		[DarkBox] public WaterJetState.Settings          WaterJet;
		[DarkBox] public LaunchState.Settings            Launched;
		[DarkBox] public PlayerActor.SlashState.Settings Slash;
		[DarkBox] public DoubleJumpState.Settings		 DoubleJump;
		[DarkBox] public WallSlideState.Settings		 WallSlide;
		[DarkBox] public WallJumpState.Settings			 WallJump;
		[DarkBox] public WallTakeoffState.Settings		 WallTakeoff;
		[DarkBox] public BonkState.Settings				 WallBonk;

		[Title("Effects")]
		public float ActorSpeedForRunAnim = 0.8f;
		public float             ActorSpeedForWalkAnim = 0.2f;
		public float             StandTimeForFancyIdle;
		public int               FancyIdleRepeats         = 3;
		public AnimationCurve    LandAnimDurationByHSpeed = AnimationCurve.Linear(0, 0.21f, 10f, 0f);
		public bool              EnableSpeedTrail;
		public LinearForceConfig RunRollforce;
		public AnimationCurve    WalkAnimSpeed;
		public AnimationCurve    DashAnimSpeed = AnimationCurve.Linear(0, 0.75f, 0.45f, 1f);
		public AnimationCurve    RunAnimSpeed;
		public AnimationCurve    SwimAnimSpeed;
		public float             SwimEntryCooldown;
		public float             SwimExitCooldown;
		public float             SwimEntryBigSplashHeight = 10f;
		public float             SwimEntryLightSplashHeight = 2f;
		[Space]
		public float GroundTiltPitchMax = 22.5f;
		public float          GroundTiltPitchDamping = 2f;
		public float          GroundTiltRollDamping  = 2f;
		public float          AirTiltPitchDamping    = 1.5f;
		public float          AirTiltRollDamping     = 1.5f;
		public AnimationCurve PogoPitchHorizontal    = AnimationCurve.Constant(0, 1, 0);
		public AnimationCurve PogoPitchVertical      = AnimationCurve.Constant(0, 1, 0);
		public float          PogoPitchLerping       = 0.97f;
		public FloatRange     DolphinDiveSpritePitchRange;
		public FloatRange     DolphinDiveSpritePitchVelocityRange;
		[Space]
		public float SwimBobbingAmplitude;
		public float SwimBobbingSpeed;
		public float SwimBobbingLerpSpeed = 1.5f;
		[Space]
		public AnimationCurve SpeedToTrailImageCount;
		public AnimationCurve SpeedToTrailImageSpacing;
		public float          SpeedToTrailImageCountDamping               = 2.5f;
		public float          SpeedToTrailImageSpacingDamping             = 1.1f;
		public float          SpeedToTrailImageCountDampingDecelerating   = 6.5f;
		public float          SpeedToTrailImageSpacingDampingDecelerating = 1.5f;
		// public AnimationCurve SpeedToTrailDeceleratingImageCount;
		// public AnimationCurve SpeedToTrailDeceleratingImageSpacing;
		public FloatRange SpeedToTrailTransitionRange = new FloatRange(-0.25f, -0.05f);

		public AnimationCurve Swim_HSpeed_To_AnimationSpeed = AnimationCurve.Linear(0, 0, 8, 1);

		[Title("Sounds")]
		public AudioDef SFX_Step;
		public AudioDef SFX_Land;
		public AudioDef SFX_JumpFallStart;
		public AudioDef SFX_JumpLaunch;
		public AudioDef SFX_JumpPrepare;
		public AudioDef SFX_DashDive;
		public AudioDef SFX_SlideHopOut;
		public AudioDef SFX_GlideStart;
		public AudioDef SFX_GlideEnd;
		public AudioDef SFX_SlideStart;
		public AudioDef SFX_PenguinHop;
		public AudioDef SFX_SlideToWalk;
		public AudioDef SFX_Sliding;
		public AudioDef SFX_PogoEnter;
		public AudioDef SFX_PogoBounce;
		public AudioDef SFX_PogoJumpOut;
		public float    SFX_StepSoundDelay;
		public AudioDef SFX_Bonk;
		public AudioDef SFX_WallJump;

		public AudioDef SFX_WaterEntryTiny;
		public AudioDef SFX_WaterEntryLight;
		public AudioDef SFX_WaterEntryHeavy;

		public AudioDef SFX_WaterIdle;

		public AudioDef SFX_WaterExitJump;

		public AudioDef SFX_Swim;
		//public AudioDef

		[Title("Toggles")]
		public bool EnableSaveFlags;
		public bool EnableSword;
		public bool EnableDiveDash;
		public bool EnableGlide;
		public bool EnablePogo;
	}
}