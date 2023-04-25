namespace Combat.Data
{
	public struct TriggerState
	{
		public Trigger value;

		/// <summary>
		/// The current life of the trigger. -1 for infinite/immortal
		/// When the life falls to 0, the trigger can no longer handle any event and will be removed.
		/// It is an arbitrary number that can be in any way to control the lifetime and activation count of the trigger.
		/// For example, settings this to 5 with a 'start-action' signals means the life will correspond
		/// to the number of turns left before expiring. (and by extension, the number of turns the trigger will fire)
		/// </summary>
		public int life;

		/// <summary>
		/// How many time the trigger has fired.
		/// </summary>
		public int numHandled;

		public TriggerState(Trigger value, int life)
		{
			this.value      = value;
			this.life       = life;
			this.numHandled = 0;
		}
	}
}