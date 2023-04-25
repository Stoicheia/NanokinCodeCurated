using System;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Editor;
using Anjin.Nanokin.Map;
using Anjin.Scripting;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.Nanokin
{
	// TODO Merge into a single Warp component with WarpReceiver
	// TODO - Warp is a receiver if it has only a
	// TODO - Warp is a warp if it has a targer receiver and a region
	// TODO - Warp can be both a receiver and warp (bidirectional)
	[LuaUserdata]
	public class WarpVolume : Trigger
	{
 
		public WarpTargetType Type = WarpTargetType.Level;

		public LevelManifest  TargetLevel;
		public SceneReference TargetScene;


		[WarpRef]
		public int TargetRecieverID = WarpReceiver.NULL_WARP;
		public float FadeOutTime = 1.5f;

		[ReadOnly] public TransitionVolume AttachedTransition;

		private void OnEnable()  => AttachedTransition = GetComponent<TransitionVolume>();
		private void OnDisable() => AttachedTransition = null;

		public override bool RequiresControlledPlayer => false;
		public override bool AutoLuaCall              => false;

		public override void OnTrigger()
		{
			base.OnTrigger();

			DynValue ret = Lua.InvokeGlobalFirst(gameObject.name, new object[] {this});

			WarpInstructions instructions = new WarpInstructions {
				type               = Type,
				target_level       = TargetLevel,
				target_scene       = TargetScene,
				target_reciever_id = TargetRecieverID,
				fade_time          = (FadeOutTime, FadeOutTime)
			};

			if (ret.IsNotNil() && ret.AsUserdata<WarpInstructions>(out var script_ins)) {
				instructions = script_ins;
			}

			//if (GameController.Live.DoWarp(TargetLevel, TargetRecieverID, (FadeOutTime, FadeOutTime))) {
			if (GameController.Live.DoWarp(instructions)) {
				if (ActorController.playerActor.activeBrain == ActorController.Live.TransitionBrain &&
					AttachedTransition                      == ActorController.Live.TransitionBrain.Volume) {
					ActorController.Live.TransitionBrain.isWarpOut = true;
					ActorController.Live.TransitionBrain.actor.LeaveArea(0, instructions.fade_time.GetValueOrDefault((0, 0)).out_time);
					GameCams.SetBlendOverride(GameCams.Cut, ActorController.Live.TransitionBrain);
				}
			}
		}

	}

	[EnumToggleButtons]
	public enum WarpTargetType { Level, Scene}

	public enum WarpSpawnType  { None, Reciever, Savepoint, Position }

	[LuaUserdata]
	public struct WarpInstructions {

		public WarpTargetType type;

		public LevelManifest  target_level;
		public SceneReference target_scene;

		public WarpSpawnType spawnType;

		public int?            target_reciever_id;
		public (float out_time, float in_time)? fade_time;

		public string  save_point_id;

		public Vector3 position;
		public Vector3 facing;

		public bool halt_music;

		/// <summary>
		/// Does the warp have a set destination, either a target level, scene, or in-level receiver?
		/// </summary>
		public bool valid {
			get {
				bool valid_loc    = true;

				if (spawnType == WarpSpawnType.Reciever && (target_reciever_id == null || target_reciever_id == WarpReceiver.NULL_WARP))
					valid_loc = false;

				if(type == WarpTargetType.Level && target_level == null)	return valid_loc;
				if(type == WarpTargetType.Scene && target_scene.IsInvalid)	return valid_loc;

				return true;
			}
		}

		[UsedImplicitly]
		[LuaGlobalFunc]
		public static WarpInstructions warp_instructions(Table table)
		{
			//Todo: Update with new features

			WarpInstructions ins = new WarpInstructions();

			if(table.TryGet("level", out DynValue lvl)) {
				ins.type = WarpTargetType.Level;

				if (lvl.AsUserdata(out ins.target_level, ins.target_level))	{ }
				else if (lvl.AsString(out string name))	{
					foreach (var manifest in LevelManifestDatabase.LoadedDB.Manifests) {
						if (manifest.name == name) {
							ins.target_level = manifest;
							break;
						}
					}
				}
			} else if (table.TryGet("scene", out DynValue scene)) {
				ins.type = WarpTargetType.Scene;
				if (scene.AsString(out string name)) {
					ins.target_scene = new SceneReference(name);
				} else if(scene.AsUserdata(out ins.target_scene, ins.target_scene)) { }
			}

			if(table.TryGet("target", out DynValue target)) {
				if (target.AsInt(out int result))
					ins.target_reciever_id = result;
			}

			bool has_fade_out = table.TryGet("fade_out", out float fade_out, 0);
			bool has_fade_in = table.TryGet("fade_in",   out float fade_in,  0);
			if (has_fade_out || has_fade_in) {
				ins.fade_time = (fade_out, fade_in);
			} else {
				ins.fade_time = (0, 1);
			}

			return ins;
		}
	}
}