using Data.Shops;
using Overworld.Cutscenes;
using SaveFiles.Elements.Inventory.Items;
using UnityEngine.Serialization;

namespace Anjin.Nanokin.Map
{
	public class LootCollectable : Collectable
	{
		public LootEntry Loot;

		[FormerlySerializedAs("DoesItemGetcutscene")]
		public bool AutoPlayItemGetCutscene;

		public override bool OnCollect()
		{
			if (!base.OnCollect())
				return false;

			if (!Loot.IsValid())
				return false;

			if (AutoPlayItemGetCutscene && Loot != null)
			{
				ItemGetCutscene.SetLoot(Loot);
				ItemGetCutscene.get_cut().Play();
			}

			return true;
		}
	}
}