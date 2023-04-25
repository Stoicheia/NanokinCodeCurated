using Assets.Nanokins;
using Cysharp.Threading.Tasks;

namespace SaveFiles
{
	/// <summary>
	/// Some extensions for savefiles, mostly utilities and cheats.
	/// </summary>
	public static class SaveFileExtensions
	{
		public static async UniTask GainAllLimbsMaxed(this SaveData save)
		{
			save.Limbs.Clear();

			await NanokinLimbCatalogue.Instance.LoadAll(list =>
			{
				foreach (NanokinLimbAsset asset in list)
				{
					save.AddLimb(asset.Address, StatConstants.MAX_MASTERY);
				}
			});
		}

		public static async UniTask GainPitchBuildLimbs(this SaveData save)
		{
			save.Limbs.Clear();

			await NanokinLimbCatalogue.Instance.LoadAll();

			save.AddAllMonsterLimbs("captain-catfish",   3);
			save.AddAllMonsterLimbs("prvtpuffer",        3);
			save.AddAllMonsterLimbs("peggy-star",        3);
			save.AddAllMonsterLimbs("bigfoot",           3);
			save.AddAllMonsterLimbs("pangzoran",         3);
			save.AddAllMonsterLimbs("kuchi-oona",        3);
			save.AddAllMonsterLimbs("g-lanza",           3);
			save.AddAllMonsterLimbs("pengan-police",     3);
			save.AddAllMonsterLimbs("nightshade",        3);
			//save.AddAllMonsterLimbs("lycan",			 3);
			save.AddAllMonsterLimbs("frosty-friend",     3);
			save.AddAllMonsterLimbs("dr-flei",           3);
			save.AddAllMonsterLimbs("red-riding-pom",    3);
			save.AddAllMonsterLimbs("hamrin-head",		 3);
			save.AddAllMonsterLimbs("beak-brigade",      3);
			save.AddAllMonsterLimbs("jellywup",          3);
		}
	}


}