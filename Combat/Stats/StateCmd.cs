using Anjin.Scripting;
using Data.Combat;

namespace Combat.Data
{
	/// Math command to operate on a stat.
	/// It is reversible.
	/// </summary>
	[LuaUserdata]
	public readonly struct StateCmd
	{
		public readonly StateStat    stat;
		public readonly StatOp      op;
		public readonly float       value;
		public readonly EngineFlags flag;

		public StateCmd(StatOp op, StateStat stat, float value) : this()
		{
			this.op    = op;
			this.stat  = stat;
			this.value = value;
		}

		public StateCmd(EngineFlags flag) : this()
		{
			op        = StatOp.flag;
			stat      = StateStat.eflag;
			this.flag = flag;
		}

		public bool Raises => op == StatOp.up && value > 0 ||
		                      op == StatOp.low && value < 0 ||
		                      op == StatOp.scale && value > 1; // TODO switch to the same approach as ProcStats

		public bool Lowers => op == StatOp.low && value > 0 ||
		                      op == StatOp.up && value < 0 ||
		                      op == StatOp.scale && value < 1; // TODO switch to the same approach as ProcStats

		public void Apply(ref Statf flat, ref Statf scale)
		{
			switch (op)
			{
				case StatOp.up:
					flat += value;
					break;

				case StatOp.low:
					flat -= value;
					break;

				case StatOp.scale:
					// TODO switch to the same approach as ProcStats
					scale += value;
					break;
			}
		}

		public void Apply(ref Pointf flat, ref Pointf scale)
		{
			switch (op)
			{
				case StatOp.up:
					flat += value;
					break;

				case StatOp.low:
					flat -= value;
					break;

				case StatOp.scale:
					// TODO switch to the same approach as ProcStats
					scale += value;
					break;
			}
		}

		public void Apply(ref float flat, ref float scale)
		{
			switch (op)
			{
				case StatOp.up:
					flat += value;
					break;

				case StatOp.low:
					flat -= value;
					break;

				case StatOp.scale:
					// TODO switch to the same approach as ProcStats
					scale += value;
					break;
			}
		}

		public void Apply(ref Elementf flat, ref Elementf scale)
		{
			switch (op)
			{
				case StatOp.up:
					flat += value;
					break;

				case StatOp.low:
					flat -= value;
					break;

				case StatOp.scale:
					// TODO switch to the same approach as ProcStats
					scale += value;
					break;
			}
		}


		public void Undo(ref Statf flat, ref Statf scale)
		{
			switch (op)
			{
				case StatOp.up:
					flat -= value;
					break;

				case StatOp.low:
					flat += value;
					break;

				case StatOp.scale:
					// TODO switch to the same approach as ProcStats
					scale -= value;
					break;
			}
		}

		public void Undo(ref Pointf flat, ref Pointf scale)
		{
			switch (op)
			{
				case StatOp.up:
					flat -= value;
					break;

				case StatOp.low:
					flat += value;
					break;

				case StatOp.scale:
					// TODO switch to the same approach as ProcStats
					scale -= value;
					break;
			}
		}

		public void Undo(ref float flat, ref float scale)
		{
			switch (op)
			{
				case StatOp.up:
					flat -= value;
					break;

				case StatOp.low:
					flat += value;
					break;

				case StatOp.scale:
					// TODO switch to the same approach as ProcStats
					scale -= value;
					break;
			}
		}

		public void Undo(ref Elementf flat, ref Elementf scale)
		{
			switch (op)
			{
				case StatOp.up:
					flat -= value;
					break;

				case StatOp.low:
					flat += value;
					break;

				case StatOp.scale:
					// TODO switch to the same approach as ProcStats
					scale -= value;
					break;
			}
		}
	}
}