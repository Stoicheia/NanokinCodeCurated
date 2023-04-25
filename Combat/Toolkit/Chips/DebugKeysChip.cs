using Anjin.Nanokin;
using Combat.Components;
using Combat.Scripting;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Combat.Toolkit
{
	public class DebugKeysChip : Chip
	{
		public override UniTask InstallAsync()
		{
			return base.InstallAsync();
		}

		public override void Uninstall()
		{
			base.Uninstall();
		}

		public override void Update()
		{
			base.Update();

			if (GameInputs.IsShortcutPressed(Key.W))
			{
				// unfortunately we can't put this in EditorTeamBrain, else it doesn't work well with 2 brains or more...
				DebugBrain.waiting = !DebugBrain.waiting;
			}

			if (Keyboard.current != null)
			{
				if (GameInputs.IsShortcutPressed(Key.S, Key.Tab))
				{
					// Stop core
					runner.Stop();
					DebugBrain.nextAnim = new MockAnim();
					DebugBrain.go         = true;
				}

				if (GameInputs.IsShortcutPressed(Key.R, Key.Tab))
				{
					// Stop core and start again with same configuration
					Debug.ClearDeveloperConsole();

					LuaChangeWatcher.FlushChanges();

					runner.Restart();
					DebugBrain.nextAnim = new MockAnim();
					DebugBrain.go         = true;
				}

				if (GameInputs.IsShortcutPressed(Key.LeftArrow, Key.Tab))
				{
					runner.CancelActions<AdvanceTurnAnim>();
					runner.CancelBrains();
				}
			}
		}
	}
}