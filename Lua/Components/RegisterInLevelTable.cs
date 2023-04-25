using System;
using Anjin.Nanokin;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;

namespace Anjin.Scripting {


	public class RegisterInLevelTable : MonoBehaviour {

		public enum Modes {
			GameObject = 0,
			Component = 1,
			Self = 2,
		}

		[EnumToggleButtons]
		public Modes     Mode         = Modes.GameObject;
		public string    OverrideName = "";
		public Component Component;

		private async void Start()
		{
			await GameController.TillIntialized();
			//await Lua.initTask;

			string _name = OverrideName.IsNullOrWhitespace() ? gameObject.name : OverrideName;

			switch (Mode) {
				case Modes.GameObject: Lua.RegisterToLevelTable(gameObject, _name); break;
				case Modes.Component:  Lua.RegisterToLevelTable(Component,  _name); break;
				case Modes.Self:       Lua.RegisterToLevelTable(this,       _name); break;
			}
		}

	}
}