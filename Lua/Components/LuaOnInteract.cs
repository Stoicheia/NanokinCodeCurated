using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Util;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Scripting
{
	[AddComponentMenu("Anjin: Lua Events/Lua on Interact")]
	public class LuaOnInteract : MonoBehaviour
	{
		[Required]
		public string FunctionName;

		public LuaAsset Asset;
		public AudioDef SFX_Interact;

		private Table        _table;
		private Interactable _interactable;

		private void Start()
		{
			_interactable = gameObject.GetOrAddComponent<Interactable>();
			if (_interactable)
			{
				_interactable.OnInteract.AddListener(OnInteract);
			}
		}

		private void OnInteract()
		{
			object input = gameObject;

			if (TryGetComponent(out Actor actor))
			{
				input = new DirectedActor(actor);
			}

			List<Coplayer> players = Lua.RunScriptOrGlobal(FunctionName, Asset, LuaUtil.Args(input));
			foreach (Coplayer player in players)
			{
				_interactable.locks++;
				player.sourceInteractable = _interactable;
			}


			GameSFX.Play(SFX_Interact, transform);
		}

		private void OnValidate()
		{
			if (string.IsNullOrEmpty(FunctionName))
			{
				FunctionName = gameObject.name.ToLower().Replace(' ', '_');
			}
		}
	}
}