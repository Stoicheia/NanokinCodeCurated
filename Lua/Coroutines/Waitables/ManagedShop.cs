using Cysharp.Threading.Tasks;
using Data.Shops;
using Overworld.Cutscenes;
using Overworld.Shopping;
using UnityEngine;

namespace Anjin.Scripting.Waitables
{
	public class ManagedShop : CoroutineManaged
	{
		private readonly Shop      _shop;
		private readonly Transform _npc;

		private bool _hasActivated;

		public ManagedShop(Shop shop, Transform npc)
		{
			_shop = shop;
			_npc  = npc;
		}

		public override bool Active => !_hasActivated || ShopMenu.menuActive;

		public override void OnStart()
		{
			base.OnStart();
			_hasActivated = false;
			ShopController.Open(_shop, _npc).Forget();
		}

		public override void OnCoplayerUpdate(float dt)
		{
			base.OnCoplayerUpdate(dt);

			if (!_hasActivated && ShopMenu.menuActive)
			{
				_hasActivated = true;
			}
		}

		public override void OnEnd(bool forceStopped, bool skipped = false)
		{
			base.OnEnd(forceStopped, skipped);
			ShopController.CloseAsync();
		}
	}
}