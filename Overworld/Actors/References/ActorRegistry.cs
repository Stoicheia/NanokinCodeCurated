using System;
using System.Collections.Generic;
using System.Text;
using Anjin.Nanokin;
using Anjin.Util;
using JetBrains.Annotations;
using Sirenix.Utilities;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	[DefaultExecutionOrder(-1000)]
	public class ActorRegistry
	{
		[NonSerialized, ShowInPlay]
		public static Dictionary<string, Actor> IDRegistry;

		[NonSerialized, ShowInPlay]
		public static Dictionary<string, Actor> FullPathRegistry;

		private static StringBuilder _sb;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			IDRegistry       = new Dictionary<string, Actor>();
			FullPathRegistry = new Dictionary<string, Actor>();

			_sb = new StringBuilder();
		}

		public static void Register(Actor actor)
		{
			if (Get(actor.Reference) == null)
			{
				if (actor.RegisterByName)
				{
					Register(actor, actor.gameObject.name);
				}
				else if (!actor.Reference.IsNullID)
				{
					Register(actor, actor.Reference);
				}
			}
		}

		public static void Deregister(Actor actor)
		{
			if (actor.RegisterByName)
			{
				if (IDRegistry.ContainsKey(actor.gameObject.name))
					IDRegistry.Remove(actor.gameObject.name);
				//Deregister(actor, actor.gameObject.name); // IMPORTANT: This will break if the actor's name changes at runtime.
			}
			else if (!actor.Reference.IsNullID) {

				//string path = null;

				var def = ActorDefinitionDatabase.LoadedDB.FindDef(actor.Reference.ID);
				if(def != null) {
					_sb.Clear();

					if (actor.Reference.Path.Length > 0) {
						_sb.Append(actor.Reference.Path);
						_sb.Append("/");
					}

					_sb.Append(actor.Reference.Name);

					_sb.Replace("//", "/");

					var str = _sb.ToString().ToLower();
					if (FullPathRegistry.ContainsKey(str))
						FullPathRegistry.Remove(str);
				}

				if (IDRegistry.ContainsKey(actor.Reference.ID))
					IDRegistry.Remove(actor.Reference.ID);

				//Deregister(actor, actor.Reference);
			}
		}

		private static void Register(Actor actor, string id)
		{
			if (actor == null || id == ActorRef.NULL_ID) return;

			if (IDRegistry.ContainsKey(id))
			{
				Debug.LogError($"ActorRegistry: Tried to register the same ID ({id}) twice.", actor as Actor);
				return;
			}

			IDRegistry[id] = actor;

			var def = ActorDefinitionDatabase.LoadedDB.FindDef(id);
			if (def != null)
			{
				IDRegistry[id] = actor;

				_sb.Clear();

				if (def.Path.Length > 0)
				{
					_sb.Append(def.Path);
					_sb.Append("/");
				}

				_sb.Append(def.Name);

				_sb.Replace("//", "/");

				FullPathRegistry[_sb.ToString().ToLower()] = actor;

				"ActorRegistry".LogTrace("Register: ", _sb.ToString().ToLower());
			}
		}

		/*private static void Deregister(Actor actor, string id)
		{
		}*/

		private static void Register(Actor actor, ActorRef reference)
		{
			if (actor == null || reference.IsNullID) return;
			if (IDRegistry.ContainsKey(reference.ID))
			{
				Debug.LogError($"ActorRegistry: Tried to register the same reference ({reference.ToString()}) twice.", actor as Actor);
				return;
			}

			Register(actor, reference.ID);
		}

		/*private static void Deregister(Actor actor, ActorRef reference)
		{
			Deregister(actor, reference.ID);
		}*/

		[CanBeNull]
		public static T FindByPath<T>([CanBeNull] string path)
			where T : MonoBehaviour
		{
			var a = FindByPath(path);
			return a == null ? null : a.GetComponent<T>();
		}

		[CanBeNull]
		public static Actor FindByPath([CanBeNull] string path)
		{
			if (path.IsNullOrWhitespace()) return null;
			if (path[0] == '$') return IDRegistry.GetOrDefault(path.Substring(1)); // Get by ID

			if (path.Substring(0, 2) == "..")
			{
				if (!GameController.ActiveLevel || !GameController.ActiveLevel.Manifest) return null;

				_sb.Clear();
				_sb.Append(GameController.ActiveLevel.Manifest.ActorReferencePath);
				_sb.Append(path.Substring(2));
				return FullPathRegistry.GetOrDefault(_sb.ToString().ToLower());
			}

			return FullPathRegistry.GetOrDefault(path.ToLower());
		}

		// Get an actor from a reference

		[CanBeNull]
		public static bool TryGet(ActorRef reference, out Actor actor)
		{
			actor = null;

			if (reference.ID == null || reference.IsNullID)
				return false;

			// Optimizes player actorref by skipping dictionary
			if (reference.ID == ActorRef.Nas.ID)
			{
				actor = ActorController.playerActor;
				return actor != null;
			}

			if (IDRegistry.TryGetValue(reference.ID, out actor))
			{
				actor = IDRegistry[reference.ID];
				return true;
			}

			return false;
		}

		[CanBeNull]
		public static bool TryGet([CanBeNull] string id, out Actor actor)
		{
			actor = null;

			if (id == null) return false;
			if (!IDRegistry.ContainsKey(id)) return false;

			actor = IDRegistry[id];
			return true;
		}

		[Obsolete]
		[CanBeNull]
		public static Actor Get(ActorRef reference)
		{
			if (reference.ID == null || reference.IsNullID || !IDRegistry.ContainsKey(reference.ID)) return null;
			return IDRegistry[reference.ID];
		}

		[Obsolete]
		[CanBeNull]
		public static Actor Get([CanBeNull] string id)
		{
			if (id == null) return null;
			if (!IDRegistry.ContainsKey(id)) return null;
			return IDRegistry[id];
		}
	}
}