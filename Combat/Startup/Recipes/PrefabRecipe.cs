using Assets.Nanokins;
using Combat.Entities;
using Util.Addressable;
using Util.Odin.Attributes;

namespace Combat.Startup
{
	public class PrefabRecipe : MonsterRecipe
	{
		public GenericInfoAsset Info;

		// [AddressFilter("Fighters/")]
		// public string Address;


		public override FighterInfo CreateInfo(AsyncHandles handles)
		{
			if (Info != null) return Info.Info;

			return new GenericInfo();
		}
	}
}