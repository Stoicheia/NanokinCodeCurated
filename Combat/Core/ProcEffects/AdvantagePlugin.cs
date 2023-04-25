namespace Combat.StandardResources
{
	public class AdvantagePlugin : BattleCorePlugin
	{
		protected readonly PlayerAlignments alignment;

		public AdvantagePlugin(PlayerAlignments alignment)
		{
			this.alignment = alignment;
		}
	}
}