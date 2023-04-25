using System;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Scripting;
using Anjin.Util;
using Anjin.Utils;
using Assets.Drawing;
using Assets.Scripts.Utils;
using Combat.Data;
using Combat.Skills.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Controllers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Util.Addressable;
using Object = UnityEngine.Object;

namespace Combat.Toolkit
{
	public class FXUpdater : MonoBehaviour
	{
		[NonSerialized]
		public FX fx;

		public void Update()
		{
			if (fx != null)
			{
				fx.UpdateState();
			}
		}
	}

	/// <summary>
	/// A struct for implementing a FX into an existing
	/// architecture. FX is a consistent and efficient API
	/// for spawning and animating particles, special effects,
	/// and projectiles.
	///
	/// - Flipping based on from's facing direction
	///
	/// </summary>
	[LuaUserdata]
	public class FX
	{
		// Spawn settings
		public string     address;
		public GameObject prefab;

		// Fx settings
		[CanBeNull]
		public Table config;
		public  GameObject from;
		public  WorldPoint onto;
		public  bool       staying;
		public  bool       locking;
		private FxInfo     fxinfo;

		// Context
		public Battle    battle;
		public ProcTable procs;

		// State
		public bool       isLoading;
		public bool       isActive;
		public GameObject gameObject;
		public Vector3    lastPosition;
		public float      exitTime;

		private AsyncOperationHandle<GameObject> _handle;
		private GameObject                       _prefab;

		private bool              _spawned;
		private FXUpdater         _updater;
		private bool              _hasMotion;
		private bool              _hasBezierMotion;
		private bool              _hasEffectMaster;
		private bool              _hasTextureSwapper;
		private MotionBehaviour   _motion;
		private TendrilMotion     _curve;
		private TrailEffectMaster _effmaster;
		private ParticleRef       _ref;
		private Vector3?          _lastPosition;
		private PSTextureSwapper  _textureSwapper;

		public FX() { }

		public FX(GameObject prefab) : this()
		{
			this.prefab = prefab;
		}

		public FX(string address) : this()
		{
			this.address = address;
		}

		public FX(string address, Vector3 pos) : this()
		{
			this.address = address;
			this.onto    = pos;
		}

		public bool IsExiting => _hasMotion && _motion.state == MotionBehaviour.State.Exit;


		public async UniTask<GameObject> Start(GameObject prefab = null)
		{
			prefab = prefab ? prefab : this.prefab;

			// Load prefab
			// ----------------------------------------
			if (prefab != null)
			{
				isActive = true;
			}
			else if (address != null)
			{
				isActive = true;

				isLoading = true;
				prefab    = await Addressables.LoadAssetAsync<GameObject>(address);
				isLoading = false;
			}

			if (prefab == null)
				return null;

			ReadConfig();

			// Spawn
			gameObject  = Object.Instantiate(prefab);
			_updater    = gameObject.AddComponent<FXUpdater>();
			_updater.fx = this;
			_spawned    = true;

			fxinfo = gameObject.GetComponent<FxInfo>();

			_ref = new ParticleRef(gameObject);

			_hasMotion         = gameObject.TryGetComponent(out _motion);
			_hasBezierMotion   = gameObject.TryGetComponent(out _curve);
			_hasEffectMaster   = gameObject.TryGetComponent(out _effmaster);
			_hasTextureSwapper = gameObject.TryGetComponent(out _textureSwapper);

			if (_hasEffectMaster)
			{
				SetPositionToOnto();
				_effmaster.Play().Forget();
			}
			else
			{
				UpdateConfig(true);
			}

			return gameObject;
		}

		private void ReadConfig()
		{
			if (config != null)
			{
				config.TryGet("from", out from, from);
				config.TryGet("onto", out onto, onto);
				config.TryGet("locking", out locking, locking);
				config.TryGet("staying", out staying, staying);
			}
		}

		public void UpdateConfig(Table conf)
		{
			locking     = false;
			staying     = false;
			this.config = conf;
			@from       = null;
			onto        = WorldPoint.Default;
			ReadConfig();
			UpdateConfig(false);
		}

		private void UpdateConfig(bool just_spawned)
		{
			var ontoObj = onto.ToGameObject();

			// Motion
			// ----------------------------------------

			// Add motion behavior if it's required by config
			if (!_hasMotion && (config?.Length > 0 || locking))
			{
				_motion    = gameObject.AddComponent<MotionBehaviour>();
				_hasMotion = true;
			}

			if (_hasTextureSwapper)
			{
				UpdatePSTexture();
			}

			if (_hasBezierMotion)
				UpdateBezierMotion();
			else if (_hasMotion)
				UpdateMotion();
			else
			{
				SetPositionToOnto();

				// Auto-destroy only when we're not managed by a motion
				if (_motion == null)
					gameObject.SetAutoDestroyPS(GetDestroyMessage(gameObject, $"ParticleAutoDestroyer"));
			}

			// Flipping based on from's facing direction
			// (subject to change)
			// ------------------------------------------
			if (from != null && from.TryGetComponent(out ActorBase fromActor) && fromActor.facing.x > 0)
			{
				Vector3 scale = gameObject.transform.localScale;
				if (!_hasBezierMotion)
					scale.x *= -1;

				Transform fliproot = gameObject.transform;
				if (fxinfo != null && fxinfo.FlipRoot != null)
					fliproot = fxinfo.FlipRoot;

				fliproot.localScale = scale;
			}

			void UpdateBezierMotion()
			{
				_curve.Reset();

				if (config != null)
				{
					_curve.Configure(config);
				}

				_curve.Play();
				_ref?.SetPlaying(true);
			}

			void UpdateMotion()
			{
				_motion.Reset();

				_motion.Follow     = onto;
				_motion.battle     = battle;
				_motion.procs      = procs;
				_motion.EnableStay = staying;

				if (config != null)
				{
					_motion.ConfigureTB(config);

					if (config.Length == 0)
					{
						// No MotionBehaviour parameters, set directly onto onto
						SetPositionToOnto();
					}
				}

				// Easy lock with fxl
				if (locking)
					_motion.Main = MotionDef.Lock;

				_motion.Play(just_spawned);
			}
		}


		private void UpdatePSTexture()
		{
			if (config != null)
			{
				_textureSwapper.Configure(config);
			}
		}


		public void UpdateState()
		{
			lastPosition = gameObject.transform.position;
		}

		public bool CheckDeath()
		{
			if (_spawned && gameObject == null)
			{
				_spawned = false;
				isActive = false;
			}

			return isActive;
		}

		public void UpdateTimescale(float scale)
		{
			_ref?.SetTimescale(scale);
		}

		private void SetPositionToOnto()
		{
			// TODO GetOriginPosition directly from WorldPoint to simplify this
			var gobject = onto.ToGameObject();

			gameObject.transform.position = gobject != null
				? gobject.GetOriginPosition(gameObject) + onto.offset
				: onto.Get();

			// Billboard Spawn
			// ----------------------------------------
			if (gobject != null && fxinfo != null && fxinfo.BillboardSpawn)
			{
				// Move towards camera a bit
				Vector3 dir = gobject.transform.position.Towards(GameCams.Live.UnityCam.transform.position);
				gameObject.transform.position += dir * 0.8f;
			}
		}

		public Vector3 GetPosition()
		{
			if (gameObject != null) return gameObject.transform.position;
			if (_lastPosition.HasValue) return _lastPosition.Value;

			Vector3 pos;
			if (onto.TryGet(out pos)) return pos;
			if (from != null) return from.transform.position;

			return Vector3.zero;
		}

		public void Stop(bool immediate = false)
		{
			// DebugLogger.Log("FX.Stop", LogContext.Graphics | LogContext.Combat, LogPriority.Low);

			if (gameObject == null)
				return;

			if (_ref == null)
			{
				Destroy(gameObject, "FX.Stop 1");
			}
			else
			{
				_ref?.SetPlaying(false);
				if (_ref?.AnyAlive == false || immediate)
					Destroy(gameObject, "FX.Stop 2");
			}
		}

		public void Retract()
		{
			if (_curve != null)
			{
				_curve.Retract();
			}
		}

		public void Cleanup()
		{
			Addressables2.ReleaseSafe(_handle);
		}

		public static void Destroy(GameObject o, string label)
		{
			Object.Destroy(o);
			AjLog.LogVisual("--", GetDestroyMessage(o, label));
		}

		public static void DestroyOrReturn([NotNull] GameObject o, string label)
		{
			PrefabPool.DestroyOrReturn(o);
			o.LogVisual("xx", GetDestroyMessage(o, label));
		}

		[NotNull] public static string GetDestroyMessage(GameObject o, string label) => $"fxdestroy({o}) -- {label}";
	}
}