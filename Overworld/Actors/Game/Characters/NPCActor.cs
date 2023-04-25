using System;
using Assets.Scripts.Utils;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityUtilities;
using Util;
using Util.Assets;
using Util.Components.Timers;
using Util.Odin.Attributes;
#if UNITY_EDITOR

#endif

namespace Anjin.Actors
{
	public enum NPCState
	{
		// Normal States
		Ground,
		Swimming,

		// Jump
		JumpWindup,
		Jump,
		Fall,
		Land,

		Sitting,
		//ReachedTarget
	}

	//[RequireComponent(typeof(ActorRenderer))]
	public class NPCActor : Actor
	{
		[HideInEditorMode, NonSerialized]
		private CharacterInputs charInputs;

		[HideInEditorMode, NonSerialized]
		private RenderState _animState;

		[FormerlySerializedAs("MySettings")]
		[TitleGroup("NPC", order:10)]
		public NPCActorSettingsAsset SettingsAsset;

		public Settings CSettings => SettingsAsset.Settings;

		private NPCState _state;

		[NonSerialized]
		public NPCState PrevState;
		[ShowInPlay]
		public NPCState State {
			get => _state;
			set {
				if(value != State) {
					_stateChanged = true;
					PrevState     = _state;
					_state        = value;
				}
			}
		}

		[TitleGroup("NPC", order:10)]
		public bool      GroundSnapping = false;

		[TitleGroup("NPC")]
		public Transform GroundSnapPivot;

		[TitleGroup("NPC")]
		public bool		FaceTowardsForwards;
		public Vector3	VisualUp = Vector3.up;

		/*[SerializeField] private ParticlePrefab FX_SwimEntryTiny;
		[SerializeField] private ParticlePrefab FX_SwimEntryLight;
		[SerializeField] private ParticlePrefab FX_SwimEntryHeavy;*/


		[TitleGroup("NPC")]
		[SerializeField] private ParticlePrefab FX_SwimEntry;
		[TitleGroup("NPC")]
		[SerializeField] private ParticlePrefab FX_SwimExit;

		[TitleGroup("NPC")]
		[SerializeField] private ParticleRef FX_SwimmingIdle;
		[TitleGroup("NPC")]
		[SerializeField] private ParticleRef FX_SwimmingMove;

		[TitleGroup("NPC")]
		public AudioDef SFX_SwimEntry;
		[TitleGroup("NPC")]
		public AudioDef SFX_SwimExit;

		[ToggleGroup("NPC/EnableFancyIdle", "Fancy Idle")]
		public bool EnableFancyIdle;

		[ToggleGroup("NPC/EnableFancyIdle")] public FloatRange StandTimeForFancyIdle;
		[ToggleGroup("NPC/EnableFancyIdle")] public int        FancyIdleRepeats;



		[DebugVars]
		[NonSerialized] public ActorDesigner designer;

		[NonSerialized] public Vector3       actionStart;
		[NonSerialized] public Vector3       actionTarget;
		[NonSerialized] public float         actionTimer;
		[NonSerialized] public float         actionSeconds;
		[NonSerialized] public Option<float> actionHeight;

		[NonSerialized]
		public ValTimer DelayTimer;

		private Vector3 _defaultFacing;
		private bool    _stateChanged;
		private bool    _hasGroundPivot;
		private float   _startingGroundOffset;
		private float   _standElapsed;
		private float   _standTimeForIdle = 4f;

		[ShowInPlay]
		private ValTimer	_swimEntryDepthTmr;

		private float		_swimBob;

		public void JumpTo(Vector3 goal, Option<float> height = default) => JumpTo(transform.position, goal, height);
		public void JumpTo(Vector3 start, Vector3 goal, Option<float> height = default)
		{
			State = NPCState.Jump;
			actionStart  = start;
			actionTarget = goal;
			actionTimer  = 0;
			actionHeight = height;
		}

		protected override void Awake()
		{
			base.Awake();

			designer = GetComponent<ActorDesigner>();

			_hasGroundPivot       = GroundSnapPivot != null;
			_startingGroundOffset = 0;
			if (_hasGroundPivot)
				_startingGroundOffset = GroundSnapPivot.localPosition.y;
		}

		protected override void Start()
		{
			base.Start();
			_animState = new RenderState(AnimID.Stand);
			charInputs = CharacterInputs.DefaultInputs;

			if(hasCharRenderer) {
				renderer.Animable.onCompleted += AnimatorOnCompleted;
				renderer.Animable.onRepeat    += AnimatorOnCompleted;
			}

			UpdateDefaultFacing();
		}

		protected override void Update()
		{
			base.Update();

			bool ground_snap = true;

			bool delayed = !DelayTimer.Tick();

			switch (State) {
				case NPCState.Ground:
					PollCharacterBrainInputs(ref charInputs);
					break;

				case NPCState.Swimming:
					PollCharacterBrainInputs(ref charInputs);

					if (_stateChanged) {
						FX_SwimEntry.Instantiate(transform);
						GameSFX.Play(SFX_SwimEntry, transform.position);
					}
					break;

				case NPCState.JumpWindup: break;

				case NPCState.Jump:

					if (delayed) break;

					if (_stateChanged && PrevState == NPCState.Swimming) {
						FX_SwimExit.Instantiate(transform);
						GameSFX.Play(SFX_SwimExit, transform.position);
					}

					float dist = Vector3.Distance(actionStart, actionTarget);

					if (dist < 0.001f) {
						EndMovingToTarget();
					} else {
						float height = actionHeight.ValueOrDefault(CSettings.JumpApexHeight);

						dist = MathUtil.ParabolaLength(dist, height);

						float speed = CSettings.JumpAcrossSpeed * (dist / 2);

						if (actionTarget.y > actionStart.y + 0.5f)
						{
							// Jump Up
							speed = CSettings.JumpUpSpeed * CSettings.JumpUpAccel.Evaluate(actionSeconds);
						}
						else if (actionTarget.y < actionStart.y - 0.5f)
						{
							// Jump Down
							speed = CSettings.JumpDownSpeed * CSettings.JumpDownAccel.Evaluate(actionSeconds);
						}

						charInputs.LookStripY(actionTarget - actionStart);

						Teleport(MathUtil.EvaluateParabola(actionStart, actionTarget, height, Mathf.Clamp01(actionTimer)));


						actionTimer   += (Time.deltaTime / dist) * speed;
						actionSeconds += Time.deltaTime;


						if (actionTimer >= 1)
							EndMovingToTarget();
					}
					break;

				case NPCState.Fall:       break;
				case NPCState.Land:       break;
				case NPCState.Sitting:    break;
			}

			if (State == NPCState.JumpWindup || State == NPCState.Land) {

				actionSeconds = 0;
				charInputs.NoMovement();

				charInputs.LookStripY(actionTarget - actionStart);
			}

			ground_snap = !(State == NPCState.Jump || State == NPCState.JumpWindup || State == NPCState.Land || State == NPCState.Swimming);

			// Lower the billboard if the character's sitting.
			if (_stateChanged)
			{
				// TODO shouldn't this be done with RenderState??
				// Transform billboard = charRenderer.BillboardRoot;
				// billboard.localPosition = new Vector3(billboard.localPosition.x, State == NPCState.Sitting ? SittingYOffset : 0, billboard.localPosition.z);
			}

			if(delayed)
				charInputs.NoMovement();

			// Update facing direction
			// ----------------------------------------

			if (!FaceTowardsForwards)
			{

				Vector3 lastFacing = facing;
				Vector3 nextFacing = _defaultFacing;

				if (charInputs.look != null)
					nextFacing = charInputs.look.Value;
				else if (velocity.magnitude > 0.001f)
					nextFacing = velocity;

				if (lastFacing != nextFacing)
				{
					facing = Vector3.Slerp(facing, nextFacing, charInputs.LookDirLerp);

					if (facing.magnitude < Mathf.Epsilon)
					{
						facing = lastFacing;
					}

					if (facing.magnitude > 0.001f)
					{
						transform.localRotation = Quaternion.LookRotation(facing.ChangeY(0), Vector3.up);
					}
				}
			} else {
				//transform.localRotation = Quaternion.LookRotation(Vector3.forward.ChangeY(0), VisualUp);
				facing                  = transform.forward;
			}

			var spd = CSettings.WalkSpeed;
			if (_state == NPCState.Swimming)
				spd = CSettings.SwimSpeed;
			if (charInputs.hasMove)
				spd = charInputs.moveSpeed.GetValueOrDefault(spd);

			// var dir = new Vector3(charInputs.move.x, 0, charInputs.move.y);

			velocity = Time.deltaTime * spd * charInputs.move; //.normalized;
			if (velocity.magnitude > Mathf.Epsilon)
				transform.position += velocity;

			if (ground_snap && GroundSnapping && _hasGroundPivot)
			{
				float ground_y_offset = 0;

				if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out var ray, 4f, Layers.Walkable.mask, QueryTriggerInteraction.Ignore))
				{
					ground_y_offset = ray.point.y - transform.position.y;
				}

				GroundSnapPivot.localPosition = new Vector3(0, ground_y_offset + _startingGroundOffset, 0);
			}


			charInputs.OnProcessed();
		}

		public override void UpdateRenderState(ref RenderState state)
		{
			if (_stateChanged)
			{
				state = new RenderState(AnimID.Stand);

				if (State == NPCState.Swimming) {
					_swimEntryDepthTmr.Set(CSettings.SwimEntryDepthTime, true);
				}
			}

			if (velocity.magnitude > Mathf.Epsilon)
			{
				state.animID    = AnimID.Walk;
				state.animSpeed = Mathf.Clamp(velocity.magnitude / Time.deltaTime / 4, 0, 1);

				_standElapsed = 0;
			}
			else
			{
				state.animID = AnimID.Stand;

				if (EnableFancyIdle && this.DoFancyIdle1(
					    ref state,
					    ref _standElapsed,
					    _standTimeForIdle,
					    FancyIdleRepeats) == FancyIdleStates.BecameInactive)
				{
					_standTimeForIdle = StandTimeForFancyIdle.RandomInclusive;
				}
			}

			if(State != NPCState.Swimming)
				state.offset.y = 0;

			switch (State)
			{
				case NPCState.Sitting:
					state.animID = AnimID.Sit;
					state.offset = Vector3.down * CSettings.SittingYOffset; // TODO revert this if it doesnt work
					break;

				case NPCState.Swimming:

					_swimEntryDepthTmr.Tick();

					float targetY = CSettings.SwimBaseYOffset + _swimBob - (_swimEntryDepthTmr.norm_1to0 * CSettings.SwimEntryDepth);
					state.offset.y = Mathf.Lerp(state.offset.y, targetY, CSettings.SwimBobLerp);
					_swimBob       = Mathf.Sin(Time.time * CSettings.SwimBobSpeed) * CSettings.SwimBobStrength;
					break;

				case NPCState.JumpWindup:
					state.animID    = AnimID.Jump;
					state.animSpeed = CSettings.JumpWindupSpeed;
					break;

				case NPCState.Land:
					state.animID = AnimID.Land;
					break;

				case NPCState.Jump:
					if (!DelayTimer.done) {
						if(PrevState == NPCState.Swimming)
							state.animID = AnimID.SwimIdle;
						else
							state.animID = AnimID.Stand;
					} else {
						state.animID = actionTimer <= 0.5f ? AnimID.Rise : AnimID.Fall;
					}
					break;
			}
		}

		private void AnimatorOnCompleted()
		{
			if (State == NPCState.JumpWindup)
				State = NPCState.Jump;
			else if (State == NPCState.Land)
				State = NPCState.Ground;
		}

		private void EndMovingToTarget()
		{
			DebugLogger.Log(name + ": End of action", LogContext.Overworld, LogPriority.Low);
			DebugDraw.DrawMarker(actionTarget, 0.5f, Color.yellow, 5f, false);
			Teleport(actionTarget);
			State = NPCState.Ground;
			charInputs.NoMovement();
		}

		public override void ClearVelocity(bool x, bool y, bool z) { }

		public override void AddForce(Vector3 force, bool setY = false, bool setXZ = false) { }

		public override void Reorient(Quaternion rot)
		{
			// BUG this doesn't consider _initialFacing
			FaceDegreesOffset = rot.eulerAngles.y;
			UpdateDefaultFacing();
		}

		public void UpdateDefaultFacing()
		{
			// This is cached for performance reasons. It adds up to quite a bit of wasted CPU with all the NPCs in a level
			_defaultFacing = Quaternion.AngleAxis(FaceDegreesOffset, Vector3.up) * initialFacing;
		}

		private void LateUpdate()
		{
			if(DelayTimer.done)
				_stateChanged = false;
		}

		//		TEMP TEMP TEMP
		//------------------------------------------------
		/*public Vector3 GetJumpPos(float pos)
		{
			Vector2 start = new Vector2(ActionStart.x, ActionStart.z);
			Vector2 end   = new Vector2(ActionTarget.x, ActionTarget.z);

			float jump_height = 5;
			Vector2 current  = Vector2.Lerp(start, end, pos);

			float dist 	 = Vector2.Distance(start, end);
			float base_y = Mathf.Lerp(ActionStart.y, ActionTarget.y, pos);
			float arc    = jump_height * ( current - start ) * ( current - end )

			Vector2 midPoint = Vector2.Lerp(start, end, 0.5f);

			Vector2 temp = ;

			float y = ( -20 / ( ( midPoint - start ) * ( midPoint - end ) ) );

		}*/

		public Vector3 Parabola(Vector3 start, Vector3 end, float height, float t)
		{
			//float Func(float x) => -4 * height * x * x + 4 * height * x;

			var mid = Vector3.Lerp(start, end, t);

			var tt = -4 * height * t * t + 4 * height * t;

			return new Vector3(mid.x, tt + Mathf.Lerp(start.y, end.y, t), mid.z);
		}

		public class Settings {


			//[Title("Stand / Move")]

			public float          WalkSpeed       = 4;
			public float          SwimSpeed       = 3;
			public float          SittingYOffset  = 0.06f;

			public float          JumpAcrossSpeed = 4;
			public float          JumpUpSpeed     = 4;
			public float          JumpDownSpeed   = 4;
			public float          JumpApexHeight = 2;
			public float          JumpWindupSpeed = 1;
			public AnimationCurve JumpUpAccel;
			public AnimationCurve JumpDownAccel;

			public float SwimBaseYOffset = -0.5f;
			public float SwimBobSpeed    = 1;
			public float SwimBobLerp     = 0.1f;
			public float SwimBobStrength = 1;

			public float SwimEntryDepth			= 1;
			public float SwimEntryDepthTime		= 0.5f;

			public AudioDef SFX_WaterEntryTiny;
			public AudioDef SFX_WaterEntryLight;
			public AudioDef SFX_WaterEntryHeavy;

			public AudioDef SFX_WaterIdle;

			public AudioDef SFX_WaterExitJump;

			public AudioDef SFX_Swim;

		}


#if UNITY_EDITOR
		[UsedImplicitly]
		private bool HideFaceDegreeOffset => FaceDegreesOffset < Mathf.Epsilon;
#endif
	}
}