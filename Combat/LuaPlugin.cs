using System;
using System.Collections.Generic;
using Combat.Toolkit;
using JetBrains.Annotations;

namespace Combat
{
	/// <summary>
	/// A battle plugin which can register logic from lua scripts.
	/// Event functions:
	/// - on_register
	/// </summary>
	[Serializable]
	public class LuaPlugin : BattleCorePlugin
	{
		public string ScriptName = "";

		[CanBeNull]
		public List<string> ScriptNames = null;

		private List<LuaInstance> _scriptHosts;

		public LuaPlugin() { }

		public LuaPlugin(string scriptName)
		{
			ScriptName = scriptName;
		}

		public LuaPlugin(List<string> scripts)
		{
			ScriptNames = scripts;
		}

		public override void Register(BattleRunner runner, Battle battle)
		{
			base.Register(runner, battle);

			_scriptHosts = new List<LuaInstance>();

			if (!string.IsNullOrEmpty(ScriptName))
			{
				RegisterScript(ScriptName);
			}

			if (ScriptNames != null)
			{
				foreach (string scriptName in ScriptNames)
				{
					RegisterScript(scriptName);
				}
			}

			// ----------------------------------------
			void RegisterScript(string scriptName)
			{
				var host = new LuaInstance(battle, scriptName);
				_scriptHosts.Add(host);

				BattleAnim anim = host.OnRegister();
				if (anim != null)
					runner.Submit(anim);
			}

			// ----------------------------------------
		}


		public override void Unregister()
		{
			base.Unregister();

			foreach (LuaInstance host in _scriptHosts)
			{
				host.OnUnregister();
			}
		}

		public sealed class LuaInstance : BattleLua
		{
			public LuaInstance(Battle battle, string scriptName) : base(battle, scriptName: scriptName)
			{
				Reinitialize();
			}

			[CanBeNull]
			public BattleAnim OnRegister()
			{
				animsm.Start(baseEnv);
				Invoke(LuaEnv.FUNC_INIT);
				return animsm.EndInstant();
			}

			public void OnUnregister()
			{
				Invoke("on_unregister");
			}
		}
	}
}