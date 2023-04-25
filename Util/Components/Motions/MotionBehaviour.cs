using System;
using System.Collections;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Scripting;
using Anjin.Util;
using Assets.Scripts;
using Assets.Scripts.Utils;
using Combat;
using Combat.Data;
using Combat.Skills.Generic;
using Combat.Toolkit;
using DG.Tweening;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Util;
using Util.UniTween.Value;

namespace Anjin.Utils
{
	/// <summary>
	/// A motion to move an object towards a followed object.
	/// </summary>
	public sealed class MotionBehaviour : MonoBehaviour
	{
		private const float PROXIMITY_INTERSECTION_DISTANCE = 0.75f;

		public enum State { None, Main, Stay, Exit }

		// Config
		// ----------------------------------------
		[SerializeField]
		public MotionDef Main = MotionDef.Default;

		[ShowIf("EnableStay")]
		[SerializeField]
		public MotionDef Stay = MotionDef.Lock;

		[SerializeField]
		public MotionDef Exit = MotionDef.Lock;

		[ShowIf("IsMainInternal")]
		[SerializeField]
		public WorldPoint2 Follow;

		[ShowIf("IsMainInternal")]
		[SerializeField]
		public ContactCallback Reach;

		[Tooltip("Makes it so the object self-destructs after exiting.")]
		public bool AutoDestroy = true;

		/// <summary>
		/// Makes it so the motion 'stays' on forever once main finishes.
		/// </summary>
		[Tooltip("Enable the stay state that will be used when the main state finishes.")]
		public bool EnableStay = false;

		[NonSerialized, CanBeNull] public MotionPath                    mainPath;
		[NonSerialized, CanBeNull] public MotionPath                    stayPath;
		[NonSerialized, CanBeNull] public MotionPath                    exitPath;
		[NonSerialized]            public List<TargetedContactCallback> contacts;
		[NonSerialized]            public List<TargetedContactCallback> contactsProximity;
		[NonSerialized]            public List<TargetedContactCallback> contactsUnity;

		[NonSerialized, CanBeNull] public GameObject from;
		[NonSerialized, CanBeNull] public WorldPoint impactPoint;

		// State
		// ----------------------------------------
		[NonSerialized] public Battle    battle;
		[NonSerialized] public ProcTable procs;
		[NonSerialized] public Proc      proc;

		/// <summary>
		/// Do not assign directly.
		/// </summary>
		[NonSerialized]
		public State state;

		private TimeScalable _timescale;
		private FxInfo       _origin;
		private bool         _hasOrigin;
		private ParticleRef  _selfParticles;
		private CustomMotion _customMotion;

		private int         _waypoint;
		private Vector3     _startPosition;
		private MotionDef   _motion;
		private WorldPoint2 _target;
		private WorldPoint2 _lastTarget;
		private MotionPath  _path;

		private TweenableVector3 _position;
		private Vector3          _velocity;
		private Vector3          _lastPos;
		private float            _traveledDist;

		private Transform[] _childTransforms;
		private bool        _motionStarted;


		private Vector3 _followOffset = Vector3.zero;

		public int LastWaypoint => _path.waypoints.Count - 1;

		public bool    IsCombatParticle;
		public Vector3 TargetPos => GetTargetPosition(ref _target);

		public FighterActor Owner => _target.P2.actor as FighterActor == null
			? _target.P1.actor as FighterActor
			: _target.P2.actor as FighterActor;

		private void Awake()
		{
			_childTransforms = GetComponentsInChildren<Transform>();

			contacts          = new List<TargetedContactCallback>();
			contactsProximity = new List<TargetedContactCallback>();
			contactsUnity     = new List<TargetedContactCallback>();

			_position      = new TweenableVector3();
			_selfParticles = new ParticleRef(gameObject);

			if (TryGetComponent(out _customMotion))
				_customMotion.motion = this;

			_timescale           =  gameObject.GetOrAddComponent<TimeScalable>();
			_timescale.onRefresh += OnTimescaleRefresh;
		}

		public void Start()
		{
			if (state == State.None) // Could already have been started externally
				Play();
		}

		public void Play(bool set_start_pos = false)
		{
			_hasOrigin = gameObject.TryGetComponent(out _origin);

			StartMotion(set_start_pos);
			_lastPos = transform.position;
		}

		public void Reset()
		{
			state = State.None;

			_position.StopIfTweening();
			_position.value = transform.position;
			_motion         = new MotionDef();
			_target         = new WorldPoint2();
			_path           = null;
			_velocity       = Vector3.zero;

			mainPath = null;
			stayPath = null;
			exitPath = null;

			contacts.Clear();
			contactsProximity.Clear();
			contactsUnity.Clear();
		}

		public void Stop()
		{
			state = State.None;
		}

		private void OnTriggerEnter(Collider other)
		{
			foreach (TargetedContactCallback fcc in contactsUnity)
			{
				if (fcc.t_object == other.gameObject && fcc.uevent == TargetedContactCallback.UnityEvents.Enter)
				{
					Contact(ref fcc.callback);
					return;
				}
			}
		}

		private void OnTriggerExit(Collider other)
		{
			foreach (TargetedContactCallback fcc in contactsUnity)
			{
				if (fcc.t_object == other.gameObject && fcc.uevent == TargetedContactCallback.UnityEvents.Exit)
				{
					Contact(ref fcc.callback);
					return;
				}
			}
		}

		private void StartMotion(bool set_start_pos = false)
		{
			_startPosition = transform.position;
			_target        = new WorldPoint2(transform);

			mainPath?.Init();
			stayPath?.Init();
			exitPath?.Init();

			if (contactsUnity.Count > 0)
			{
				// Already has collider
				if (!TryGetComponent(out Collider collider))
				{
					// Create sphere collider
					SphereCollider sphere = gameObject.AddComponent<SphereCollider>();
					sphere.radius = 0.5f;
					collider      = sphere;
				}

				if (!TryGetComponent(out Rigidbody rb))
					rb = gameObject.AddComponent<Rigidbody>();

				// Important! we need these to function at all
				collider.isTrigger        = true;
				rb.isKinematic            = true;
				rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			}

			ChangeState(State.Main, ref Follow, set_start_pos);
		}

		private void OnTimescaleRefresh(float dt, float scale)
		{
			if (_position.activeTween != null)
				_position.activeTween.timeScale = scale == 0 && IsCombatParticle ? 1 : scale;
		}

		#region Lua

		public void ConfigureTB([NotNull] Table tbl)
		{
			Reset();

			mainPath = MotionPath.FromTB(tbl);
			if (tbl.TryGet("main", out Table tbmain)) mainPath = MotionPath.FromTB(tbmain);
			if (tbl.TryGet("stay", out Table tbstay)) stayPath = MotionPath.FromTB(tbstay);
			if (tbl.TryGet("exit", out Table tbexit)) exitPath = MotionPath.FromTB(tbexit);

			ConfigureContactsTB(tbl);

			if (tbl.TryGet("lock_offset_x", out float x)) _followOffset.x += x;
			if (tbl.TryGet("lock_offset_y", out float y)) _followOffset.y += y;
			if (tbl.TryGet("lock_offset_z", out float z)) _followOffset.z += z;
			if (tbl.TryGet("from", out Fighter from)) this.from           =  from.actor.gameObject;
			if (tbl.TryGet("impact", out Fighter impact)) impactPoint     =  new WorldPoint(impact.actor.gameObject);
		}

		private void ConfigureContactsTB([NotNull] Table tbl)
		{
			for (var i = 1; i <= tbl.Length; i++)
			{
				DynValue dv = tbl.Get(i);

				if (dv.AsObject(out TargetedContactCallback fct))
				{
					fct.InitCache(gameObject);

					switch (fct.type)
					{
						case TargetedContactCallback.Type.Proximity:
							fct.id = contactsProximity.Count;
							contactsProximity.Add(fct);
							break;

						case TargetedContactCallback.Type.Unity:
							fct.id = contactsUnity.Count;
							contactsUnity.Add(fct);
							break;

						default:
							fct.id = contacts.Count;
							contacts.Add(fct);
							break;
					}
				}
				else if (dv.AsObject(out Table tb)) ConfigureContactsTB(tb);
			}
		}

		#endregion

		#region Internal

		private void ChangeState(State state, ref WorldPoint2 target, bool set_start_pos = false)
		{
			_target       = target;
			_waypoint     = -1;
			_traveledDist = 0;

			switch (state)
			{
				case State.Main:
					SetParticles(true);
					_motion = Main;
					_path   = mainPath;
					break;

				case State.Stay:
					SetParticles(true);
					_motion = Stay;
					_path   = stayPath;
					break;

				case State.Exit:
					SetParticles(false);
					_motion = Exit;
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(state), state, null);
			}

			if (_path == null)
			{
				StartMotion(ref _motion, ref _target);
			}
			else
			{
				if (_path.defaultDef.HasValue)
					_motion = _path.defaultDef.Value;

				// Start override waypoints
				// ----------------------------------------
				int len = _path.waypoints.Count;

				if (set_start_pos)
				{
					SetWaypoint(0);
					if (len >= 2)
						StartMotion(1);
				}
				else
				{
					if (len >= 2) StartMotion(1);
					else if (len >= 1) StartMotion(0);
				}
			}

			if (_target.IsValid())
			{
				this.state = state;
			}
		}

		private void InitWaypoint(MotionPoint wp)
		{
			WorldPoint2 tmp = _target;

			_motion = wp.motion ?? _path.defaultDef ?? MotionDef.Prev;

			_target = wp.target;
			if (_target.Type == WorldPoint2.Types.Previous)
				_target = _lastTarget;

			if (!_target.IsValid())
				_target = new WorldPoint2(transform);

			_lastTarget = tmp;
		}

		private void SetWaypoint(int i)
		{
			MotionPoint wp = _path.waypoints[i];

			InitWaypoint(wp);
			transform.position = GetTargetPosition(ref wp.target);
			_lastPos           = transform.position;
		}

		private void StartMotion(int i)
		{
			MotionPoint wp = _path.waypoints[i];
			_waypoint = i;
			StartMotion(ref wp);
		}

		private void StartMotion(ref MotionPoint wp)
		{
			InitWaypoint(wp);
			StartMotion(ref _motion, ref _target);
		}

		private void StartMotion(ref MotionDef motion, ref WorldPoint2 target)
		{
			switch (motion.Type)
			{
				case Motions.Tween when target.IsValid():
					_position.StopIfTweening();
					_position
						.FromTo(transform.position, GetTargetPosition(ref target), motion.Tween)
						.OnComplete(OnReach);

					motion.Tween.speedBased = motion.SpeedBased;
					break;


				case Motions.Custom:
					_customMotion.OnMotionStart();
					break;
			}

			_traveledDist = 0;
		}

		private void UpdateMotion(ref MotionDef def, ref WorldPoint2 target)
		{
			Vector3 tpos            = GetTargetPosition(ref target);
			Vector3 lastpos         = transform.position;
			bool    velocityUpdated = false;

			switch (def.Type)
			{
				case Motions.None:
					transform.position = _lastPos;
					break;

				case Motions.Lock when target.IsValid():
					transform.position = tpos + _followOffset;
					break;

				case Motions.Accelerator when target.IsValid():
					// Get distance to target
					def.Speed += _timescale.deltaTime * def.Acceleration;

					float distance = Vector3.Distance(transform.position, tpos);
					float speed    = def.Speed * _timescale.deltaTime;

					if (distance < speed)
					{
						// We've reached it!
						OnReach();
						break;
					}

					// Continue moving towards goalpos
					transform.Translate(speed * transform.position.Towards(tpos), Space.World);
					break;

				case Motions.Tween when target.IsValid():
					if (_position.activeTween != null)
					{
						// ((Tweener)_position.activeTween).ChangeEndValue(GetEndPosition(ref target));
						_position.activeTween.timeScale = _timescale.current;
					}

					transform.position = _position.value;
					break;

				case Motions.Damper:
					transform.position = transform.position.LerpDamp(tpos, def.Damping);
					break;

				case Motions.SmoothDamp:
					Vector3 pos = transform.position;
					pos.x = Mathf.SmoothDamp(pos.x, tpos.x, ref _velocity.x, def.SmoothTime, def.MaxSpeed, _timescale.deltaTime);
					pos.y = Mathf.SmoothDamp(pos.y, tpos.y, ref _velocity.y, def.SmoothTime, def.MaxSpeed, _timescale.deltaTime);
					pos.z = Mathf.SmoothDamp(pos.z, tpos.z, ref _velocity.z, def.SmoothTime, def.MaxSpeed, _timescale.deltaTime);

					if (!pos.AnyNAN())
						transform.position = pos;

					velocityUpdated = true;
					break;

				case Motions.Custom:
					_customMotion.OnMotionUpdate();
					break;
			}

			// Calculate velocity so we can still use it for other things
			if (!velocityUpdated)
				_velocity = transform.position - lastpos;

			if (_velocity.AnyNAN())
				_velocity = Vector3.zero;
		}

		/// <summary>
		/// Internal reach event for the built-in single path motions. (not used for custom)
		/// See OnUpdate for uses
		/// </summary>
		public void OnReach()
		{
			Finish("reach", ref _target, ref Reach);
		}

		/// <summary>
		/// Terminate the motion.
		/// </summary>
		private void OnTerminate()
		{
			if (AutoDestroy)
				FX.Destroy(gameObject, "MotionBehaviour.AutoDestroy");
		}

		private void Update()
		{
			if (state == State.None) return;
			if (state == State.Exit && !_selfParticles.AnyAlive)
				OnTerminate();

			UpdateMotion(ref _motion, ref _target);

			// Proximity contacts
			// ----------------------------------------
			for (var i = 0; i < contactsProximity.Count; i++)
			{
				TargetedContactCallback fct = contactsProximity[i];

				// Debug.Log(fct.gobjectCheck);
				Vector3 ctpos = fct.Position;

				// Check overlap
				// ----------------------------------------
				float dist = MathUtil.DistanceToLine(ctpos, _lastPos, transform.position);

				// float dist   = Vector3.Distance(contactpos, transform.position);
				float radius = fct.Radius ?? PROXIMITY_INTERSECTION_DISTANCE;

				// Debug.Log($"{fct.gobjectCheck.name} : {dist} < {radius}", fct.gobjectCheck);

				bool overlaped = dist < radius;
				if (!overlaped)
				{
					fct.overlaped = false;
				}
				else if (!fct.overlaped)
				{
					// Debug.Log("OVERLAPED DETECTED");
					fct.overlaped = true;

					Contact(ref fct.callback, ctpos);
				}
			}

			_traveledDist = Vector3.Distance(_lastPos, transform.position);
			_lastPos      = transform.position;
		}


		private void SetParticles(bool b)
		{
			_selfParticles.SetPlaying(b);
		}

		#endregion

		#region API

		public Vector3 GetTargetPosition(ref WorldPoint2 target)
		{
			switch (target.Type)
			{
				case WorldPoint2.Types.Previous: // This is just in case, it should have been auto-replaced already. so just return our current pos.
					Debug.LogError("Something weird has befallen the atmosphere, thy should not have reached this location. please record this occurence to the wizard association");

					return transform.position;

				case WorldPoint2.Types.Point:
					return target.P1.GetFxPosition(_origin, _hasOrigin);

				case WorldPoint2.Types.Midpoint:
					return Vector3.Lerp(
						target.P1.GetFxPosition(_origin, _hasOrigin),
						target.P2.GetFxPosition(_origin, _hasOrigin),
						target.Midpoint);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}


		#region Contact Stuff

		/// <summary>
		/// Trigger a motion contact.
		/// </summary>
		public void Contact(string contact_id, ref WorldPoint2 target, ref ContactCallback ct)
		{
			Contact(ref ct);
			Contact(ref target);
			Contact(contact_id);
		}

		/// <summary>
		/// Trigger a motion contact.
		/// </summary>
		public void Contact(ref ContactCallback ct, Vector3? pos = null)
		{
			pos = pos ?? transform.position;

			// PROCS
			if ((ct.Features & ContactFeatures.Proc) != 0)
			{
				if (!string.IsNullOrWhiteSpace(ct.ProcID))
					Proc(ct.ProcID);
				else if (ct.proc == null) // This acts as an override
					Proc();
			}

			// Explicit proc always applied, regardless of the features enabled
			if (ct.proc != null)
				battle?.Proc(ct.proc);

			// PARTICLES
			if ((ct.Features & ContactFeatures.Particles) != 0 && ct.SpawnParticles != null)
			{
				GameObject go = Instantiate(ct.SpawnParticles, pos.Value, Quaternion.identity);
				go.AddComponent<ParticleAutoDestroyer>();

				var pr = new ParticleRef(go);
				pr.SetPlaying();
			}

			// SFX
			if ((ct.Features & ContactFeatures.Sound) != 0)
			{
				GameSFX.Play(ct.SFX, pos.Value);
			}

			// LUA
			if (ct.luaClosure != null)
			{
				Lua.Invoke(ct.luaClosure);
			}

			// SELF-DESTRUCT
			if ((ct.Features & ContactFeatures.DestroySelf) != 0)
			{
				FX.Destroy(gameObject, "MotionContact.DestroySelf");
			}
		}

		/// <summary>
		/// Trigger the external contact handlers by ID.
		/// </summary>
		public void Contact(string contact_id)
		{
			for (var i = 0; i < contacts.Count; i++)
			{
				TargetedContactCallback callback = contacts[i];

				// By single ID
				// ----------------------------------------
				if (callback.t_wpid == contact_id)
				{
					Contact(ref callback.callback);
				}
			}
		}

		/// <summary>
		/// Trigger the external contact handlers for the object.
		/// </summary>
		public void Contact(ref WorldPoint2 target)
		{
			if (!target.IsValid()) return;

			Vector3 pos = GetTargetPosition(ref target);

			void TriggerContacts(GameObject gobject, Vector3 p)
			{
				for (var i = 0; i < contacts.Count; i++)
				{
					TargetedContactCallback callback = contacts[i];
					if (callback.t_object == gobject.gameObject) Contact(ref callback.callback, p);
				}
			}

			if (target.P1.gameobject != null)
			{
				TriggerContacts(target.P1.gameobject, pos);
			}

			if (target.Type == WorldPoint2.Types.Midpoint)
				if (target.P2.gameobject != null)
				{
					TriggerContacts(target.P2.gameobject, pos);
				}
		}


		/// <summary>
		/// Trigger the external contact handlers for the object.
		/// </summary>
		public void Contact(GameObject obj)
		{
			for (var i = 0; i < contacts.Count; i++)
			{
				TargetedContactCallback callback = contacts[i];
				if (callback.t_object == obj)
				{
					Contact(ref callback.callback);
				}
			}
		}

		#endregion

		#region MyRegion

		/// <summary>
		/// Fire the next proc.
		/// </summary>
		public void Proc()
		{
			if (proc != null)
			{
				battle?.Proc(proc);
				proc = null;
				return;
			}

			if (procs.PopNext(out Proc p))
			{
				battle?.Proc(p);
			}
		}

		/// <summary>
		/// Fire the next proc by id.
		/// </summary>
		public void Proc(string id)
		{
			if (procs.Pop(id, out Proc proc))
			{
				battle.Proc(proc);
			}
		}

		/// <summary>
		/// Fire the next proc by index.
		/// </summary>
		/// <param name="id"></param>
// ReSharper disable once UnusedMember.Local
		private void Proc(int id)
		{
			if (procs.Remaining > id)
			{
				battle.Proc(procs[id]);
			}
		}

		/// <summary>
		/// Finish event with a contact.
		/// </summary>
		public void Finish(string id, ref WorldPoint2 target, ref ContactCallback cc)
		{
			Contact(id, ref target, ref cc);

			if (_waypoint != -1 && _waypoint < LastWaypoint)
			{
				_waypoint++;
				StartMotion(_waypoint);
				return;
			}

			if (EnableStay)
				ChangeState(State.Stay, ref _target);
			else
				ChangeState(State.Exit, ref _target); // Continue towards current
		}

		#endregion

		#endregion


		#region UnityEditor stuff

#if UNITY_EDITOR
		[UsedImplicitly]
		private bool IsMainInternal => Main.Type != Motions.Custom;

		[Button, HideInPlayMode]
		[UsedImplicitly]
		public void Test()
		{
			EditorApplication.EnterPlaymode();
			AutoDestroy = false;
		}

		[Button, HideInEditorMode]
		[UsedImplicitly]
		public void Replay()
		{
			transform.position = _startPosition;
			StartMotion(false);
		}
#endif

		#endregion
	}
}