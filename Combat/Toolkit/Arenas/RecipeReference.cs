using Anjin.Scripting;
using Combat.Launch;
using Combat.Startup;
using JetBrains.Annotations;
using MoonSharp.Interpreter;

namespace Combat
{
	public struct RecipeReference
	{
		public BattleRecipe      direct;
		public BattleRecipeAsset asset;
		public LuaAsset          luaAsset;
		public string            call;

		[NotNull]
		public string Name => call ?? (asset != null ? asset.name : null) ?? "n/a";

		public RecipeReference(BattleRecipe direct) : this()
		{
			this.direct = direct;
		}

		public RecipeReference(BattleRecipeAsset asset) : this()
		{
			this.asset = asset;
		}

		public RecipeReference(string call) : this()
		{
			this.call = call;
		}

		public BattleRecipe Get()
		{
			if (direct != null) return direct;
			if (asset != null) return asset.Value;

			if (luaAsset != null && call == null)
			{
				DynValue dynValue = Lua.Invoke(luaAsset);
				return BattleRecipe.new_recipe(dynValue.Table);
			} else if (luaAsset == null && call != null)
			{
				DynValue dynValue = Lua.InvokeGlobalFirst(call);
				return BattleRecipe.new_recipe(dynValue.Table);
			}

			return null;
		}

		public static implicit operator RecipeReference(string str)
		{
			return new RecipeReference(str);
		}

		public static implicit operator RecipeReference(BattleRecipe recipe)
		{
			return new RecipeReference(recipe);
		}

		public static implicit operator RecipeReference(BattleRecipeAsset recipe)
		{
			return new RecipeReference(recipe);
		}
	}
}