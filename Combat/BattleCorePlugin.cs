namespace Combat
{
	/// <summary>
	/// Hot-pluggable effects that can be added to a battle.
	/// Hook just after the recipe builds the battle.
	/// </summary>
	public abstract class BattleCorePlugin
	{
		protected BattleRunner  runner;
		protected Battle battle;

		public virtual void Register(BattleRunner runner, Battle battle)
		{
			this.runner   = runner;
			this.battle = battle;

			OnApply();
		}

		protected virtual void OnApply() { }

		public virtual void Unregister()
		{
			battle = null;
			runner   = null;
		}
	}
}