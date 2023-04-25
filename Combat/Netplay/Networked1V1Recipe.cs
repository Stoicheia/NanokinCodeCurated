using System;
using Anjin.Util;
using Combat.Startup;
using Cysharp.Threading.Tasks;

namespace Combat
{
	[Serializable]
	public class Networked1V1Recipe : BattleRecipe
	{
		public Player player;
		public Remote remote;

		protected override async UniTask OnBake()
		{
			// var team1 = await View.AddTeam(null, , );
			Team team1 = battle.AddTeam(overridePlayerBrain ?? new PlayerBrain {netplayClient = player.client}, arena.GetSlotLayout(player.slots));
			Team team2 = battle.AddTeam(overrideEnemyBrain ?? new NetplayBrain(), arena.GetSlotLayout(remote.slots));

			team1.isPlayer = true;

			await AddSavedata(team1);
			await AddTeam(team2, null);

			remote.OnCreatingController?.Invoke(new NetplayBrain());
		}

		public class Player
		{
			public readonly BattleClient client;
			public readonly string       slots;

			public Player(BattleClient client, string slots)
			{
				this.client = client;
				this.slots  = slots;
			}
		}

		public class Remote
		{
			public readonly string     slots;
			public readonly TeamRecipe units;

			public Remote(string slots, TeamRecipe units)
			{
				this.slots = slots;
				this.units = units;
			}

			public Action<NetplayBrain> OnCreatingController;
		}
	}
}