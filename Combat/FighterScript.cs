using Combat.Data;
using Combat.Scripting;
using Combat.Toolkit;
using JetBrains.Annotations;
using static Combat.LuaEnv;

namespace Combat
{
	public class FighterScript : BattleLua
	{
		public readonly Fighter            self;
		public readonly ObjectFighterAsset asset;

		protected override string EnvName => asset.script.Asset != null
			? $"object-{asset.script.Asset.name}"
			: $"object-{asset.name}";

		protected override ScriptStore ScriptStore => asset.script.Store;

		public FighterScript([NotNull] Fighter fighter, [NotNull] ObjectFighterAsset asset) : base(fighter.battle)
		{
			this.self  = fighter;
			this.asset = asset;

			user        = fighter;
			targeting   = new Targeting();
			scriptAsset = asset.script.Asset;
		}

		protected override void UpdateGlobals(ref LuaEnv env)
		{
			base.UpdateGlobals(ref env);
			env.Set("self", self);
		}

		[CanBeNull]
		public BattleAnim Init()
		{
			targeting.Clear();
			targeting.AddPick(new Target(self));

			UpdateGlobals(ref baseEnv);

			animsm.Start(baseEnv);
			Invoke(FUNC_INIT);
			return animsm.EndInstant();
		}
	}
}