using System;

namespace Combat.Data
{
	public class ConsumeMarks : ProcEffect
	{
		public string id;

		public ConsumeMarks(string id)
		{
			this.id = id;
		}

		protected override ProcEffectFlags ApplyFighter() => throw new NotImplementedException();
	}
}