using System;
using Combat.Startup;
using JetBrains.Annotations;
using NanokinBattleNet.Library.Utilities;

namespace Combat
{
	public static class PacketReaderExtensions
	{
		[NotNull]
		public static TeamRecipe UnitTeam([NotNull] this PacketReader pr)
		{
			var teamRecipe = new TeamRecipe();

			byte nMonsters = pr.Byte();
			for (var i = 0; i < nMonsters; i++)
			{
				// string name = pr.String();
				// short  hp   = pr.Short();
				// short  sp   = pr.Short();
				//
				// byte           nLimbs = pr.Byte();
				// LimbInstance[] limbs  = new LimbInstance[nLimbs];
				//
				// for (int j = 0; j < nLimbs; j++)
				// {
				// 	byte   level     = pr.Byte();
				// 	string assetGuid = pr.String();
				//
				// 	NanokinLimbCatalogue.Instance.LoadAssetAsync(assetGuid);
				// 	limbs[j] = new LimbInstance(assetGuid, level);
				// }
				//
				// NanokinInstance monster = new NanokinInstance();
				// monster.SetLimbs(limbs);
				// monster.Name            = name;
				// monster.Points = new PointFloats(hp, sp);
				//
				// teamRecipe.AddMonster(monster);
				throw new NotImplementedException();
			}

			return teamRecipe;
		}
	}
}