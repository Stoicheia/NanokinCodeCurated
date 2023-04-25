using System.Collections.Generic;
using Anjin.Util;
using JetBrains.Annotations;
using Overworld.Tags;
using UnityEngine;

namespace Overworld.Controllers
{
	public static class Enabler
	{
		private static readonly Dictionary<int, EnableState> _states = new Dictionary<int, EnableState>();

		private static readonly List<int>        _oids              = new List<int>();
		private static readonly List<IActivable> _scratchActivables = new List<IActivable>(4);

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{
			_states.Clear();
		}

		public struct EnableState
		{
			/// <summary>
			/// Systems that disable this object.
			/// If any flag is turned off, the state is considered disabled.
			/// </summary>
			public SystemID states;

			/// <summary>
			/// A link to the object for reverse lookup.
			/// </summary>
			public readonly GameObject obj;


			/// <summary>
			/// If the object should be searched for activables.
			/// This is a performance feature, not every object needs this,
			/// only those that want to activate other objects.
			/// </summary>
			public bool enableActivables;

			public EnableState(GameObject obj) : this()
			{
				this.obj = obj;
				states   = SystemID.All;
			}

			/// <summary>
			/// Apply the changes, if there are any.
			/// </summary>
			public void Apply()
			{
				bool current = obj.activeSelf;

				bool becameInactive = states != SystemID.All && current;
				bool becameActive   = states == SystemID.All && !current;

				if (becameInactive)
				{
					if (enableActivables)
					{
						obj.GetComponentsInChildren(_scratchActivables);
						for (int j = 0; j < _scratchActivables.Count; j++)
							_scratchActivables[j].OnDeactivate();

						_scratchActivables.Clear();
					}

					obj.SetActive(false);
				}
				else if (becameActive)
				{
					obj.SetActive(true);

					if (enableActivables)
					{
						obj.GetComponentsInChildren(_scratchActivables);
						for (var j = 0; j < _scratchActivables.Count; j++)
							_scratchActivables[j].OnActivate();

						_scratchActivables.Clear();
					}
				}
			}
		}



		// TODO: enableActivatables does not take into account child objects that register in the Enabler.
		// I.E. a coplayer registers with enableActivatables as true, but the parent cutscene is the thing that
		// activated and deactivated.

		/// <summary>
		/// This simply adds the gameobject to the state.
		/// All systems will be enabled by default for this object.
		/// It's not necessary to call this, it will be done automatically, but
		/// it can be done as part of some other initialization.
		/// </summary>
		/// <param name="obj"></param>
		public static void Register([NotNull] GameObject obj, bool enableActivables = true)
		{
			int oid = obj.GetInstanceID();
			if (!_states.TryGetValue(oid, out EnableState state))
			{
				_states[oid] = new EnableState(obj)
				{
					enableActivables = enableActivables
				};
			}
			else
			{
				state.enableActivables = enableActivables;
				_states[oid]           = state;
			}
		}

		/// <summary>
		/// Remove the object when object is destroyed.
		/// Doesn't have to be called, but it's better for performance to do it if possible.
		/// </summary>
		/// <param name="obj"></param>
		public static void Deregister([NotNull] GameObject obj)
		{
			_states.Remove(obj.GetInstanceID());
		}

		private static void Register([NotNull] GameObject obj, out EnableState state, out int oid)
		{
			oid = obj.GetInstanceID();

			if (!_states.TryGetValue(oid, out state))
			{
				_states[oid] = state = new EnableState(obj);
			}
		}


		public static void Disable([NotNull] MonoBehaviour mb, SystemID id, int filter = 0)
		{
			Disable(mb.gameObject, id, filter);
		}

		public static void Enable([NotNull] MonoBehaviour mb, SystemID id, int filter = 0)
		{
			Enable(mb.gameObject, id, filter);
		}

		public static bool Disable([NotNull] GameObject obj, SystemID id,  int filter = 0)
		{
			Register(obj, out EnableState state, out int oid);

			state.states = state.states & ~id;
			state.Apply();

			_states[oid] = state;

			return state.states == SystemID.All;
		}


		public static bool Enable([NotNull] GameObject obj, SystemID id, int filter = 0)
		{
			Register(obj, out EnableState state, out int oid);

			state.states |= id;
			state.Apply();

			_states[oid] = state;
			SystemID checkState = SystemID.All - filter;

			return (state.states & checkState) == checkState;
		}

		/// <summary>
		/// Remove all non-existant objects.
		/// Call me during loading screens or when it doesn't matter.
		/// </summary>
		public static void RemoveDeadObjects()
		{
			// Note:
			// This is simply a precaution to keep the dictionary from growing endlessly.
			// Checking if an object is alive (null-check) has bad performance, so it's
			// best to use Deregister as much as possible. This functions is a last resort.

			foreach ((int oid, var state) in _states)
			{
				if (state.obj == null)
				{
					_oids.Add(oid);
				}
			}

			for (var i = 0; i < _oids.Count; i++)
			{
				_states.Remove(_oids[i]);
			}

			_oids.Clear();
		}

		public static bool Set([NotNull] GameObject go, bool state, SystemID id, int filter = 0)
		{
			if (state) return Enable(go, id, filter);
			else return Disable(go, id, filter);
		}
	}
}