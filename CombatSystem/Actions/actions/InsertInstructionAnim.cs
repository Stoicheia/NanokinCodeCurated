namespace Combat.Toolkit
{
	public class InsertInstructionAnim : BattleAnim
	{
		public readonly CoreOpcode      opcode;
		public readonly CoreInstruction instruction;

		public InsertInstructionAnim(CoreOpcode opcode)
		{
			this.opcode = opcode;
		}

		public InsertInstructionAnim(CoreOpcode opcode, CoreInstruction instruction)
		{
			this.opcode      = opcode;
			this.instruction = instruction;
		}


		public override void RunInstant()
		{
			runner.Submit(opcode, instruction);
		}
	}
}