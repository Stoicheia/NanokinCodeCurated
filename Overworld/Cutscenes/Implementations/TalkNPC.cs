using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Util;
using Combat.Scripting;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Overworld.Cutscenes.Implementations
{
	[DefaultExecutionOrder(1)]
	public class TalkNPC : SerializedMonoBehaviour/*, ILuaInit*/
	{
		[Title("NPC Lines")]
		[Required]
		public List<GameText> Lines = new List<GameText>();

		public bool DontDrawTestBust;

		private Table _script;

		private async void Start()
		{
			//Lua.OnReady(this);
			await GameController.TillIntialized();
			Init();
		}

		public void Init()
		{
			LuaChangeWatcher.BeginCollecting();

			_script               = Lua.NewEnv("TalkNPC");
			_script["self_actor"] = GetComponent<Actor>();
			_script["busts_on"]   = !DontDrawTestBust;
			Lua.LoadFileInto("talk_npc", _script);
			LuaChangeWatcher.EndCollecting(this, Init);
		}

		private void OnDestroy()
		{
			LuaChangeWatcher.ClearWatches(this);
		}

		private void OnEnable()
		{
			//DebugLogger.Log($"OnEnable {gameObject.GetNameWithPath()}, Instance ID:{GetInstanceID()}", gameObject, LogContext.Overworld, LogPriority.Low);

			Interactable interactable = gameObject.GetOrAddComponent<Interactable>();
			interactable.OnInteract.AddListener(OnInteract);
			interactable.ShowType = Interactable.Type.Talk;
		}

		private void OnDisable()
		{
			//DebugLogger.Log($"OnDisable {gameObject.GetNameWithPath()}, Instance ID:{GetInstanceID()}", gameObject, LogContext.Overworld, LogPriority.Low);
			Interactable interactable = gameObject.GetComponent<Interactable>();
			interactable.OnInteract.RemoveListener(OnInteract);
		}

		private void OnInteract()
		{
			Table tblLines = Lua.NewTable();
			foreach (GameText gtext in Lines)
				tblLines.Append(DynValue.NewString(gtext.GetString()/*.EscapeLuaString()*/));	//NOTE: Not sure what EscapeLuaString was supposed to fix with the dialogue, but commenting that function call out resolved the NPC dialogue issue with \" instead of ", so... *shrug*

			Lua.RunPlayer(_script, "toggle_bust", new object[] {!DontDrawTestBust});
			Lua.RunPlayer(_script, "main", new object[] {tblLines});
		}
	}
}