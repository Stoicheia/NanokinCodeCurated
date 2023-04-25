using Anjin.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Combat.Toolkit
{
	public class ChainScaler : MonoBehaviour
	{
		[SerializeField] private Vector3 source;
		[SerializeField] private Vector3 destination;
		[SerializeField] private Vector3 fromOffset;
		[SerializeField] private Vector3 toOffset;

		[SerializeField] private GameObject chain;
		//[SerializeField] private ChainBender chainBender;

		//public void Configure(Vector3 source, Vector3 destination, GameObject from, GameObject to)

		public void Configure(WorldPoint from, WorldPoint to, Vector3 fromOffset = default, Vector3 toOffset = default)
		{
			source = from + fromOffset;
			destination = to + toOffset;
			this.fromOffset = fromOffset;
			this.toOffset = toOffset;

			transform.position = source;

			//destination.y = source.y;
			//destination.z = source.z;

			ScaleChain();
		}

		public void Configure(Fighter from, Fighter to, Vector3 fromOffset = default, Vector3 toOffset = default)
		{
			source = from.offset3(fromOffset.x, fromOffset.y, fromOffset.z);
			destination = to.offset3(toOffset.x, toOffset.y, toOffset.z);
			this.fromOffset = fromOffset;
			this.toOffset = toOffset;

			transform.position = source;

			//if (chainBender != null)
			//{
			//	chainBender.StartTracking(from, to, fromOffset, toOffset);
			//}

			ScaleChain();
		}

		//void Awake()
		//{
		//	if ((source != null) && (destination != null))
		//	{
		//		ScaleChain();
		//	}
		//}

		void ScaleChain()
		{
			destination.y = source.y;
			destination.z = source.z;

			float distance = Vector3.Distance(source, destination);

			Vector3 chainScale = chain.transform.localScale;
			chainScale.z = (distance/* - 0.8f*/);
			chain.transform.localScale = chainScale;
		}
	}
}
