using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Scripting;
using Combat.Scripting;
using Drawing;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Components;

namespace Overworld.Cutscenes.Implementations
{
	[DefaultExecutionOrder(1)] // This is required for the cutscene to play after CutsceneBrain
	public class ProximityTalkNPC : AnjinBehaviour, ILuaInit
	{
		[FormerlySerializedAs("Distance"), FormerlySerializedAs("TriggerProximity")]
		[Min(0)]
		public float ActivationRadius = 5;

		[Min(0)]
		public float StayRange = 2f;

		public float BubbleWaitTime = 2;

		[Required]
		public List<GameText> Lines = new List<GameText>();

		[NonSerialized] public bool proximityActive;

		private Table    _script;
		private Coplayer _player;
		private bool     _initialized;

		private void Start()
		{
			if (GameOptions.current.init_on_demand)
				Lua.OnReady(this);
		}

		public void OnLuaReady()
		{
			LuaChangeWatcher.BeginCollecting();

			_script               = Lua.NewScript("talk_npc_proximity");
			_script["self_actor"] = GetComponent<Actor>();

			LuaChangeWatcher.EndCollecting(this, OnLuaReady);

			_initialized = true;
		}

		private void OnDestroy()
		{
			LuaChangeWatcher.ClearWatches(this);
		}

		private void Update()
		{
			if (ActorController.playerActor == null) return;

			float playerDistance = Vector3.Distance(transform.position, ActorController.playerActor.position);

			float proximityRadius = ActivationRadius;
			if (proximityActive)
				proximityRadius += StayRange;

			bool inRange = playerDistance <= proximityRadius;

			if (!proximityActive && inRange)
			{
				// Entered proximity distance
				OnEnterRadius();
			}
			else if (proximityActive && !inRange)
			{
				OnLeaveRadius();
			}
		}

		private void OnEnterRadius()
		{
			if (!_initialized)
				OnLuaReady();

			proximityActive = true;

			_player                 = Lua.RunPlayer(_script, "main", new object[] {Lines, BubbleWaitTime});
			_player.afterStoppedTmp = () => _player = null;
		}

		private void OnLeaveRadius()
		{
			// Left proximity...
			proximityActive = false;

			if (_player != null)
				_player.Stop();
		}

		public override void DrawGizmos()
		{
			base.DrawGizmos();

			if (GizmoContext.InSelection(transform))
			{
				using (Draw.WithColor(Color.blue))
				{
					Draw.CircleXZ(transform.position, ActivationRadius);
				}

				using (Draw.WithColor(Color.yellow))
				{
					Draw.CircleXZ(transform.position, ActivationRadius + StayRange);
				}
			}
		}
	}
}