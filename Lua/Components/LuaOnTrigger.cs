using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityUtilities;

namespace Anjin.Scripting
{
	[AddComponentMenu("Anjin: Lua Events/Lua on Trigger")]
	public class LuaOnTrigger : MonoBehaviour
	{
		[FormerlySerializedAs("layer")]
		public LayerMask Layer;
		public string FunctionName;

		[NonSerialized]
		public Table table;

		[NonSerialized]
		public int locks = 0;

		private void Awake()
		{
			if (!TryGetComponent(out Collider collider))
			{
				Debug.LogError("LuaOnTrigger: trigger collider is missing, which is required for this component to work!", this);
				return;
			}

			if (!collider.isTrigger)
			{
				Debug.LogWarning("LuaOnTrigger: the collider was set to 'trigger' automatically, since it is required for this component to work.", this);
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (locks > 0) return;
			if (Layer.ContainsLayer(other.gameObject.layer))
			{
				List<Coplayer> players = Lua.RunGlobal(FunctionName);
				foreach (Coplayer player in players)
				{
					locks++;
					player.afterStoppedTmp += () => locks--;
				}
			}
		}
	}
}