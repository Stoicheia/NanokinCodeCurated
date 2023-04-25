using Overworld.Cutscenes;
using UnityEngine;

namespace Util.Animation
{
	public abstract class CoroutineManagedObj : CoroutineManaged
	{
		public GameObject self;

		public CoroutineManagedObj(GameObject self)
		{
			this.self = self;
		}
	}
}