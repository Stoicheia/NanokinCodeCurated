using System;
using Combat;
using Combat.Entities;
using Data.Nanokin;
using JetBrains.Annotations;
using Util.Addressable;
using Util.Odin.Attributes;

namespace Assets.Nanokins
{
	[Serializable]
	public class ExistingNanokin : MonsterRecipe
	{
		[Inline] public NanokinInstance nanokin;

		public ExistingNanokin([CanBeNull] NanokinInstance nanokin = null)
		{
			this.nanokin = nanokin;
		}

		public override FighterInfo CreateInfo(AsyncHandles handles) => new NanokinInfo(nanokin);
	}
}