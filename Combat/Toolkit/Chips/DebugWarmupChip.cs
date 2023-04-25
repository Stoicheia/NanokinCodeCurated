using Combat.Components;
using UnityEngine;

namespace Combat.Toolkit
{
	public class DebugWarmupChip : Chip
	{
		private bool _isWarmingUp = true;

		protected override void RegisterHandlers()
		{
			base.RegisterHandlers();
			Handle(CoreOpcode.Execute, OnExecuteAction);
		}

		private void OnExecuteAction(ref CoreInstruction msg)
		{
			if (msg.anim is SkillAnim)
			{
				_isWarmingUp   = false;
				Time.timeScale = 1;
			}
		}

		public override void Update()
		{
			if (_isWarmingUp)
			{
				Time.timeScale = 2.5f;
			}
		}
	}
}