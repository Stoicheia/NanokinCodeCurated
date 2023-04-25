// using System;
// using System.Collections.Generic;
// using Anjin.Core.Flags.Components;
// using Anjin.Nanokin;
// using Anjin.Scripting;
// using Sirenix.OdinInspector;
// using Sirenix.Utilities;
// using UnityEngine;

using System;
using System.Collections.Generic;
using Anjin.Core.Flags;
using Anjin.Core.Flags.Components;
using Anjin.Nanokin;
using Anjin.Scripting;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Overworld.Controllers
{
	public class LayerState
	{
		/// <summary>
		/// The state, or new state that is not yet applied.
		/// </summary>
		public bool state = true;

		/// <summary>
		/// The state that is currently active.
		/// </summary>
		public bool activeState = true;

		/// <summary>
		/// Objects that this layer controls.
		/// </summary>
		public readonly List<Layer> objects = new List<Layer>();

		/// <summary>
		/// If the state was changed, apply it to the layers.
		/// </summary>
		/// <param name="forceImmediate"></param>
		public void ApplyChange(bool forceImmediate = false, bool forceApply = false)
		{
			if (forceApply || state != activeState)
			{
				activeState = state;

				for (int i = 0; i < objects.Count; i++)
				{
					objects[i].Apply(state, forceImmediate, forceApply);
				}
			}
		}
	}

	public class LayerController : StaticBoy<LayerController>
	{
		[NonSerialized]
		[ShowInInspector]
		public static List<LayerState> all;

		[NonSerialized]
		[ShowInInspector]
		public static Dictionary<string, LayerState> allByID;

		/// <summary>
		/// Layers that have their state refresh delayed.
		/// </summary>
		[NonSerialized]
		[ShowInInspector]
		public static List<Layer> waitingObjects;

		private static bool _forcingApply;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		public static void Init()
		{
			all            = new List<LayerState>();
			allByID        = new Dictionary<string, LayerState>();
			waitingObjects = new List<Layer>();
		}

		private void Start()
		{
			Flags.OnAnyFlagUpdated -= OnAnyFlagUpdated;
			Flags.OnAnyFlagUpdated += OnAnyFlagUpdated;
		}

		private void OnAnyFlagUpdated(FlagStateBase obj)
		{
			// When any flag changes, reinvoke 'on_activate'.
			if (Lua.Ready)
				GameController.CallLuaOnActivate();
				//Lua.InvokeGlobal("on_activate", null, true);
		}

		public static void ManuallyUpdateActivation(bool forceApply = false)
		{
			if (Lua.Ready) {
				_forcingApply = forceApply;
				GameController.CallLuaOnActivate();
				//Lua.InvokeGlobal("on_activate", null, true);
				_forcingApply = false;
			}
		}

		/// <summary>
		///	Register a layer into the controller.
		/// </summary>
		public static void Register([NotNull] Layer layer)
		{
			string group_id = layer.GetID();

			if (string.IsNullOrWhiteSpace(group_id))
			{
				Debug.LogError($"Activation: Layer {layer.name} has a group ID that's either null or whitespace. We can't register it.", layer);
				return;
			}

			LayerState state = GetState(group_id);
			state.objects.Add(layer);
			layer.ForceApply(state.state);
		}

		public static void Deregister(Layer id)
		{
			for (int i = 0; i < all.Count; i++)
			{
				LayerState state = all[i];
				state.objects.Remove(id);
				// TODO remove the layer state when it's empty.
			}
		}

		[LuaGlobalFunc("activate"), Button]
		public static void Activate(string layerID, bool b = true, bool force_immediate = false)
		{
			if (string.IsNullOrWhiteSpace(layerID))
			{
				Debug.LogError($"Activation: Could not activate or deactivate the group {layerID}, as its ID is null or whitespace.", Live);
				return;
			}

			LayerState state = GetState(layerID);

			state.state = b;
			state.ApplyChange(force_immediate, _forcingApply);
		}

		/// <summary>
		/// Deactivate a layer.
		/// </summary>
		/// <param name="layerID"></param>
		/// <param name="b">Whether or not to deactivate the layer.</param>
		/// <param name="force_immediate"></param>
		[LuaGlobalFunc("deactivate"), Button]
		public static void Deactivate(string layerID, bool b = true, bool force_immediate = false) => Activate(layerID, !b, force_immediate);

		/// <summary>
		/// Get the layer state for an ID.
		/// </summary>
		public static LayerState GetState(string layerId)
		{
			if (string.IsNullOrWhiteSpace(layerId))
				return null;

			if (!allByID.TryGetValue(layerId, out LayerState state))
			{
				state            = new LayerState();
				allByID[layerId] = state;
				all.Add(state);
			}

			return state;
		}

		[LuaGlobalFunc("find_with_layer")]
		public static object FindWithLayer(string id, DynValue _type = null)
		{
			if (allByID.ContainsKey(id)) {
				LayerState state = allByID[id];

				if (state.objects.Count <= 0) return null;

				if(_type == null || !_type.AsUserdata(out Type type)) {
					return state.objects[0];
				}

				for (int i = 0; i < state.objects.Count; i++) {
					Layer obj = state.objects[i];
					if (obj.TryGetComponent(type, out Component comp))
						return comp;
				}
			}

			return null;
		}

		private void Update()
		{
			// Remove inactive Layers
			// ----------------------------------------
			for (int i = 0; i < all.Count; i++)
			{
				var grp = all[i];
				for (int j = 0; j < grp.objects.Count; j++)
				{
					if (grp.objects[j] == null)
					{
						grp.objects.RemoveAt(j);
						j--;
					}
				}
			}


			// Auto apply layers that were waiting for cutscenes to end.
			// ----------------------------------------
			ProcessWaitingForCutscene();
		}

		public static void ProcessWaitingForCutscene()
		{
			if (!GameController.Live.IsCutsceneControlled && waitingObjects.Count > 0)
			{
				for (int i = 0; i < waitingObjects.Count; i++)
				{
					Layer state = waitingObjects[i];

					if (allByID.TryGetValue(state.GetID(), out LayerState group))
					{
						state.Apply(group.state);
						state.waitingCutscenes = false;
					}
				}

				waitingObjects.Clear();
			}
		}
	}
}