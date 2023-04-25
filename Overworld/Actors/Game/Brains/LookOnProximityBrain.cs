using Anjin.Util;
using Drawing;
using UnityEngine;
#if UNITY_EDITOR

#endif

namespace Anjin.Actors
{
	[AddComponentMenu("Anjin: Actor Brain/Look On Proximity")]
	public class LookOnProximityBrain : ActorBrain, ICharacterActorBrain
	{
		public override int Priority => 0;

		public override void OnBeginControl()
		{
			/*if(actor is CharacterActor character)
			{
				normalDirection = character.FacingDirection;
			}*/
		}

		public override void OnTick(float dt) { }
		public override void OnEndControl()   { }

		public ActorRef LookTowardsActor = ActorRef.Nas;
		public float    LookAtRange      = 2f;
		public float    TurnLerp         = 1;

		//public Vector2 NormalDirection;

		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			if (ActorRegistry.TryGet(LookTowardsActor, out Actor a))
			{
				inputs.LookDirLerp = TurnLerp;

				if (Vector3.Distance(a.position, character.position) < LookAtRange)
				{
					inputs.look = a.position.Horizontal() - character.position.Horizontal();
				}
			}
		}

		public void ResetInputs(Actor character, ref CharacterInputs inputs) { }

		void OnDrawGizmos()
		{
#if UNITY_EDITOR

			// Handles.color = color;
			// Handles.DrawWireDisc(transform.position + Vector3.up * 0.5f, Vector3.up, LookAtRange);
#endif
		}

		public override void DrawGizmos()
		{
			base.DrawGizmos();

			Color color = ColorUtil.MakeColorHSVA(0.15f, 0.8f, 0.8f, 0.9f);
			Draw.CircleXZ(transform.position + Vector3.up * 0.5f, LookAtRange, color);
		}
	}
}