using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Editor;
using Anjin.Scripting;
using Cysharp.Threading.Tasks;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;

namespace Anjin.Nanokin.Map
{
	public interface ITriggerable {
		void OnTrigger(Trigger source, Actor actor, TriggerID triggerID = TriggerID.None);
	}

	[Flags]
	public enum TriggerConditions
	{
		None		= 0,
		PlrGrounded = 1 << 1,
	}

	// NOTE (C.L. 02-8-2023): I'm doing this for now to avoid having to actually pass a string. Just keep adding to this, insuring to add explicit numbers to each enum value.
	public enum TriggerID
	{
		None	= 0,
		Enter	= 1,
	}

	[LuaUserdata]
	public class Trigger : SerializedMonoBehaviour
	{
		[ToggleButton(active_h: 0.3f, active_s: 1)]
		public bool RegisterInLevelTable = false;

		[InfoBox("Gameobject must be on layer TriggerVolume.", InfoMessageType.Warning, "ShowLayerWarning")]
		public bool DeactivateOnTrigger = false;

		public bool ShowLayerWarning => gameObject.layer != Layers.TriggerVolume.id;

		[NonSerialized]
		private bool _isPlayerInside;
		[ShowInPlay]
		public bool IsPlayerInside => _isPlayerInside;

		[NonSerialized, ShowInPlay]
		[GUIColor("ColorLocks")]
		public int locks;

		public bool DiscoverChildTriggerables;

		public TriggerID         ID         = TriggerID.None;
		public TriggerConditions Conditions = TriggerConditions.None;

		public List<ITriggerable> Triggerables = new List<ITriggerable>();

		private List<ITriggerable> _runtimeTriggerables;

		private void Awake()
		{
			_runtimeTriggerables = new List<ITriggerable>();

			if(Triggerables != null)
				_runtimeTriggerables.AddRange(Triggerables);

			_runtimeTriggerables.AddUniqueRange(GetComponents<ITriggerable>());

			if(DiscoverChildTriggerables)
				_runtimeTriggerables.AddUniqueRange(GetComponentsInChildren<ITriggerable>());
		}

		private async void Start()
		{
			await GameController.TillIntialized();
			//await Lua.initTask;
			if(RegisterInLevelTable) {
				Lua.RegisterToLevelTable(this);
			}
		}

		[GUIColor(0, 0.85f, 0, 1)]
		[Button(ButtonSizes.Large)]
		[LabelText("Trigger")]
		// [DRAW_IN_HIER]
		public bool OnTriggerBase(ActorKCC actor)
		{
			if (locks > 0) return false;

			if ((Conditions & TriggerConditions.PlrGrounded) != 0 && !actor.IsGroundState) {
				return false;
			}

			_isPlayerInside = true;

			OnTrigger();

			for (int i = 0; i < _runtimeTriggerables.Count; i++) {
				_runtimeTriggerables[i].OnTrigger(this, actor, ID);
			}

			// Built-in global lua invocation
			if(AutoLuaCall) {
				List<Coplayer> coplayers = Lua.RunGlobal(gameObject.name, manual_start: true, optional: true);
				foreach (Coplayer coplayer in coplayers) {
					locks++;
					coplayer.sourceTrigger = this;
					coplayer.sourceObject  = gameObject;
					coplayer.Play().Forget();
				}
			}

			if (DeactivateOnTrigger) {
				gameObject.SetActive(false);
			}

			return true;
		}

		public void OnPlayerLeave()
		{
			_isPlayerInside = false;
		}

		public virtual void OnTrigger() { }

		public virtual bool RequiresControlledPlayer => true;

		public virtual bool AutoLuaCall => true;

#if UNITY_EDITOR
		public Color ColorLocks => locks > 0 ? Color.grey : Color.white;
#endif
	}
}