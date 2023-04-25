using Anjin.Scripting;
using JetBrains.Annotations;

namespace Overworld.Cutscenes
{
	// TODO merge ExtendedAnimation into this

	/// <summary>
	/// A coroutine managed is a background object that the coplayer manages,
	/// wrapping another existing feature of the game.
	///
	/// If the managed becomes inactive by itself midway through it will be
	/// discarded automatically. Otherwise, it will always be auto-stopped when
	/// the outer coroutine reaches completion. (i.e. to perform cleanup on this managed)
	///
	/// As such, CoroutineManaged is good to implement on/off features that
	/// must be cleaned up automatically at the end.
	///
	/// still wip, prone to refactor and restructure
	/// </summary>
	[LuaUserdata(Descendants = true)]
	public abstract class CoroutineManaged : ICoroutineWaitable
	{
		public enum State { Running, Skipping, Done}

		/// <summary>
		/// Unique ID for retrieval.
		/// </summary>
		public int id;

		public State state = State.Running;

		/// <summary>
		/// Coplayer we're a part of.
		/// </summary>
		public Coplayer coplayer;

		/// <summary>
		/// Shortcut to the coplayer's state
		/// </summary>
		protected Coplayer.State costate => coplayer.state;

		[UsedImplicitly]
		public bool manual;

		/// <summary>
		/// Indicates that the managed is still working and should
		/// receive updates from the coplayer.
		/// </summary>
		public abstract bool Active { get; }

		/// <summary>
		/// Report the total duration for this managed from Start() to Stop().
		/// Can be used for all sorts of visual effects when it's implemented.
		/// </summary>
		public virtual float ReportedDuration => 0;

		/// <summary>
		/// Report the elapsed percent off this managed while it is currently playing.
		/// Can be used for all sorts of visual effects when it's implemented.
		/// </summary>
		public virtual float ReportedProgress => 0;

		public virtual bool Skippable => true;
		//public virtual bool HasAsyncStop => false;

		public void Start()
		{
			OnStart();
		}

		public void End(bool skipped = false)
		{
			state = State.Done;
			OnEnd(skipped);
		}


		public void Stop(bool skipped = false)
		{
			state = State.Done;
			OnEnd(true, skipped);
		}


		/*public async UniTask StopAsync(bool skipped = false)
		{
			await OnStopAsync(skipped);
		}*/

		protected void BeginSkip() => state = State.Skipping;
		protected void EndSkip()   => state = State.Done;

		public virtual void OnStart() { }

		public virtual void OnEnd(bool forceStopped, bool skipped = false) { }

		//public virtual async UniTask OnStopAsync(bool wasSkipped = false) { }

		public virtual void OnCoplayerUpdate(float dt) { }

		public virtual bool CanContinue(bool justYielded, bool isCatchup = false) => !Active;

		public override string ToString() => GetType().Name;
	}
}