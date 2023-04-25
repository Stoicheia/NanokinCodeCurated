using System;
using System.Collections.Generic;
using Anjin.MP;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;

namespace Anjin.Actors
{
	/// <summary>
	/// To be attached to a WorldCutscene, for controlling actors controlled by the cutscene.
	/// </summary>
	public class CutsceneBrain : ActorBrain, ICharacterActorBrain, IAnimOverrider
	{
		public override int  Priority                     => 5 + PriorityMod;

		// Do this so controlled enemies can move around during cutscenes
		public override bool IgnoreOverworldEnemiesActive => true;

		public int PriorityMod = 0;

		[NonSerialized, ShowInInspector] public Cutscene                             cutscene;
		[NonSerialized, ShowInInspector] public Dictionary<ActorBase, DirectedActor> actors;

		private void Awake()
		{
			actors = new Dictionary<ActorBase, DirectedActor>();
		}

		public override void OnTick(float dt) { }

		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			if (!actors.TryGetValue(character, out DirectedActor actor))
				return;

			actor.PollInputs(character, ref inputs);
		}

		public void ResetInputs(Actor character, ref CharacterInputs inputs) { }

		public override bool OverridesAnim(ActorBase character)
		{
			if (!actors.TryGetValue(character, out DirectedActor actor))
				return false;

			return actor.directions.overrideAnimEnabled || actor.directions.moveMode != DirectedActor.MoveMode.None && actor.directions.moveAnimation != null;
		}

		public override RenderState GetAnimOverride(ActorBase character)
		{
			if (!actors.TryGetValue(character, out DirectedActor actor))
				return new RenderState(AnimID.Stand);

			DirectedActor.Directions dirs = actor.directions;

			if (dirs.overrideAnimEnabled)
			{
				return dirs.overrideAnimState;
			}

			if (dirs.moveMode != DirectedActor.MoveMode.None && dirs.moveAnimation != null)
			{
				return new RenderState(dirs.moveAnimation) {loops = true};
			}
			else
			{
				return new RenderState(actor.idleAnimation);
			}
		}

		public override void OnAnimEndReached(ActorBase character)
		{
			if (!actors.TryGetValue(character, out DirectedActor actor))
				return;

			ref DirectedActor.Directions dirs = ref actor.directions;
			if (!dirs.overrideAnimEnabled)
				return;

			if (dirs.pauseAtEnd)
			{
				dirs.overrideAnimState = new RenderState(dirs.overrideAnimState.animName)
				{
					animPercent = 1,
					animSpeed   = 0
				};
			}
			else
			{
				// Reset the override animation
				dirs.overrideAnimEnabled = false;
				dirs.overrideAnimState   = new RenderState(AnimID.Stand);
			}
		}

		protected override void OnDrawGizmos()
		{
			foreach (var actor in actors)
			{
				var dirs = actor.Value.directions;

				if (dirs.moveMode == DirectedActor.MoveMode.Pathing)
				{
					if (dirs.path != null)
					{
						MotionPlanning.DrawPathInEditor(dirs.path);
					}
				}
			}
		}
	}
}