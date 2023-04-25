using System.Collections.Generic;
using UnityEngine;

namespace Anjin.Actors {

	/*
		TODO MAYBE
		A way to group up multiple brains into an overall structure.
	 */

	public class ActorBrainCluster : MonoBehaviour {
		public Actor            Actor;
		public List<ActorBrain> Brains;

		public ActorBrain Active;

		public void UpdateActive()
		{

		}
	}
}