using Combat.Components.VictoryScreen.Menu;
using Cysharp.Threading.Tasks;
using Data.Combat;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Nanokin.Map
{
	public class StatCollectable : Collectable
	{
		[Title("Gains")]
		public float xp;
		public Pointf points;
		public float credit;

		public override bool OnCollect()
		{
			if(!base.OnCollect()) return false;

			CollectibleSystem.Live.OnCollect(this).Forget();
			return true;
		}
	}
}