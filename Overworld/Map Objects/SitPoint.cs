using Anjin.Actors;
using Overworld.Interactables;
using UnityEngine;

namespace Anjin.Nanokin.Map
{
	public class SitPoint : MonoBehaviour, IInteractable
	{
		public void OnInteract(Actor actor)
		{
			if (actor is PlayerActor plr)
				plr.TrySit(transform);
		}

		public bool IsBlockingInteraction(Actor actor) => actor is PlayerActor plr && plr.currentStateID == PlayerActor.STATE_SIT;
	}
}