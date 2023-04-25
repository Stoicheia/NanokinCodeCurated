using Anjin.Actors;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Minigames.Racing
{
	public class RaceMinigameTester : MonoBehaviour
	{
		private RaceMinigame _race;

		[Button]
		public void Test()
		{
			if (_race != null && _race.state != MinigameState.Off)
			{
				this.Log("Race already in progress.");
				return;
			}

			_race = GetComponent<RaceMinigame>();
			_race.AddPlayer(ActorController.playerActor);
			_race.Begin().Forget();
		}
	}
}