using Anjin.Minigames;
using Anjin.Util;

namespace Anjin.Nanokin
{
	public class MinigameSpawnPoint : SpawnPoint
	{
		public Minigame          Minigame;
		public IMinigameSettings Settings;

		public override string SpawnPointName => Minigame ? Minigame.gameObject.name : name;

		public override void OnEnable()
		{
			base.OnEnable();
			Minigame = GetComponent<Minigame>();
			if (Minigame == null)
				Minigame = GetComponentInParent<Minigame>();
		}

		public override async void OnSpawn()
		{
			if (Minigame)
			{
				await Minigame.Setup(Settings);
				Minigame.Begin().ForgetWithErrors();
			}
		}
	}
}