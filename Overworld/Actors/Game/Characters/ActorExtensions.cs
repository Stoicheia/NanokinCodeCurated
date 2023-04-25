namespace Anjin.Actors
{
	public enum FancyIdleStates
	{
		BecameInactive,
		ContinueInactive,
		BecameActive,
		ContinueActive,
	}

	public static class ActorExtensions
	{
		public static bool Changed(this FancyIdleStates state)
		{
			return state == FancyIdleStates.BecameInactive || state == FancyIdleStates.BecameActive;
		}

		public static FancyIdleStates DoFancyIdle1(this Actor       actor,
			ref                                         RenderState state,
			ref                                         float       elapsed,
			float                                                   standTimeForActivation,
			int                                                     repeats = 1)
		{
			bool wasActive = elapsed >= standTimeForActivation;

			FancyIdleStates inactive = wasActive ? FancyIdleStates.BecameInactive : FancyIdleStates.ContinueInactive;

			// Temporary measure to disable idle animations in cutscene
			if (actor.activeBrain is CutsceneBrain)
			{
				elapsed = 0;
				return inactive;
			}

			elapsed += actor.timeScale.deltaTime;
			if (elapsed >= standTimeForActivation)
			{
				FancyIdleStates active = !wasActive ? FancyIdleStates.BecameActive : FancyIdleStates.ContinueActive;

				state.animID = AnimID.Idle1;
				// state.animRepeats = repeats;

				ActorRenderer rend = actor.renderer;
				if (rend.lastAnim == AnimID.Idle1 && rend.Animable.player.elapsedRepeats >= repeats)
				{
					elapsed = 0;
				}

				return active;
			}

			return inactive;
		}
	}
}