using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Scripting;
using Cinemachine;
using Combat.Components;
using Combat.Components.VictoryScreen.Menu;
using Combat.Data;
using Drawing;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Controllers;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using Util.Components;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;

namespace Combat
{
	/// <summary>
	/// Describes an arena where battles can take place.
	/// </summary>
	public class Arena : AnjinBehaviour
	{
		[ShowInPlay]
		public static List<Arena> All = new List<Arena>();

		[Optional] public string ID;
		[SerializeField] public GameObject Grids;
		[SerializeField] public Vector3 CameraCenterOffset;
		[SerializeField] public AudioClip Music;
		[SerializeField] public ArenaIntroProperties IntroParams;
		[SerializeField] public ArenaVictoryController VictoryParams;
		[SerializeField] public ArenaCamera Camera;

		[NonSerialized, ShowInPlay]
		public bool IsLoadedAtRuntime;

		[NonSerialized, ShowInPlay]
		public Config config;

		public List<SlotLayoutEntry> SlotLayouts = new List<SlotLayoutEntry>();

		[SerializeField] public CinemachineVirtualCamera VCam;
		[SerializeField] public bool FleeingDisabled;

		private void Awake()
		{
			IsLoadedAtRuntime = false;
			config = default;
		}

		private void OnEnable() => All.AddIfNotExists(this);
		private void OnDisable() => All.Remove(this);

		public void Reset()
		{
			Profiler.BeginSample("Arena Reset");
			Profiler.EndSample();
		}

		public void OnInit()
		{
			config = default;
			if (Lua.FindFirstGlobal(gameObject.name, out Table tbl))
			{
				if (tbl.TryGet("deactivate", out DynValue deactivate))
				{
					if (deactivate.AsString(out string layer))
						config.deactivate_flags = layer;
				}

				if (tbl.TryGet("activate", out DynValue activate))
				{
					if (activate.AsString(out string layer))
						config.activate_flags = layer;
				}

				tbl.TryGet("on_begin", out config.on_begin, config.on_begin);
				tbl.TryGet("on_end", out config.on_end, config.on_end);
			}
		}

		public void OnBegin()
		{
			if (config.deactivate_flags != null) LayerController.Deactivate(config.deactivate_flags);
			if (config.activate_flags != null) LayerController.Activate(config.activate_flags);

			if (config.on_begin != null)
			{
				config.on_begin.Call();
			}

			Grids.SetActive(true);
		}

		public void OnEnd()
		{
			if (config.deactivate_flags != null) LayerController.Activate(config.deactivate_flags);
			if (config.activate_flags != null) LayerController.Deactivate(config.activate_flags);

			if (config.on_end != null)
			{
				config.on_end.Call();
			}

			Grids.SetActive(false);
		}

		public SlotLayout GetSlotLayout(string key)
		{
			bool Predicate(SlotLayoutEntry plotter)
			{
				return plotter.Key == key;
			}

			if (SlotLayouts.Any(Predicate))
				return SlotLayouts.FirstOrDefault(Predicate).Layout;

			Debug.LogError($"Could not find any slot layout with the key {key}");
			return null;
		}

		// private void OnDrawGizmos()
		// {
		// 	const float size = 0.4f;
		//
		// 	Gizmos.color = Color.black;
		// 	Gizmos.DrawCube(transform.position, Vector3.one * size);
		//
		// 	Gizmos.color = Color.cyan;
		// 	Gizmos.DrawWireCube(transform.position, Vector3.one * size);
		//
		// 	// Vector3 dir = Quaternion.Euler(0, forwardAngle, 0) * Vector3.forward;
		// 	// Gizmos.DrawLine(transform.position, transform.position + dir);
		// }

		public static bool FindByName(string name, out Arena arena)
		{
			arena = null;
			foreach (Arena a in All)
			{
				if (a.name == name)
				{
					arena = a;
					return true;
				}
			}

			return false;
		}

		public override void DrawGizmos()
		{
			const float size = 0.4f;

			using (Draw.WithLineWidth(2.5f))
			using (Draw.InLocalSpace(transform))
			{
				using (Draw.WithColor(Color.black))
				{
					Draw.SolidBox(float3.zero, Vector3.one * size);
				}

				// I think this is useless information now, we simply set the base rot on the OrbitExtension
				// using (Draw.WithColor(Color.cyan))
				// {
				// 	Draw.WireBox(Vector3.zero, Vector3.one * size);
				// 	Draw.Arrowhead(Vector3.zero + Vector3.forward * size * 2, Vector3.forward, Vector3.up, size * 0.5f);
				// }
			}
		}

		[Serializable]
		public struct SlotLayoutEntry
		{
			[FormerlySerializedAs("key")]
			public string Key;

			[FormerlySerializedAs("layout")]
			public SlotLayout Layout;
		}

		public struct Config
		{
			public Closure on_begin;
			public Closure on_end;


			// TODO(C.L.): Make it so these are lists of flags
			[CanBeNull]
			public string deactivate_flags;

			[CanBeNull]
			public string activate_flags;
		}

		[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
		public class ArenaProxy : MonoLuaProxy<Arena>
		{
		}
	}
}