using System;
using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using Cysharp.Threading.Tasks;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Anjin.UI
{
	public class ScriptedHUDElement : LuaScriptComponent
	{
		public enum State { Off, On, Transition }
		public State state = State.Off;

		public HUDElement HudElement;

		public Closure on_activate;
		public Closure on_deactivate;

		public Closure seq_show;
		public Closure seq_hide;

		Action onAfterShow;
		Action onAfterHide;

		public void Awake()
		{
			HudElement       = GetComponent<HUDElement>();

			if(HudElement != null)
				HudElement.Alpha = 0;

			onAfterShow = OnAfterShow;
			onAfterHide = OnAfterHide;
		}

		protected override void OnInit()
		{
			ScriptTable["element"] = HudElement;
			Lua.envScript.DoFileSafe("scripted_hud_element", ScriptTable);
		}

		public WaitableCoroutineInstance Show()
		{

			if (state != State.Off) return null;

			state = State.Transition;
			HudElement.SetChildrenActive(true);

			if (seq_show != null)
				RunSequence(seq_show, onAfterShow);
			else
				HudElement.Alpha = 1;

			if (on_activate == null) return null;
			return new WaitableCoroutineInstance(Lua.RunCoroutine(on_activate));
		}

		public WaitableCoroutineInstance Hide()
		{
			if (state != State.On) return null;
			state = State.Transition;

			if (seq_hide != null)
				RunSequence(seq_hide, onAfterHide);
			else
				HudElement.Alpha = 0;

			if (on_deactivate == null) return null;
			return new WaitableCoroutineInstance(Lua.RunCoroutine(on_deactivate));
		}

		void OnAfterHide() => state = State.Off;
		void OnAfterShow() => state = State.On;

		public async UniTaskVoid RunSequence(Closure seq, Action onStopped = null)
		{
			var instance = Lua.CreateCoroutine(seq);
			_player.RestoreCleared();
			_player.afterStoppedTmp = onStopped;
			await _player.Play(ScriptTable, instance);

		}

		public class ScriptedHUDElementProxy : LuaComponentBaseProxy<ScriptedHUDElement>
		{
			public WaitableCoroutineInstance show() => proxy.Show();
			public WaitableCoroutineInstance hide() => proxy.Hide();

			public Closure on_activate   { get => proxy.on_activate;   set => proxy.on_activate   = value; }
			public Closure on_deactivate { get => proxy.on_deactivate; set => proxy.on_deactivate = value; }

			public Closure seq_show { get => proxy.seq_show; set => proxy.seq_show   = value; }
			public Closure seq_hide { get => proxy.seq_hide; set => proxy.seq_hide   = value; }
		}
	}
}