using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using Anjin.Util;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Anjin.Actors
{
	[UsedImplicitly]
	[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
	public class ActorLuaProxyBase<A> : MonoLuaProxy<A> where A : Actor
	{
		public string ID => proxy.Reference.ID;

		public bool active
		{
			get => proxy.gameObject.activeSelf;
			set => proxy.gameObject.SetActive(value);
		}

		public void delete() => proxy.gameObject.Destroy();

		public Vector3 position
		{
			get => proxy.transform.position;
			set => teleport(value);
		}

		public void teleport(SpawnPoint spawn) => proxy.Teleport(spawn);

		public void teleport(Vector3 position)            => proxy.Teleport(position);
		public void teleport(float   x, float y, float z) => proxy.Teleport(new Vector3(x, y, z));

		public void teleport(ActorRef reference)
		{
			if (ActorRegistry.TryGet(reference, out var actor))
			{
				SpawnPoint spawn = actor.GetComponent<SpawnPoint>();
				if (spawn)
					proxy.Teleport(spawn);
				else
					proxy.Teleport(actor.transform.position);
			}
		}

		public void message(string msg, params DynValue[] args) => proxy.message(msg, args);

		public WaitableSpritePopup reaction(string reaction, float seconds) => new WaitableSpritePopup(proxy.Reaction(reaction, seconds));

		//BASE:
		public Closure on_interact { get => proxy.on_interact; set => proxy.on_interact = value; }

		//PATHING:
		public Closure on_path_begin      { get => proxy.on_path_begin;      set => proxy.on_path_begin = value; }
		public Closure on_path_end        { get => proxy.on_path_end;        set => proxy.on_path_end = value; }
		public Closure on_path_reach_node { get => proxy.on_path_reach_node; set => proxy.on_path_reach_node = value; }
		public Closure on_path_update     { get => proxy.on_path_update;     set => proxy.on_path_update = value; }
	}

	[UsedImplicitly]
	[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
	public class ActorLuaProxy : ActorLuaProxyBase<Actor> { }

	[UsedImplicitly]
	[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
	public class NPCActorLuaProxy : ActorLuaProxyBase<NPCActor>
	{
		public void appearance(Table options) => proxy.designer.SetSpritesUsingTable(options);
	}

	[UsedImplicitly]
	[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
	public class PropActorProxy : ActorLuaProxyBase<PropActor>
	{
		public string state          { get => proxy.CurrentState; set => proxy.CurrentState = value; }
		public void   update_state() => proxy.UpdateState();
	}

	[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
	public class ActorBrainLuaProxy : LuaProxy<ActorBrain>
	{
		//public void message(string msg, params DynValue[] args) => target.LUA_Message(msg, args);
	}

	[MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
	public class PlayerActorProxy : ActorLuaProxyBase<PlayerActor>
	{
		public PlayerCamera current_camera;
	}
}