using System;
using Anjin.Util;
using UnityEngine;

namespace Anjin.Minigames.Racing
{
	public class RaceCheckpoint : MonoBehaviour
	{
		[SerializeField] private GameObject[] ActiveObjects;
		[SerializeField] private GameObject[] InactiveObjects;

		public Transform MarkerTarget;

		[NonSerialized] public RaceMinigame minigame;
		[NonSerialized] public int          index;
		[NonSerialized] public bool         generated;

		public void OnActive()
		{
			ActiveObjects.SetActive(true);
			InactiveObjects.SetActive(false);
		}

		public void OnInactive()
		{
			ActiveObjects.SetActive(false);
			InactiveObjects.SetActive(true);
		}

		public void OnHide()
		{
			ActiveObjects.SetActive(false);
			InactiveObjects.SetActive(false);
		}

		private void OnTriggerEnter(Collider other)
		{
			if (minigame != null && minigame.state == MinigameState.Running)
				minigame.OnCheckpointReached(this, other.gameObject);
		}
	}
}