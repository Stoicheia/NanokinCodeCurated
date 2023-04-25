using Anjin.Scripting;
using Combat.Entities;
using Data.Combat;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Combat
{
	public class MockInfo : FighterInfo
	{
		public string           name;
		public Pointf           points      = Pointf.Zero;
		public Statf            stats       = Statf.Zero;
		public Elementf         resistances = Elementf.Zero;
		public int              actions     = 0;
		public int              priority    = 0;
		public int              xpLoot      = 0;
		public int              rpLoot      = 0;
		public FighterBaseState baseState;

		[UsedImplicitly] public int hp { get => (int)points.hp; set => points.hp = value; }
		[UsedImplicitly] public int sp { get => (int)points.sp; set => points.sp = value; }
		[UsedImplicitly] public int op { get => (int)points.op; set => points.op = value; }

		[UsedImplicitly] public int power { get => (int)stats.power; set => stats.power = value; }
		[UsedImplicitly] public int speed { get => (int)stats.speed; set => stats.speed = value; }
		[UsedImplicitly] public int will  { get => (int)stats.will;  set => stats.will = value; }

		public override string   Name        => name;
		public override Pointf   Points      => points;
		public override Statf    Stats       => stats;
		public override Elementf Resistances => resistances;
		public override int      Actions     => actions;
		public override int      Priority    => priority;
		public override int      XPLoot      => xpLoot;
		public override int      RPLoot      => rpLoot;

		public override void ConfigureTB(Table tb)
		{
			if (tb == null) return;
			tb.TryGet("hp", out points.hp);
			tb.TryGet("sp", out points.sp);
			tb.TryGet("op", out points.op);

			tb.TryGet("actions", out points.op);
			tb.TryGet("priority", out points.op);
			tb.TryGet("xp", out xpLoot);
			tb.TryGet("rp", out rpLoot);

			if (tb.TryGet("prefab", out GameObject prefab))
			{
				if (prefab.TryGetComponent(out ObjectFighterActor actor))
				{
					ObjectFighterAsset a = actor.Asset;
					if (a != null)
					{
						SetAsset(a);
					}
				}
			}

			if (tb.TryGet("asset", out ObjectFighterAsset a2))
			{
				SetAsset(a2);
			}
		}

		private void SetAsset([NotNull] ObjectFighterAsset asset)
		{
			resistances = asset.Resistances;
			stats       = asset.Stats;
			points      = asset.Points;
			baseState   = asset.BaseState;
		}
	}
}