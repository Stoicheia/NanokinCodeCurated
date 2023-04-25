using System;
using Anjin.Actors;
using Anjin.Nanokin;
using Anjin.Nanokin.Map;
using Anjin.Scripting;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Overworld.Cutscenes.Implementations {
	[LuaUserdata]
	public class PlayerBlocker : SerializedMonoBehaviour, ITriggerable {

		public LuaContainer Container = new LuaContainer();

		public Transform TargetWalkPoint;

		[NonSerialized, ShowInPlay]
		public Closure EnclosingCutscene;

		private bool _lock;

		private async void Start()
		{
			Container.OnStart(this, con => {
				Lua.envScript.DoFileSafe("player_blocker", con._state.table);
			});

			await GameController.TillLuaIntialized();
			Lua.RegisterToLevelTable(this);
		}

		public void OnTrigger(Trigger source, Actor actor, TriggerID triggerID = TriggerID.None)
		{
			if (_lock || !GameController.Live.IsPlayerControlled) return;
			_lock = true;
			Play().ForgetWithErrors();
		}

		public async UniTask Play()
		{
			try {
				await Container.TryPlayAsync("main");
			} catch (Exception e) {
				Debug.LogException(e);
				throw;
			}
			_lock = false;
		}
	}
}