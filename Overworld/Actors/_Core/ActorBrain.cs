using System;
using MoonSharp.Interpreter;
using UnityEngine;
using Util.Components;

namespace Anjin.Actors
{
	public abstract class ActorBrain : AnjinBehaviour, IComparable<ActorBrain>
	{
		/// <summary>
		/// When an actor needs to automatically choose an attached active brain when starting, it will
		/// use this number to determine which brain should be given control.
		/// </summary>
		public abstract int Priority { get; }

		[NonSerialized]
		public Actor actor;

		/// <summary>
		/// Indicate that controls for the actor should be disabled.
		/// Can be used externally by pluggable features.
		/// </summary>
		[NonSerialized]
		public bool disableControls;

		public virtual void SetActor(Actor actor) => this.actor = actor;

		public int CompareTo(ActorBrain other)
		{
			if (ReferenceEquals(this, other)) return 0;
			if (ReferenceEquals(null, other)) return 1;
			return Priority.CompareTo(other.Priority);
		}

		/*TODO: We may want to have brain call ticks on their controllables instead of controllables calling
				their ticks. This is because we currently can't have a method like OnBeforeTick(dt), or OnAfterTick(dt),
				which would be useful for setting things up before all actors process.*/

		public virtual void OnBeginControl() { }
		public virtual void OnTick(float dt) { }
		public virtual void OnEndControl()   { }

		// Note(C.L. 7-7-22): This is sort of a hack but it probably isn't that bad for now
		public virtual bool IgnoreOverworldEnemiesActive => false;

		//	LUA
		//----------------------------------------------------------------
		public virtual void Lua_Message(string msg, params DynValue[] inputs)
		{
			DebugLogger.Log(msg, LogContext.Overworld, LogPriority.Low);

			foreach (var dynValue in inputs)
			{
				DebugLogger.Log(dynValue.ToDebugPrintString(), LogContext.Overworld, LogPriority.Low);
			}
		}

		public virtual bool OverridesAnim(ActorBase actor)
		{
			return false;
		}

		public virtual RenderState GetAnimOverride(ActorBase actor)
		{
			return default;
		}

		public virtual void OnAnimEndReached(ActorBase actor) { }
	}

	public abstract class ActorBrain<T> : ActorBrain
		where T : Actor
	{
		[NonSerialized]
		public new T actor;

		public override void OnBeginControl()
		{
			if (actor != null)
				OnBeginControl(actor);
		}

		public override void OnTick(float dt)
		{
			if (actor is T cast) OnTick(cast, dt);
		}

		public override void OnEndControl()
		{
			if (actor is T cast)
			{
				OnEndControl(cast);
				this.actor = null;
			}
		}

		public sealed override void SetActor(Actor actor)
		{
			if (actor is T t)
				this.actor = t;
			else if (actor == null)
				this.actor = null;
		}

		public sealed override RenderState GetAnimOverride(ActorBase actor)
		{
			if (actor is T t)
				return GetAnimOverride(t);

			return default;
		}

		public virtual void OnBeginControl(T actor) { }

		public abstract void OnTick(T actor, float dt);

		public virtual void OnEndControl(T actor) { }

		public virtual RenderState GetAnimOverride(T actor) => default;

		protected override void OnRegisterDrawer() => DrawingManagerProxy.Register(this);
		private            void OnDestroy()        => DrawingManagerProxy.Deregsiter(this);
	}
}