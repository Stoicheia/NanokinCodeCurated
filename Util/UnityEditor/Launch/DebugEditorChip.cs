#if UNITY_EDITOR
using Combat.Components;
using Combat.Data;
using UnityEditor;
using UnityEngine;

namespace Combat.Launch
{
	public class DebugEditorChip : Chip
	{
		public override void Install()
		{
			base.Install();

			battle.AddTriggerCsharp(OnUseSkill, "debug_editor_selection", Signals.start_skill);
		}

		private static void OnUseSkill(TriggerEvent obj)
		{
			// Note(C.L.): This was annoying. Let's find another way to do this please.
			//Selection.objects = new Object[] { ((SkillEvent)obj)?.instance?.asset };
		}
	}
}
#endif