using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Anjin.Actors.States
{
	public class State
	{
		[NonSerialized] public int             id;
		[NonSerialized] public bool            active;
		[NonSerialized] public bool            justActivated;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator bool(State state) => state != null && state.active;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator int(State state) => state.id;

		public virtual void OnActivate(State   prev) { }
		public virtual void OnDeactivate(State next) { }
		public virtual void OnUpdate(float     dt)   { }

		public virtual bool IsDone => false;
	}

	// NOTE (C.L. 01-23-2023): Remove/Compress this later if they turn out to be superfluitive
	public abstract class ActorState : State {
		[NonSerialized]
		public Actor actor;
		public virtual void setActor(Actor _actor) => actor = _actor;
	}

	public abstract class StateT<TActor> : ActorState where TActor : Actor
	{
		[NonSerialized]
		public new TActor actor;
		public override void setActor(Actor _actor) => actor = _actor as TActor;
	}

	public interface IStateMachineUser
	{
		State GetDefaultState();
		State GetNextState(float       dt);
		void  OnChangeState(State      prev, State next);
		void  OnBeforeDeactivate(State prev, State next);
		void  OnBeforeActivate(State   prev, State next);
	}

	public class StateMachine
	{
		public IStateMachineUser User;
		public Actor             Actor;

		private bool _hasUser;

		[NonSerialized]
		public State Current;

		[NonSerialized]
		public List<State> AllStates = new List<State>();

		[NonSerialized] public float elapsedTime;
		[NonSerialized] public int   currentStateID  = -1;
		[NonSerialized] public int   previousStateID = -1;
		[NonSerialized] public bool  stateChanged;

		public StateMachine(IStateMachineUser user)
		{
			User     = user;
			Actor    = user as Actor;
			_hasUser = user != null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator State(StateMachine machine) => machine.Current;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static implicit operator bool(StateMachine  state)   => state.Current != null;

		public TState Register<TState>(TState state, int id = -1) where TState : State
		{
			state.id = id > -1 ? id : AllStates.Count;
			if(state is ActorState astate) astate.setActor(Actor);
			AllStates.Add(state);
			return state;
		}

		public void Boot()           => ToDefaultState();
		public void ToDefaultState() => Change(User?.GetDefaultState());

		public void Change(State next)
		{
			if (next == Current)
				return;

			State prev = Current;

			if(_hasUser) {
				User.OnBeforeDeactivate(prev, next);
			}

			if(prev != null) {
				prev.OnDeactivate(next);
				prev.justActivated = false;
				prev.active        = false;
				previousStateID    = prev.id;
			} else {
				previousStateID = -1;
			}

			Current          = next;
			currentStateID   = -1;
			elapsedTime = 0;
			stateChanged     = true;

			if(_hasUser) {
				User.OnBeforeActivate(prev, next);
			}

			if (Current != null) {
				currentStateID			= Current.id;
				Current.active			= true;
				Current.justActivated	= true;
				Current.OnActivate(prev);
			}

			if(_hasUser) {
				User.OnChangeState(prev, next);
			}
		}

		public void Update(float dt)
		{
			if (!_hasUser) return;

			elapsedTime += dt;

			State next = User.GetNextState(dt);

			if (next != null)
				Change(next);

			foreach (State state in AllStates)
				state.OnUpdate(dt);
		}
	}
}