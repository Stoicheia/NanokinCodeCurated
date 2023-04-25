using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using Anjin.Nanokin;
using Anjin.Nanokin.Map;
using Anjin.Nanokin.Park;
using Anjin.Util;
using Anjin.Utils;
using Drawing;
using JetBrains.Annotations;
using KinematicCharacterController;
using Overworld.Terrains;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityUtilities;
using Util;
using Util.Components.Timers;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	/// <summary>
	/// Bse class for any KCC character.
	/// Provides a higher-level state system for manageable controller logic.
	/// </summary>
	[RequireComponent(typeof(KinematicCharacterMotor))]
	public abstract class ActorKCC : Actor,
		ICharacterController,
		BounceInfo.IHandler {
		public const int PLATFORMING_VOLUME_ARRAY_SIZE = 8;

		private static readonly List<Material> _tmpSharedMaterials = new List<Material>(8);
		private static readonly Collider[]     _tmpWaterVolumes    = new Collider[4];
		private static readonly Collider[]     _tmpPlatformingPhys = new Collider[8];

		// Configuration
		[Title("KCC")]
		[HideInInspector] public KinematicCharacterMotor Motor;

		[NonSerialized] public Rigidbody Body;

		[FormerlySerializedAs("validCollisionLayers"), FormerlySerializedAs("ValidCollisionLayers")]
		public LayerMask CollisionMask = 0;

		[SerializeField]
		private LayerMask WaterMask;

		[SerializeField]
		private LayerMask PlatformingPhysMask;

		[Title("Physics")]
		public Vector3 GravityDirection = new Vector3(0, -1f, 0);

		[FormerlySerializedAs("BaseGravityForce")]
		[FormerlySerializedAs("GravityForce")]
		public float GravityScalable = 30f;
		public float AirDrag     = 3f;
		public float AirMaxSpeed = 30f;

		[Space]
		public float DefaultSpeedSharpness = 10f;

		public float DefaultTurnSharpness = 10f;

		[TitleGroup("Debug", order: 99)]
		[SerializeField, TitleGroup("Debug"), FormerlySerializedAs("_drawFacingDirection")]
		private bool DrawFacingDirection;

		// [DebugVars]
		[NonSerialized]             public float             elapsedStateTime;
		[NonSerialized]             public int               currentStateID  = -1;
		[NonSerialized]             public int               previousStateID = -1;
		[NonSerialized, ShowInPlay] public StateKCC          currentState   = null;
		[NonSerialized, ShowInPlay] public InertiaForce      inertia;
		[NonSerialized]             public AirMetrics        airMetrics;
		[NonSerialized, ShowInPlay] public CharacterInputs   inputs;
		[NonSerialized, ShowInPlay] public GravityVolume     gravityVolume;
		[NonSerialized, ShowInPlay] public EncounterBounds   bounds;
		[NonSerialized]             public Vector3           slopeDir;
		[NonSerialized]             public Vector3           deltaVelocity;
		[NonSerialized]             public Surface           surface;
		[NonSerialized]             public TerrainProperties terrain;

		[NonSerialized, ShowInPlay]
		public bool physicsEnabled;

		// Platforming Features
		[NonSerialized, ShowInPlay] public bool hasSurface;
		[NonSerialized, ShowInPlay] public bool hasTerrain;

		[NonSerialized, ShowInPlay] public PlatformingVolume[]	platformingVolumes;
		[NonSerialized, ShowInPlay] public int					numPlatformingVolumes;

		[NonSerialized, ShowInPlay] public bool     SpeedBoost;
		[NonSerialized, ShowInPlay] public bool     SpeedBoostContact;
		[NonSerialized, ShowInPlay] public float    SpeedBoostMultiplier;
		[NonSerialized, ShowInPlay] public ValTimer SpeedBoostDuration;

		[NonSerialized, ShowInPlay] public Collider water;

		[NonSerialized, ShowInPlay] public bool     hasWater;

		[NonSerialized, ShowInPlay] public WaterJet waterJet;
		[NonSerialized, ShowInPlay] public bool     hasWaterJet;

		[NonSerialized, ShowInPlay] public LaunchPad launchPad;
		[NonSerialized, ShowInPlay] public bool      hasLaunchPad;

		/// <summary>
		/// Indicates that the state has changed since the last update.
		/// Should only be used in Update(), not FixedUpdate() calls (including
		/// all of KCC's call like UpdateVelocity, etc.)
		/// </summary>
		protected bool stateChanged;

		private Vector3                      _lastVelocity;
		private bool                         _hasLanded;
		private List<StateKCC>               _allStates;
		private Queue<DelayedVelocityChange> _delayedMovements;
		private Queue<DelayedFacingChange>   _delayedFacing;

		public float Gravity => GetGravity();

		public Vector3 GravityVector => GravityDirection * Gravity;

		// Shortcuts
		// ----------------------------------------

		public override Vector3 Up => Motor.CharacterUp;

		public override bool IsMotorStable => Motor ? Motor.GroundingStatus.IsStableOnGround : false;

		public bool IsGroundState => currentState.IsGround;

		public bool IsAirState => currentState.IsAir;

		public abstract float MaxJumpHeight { get; }

		public abstract StateKCC GetDefaultState();

		protected override void Awake()
		{
			base.Awake();

			timeScale         = gameObject.GetOrAddComponent<TimeScalable>();
			if(_delayedMovements == null)
				_delayedMovements = new Queue<DelayedVelocityChange>();
			if(_delayedFacing == null)
				_delayedFacing    = new Queue<DelayedFacingChange>();
			_allStates        = new List<StateKCC>();

			airMetrics = new AirMetrics();
			inertia    = new InertiaForce(this);

			platformingVolumes    = new PlatformingVolume[PLATFORMING_VOLUME_ARRAY_SIZE];
			numPlatformingVolumes = 0;

			Motor                     = GetComponent<KinematicCharacterMotor>();
			Motor.CharacterController = this;

			Body = GetComponent<Rigidbody>();

			physicsEnabled = true;

			RegisterStates();
			ChangeState(GetDefaultState());
		}

		public void RegisterState(StateKCC state)
		{
			RegisterState(-1, state);
		}


		public void RegisterState(int id, StateKCC state)
		{
			state.id    = id > -1 ? id : _allStates.Count;
			state.actor = this;

			if (this is PlayerActor plr) {
				state.player    = plr;
				state.hasPlayer = true;
			}

			_allStates.Add(state);
		}

		public void ChangeState(StateKCC nextState)
		{
			if (nextState == currentState)
				return; // Already the current state.

			StateKCC prevState = currentState;

			if (prevState != null) {
				prevState.OnDeactivate();
				prevState.justActivated = false; // just in case
				prevState.active        = false;

				previousStateID = prevState.id;
			} else {
				previousStateID = -1;
			}

			currentState     = nextState;
			currentStateID   = -1;
			elapsedStateTime = 0;
			stateChanged     = true;

			if (currentState != null)
			{
				currentStateID             = currentState.id;
				currentState.active        = true;
				currentState.justActivated = true;

				currentState.inputs     = inputs;
				currentState.airMetrics = airMetrics;
				currentState.OnActivate();

				if (currentState.IsGround && (prevState == null || !prevState.IsGround))
				{
					OnLand.Invoke();
					OnLanding();
				}
			}

			OnStateTransition(prevState, ref nextState);
		}

		// public void BeforeCharacterUpdate(float deltaTime)
		// {
		// 	// Do this once, so we don't do the check multiple times per frame.
		// 	gravityVolume = null;
		//
		// 	Vector3 origin = Motor.transform.position;
		// 	int     num    = Physics.OverlapCapsuleNonAlloc(origin + Vector3.up * 0.2f, origin + Vector3.up * 1f, 0.2f, _scratchColliders, Layers.TriggerVolume.mask);
		//
		// 	// TODO: This will need to respect actor rotation
		// 	if (num > 0)
		// 	{
		// 		gravityVolume = _scratchColliders[0].GetComponent<GravityVolume>();
		// 	}
		// }

		protected override void Update()
		{
			if (GameController.IsWorldPaused) {
				return;
			}

			if (inputs.processed)
			{
				ResetCharacterBrainInputs(ref inputs);
				inputs.processed = false;
			}

			base.Update();

			stateChanged = false;

			// If we have a brain controller (which we probably should), send it
			// the inputs this frame to modify how it sees fit
			PollCharacterBrainInputs(ref inputs);

			float dt = timeScale.deltaTime;
			foreach (StateKCC state in _allStates)
			{
				state.OnUpdate(dt);
			}

			elapsedStateTime += dt;

#if UNITY_EDITOR
			if (DrawFacingDirection)
				Debug.DrawLine(transform.position, transform.position + inputs.move * 1.5f, Color.red);
#endif

			/*if (DebugSystem.Opened)
			{
				Draw.ingame.WireSphere(Motor.GroundingStatus.GroundPoint, 0.15f, Color.black);
				Draw.ingame.WireSphere(Motor.GroundingStatus.GroundPoint, 0.16f, Color.white);
			}*/
		}

		public void UpdateVelocity(ref Vector3 cvelocity, float deltaTime)
		{
			deltaTime *= timeScale.current;

			if (!actorActive || disablePhysics)
			{
				cvelocity = Vector3.zero;
				velocity  = cvelocity;
				return;
			}

			// Check for state transitions.
			velocity = cvelocity;
			position = Motor.TransientPosition;

			// We cannot change velocity outside UpdateVelocity, so this allows us to do some modifications elsewhere.
			while (_delayedMovements.Count > 0)
			{
				DelayedVelocityChange change = _delayedMovements.Dequeue();

				cvelocity   += change.add.GetValueOrDefault(Vector3.one);
				cvelocity.x =  change.xset ?? cvelocity.x;
				cvelocity.y =  change.yset ?? cvelocity.y;
				cvelocity.z =  change.zset ?? cvelocity.z;
				cvelocity   =  Vector3.Scale(change.scale.Value, cvelocity);

				if (cvelocity.y > 0)
					Motor.ForceUnground();
			}

			if (IsMotorStable)
				airMetrics.UpdateGround(transform, deltaTime);
			else
				airMetrics.UpdateAir(transform, deltaTime);

			// Update ground descent
			var acrossNormal = Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal);
			slopeDir = Vector3.Cross(acrossNormal, Motor.GroundingStatus.GroundNormal);


			StateKCC nextState = GetNextState(ref cvelocity, deltaTime);
			velocity = cvelocity;

			OnAfterStateDecision(ref cvelocity, deltaTime, nextState != currentState);
			velocity = cvelocity;

			if (nextState != null)
				ChangeState(nextState);


			if (airMetrics.airborn)
			{
				inertia.Update(deltaTime);
			}

			currentState.inputs     = inputs;
			currentState.airMetrics = airMetrics;
			currentState.UpdateVelocity(ref cvelocity, deltaTime);

			//ApplySpeedEffect(ref cvelocity, deltaTime, currentState);

			deltaVelocity = cvelocity - _lastVelocity;
			_lastVelocity = cvelocity;
		}


		protected virtual void OnAfterStateDecision(ref Vector3 currentVelocity, float deltaTime, bool changedState) { }

		protected virtual void OnLanding() { }

		public void BeforeCharacterUpdate(float deltaTime) { }

		public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
		{
			deltaTime *= timeScale.current;

			Vector3 updatedDirection = Motor.CharacterForward;

			// Apply delayed modifications.
			// ----------------------------------------
			while (_delayedFacing.Count > 0)
			{
				DelayedFacingChange change = _delayedFacing.Dequeue();
				updatedDirection = change.set; // Quaternion.LookRotation(change.set, Motor.CharacterUp);
			}

			if (actorActive && !disablePhysics)
			{
				// Update rotation according to current state
				// ----------------------------------------
				currentState.inputs     = inputs;
				currentState.airMetrics = airMetrics;
				currentState.UpdateFacing(ref updatedDirection, deltaTime);

				// Just to make sure! When we rotate on any axis other than the yaw, shit fucks up BADLY.
				// Update: this is firing msgs like nuts, lagging the game. No time to figure out why rn
// #if UNITY_EDITOR
				// if (!Mathf.Approximately(updatedDirection.y, 0))
				// this.LogWarning("Got fucked up rotation ");
// #endif

				updatedDirection.y = 0;
				updatedDirection   = updatedDirection.normalized;
			}


			currentRotation = Quaternion.LookRotation(updatedDirection, Motor.CharacterUp);
			facing          = updatedDirection;
		}

		public virtual void AfterCharacterUpdate(float deltaTime)
		{
			inputs.OnProcessed();

			if (currentStateID > -1)
				currentState.justActivated = false;

			airMetrics.OnAfterVelocityUpdate(transform, deltaTime);

			// Manual adjustments of position to take time scaling into account.
			Motor.TransientPosition = Motor.InitialTickPosition.Towards(Motor.TransientPosition, timeScale.current);
			Motor.TransientRotation = Quaternion.Slerp(Motor.InitialTickRotation, Motor.TransientRotation, timeScale.current);

			if (bounds)
				bounds.LimitKCC(Motor);

			position = Motor.TransientPosition;

			currentState.inputs     = inputs;
			currentState.airMetrics = airMetrics;
			currentState.AfterCharacterUpdate(deltaTime);
		}

		public void PostGroundingUpdate(float deltaTime) { }

		public bool IsColliderValidForCollisions(Collider coll)
		{
			if (CollisionMask.ContainsLayer(coll.gameObject.layer))
				return true;

			else return false;
		}

		public virtual void OnGroundHit(Collider collider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
		{
			OnColliderHit(collider, hitNormal, hitPoint, ref hitStabilityReport);


			hasTerrain = false;

			// Update material terain
			// ----------------------------------------
			MeshRenderer mr = collider.GetComponent<MeshRenderer>();
			if (mr != null && mr.enabled)
			{
				mr.GetSharedMaterials(_tmpSharedMaterials);
				foreach (Material material in _tmpSharedMaterials)
				{
					TerrainProperties properties = SceneTerrainProperties.GetProperties(collider.gameObject.scene, material);
					if (properties)
					{
						terrain    = properties;
						hasTerrain = true;
						break;
					}
				}
			}

			// Update component surface
			// ----------------------------------------

			if (hasSurface = Surface.all.TryGetValue(collider.gameObject.GetInstanceID(), out Surface surface))
			{
				this.surface = surface;
			}
		}

		public virtual void OnMovementHit(Collider collider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport report)
		{
			OnColliderHit(collider, hitNormal, hitPoint, ref report);
		}


		// public bool HasSwimVolume => CurrentSwimVolume != null;

		public virtual void OnColliderHit(Collider collider,
			Vector3                                hitNormal,
			Vector3                                hitPoint,
			ref HitStabilityReport                 hitStability
		) { }

		public virtual void ProcessHitStabilityReport(Collider collider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport stability)
		{
			if (collider == null)
				return;

			if (Surface.all.TryGetValue(collider.gameObject.GetInstanceID(), out Surface slope))
			{
				switch (slope.Behavior)
				{
					case Surface.Behaviors.ForceStand:
					case Surface.Behaviors.ForceSlope:
						stability.IsStable = true;
						break;

					case Surface.Behaviors.ForceUnstable:
						stability.IsStable = false;
						break;
				}
			}
		}

		public virtual void OnDiscreteCollisionDetected(Collider collider) { }

		public float ApplySpeedBoost(float speed)
		{
			if (SpeedBoost)
				return speed * SpeedBoostMultiplier;

			return speed;
		}

		/*public virtual bool GetSpeedBoostMultiplier(out float multiplier)
		{
			multiplier = SpeedBoostMultiplier;
			return SpeedBoost;
		}*/

		/*public virtual void ApplySpeedEffect(ref Vector3 velocity, float dt, StateKCC state)
		{
			if (GetSpeedBoostMultiplier(out float multi))
				velocity *= multi;
		}*/

		public Vector3 GetGroundNormal(Vector3 velocity)
		{
			if (velocity.magnitude > 0f && Motor.GroundingStatus.SnappingPrevented)
			{
				// Take the normal from where we're coming from
				Vector3 groundPointToCharacter = Motor.TransientPosition - Motor.GroundingStatus.GroundPoint;
				if (Vector3.Dot(velocity, groundPointToCharacter) >= 0f)
					return Motor.GroundingStatus.OuterGroundNormal;

				return Motor.GroundingStatus.InnerGroundNormal;
			}

			return Motor.GroundingStatus.GroundNormal;
		}


		public virtual void UpdateOverlaps()
		{
			water    = null;
			hasWater = false;

			waterJet    = null;
			hasWaterJet = false;

			launchPad    = null;
			hasLaunchPad = false;

			numPlatformingVolumes = 0;

			if (WaterMask != 0) {


				int overlapCount = Motor.CharacterOverlap(transform.position, transform.rotation, _tmpWaterVolumes, WaterMask, QueryTriggerInteraction.Collide);
				water = overlapCount == 0 ? null : _tmpWaterVolumes[0];

				var sphereOffset = new Vector3(0f, 0.65f, 0f);
				if (!Physics.CheckSphere(transform.position + sphereOffset, 0.05f, WaterMask)) {
					water = null;
				}

				hasWater = water != null;
			}

			if (PlatformingPhysMask != 0) {

				int overlapCount = Motor.CharacterOverlap(transform.position, transform.rotation, _tmpPlatformingPhys, PlatformingPhysMask, QueryTriggerInteraction.Collide);

				for (int i = 0; i < overlapCount; i++) {
					Collider collider = _tmpPlatformingPhys[i];

					// Launch Pads
					if (!hasLaunchPad && collider.TryGetComponent(out LaunchPad pad)) {
						hasLaunchPad = true;
						launchPad    = pad;
					}

					// Water Jets
					if (!hasWaterJet && collider.TryGetComponent(out WaterJet jet)) {
						Vector3 feetPos = transform.position;
						Vector3 headPos = feetPos + Motor.CharacterTransformToCapsuleTop;

						if(collider == jet.StreamCollider || collider == jet.HeadCollider && headPos.y >= jet.HeadYMinPos.y && feetPos.y <= jet.HeadYMaxPos.y) {
							waterJet    = jet;
							hasWaterJet = true;
						}
					}

					if (numPlatformingVolumes < PLATFORMING_VOLUME_ARRAY_SIZE && collider.TryGetComponent(out PlatformingVolume volume)) {
						platformingVolumes[numPlatformingVolumes++] = volume;
					}

					OnPlatformingPhysicsOverlap(collider);
				}
			}
		}

		public virtual void OnPlatformingPhysicsOverlap(Collider collider)
		{

		}

		public void Jump()
		{
			Jump(MaxJumpHeight);
		}

		public void Jump(float height)
		{
			_delayedMovements.Enqueue(new DelayedVelocityChange(set: Vector3.up * MathUtil.CalculateJumpForce(height, currentState.Gravity) * GetJumpHeightModifier()));
			Motor.ForceUnground();
		}

		public void Jump(ref Vector3 currentVelocity, float jumpHeight, bool delayed = false)
		{
			if (delayed)
			{
				_delayedMovements.Enqueue(new DelayedVelocityChange(yset: MathUtil.CalculateJumpForce(height, currentState.Gravity) * GetJumpHeightModifier()));
			}
			else
			{
				currentVelocity.y = MathUtil.CalculateJumpForce(jumpHeight * GetJumpHeightModifier(), currentState.Gravity);
			}

			Motor.ForceUnground();
		}

		public void Jump(ref Vector3 currentVelocity, float jumpHeight, float jumpHorzSpeed, Vector3 jumpDirection, bool delayed = false, bool heightCalc = true)
		{
			if (delayed)
			{
				Dbg.LogWarning("Delayed horizontal jump not implemented", LogContext.Overworld, LogPriority.High);
				_delayedMovements.Enqueue(new DelayedVelocityChange(yset: MathUtil.CalculateJumpForce(height, currentState.Gravity) * GetJumpHeightModifier()));
			}
			else
			{
				if (heightCalc)
				{
					currentVelocity.y = MathUtil.CalculateJumpForce(jumpHeight * GetJumpHeightModifier(), currentState.Gravity);
				}

				currentVelocity += jumpDirection * jumpHorzSpeed;
			}

			Motor.ForceUnground();
		}

		/// <summary>
		/// Useful vector pointing either towards the joystick or the facing direction when the joystick is idle.
		/// </summary>
		public Vector3 JoystickOrFacing
		{
			get
			{
				if (inputs.hasMove) return inputs.move;
				return facing;
			}
		}

		/// <summary>
		/// Vector pointing either towards the velocity or the facing direction when we are not moving.
		/// </summary>
		public Vector3 VelocityOrFacing
		{
			get
			{
				if (velocity.sqrMagnitude > Mathf.Epsilon) return velocity;
				return facing;
			}
		}

		/// <summary>
		/// Useful dot product to know how much the joystick is pointing towards the facing direction.
		/// dot = 0.0 : The joystick is pushed to the opposite of the facing direction.
		/// dot = 0.5 : The joystick is pushed 90 degree to the side of the jump direction.
		/// dot = 1.0 : The joystick is pushed perfectly towards the facing direction.
		/// </summary>
		public float NormalizedJoystickFacingDot
		{
			get
			{
				// How much towards the current facing direction we are pushing the joystick:
				if (inputs.moveMagnitude < 0.1f)
					return 1;

				float angleDot = Vector3.Dot(inputs.move, facing); // [-1, 1]
				angleDot += 1;                                     // [0, 2]
				angleDot *= 0.5f;                                  // [0, 1]

				return angleDot;
			}
		}

		protected abstract void RegisterStates();

		protected abstract StateKCC GetNextState(ref Vector3 currentVelocity, float deltaTime);

		/// <summary>
		/// Callback to be implemented to group repetitive and similar functionalities around state changes
		/// that would otherwise clutter up GetNextState().
		/// </summary>
		protected virtual void OnStateTransition([CanBeNull] StateKCC prev, [CanBeNull] ref StateKCC next) { }

		public override void ClearVelocity(bool x = true,
			bool                                y = true,
			bool                                z = true
		)
		{
			Vector3 value = velocity;

			if (x) value.x = 0;
			if (y) value.y = 0;
			if (z) value.z = 0;

			// note: unsafe if we enqueue a lot of things prior. Cannot be used twice in a row.
			_delayedMovements = new Queue<DelayedVelocityChange>();
			_delayedMovements.Enqueue(new DelayedVelocityChange(set: value));
		}

		public override void Teleport(Vector3 pos)
		{
			Motor.SetPosition(pos);
			ChangeState(GetDefaultState());
			ClearVelocity();
			OnTeleport?.Invoke(pos);
		}

		[Button]
		public void SetPhysicsEnabled(bool enabled = true)
		{
			physicsEnabled = enabled;
			if (enabled) {
				Motor.SetPosition(transform.position);
				Motor.SetRotation(transform.rotation);

				Motor.enabled    = true;
				Body.isKinematic = false;

			} else {
				Motor.enabled    = false;
				Body.isKinematic = true;
			}
		}

		public void Reset()
		{
			ChangeState(GetDefaultState());
			ClearVelocity();
		}

		public override void Reorient(Quaternion rot)
		{
			Motor.SetRotation(rot);
			facing = rot * Vector3.forward;

			// For some reason, Motor.SetRotation doesn't work every time..? This will ensure that the reorient facing is set without fail
			if (_delayedFacing == null) _delayedFacing = new Queue<DelayedFacingChange>();
			_delayedFacing.Enqueue(new DelayedFacingChange(facing));
		}

		[Button, TitleGroup("Debug")]
		public override void AddForce(Vector3 force, bool setY = false, bool setXZ = false)
		{
			if (!setY && !setXZ)
			{
				_delayedMovements.Enqueue(new DelayedVelocityChange(force));
				return;
			}

			Vector3 add = Vector3.zero;
			Vector3 set = Vector3.zero;

			if (setY)
			{
				set.y = force.y;

				if (set.y > Mathf.Epsilon)
					Motor.ForceUnground();
			}
			else
			{
				add.y = force.y;
				set.y = velocity.y;

				if (velocity.y + add.y > Mathf.Epsilon)
					Motor.ForceUnground();
			}


			if (setXZ)
			{
				set.x = force.x;
				set.z = force.z;
			}
			else
			{
				add.x = force.x;
				add.z = force.z;
				set.x = velocity.x;
				set.z = velocity.z;
			}

			// if (setY || setXZ)
			// {
			// 	if (force.y > 0) Motor.ForceUnground();
			// 	_delayedMovements.Enqueue(new DelayedVelocityChange(set: force));
			//
			// 	if (CurrentVelocity.y + force.y > 0) Motor.ForceUnground();
			// 	_delayedMovements.Enqueue(new DelayedVelocityChange(set: force + Vector3.up * (CurrentVelocity.y + force.y)));
			// }

			_delayedMovements.Enqueue(new DelayedVelocityChange(set: set));
			_delayedMovements.Enqueue(new DelayedVelocityChange(add));
		}

		public bool CanBounce => true;

		public virtual void OnBounce(BounceInfo info)
		{
			Vector3 force = info.AffectVelocity(velocity, this);
			_delayedMovements.Enqueue(new DelayedVelocityChange(set: force));
			if (force.y > 0.1f)
				Motor.ForceUnground();
		}

		// /// <summary>
		// /// Instantiate a particle prefab and automatically add a DestroyOnInactiveParticles component if it is missing.
		// /// </summary>
		// public void InstantiateParticles(ParticlePrefab particleRef, Vector3? pos = null, float angleLimit = 0)
		// {
		// 	pos = pos ?? Position;
		// 	particleRef.Instantiate(transform, pos.Value, angleLimit);
		// }

		public float CalculateJumpForce(float height)
		{
			return MathUtil.CalculateJumpForce(height * GetJumpHeightModifier(), currentState.Gravity);
		}

		public float GetJumpHeightModifier()
		{
			float heightMod = 1;
			if (gravityVolume)
				heightMod /= gravityVolume.GravityScale;
			return heightMod;
		}

		public float GetGravity()
		{
			float mod = 1;
			if (gravityVolume)
				mod = gravityVolume.GravityScale;
			return GravityScalable * mod;
		}

		/// <summary>
		/// A change in facing to be processed in a future frame.
		/// </summary>
		public readonly struct DelayedFacingChange
		{
			public readonly Vector3 set;

			public DelayedFacingChange(Vector3 set)
			{
				this.set = set;
			}
		}

		/// <summary>
		/// A change in velocity to be processed in a future frame.
		/// </summary>
		public readonly struct DelayedVelocityChange
		{
			public readonly Vector3? add;
			public readonly Vector3? scale;
			public readonly float?   xset;
			public readonly float?   yset;
			public readonly float?   zset;

			public DelayedVelocityChange(
				Vector3? add   = null,
				Vector3? scale = null,
				Vector3? set   = null,
				float?   xset  = null,
				float?   yset  = null,
				float?   zset  = null
			)
			{
				this.add   = add;
				this.scale = scale ?? Vector3.one;
				this.xset  = set?.x ?? xset;
				this.yset  = set?.y ?? yset;
				this.zset  = set?.z ?? zset;
			}
		}
	}

	/// <summary>
	/// A state for a KCC character.
	/// By default is both air and ground at once based on the grounding state of the motor.
	/// </summary>
	[Serializable]
	public abstract class StateKCC
	{
		[NonSerialized] public int             id;
		[NonSerialized] public ActorKCC        actor;
		[NonSerialized] public PlayerActor     player;

		[NonSerialized] public bool            active;
		[NonSerialized] public bool            justActivated;
		[NonSerialized] public CharacterInputs inputs;
		[NonSerialized] public AirMetrics      airMetrics;

		public bool hasPlayer = false;

		// Shortcuts
		protected KinematicCharacterMotor Motor => actor.Motor;

		protected TimeScalable timeScale => actor.timeScale;

		/// <summary>
		/// Sharpness indicating how fast the actor should turn to the target facing direction.
		/// Used for default turn behavior with sharpness.
		/// </summary>
		protected virtual float TurnSpeed => actor.DefaultTurnSharpness;

		/// <summary>
		/// Desired direction that the character should face eventually.
		/// Used for default turn behavior with sharpness.
		/// </summary>
		protected virtual Vector3 TurnDirection => actor.JoystickOrFacing;

		/// <summary>
		/// Is the character grounded during this state?
		/// </summary>
		public virtual bool IsGround => Motor.GroundingStatus.FoundAnyGround;

		/// <summary>
		/// Is the character airborn during this state?
		/// </summary>
		public virtual bool IsAir => !Motor.GroundingStatus.FoundAnyGround;

		public virtual float Gravity => actor.Gravity;

		/// <summary>
		/// Callback to be implemented to prime up the state when it is activated.
		/// </summary>
		public virtual void OnActivate() { }

		/// <summary>
		/// Callback to be implemented to clean up internal values when it is deactivated.
		/// </summary>
		public virtual void OnDeactivate() { }

		/// <summary>
		/// Update function called regardless of the active status of this state.
		/// Verify whether or not the state is active to get the correct behavior.
		/// </summary>
		/// <param name="dt"></param>
		public virtual void OnUpdate(float dt) { }

		/// <summary> velocity of the character for this state.
		/// </summary>
		/// Update the current
		public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) { }

		/// <summary>
		/// Update the current rotation of the KCC character for this state.
		/// Return true to override TurnSharpness and TargetFacingDirection.
		/// </summary>
		/// <returns>Whether or not we should override the default turn behavior with sharpness.</returns>
		public virtual void UpdateFacing(ref Vector3 facing, float dt)
		{
			Vector3 targetFacingDirection = TurnDirection;

			if (targetFacingDirection.sqrMagnitude > Mathf.Epsilon) // Just in case we get a shitty vector with magnitude == 0
			{
				float turnSharpness = TurnSpeed;
				MathUtil.SlerpWithSharpness(ref facing, targetFacingDirection, turnSharpness, dt);
			}
		}

		public virtual void AfterCharacterUpdate(float dt) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator bool(StateKCC state) => state != null && state.active;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator int(StateKCC state) => state.id;

		protected void UpdateAir(ref Vector3 currentVelocity, float dt, float moveSharpness = 3)
		{
			// Usually AirState inheritors should instead override UpdateHorizontalMomentum and UpdateVerticalMomentum.
			var currentAxes = new SplitAxisVector(currentVelocity);

			Vector3 newHorizontal = currentAxes.horizontal;
			Vector3 newVertical   = currentAxes.vertical;

			UpdateHorizontal(ref newHorizontal, dt);
			UpdateVertical(ref newVertical, dt);

			// Lerp the horizontal axis.
			newHorizontal = MathUtil.LerpWithSharpness(currentAxes.horizontal, newHorizontal, moveSharpness, dt);
			newHorizontal = Vector3.ClampMagnitude(newHorizontal, actor.AirMaxSpeed);

			currentVelocity = newHorizontal + newVertical;
			// currentVelocity = Vector3.ClampMagnitude(currentVelocity, actor.AirMaxSpeed);
		}

		public void ApplyAirPhysics(ref Vector3 velocity, Vector3 gravity, float dragMultiplier = 1)
		{
			velocity += gravity * timeScale;

			float drag = 1f / (1f + actor.AirDrag * dragMultiplier * Time.deltaTime);
			velocity.x *= drag;
			velocity.z *= drag;
		}

		protected void ReorientToGround(ref Vector3 horizontal)
		{
			// Prevent air movement from making you move up steep sloped walls
			if (Motor.GroundingStatus.FoundAnyGround)
			{
				Vector3 perpendicularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
				horizontal = Vector3.ProjectOnPlane(horizontal, perpendicularObstructionNormal);
			}
		}

		protected virtual void UpdateHorizontal(ref Vector3 hvel, float dt)
		{
			hvel = actor.inertia.vector;
			ReorientToGround(ref hvel);
		}

		protected virtual void UpdateVertical(ref Vector3 vel, float dt)
		{
			ApplyAirPhysics(ref vel, actor.GravityDirection * Gravity);
		}

		protected void AddSurfaceAcceleration(ref Vector3 currentVelocity)
		{
			if (actor.hasSurface)
			{
				Surface surface = actor.surface;
				if (surface.Acceleration.magnitude > 0)
				{
					switch (surface.AccelerationDirection)
					{
						case Surface.AccelerationDirections.LocalSlope:
							Vector3 forward = actor.slopeDir;
							Vector3 right   = Vector3.Cross(forward, actor.Up);
							Vector3 up      = Vector3.Cross(Vector3.right, forward);

							currentVelocity += forward * surface.Acceleration.z +
							                   right * surface.Acceleration.x +
							                   up * surface.Acceleration.y;
							break;

						case Surface.AccelerationDirections.World:
							break;

						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}
		}

		protected void RotateHoriz(
			ref Vector3 hdir,
			Vector3     steerDir,
			float       steerSpeed,
			float       dt)
		{
			var quaternion = Quaternion.RotateTowards(
				Quaternion.LookRotation(hdir.normalized),
				Quaternion.LookRotation(steerDir),
				steerSpeed * dt
			);

			hdir = quaternion * Vector3.forward;
		}
	}

}