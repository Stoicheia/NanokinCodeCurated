using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Combat.Toolkit
{
	public class ChainBender : MonoBehaviour
	{
		[SerializeField] private bool endOfChain;

		[SerializeField] private GameObject movableAnchor;

		private bool tracking;

		private Fighter target;

		private Vector3 offset;

		public void StartTracking(Fighter source, Fighter destination, Vector3 sourceOffset, Vector3 destinationOffset)
		{
			target = (!endOfChain ? source : destination);
			offset = (!endOfChain ? sourceOffset : destinationOffset);

			tracking = true;
		}

		// Start is called before the first frame update
		void Awake()
		{
			tracking = false;
		}

		// Update is called once per frame
		void Update()
		{
			if (tracking)
			{
				//Vector3 position = transform.position;
				//position.z = target.position.z;
				transform.position = target.offset3(offset.x, offset.y, offset.z);
			}
		}
	}
}
