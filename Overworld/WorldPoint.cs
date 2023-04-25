using System;
using Anjin.Actors;
using Anjin.Scripting;
using Combat.Skills.Generic;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anjin.Util
{
	public class SimpleWorldPointAttribute : Attribute { }

	/// <summary>
	/// This is so we can directly pass some objects like CoroutineFX to a
	/// worldpoint target. We could write a moonsharp converter, but then
	/// the CoroutineFX might not be loaded/instantiated yet by the time we
	/// create the WorldPoint.
	/// </summary>
	public interface IWorldPoint
	{
		Vector3    GetPosition();
		GameObject ToGameObject();
		ActorBase  ToActor();
	}

	[Serializable, HideReferenceObjectPicker]
	[LuaUserdata(StaticName = "WorldPoint")]
	public struct WorldPoint
	{
		[Serializable]
		public enum WorldPointMode
		{
			/// <summary>
			/// Using an actorref to lookup the global actor registry.
			/// </summary>
			ActorRef,

			/// <summary>
			/// Pointing at a specific gameobject.
			/// </summary>
			GameObject,

			/// <summary>
			/// Pointing at a position.
			/// </summary>
			Position,

			/// <summary>
			/// Pointing at a specific actor.
			/// </summary>
			Actor,

			/// <summary>
			/// Pointing at a specific transform.
			/// Offset can be local to the transform's orientation
			/// </summary>
			Transform,

			/// <summary>
			/// Pointing at a delegate position.
			/// </summary>
			Proxy,
		}

		public static WorldPoint Default => new WorldPoint
		{
			mode       = WorldPointMode.Position,
			reference  = ActorRef.NullRef,
			gameobject = null,
			position   = Vector3.zero
		};

		public WorldPointMode mode;

		public ActorRef   reference;
		public GameObject gameobject;
		public Vector3    position;
		public Vector3    offset;
		public OffsetMode offsetMode;
		public Transform  transform;

		[NonSerialized]
		public ActorBase actor;

		[NonSerialized]
		public IWorldPoint proxy;

		[FormerlySerializedAs("use_rot")]
		public bool rotOffset;
		[FormerlySerializedAs("use_scale")]
		public bool scaleOffset;

#if UNITY_EDITOR
		[NonSerialized] public bool editorExpand;
		[NonSerialized] public bool editorHover;
#endif

		public enum OffsetMode
		{
			Local,
			World,
			LocalPolar,
		}

		/// <summary>
		/// Creates a new worldpoint pointing at the given position.
		/// </summary>
		/// <param name="position"></param>
		public WorldPoint(Vector3 position) : this()
		{
			mode          = WorldPointMode.Position;
			reference     = ActorRef.NullRef;
			gameobject    = null;
			this.position = position;

			rotOffset   = false;
			scaleOffset = false;
		}

		/// <summary>
		/// Create a new worldpoint pointing at the given gameobject.
		/// </summary>
		/// <param name="mono"></param>
		public WorldPoint([NotNull] MonoBehaviour mono) : this(mono.gameObject) { }

		/// <summary>
		/// Create a new worldpoint pointing at the given actor.
		/// </summary>
		/// <param name="ref"></param>
		public WorldPoint(ActorRef @ref) : this()
		{
			mode       = WorldPointMode.ActorRef;
			reference  = @ref;
			gameobject = null;
			position   = new Vector3();

			rotOffset   = false;
			scaleOffset = false;
		}

		/// <summary>
		/// Create a new worldpoint pointing at the given gameobject, with an offset.
		/// </summary>
		/// <param name="gameobject"></param>
		/// <param name="offset"></param>
		/// <param name="rotOffset"></param>
		/// <param name="scaleOffset"></param>
		public WorldPoint(
			[CanBeNull] GameObject gameobject,
			Vector3                offset      = default,
			bool                   rotOffset   = false,
			bool                   scaleOffset = false) : this()
		{
			mode      = WorldPointMode.GameObject;
			reference = ActorRef.NullRef;

			this.gameobject  = gameobject;
			this.offset      = offset;
			this.rotOffset   = rotOffset;
			this.scaleOffset = scaleOffset;
		}

		/// <summary>
		/// Create a new worldpoint pointing at the given transform, with an offset.
		/// </summary>
		/// <param name="tf"></param>
		/// <param name="offset"></param>
		/// <param name="rotOffset"></param>
		/// <param name="scaleOffset"></param>
		public WorldPoint(Transform tf,
			Vector3                 offset      = default,
			bool                    rotOffset   = false,
			bool                    scaleOffset = false) : this()
		{
			mode      = WorldPointMode.Transform;
			reference = ActorRef.NullRef;

			transform        = tf;
			gameobject       = tf != null ? tf.gameObject : null;
			this.offset      = offset;
			this.rotOffset   = rotOffset;
			this.scaleOffset = scaleOffset;
		}

		/// <summary>
		/// Create a new worldpoint pointing at the given actor, with an offset.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="offset"></param>
		/// <param name="rotOffset"></param>
		/// <param name="scaleOffset"></param>
		public WorldPoint(
			[CanBeNull] ActorBase actor,
			Vector3               offset      = default,
			bool                  rotOffset   = false,
			bool                  scaleOffset = false) : this()
		{
			mode        = WorldPointMode.Actor;
			reference   = ActorRef.NullRef;
			this.actor  = actor;
			gameobject  = null;
			this.offset = offset;

			this.rotOffset   = rotOffset;
			this.scaleOffset = scaleOffset;
		}

		/// <summary>
		/// Create a new worldpoint pointing at the given delegate.
		/// </summary>
		/// <param name="proxy"></param>
		public WorldPoint(IWorldPoint proxy) : this()
		{
			mode       = WorldPointMode.Proxy;
			reference  = ActorRef.NullRef;
			this.proxy = proxy;
		}

		public static implicit operator Vector3(WorldPoint               p)    => p.Get();
		public static implicit operator Vector3?(WorldPoint              p)    => p.TryGet(out Vector3 pos) ? pos : (Vector3?)null;
		public static implicit operator WorldPoint(Vector3               p)    => new WorldPoint(p);
		public static implicit operator WorldPoint(GameObject            p)    => new WorldPoint(p);
		public static implicit operator WorldPoint([CanBeNull] Transform p)    => new WorldPoint(p == null ? null : p.gameObject);
		public static implicit operator WorldPoint(ActorRef              aref) => new WorldPoint(aref);

		/// <summary>
		/// Get the vector3 world position, or a default if it's unavailable.
		/// </summary>
		public Vector3 Get(Vector3 defaultValue = default)
		{
			return TryGet(out Vector3 result) ? result : defaultValue;
		}

		/// <summary>
		/// Gets the vector3 world position.
		/// </summary>
		/// <returns>Success</returns>
		public bool TryGet(out Vector3 pos)
		{
			pos = Vector3.zero;

			switch (mode)
			{
				case WorldPointMode.ActorRef:
				{
					if (ActorRegistry.TryGet(reference, out Actor actor))
					{
						pos = actor.transform.position + GetOffset(actor.transform);
						return true;
					}

					return false;
				}

				case WorldPointMode.Position:
				{
					pos = position + GetOffsetGlobal();
					return true;
				}

				case WorldPointMode.Actor:
				{
					Vector3 right = Vector3.Cross(actor.facing, Vector3.up);
					pos = actor.center + GetOffset(actor.facing, actor.Up, right);
					return true;
				}

				case WorldPointMode.GameObject:
				{
					if (gameobject == null) break;

					pos = gameobject.transform.position + GetOffset(gameobject.transform);
					return true;
				}

				case WorldPointMode.Transform:
				{
					if (transform == null) break;

					pos = transform.position + GetOffset(transform);
					return true;
				}

				case WorldPointMode.Proxy:
				{
					pos = proxy.GetPosition() + GetOffsetGlobal();
					return true;
				}

				default:
					throw new ArgumentOutOfRangeException();
			}

			return false;
		}

		public Vector3 TryGet()
		{
			var pos = Vector3.zero;

			switch (mode)
			{
				case WorldPointMode.ActorRef:
				{
					if (ActorRegistry.TryGet(reference, out Actor actor))
					{
						pos = actor.transform.position + GetOffset(actor.transform);
						return pos;
					}

					return pos;
				}

				case WorldPointMode.Position:
				{
					pos = position + GetOffsetGlobal();
					return pos;
				}

				case WorldPointMode.Actor:
				{
					Vector3 right = Vector3.Cross(actor.facing, Vector3.up);
					pos = actor.center + GetOffset(actor.facing, actor.Up, right);
					return pos;
				}

				case WorldPointMode.GameObject:
				{
					if (gameobject == null) break;

					pos = gameobject.transform.position + GetOffset(gameobject.transform);
					return pos;
				}

				case WorldPointMode.Transform:
				{
					if (transform == null) break;

					pos = transform.position + GetOffset(transform);
					return pos;
				}

				case WorldPointMode.Proxy:
				{
					pos = proxy.GetPosition() + GetOffsetGlobal();
					return pos;
				}

				default:
					throw new ArgumentOutOfRangeException();
			}

			return Vector3.zero;
		}

		private Vector3 GetOffsetGlobal()
		{
			return GetOffset(Vector3.forward, Vector3.up, Vector3.right);
		}

		private Vector3 GetOffset([NotNull] Transform transform) => GetOffset(transform.forward, transform.up, transform.right);

		private Vector3 GetOffset(Vector3 fwd, Vector3 up, Vector3 right)
		{
			// Vector3 offset = Matrix4x4.TRS(Vector3.zero,
			// 	rotOffset ? gameobject.transform.rotation : Quaternion.identity,
			// 	scaleOffset ? gameobject.transform.localScale : Vector3.one).MultiplyPoint3x4(position);

			switch (offsetMode)
			{
				case OffsetMode.Local:
					return fwd * offset.z + up * offset.y + right * offset.x;

				case OffsetMode.World:
					return offset;

				case OffsetMode.LocalPolar:
					float rad   = offset.y;
					float angle = offset.z;

					float cos = Mathf.Cos(angle * Mathf.Deg2Rad);
					float sin = Mathf.Sin(angle * Mathf.Deg2Rad);

					return rad * (
						       cos * fwd +
						       sin * actor.Up)
					       + right * offset.x;

				default:
					throw new ArgumentOutOfRangeException();
			}


			return offset;
		}

		[CanBeNull]
		public GameObject ToGameObject()
		{
			switch (mode)
			{
				case WorldPointMode.ActorRef:
				{
					if (ActorRegistry.TryGet(reference, out Actor actor))
						return actor.gameObject;

					break;
				}

				case WorldPointMode.GameObject:
					return gameobject;

				case WorldPointMode.Transform:
					return transform == null ? null : transform.gameObject;

				case WorldPointMode.Position:
					return gameobject;
				case WorldPointMode.Proxy:
					return proxy.ToGameObject();

				case WorldPointMode.Actor:
					return actor.gameObject;

				default: throw new ArgumentOutOfRangeException();
			}

			return null;
		}

		[CanBeNull]
		public ActorBase ToActor()
		{
			switch (mode)
			{
				case WorldPointMode.ActorRef:
				{
					if (ActorRegistry.TryGet(reference, out Actor actor))
						return actor;

					break;
				}

				case WorldPointMode.GameObject:
					if (gameobject.TryGetComponent<Actor>(out Actor a))
					{
						return a;
					}

					return null;

				case WorldPointMode.Transform:
					if (transform != null && transform.TryGetComponent<Actor>(out Actor ac))
					{
						return ac;
					}

					return null;

				case WorldPointMode.Position: break;
				case WorldPointMode.Proxy:
					return proxy.ToActor();

				case WorldPointMode.Actor:
					return actor;

				default: throw new ArgumentOutOfRangeException();
			}

			return null;
		}

		[CanBeNull]
		public Transform ToTransform()
		{
			switch (mode)
			{
				case WorldPointMode.ActorRef:
				{
					if (ActorRegistry.TryGet(reference, out Actor a))
						return a.transform;

					break;
				}
				case WorldPointMode.GameObject:
					return gameobject != null ? gameobject.transform : null;

				case WorldPointMode.Position: return gameobject != null ? gameobject.transform : null;
				case WorldPointMode.Proxy:
					var go = proxy.ToGameObject();
					return go != null ? go.transform : null;

				case WorldPointMode.Actor:     return actor.transform;
				case WorldPointMode.Transform: return transform;
				default:                       throw new ArgumentOutOfRangeException();
			}

			return null;
		}

		public void OnDuplicate() => reference = ActorRef.NullRef;

		public Vector3 GetFxPosition(FxInfo origin, bool hasOrigin)
		{
			if (mode == WorldPointMode.Position) return GetOffsetGlobal() + position;

			ActorBase  actr = ToActor();
			GameObject go   = ToGameObject();

			var offsetVec = actr != null
				? GetOffset(actr.facing, actr.Up, Vector3.Cross(actr.facing, actr.Up))
				: GetOffset(go.transform);

			return go != null
				? go.GetOriginPositionFast(actr, origin, actor != null, hasOrigin) + offsetVec
				: Vector3.zero;
		}

		// From MotionWaypoint
		// public void Init()
		// {
		// 	if (Value.mode == WorldPoint.WorldPointMode.ActorRef && !hasActor && ActorRegistry.TryGet(Value.reference, out Actor _))
		// 	{
		// 		Value = new WorldPoint(gameObject);
		// 	}
		//
		// 	switch (Value.mode)
		// 	{
		// 		case WorldPoint.WorldPointMode.GameObject:
		// 			gameObject = Value.gameobject;
		// 			valid      = gameObject != null;
		// 			if (valid)
		// 			{
		// 				gameObject = gameObject.gameObject;
		// 				hasActor   = gameObject.TryGetComponent(out actor);
		// 			}
		//
		// 			break;
		//
		// 		case WorldPoint.WorldPointMode.Position:
		// 			valid = true;
		// 			break;
		//
		// 		case WorldPoint.WorldPointMode.Proxy:
		// 			valid = Value.proxy != null;
		// 			break;
		// 	}
		// }

		public void SetActorRef(ActorRef reference)
		{
			this.reference = reference;
		}


		// TODO: Make this more versatile

		[UsedImplicitly]
		[LuaGlobalFunc("world_point")]
		public static WorldPoint luaConstructor([NotNull] params DynValue[] args)
		{
			WorldPoint p = Default;

			if (args.Length > 0)
				if (GetFromDynValue(args[0], out var _wp, Default))
					p = _wp;

			if (args.Length > 1)
			{
				if (args[1].AsUserdata(out Vector3 vec))
					p.position = vec;
			}

			return p;
		}

		public static bool GetFromDynValue(DynValue val, out WorldPoint wp, WorldPoint Default)
		{
			wp = Default;

			if (val.Type == DataType.UserData)
			{
				switch (val.UserData.Object)
				{
					case WorldPoint _wp:
						wp = _wp;
						return true;

					case Vector3 pos:
						wp = new WorldPoint(pos);
						return true;

					case GameObject obj:
						wp = new WorldPoint(obj);
						return true;

					case Transform trans:
						wp = new WorldPoint(trans.gameObject);
						return true;

					case MonoBehaviour mono:
						wp = new WorldPoint(mono.gameObject);
						return true;

					case DirectedActor dactor:
						wp = new WorldPoint(dactor.gameObject);
						return true;
				}
			}

			return false;
		}

		public Vector3 vec()
		{
			return position;
		}

		public override string ToString() => $"WorldPoint({mode}, {position}, {reference}, {gameobject}, {proxy}, {actor}, {transform})";
	}

	[UsedImplicitly]
	public class WorldPointLuaProxy : LuaProxy<WorldPoint>
	{
		[UsedImplicitly]
		public Vector3 get() => proxy.Get();

		[UsedImplicitly]
		public static WorldPoint make_actor(string id) => new WorldPoint(new ActorRef(id, ""));

		[UsedImplicitly]
		public static WorldPoint make_position(Vector3 pos) => new WorldPoint(pos);
	}
}