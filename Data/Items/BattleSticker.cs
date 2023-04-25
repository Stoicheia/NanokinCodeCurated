using Anjin.Scripting;
using Combat;
using Combat.Data;
using Combat.Scripting;
using Combat.Toolkit;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using static Combat.LuaEnv;

namespace SaveFiles.Elements.Inventory.Items.Scripting
{
	public sealed class BattleSticker : BattleLua
	{
		private const string EQUIP_BUFFTAG = "equip";

		public readonly StickerAsset    asset;
		public readonly StickerInstance instance;

		public BattleSticker(Battle battle, StickerInstance instance) : base(battle)
		{
			this.instance = instance;
			asset         = instance.Asset;
			Reinitialize();
		}

		public bool HasUse => baseEnvTable[FUNC_USE] != null;

		public bool IsPassive => baseEnvTable.ContainsKey(FUNC_PASSIVE) && !baseEnvTable.ContainsKey(FUNC_USE);

		public bool HasCharges() => instance.Charges > 0;

		public bool UsableOnFighter(Fighter fighter)
		{
			DynValue ret = Invoke(FUNC_USABLE, args : new []{ fighter }, optional: true);
			if (ret.Type == DataType.Boolean)
				return ret.Boolean;

			return true;
		}


		protected override string EnvName => $"sticker-{asset.name}";

		protected override ScriptStore ScriptStore => asset.script.Store;

		public override void Reinitialize()
		{
			scriptAsset = asset.script.Asset;
			base.Reinitialize();
		}

		protected override void OnEnvCreated(LuaEnv env)
		{
			env.sticker = this;
		}

		[CanBeNull]
		public BattleAnim Passive()
		{
			// Default targeting onto ourselves
			targeting = new Targeting();
			targeting.AddPick(new Target(user));

			UpdateGlobals(ref baseEnv);

			animsm.Start(baseEnv);

			// 1. Generic passive functionality
			Invoke(FUNC_PASSIVE, optional: true);

			// 2. Equipment data (implicit state with 'equip' tag)
			//if (asset.IsEquipment)
			//{
			State equip = animsm.state();
			equip.ID   = $"{asset.name}/equip";
			equip.life = -1;
			equip.tags.Add(EQUIP_BUFFTAG);
			equip.status.Add(StatOp.up, asset.PointGain);
			equip.status.Add(StatOp.up, asset.StatGain);
			equip.status.Add(StatOp.up, asset.EfficiencyGain);
			//}

			return animsm.EndCoplayer(FUNC_PASSIVE_ANIM);
		}

		public void Target(Targeting target)
		{
			targeting = target;
			UpdateGlobals(ref baseEnv);

			if (baseEnvTable[FUNC_TARGET] != null)
				Lua.Invoke(baseEnvTable, FUNC_TARGET);
			else
				Lua.Invoke(baseEnvTable, FUNC_TARGET_DEFAULT);
		}

		[CanBeNull]
		public BattleAnim Use()
		{
			LuaEnv env = GetBaseEnvOrFork();

			UpdateGlobals(ref env);
			animsm.Start(env);
			Lua.Invoke(baseEnvTable, FUNC_USE);
			return animsm.EndCoplayer(FUNC_USE_ANIM, null, true);
		}
	}
}