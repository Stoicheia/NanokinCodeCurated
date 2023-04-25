using System;
using Anjin.Actors;
using Anjin.Scripting;
using Anjin.Util;
using Combat.Data.VFXs;
using Combat.Entities;
using DG.Tweening;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;
using Util.Animation;
using Util.UniTween.Value;

namespace Combat.Toolkit
{
	/// <summary>
	/// A move using a tween.
	/// </summary>
	public class CoroutineMove : CoroutineManagedObj
	{
		public DynValue overrideMove;
		public Options  options = Options.Default;

		private WorldPoint goal;

		// Execution
		private FrontMetrics _frontMetrics;

		private TweenableVector3 _position;
		private ActorBase        _actor;
		private AnimVFX          _vfx;
		private Tween            _tween;
		private float            _goalDistanceNow;
		private bool             _started = false;

		private Vector3 _startPosition;

		public CoroutineMove(GameObject self) : base(self) { }

		public override bool  Active           => _tween != null && _tween.active;
		public override float ReportedProgress => _tween.position / _tween.Duration();
		public override float ReportedDuration => _tween.Duration();

		public override bool CanContinue(bool justYielded, bool isCatchup)
		{
			if (_started)
			{
				// Normal end condition
				if (_tween == null || _tween.active == false)
					return true;

				// End based on distance from goal
				if (options.endRadius > float.Epsilon && _goalDistanceNow < options.endRadius)
					return true;

				// End based on animation percent
				if (_tween != null && options.endPercent > float.Epsilon && _tween.ElapsedPercentage() > options.endPercent)
					return true;
			}

			return false; // Not started yet
		}

		public override void OnStart()
		{
			base.OnStart();

			_started = true;

			_actor         = self.GetComponent<ActorBase>();
			_startPosition = self.transform.position;
			_position      = self.transform.position;

			if (Mathf.Approximately(Vector3.Distance(_actor.transform.position, CalcGoal()), 0))
				return;

			// Create movement options
			// ----------------------------------------

			// Base options
			options.tweener = EaserTo.Default;
			if (self.gameObject.TryGetComponent(out TweenMovable movable))
			{
				string func = movable.BaseMove;

				Table table = Lua.NewScript("std-anim");
				if (table.TryGet(func, out DynValue dvmove))
				{
					options.Apply(dvmove);
				}
				else DebugLogger.LogError($"Couldn't find move function '{func}'", LogContext.Combat, LogPriority.High);
			}

			// Override options
			if (overrideMove != null)
				options.Apply(overrideMove);

			// Front metrics
			// ----------------------------------------
			if (options.frontGoal)
			{
				if (goal.mode == WorldPoint.WorldPointMode.GameObject)
				{
					ActorBase act1 = self.GetComponent<ActorBase>();
					// _frontMetrics.p1 = (act1.transform.position + Vector3.up * act1.height).DropToGround();
					_frontMetrics.r1 = act1.radius;
					_frontMetrics.f1 = act1.facing;

					ActorBase act2 = goal.gameobject.GetComponent<ActorBase>();
					// _frontMetrics.p2 = (_goalTransform.transform.position + Vector3.up * act2.height).DropToGround();
					_frontMetrics.r2 = act2.radius;
					_frontMetrics.f2 = act2.facing;
				}
				else
				{
					options.frontGoal = false;
				}
			}

			// Look fixes
			// ----------------------------------------
			if (options.look == LookDir.Goal)
			{
				Vector3 goal = CalcGoal();

				if (_actor.IsSameFacing(goal))
					options.look = LookDir.Keep;
			}

			// Create tween
			// ----------------------------------------

			// Start the movement

			TweenerTo tweener = options.tweener;
			if (tweener == null)
			{
				tweener = new EaserTo(0.5f, Ease.OutFlash);
				DebugLogger.Log("Attempting to tween with a null tweener. Defaulting to OutFlash(0.5s).", LogContext.Graphics, LogPriority.Low);
			}

			tweener.speedBased = true;

			_tween = _position.To(CalcGoal(), tweener);
			_tween.OnStart(Tween_OnStart);
			_tween.OnComplete(Tween_OnComplete);
			_tween.timeScale = costate.timescale.current;


			OnCoplayerUpdate(costate.timescale.deltaTime);
			// _distFromGoal    = Vector3.Distance(@object.transform.position, GoalPosition);
		}

		public void Tween_OnStart()
		{
			if (!string.IsNullOrEmpty(options.anim))
			{
				_vfx = new AnimVFX(options.anim);
				self.GetComponent<INameAnimable>().Play(options.anim);
				self.GetComponent<VFXManager>().Add(_vfx);
			}
			//else if (options.puppetAnim != null)
			//{
			//	var puppetPlayer = self.GetComponent<API.PropertySheet.Runtime.PuppetPlayer>();

			//	if (puppetPlayer != null)
			//	{
			//		puppetPlayer.Play(anim: options.puppetAnim, option: PlayOptions.Continue);
			//	}
			//}
		}

		public void Tween_OnComplete()
		{
			if (!string.IsNullOrEmpty(options.anim))
			{
				self.GetComponent<VFXManager>().Remove(_vfx);
				self.GetComponent<INameAnimable>().Play(null);
			}
		}


		public override void OnEnd(bool forceStopped, bool skipped = false)
		{
			if (_position != null)
			{
				self.transform.position = _position.value;
			}

			if (options.lookafter.HasValue)
			{
				_actor.facing = options.lookafter.Value;
			}
		}

		public override void OnCoplayerUpdate(float dt)
		{
			if (!Active)
				return;

			_goalDistanceNow = Vector3.Distance(self.transform.position, CalcGoal());
			Vector3? lookpos = null;

			// Update facing
			// ----------------------------------------
			switch (options.look)
			{
				case LookDir.Keep:
				case LookDir.None:
					break;

				case LookDir.Goal:
					lookpos = CalcGoal();
					break;

				case LookDir.Start:
					lookpos = _startPosition;
					break;

				case LookDir.Point:
					lookpos = options.lookpos;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			// Avoid some weirdness
			if (lookpos.HasValue && Vector3.Distance(self.transform.position.Horizontal(), lookpos.Value.Horizontal()) > Mathf.Epsilon * 2)
			{
				_actor.FaceTowards(lookpos.Value);
			}

			// Update position
			// ----------------------------------------
			if (_tween != null)
				_tween.timeScale = _actor.timescale.current;

			self.transform.position = _position.value;
		}

		public Vector3 CalcGoal()
		{
			Vector3 goalPosition;
			if (options.frontGoal)
			{
				float   span   = _frontMetrics.r1 + _frontMetrics.r2;
				Vector3 offset = (span + options.frontSpacing) * _frontMetrics.f2;

				goalPosition = (goal.Get(_startPosition) + offset + Vector3.up * 2).DropToGround();
			}
			else
			{
				// ReSharper disable once PossibleNullReferenceException
				goalPosition = goal.Get(_startPosition);
			}

			float   overshootDistance = options.overshoot;
			Vector3 direction         = _startPosition.Towards(goalPosition);
			Vector3 overshoot         = direction * overshootDistance;

			goalPosition += overshoot;

			return goalPosition;
		}

		public void SetGoal(DynValue dvgoal)
		{
			if (dvgoal.AsUserdata(out ActorBase actor)) SetGoal(actor);
			else if (dvgoal.AsUserdata(out Fighter fter)) SetGoal(fter.actor);
			else if (dvgoal.AsUserdata(out Slot slot)) SetGoal(slot);
			else if (dvgoal.AsUserdata(out Vector3 v3)) SetGoal(v3);
			else if (dvgoal.AsUserdata(out WorldPoint wp)) SetGoal(wp);
		}

		public void SetGoal(WorldPoint wp)
		{
			goal = wp;
		}

		public void SetGoal(Vector3 pos)
		{
			goal = pos;
		}

		public void SetGoal(Transform transform)
		{
			goal = transform;
		}

		public void SetGoal([NotNull] Slot slot)
		{
			goal = slot.actor.transform;

			if (!options.frontGoal)
				options.lookafter = slot.facing;
		}

		[UsedImplicitly]
		public void SetGoal([NotNull] ActorBase actor)
		{
			goal              = actor.transform;
			options.frontGoal = true;
		}

		/// <summary>
		/// Metrics for movement to front of a monster.
		/// </summary>
		private struct FrontMetrics
		{
			public float r1, r2;
			// public Vector3 p1, p2;
			public Vector3 f1, f2;
		}

		[LuaEnum("look_positions", StringConvertible = true)] // obsolete; simply pass strings instead: 'goal', 'start', battle.center, etc.
		public enum LookDir
		{
			/// <summary>
			/// Don't touch the look direction.
			/// </summary>
			None,

			/// <summary>
			/// Keep the existing look direction.
			/// </summary>
			Keep,

			/// <summary>
			/// Look towards the goal point.
			/// </summary>
			Goal,

			/// <summary>
			/// Look back at the start position of the movement.
			/// </summary>
			Start,

			/// <summary>
			/// Look at a set point.
			/// </summary>
			Point
		}

		public struct Options
		{
			public TweenerTo tweener;
			public string    anim;
			public API.PropertySheet.PuppetAnimation puppetAnim;
			public float     overshoot;
			public bool      frontGoal;
			public float     frontSpacing;
			public float     endRadius;
			public float     endPercent;
			public float     leeway;
			public LookDir   look;
			public Vector3   lookpos;
			public Vector3?  lookafter;

			public static readonly Options Default = new Options
			{
				anim         = "dash",
				puppetAnim	 = null,
				look         = LookDir.Goal,
				endPercent   = 1,
				frontSpacing = 0.15f
			};

			public void Apply(DynValue dv)
			{
				if (dv.AsFunction(out Closure closure))
				{
					DynValue ret = Lua.Invoke(closure);
					Apply(ret);
				}
				else if (dv.AsTable(out Table tbl))
				{
					Apply(tbl);
				}
			}

			public void Apply(Table tbl)
			{
				tbl.TryGet("tween", out tweener, tweener);
				tbl.TryGet("anim", out anim, anim);
				tbl.TryGet("start_anim", out anim, anim);
				tbl.TryGet("overshoot", out overshoot, overshoot);
				tbl.TryGet("leeway", out leeway, leeway);
				tbl.TryGet("radius", out endRadius, endRadius);
				tbl.TryGet("percent", out endPercent, endPercent);
				//tbl.TryGet("puppetAnim", out puppetAnim, puppetAnim);
				tbl.TryGet("end_distance", out endRadius, endRadius);  // deprecated
				tbl.TryGet("end_percent", out endPercent, endPercent); // deprecated

				if (tbl.TryGet("look", out DynValue dv))
				{
					if (Enum.TryParse(dv.String, out LookDir dir)) look = dir; // For some reason LuaUtil EnumStringConverter doesn't work for this.......
					if (dv.AsObject(out LookDir s)) look                = s;
					if (dv.AsObject(out Vector3 pos))
					{
						look    = LookDir.Point;
						lookpos = pos;
					}
				}


				if (tbl.TryGet("look_after", out Vector3 lookposAfter))
				{
					lookafter = lookposAfter;
				}
			}
		}

		// [Serializable]
		// public struct TweenStepDriver : IDriver
		// {
		// 	[FormerlySerializedAs("_steps"), SerializeField, Inline]
		// 	public List<Step> steps;
		//
		// 	public Tween Start(MoveAnimation anim)
		// 	{
		// 		Sequence seq = DOTween.Sequence();
		//
		// 		for (var i = 0; i < steps.Count; i++)
		// 		{
		// 			Step step = steps[i];
		//
		// 			Tween stepTween = step.Start(anim);
		//
		// 			seq.Append(stepTween);
		// 		}
		//
		// 		return seq;
		// 	}
		//
		// 	public void Update(MoveAnimation moveAnimation, MoveAnimation parameters) { }
		//
		// 	public struct Step
		// 	{
		// 		[Optional]
		// 		public TweenerTo Tweener;
		//
		// 		public string anim;
		// 		public string animAfter;
		// 		public float  pathPercent;
		// 		public float  minDistance;
		//
		// 		public Tween Start(MoveAnimation anim)
		// 		{
		// 			GameObject entity = anim.@object;
		//
		// 			Vector3 p1 = entity.transform.position;
		// 			Vector3 p2 = anim.GoalPosition;
		//
		// 			float distanceToGoal = Vector3.Distance(p1, p2);
		// 			if (distanceToGoal < minDistance)
		// 				return null;
		//
		// 			var goal = Vector3.Lerp(p1, p2, pathPercent);
		//
		// 			TweenerTo tweener = Tweener ?? anim.options.Tweener;
		// 			Tween     tween   = anim.MakeTween(tweener, goal);
		//
		// 			string movAnim = this.anim;
		// 			string endAnim = this.anim;
		//
		// 			tween.OnStartExtended(() => entity.GetComponent<INamePlayer>().Play(movAnim), tweener);
		// 			tween.OnComplete(() => entity.GetComponent<INamePlayer>().Play(endAnim));
		//
		// 			return tween;
		// 		}
		// 	}
		// }
	}
}