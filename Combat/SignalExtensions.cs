namespace Combat.Data
{
	public static class SignalExtensions
	{
		public static bool IsStart(this Signals signal)
		{
			switch (signal)
			{
				case Signals.start_turn:
				case Signals.start_round:
				case Signals.start_turn_first:
				case Signals.start_turns:
				case Signals.start_skill:
					return true;
			}

			return false;
		}

		public static bool IsEnd(this Signals signal)
		{
			switch (signal)
			{
				case Signals.end_turn:
				case Signals.end_round:
				case Signals.end_turns:
				case Signals.end_skill:
					return true;
			}

			return false;
		}
	}
}