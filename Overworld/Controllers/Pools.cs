using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Overworld.Controllers
{
	public enum PooledRefState
	{
		Instantiated,	// The object was successfuly 'instantiated' from the pool.
		AssetLoading,	// The asset hasn't been loaded yet and we need to wait for it to load.
		Error,			// There was some kind of error loading the asset.
		Invalid			// The object is invalid. Most likely because it has been returned to the pool.
	}

	//TODO: Lua Scripting

	/// <summary> The public-facing API for interacting with a pooled object. </summary>
	public struct PooledRef<T>
		where T : Object
	{
		public readonly int ID;
		[ShowInInspector]
		Pools.ObjGroup<T> Group;

		public PooledRef(Pools.ObjGroup<T> group, int id)
		{
			Group 	= group;
			ID 		= id;
		}

		/// <summary> Warning: Calling this will update the reference state. </summary>
		public T Object {
			get {
				if (GetState(out var state))
					return state.Obj;
				return null;
			}
		}

		/// <summary> Warning: Calling this will update the reference state. </summary>
		public PooledRefState State {
			get {
				if (GetState(out var state)) return state.State;
				return PooledRefState.Invalid;
			}
		}

		[Button]
		public void Return()
		{
			if(GetState(out var state)) {
				state.Return();
			}
		}

		/// <summary>
		/// ONLY USE IN NON-VOLATILE SITUATIONS SUCH AS INITIALIZATION.
		/// DO NOT USE WHEN YOU ARE PULLING AND PUSHING THINGS IN AND OUT OF A POOL CONSTANTLY.
		/// </summary>
		public Action<T> OnInstantiate {
			set {
				if (GetState(out var state)) state.SetOnInstantiate(value);
			}
		}

		/// <summary>
		/// This flag will be false if the asset is loading. Once the asset was loaded, (or if the asset was already loaded), it'll
		/// be set to true until it is accessed. This means you shouldn't 'need an additional bool in the object holding the reference.
		/// Use this in place of the OnLoaded delegate in situations where reducing allocations is important.
		/// </summary>
		public bool InstantiateTrigger {
			get {
				if (!GetState(out var state)) return false;
				if (!state.InstantiateTrigger) return false;
				state.InstantiateTrigger = false;
				return true;
			}
		}

		//TODO: This is fairly slow to do on large groups of objects. Improve this so we don't need to do a dictionary serach every time.
		bool GetState(out Pools.ObjGroup<T>.RefState<T> state) {
			state = null;

			if (Group.ActiveStateRegistry.TryGetValue(ID, out var _state)) {
				state = _state;
				return true;
			}
			return false;
		}

		[ShowInInspector] public bool IsLoading => State == PooledRefState.AssetLoading;
		[ShowInInspector] public bool IsValid 	=> State == PooledRefState.Instantiated;

		public static implicit operator T(PooledRef<T> pr) 		=> pr.Object;
		public static implicit operator bool(PooledRef<T> pr) 	=> pr.IsValid;
	}

	public class Pools : StaticBoy<Pools>
	{
		public Transform Root;

		[NonSerialized] public Dictionary<string, Object> LoadedAssets;

		// GROUPS
		[NonSerialized] public List<BaseObjGroup>               Groups;
		[NonSerialized] public Dictionary<string, BaseObjGroup> GroupAddresses;
		[NonSerialized] public Dictionary<Type, BaseObjGroup>   InactiveGroups;

		[NonSerialized] public List<BaseObjGroup>               LoadingGroups;
		[NonSerialized] public Dictionary<string, BaseObjGroup> LoadingGroupAdresses;


		protected override void OnAwake()
		{
			Root = transform;

			LoadedAssets = new Dictionary<string, Object>();

			Groups 			= new List<BaseObjGroup>();
			GroupAddresses 	= new Dictionary<string, BaseObjGroup>();
			InactiveGroups	= new Dictionary<Type, BaseObjGroup>();

			LoadingGroups 		 = new List<BaseObjGroup>();
			LoadingGroupAdresses = new Dictionary<string, BaseObjGroup>();

			Root.hierarchyCapacity = 10000;

		}

		//TODO: AssetReference<T> version of Get<T>
		public static PooledRef<T> Get<T>(string address)
			where T : Object => Live.get<T>(address);

		PooledRef<T> get<T>(string address) where T : Object
		{
			//Is the asset already loaded? Get the group for it.
			if (Live.LoadedAssets.ContainsKey(address))
				return GetLoadedGroup<T>(address).Get();

			//If the address has a loading group, we need to return that.
			if (LoadingGroupAdresses.TryGetValue(address, out BaseObjGroup loading)) {
				var group = loading as ObjGroup<T>;
				return group.Get();
			}

			//Start Loading
			var handle = Addressables.LoadAssetAsync<T>(address);

			//If the asset loaded immediately, we just immediately return a group for it.
			if (handle.Result != null) {
				LoadedAssets[address] = handle.Result;
				return GetLoadedGroup<T>(address).Get();
			}

			//If not, generate a loading group.
			return GetLoadingGroup(address, handle).Get();
		}

		ObjGroup<T> GetLoadedGroup<T>(string address) where T : Object
		{
			if (GroupAddresses.ContainsKey(address)) {
				return GroupAddresses[address] as ObjGroup<T>;
			} else if (InactiveGroups.TryGetValue(typeof(T), out BaseObjGroup existing)) {
				var group = existing as ObjGroup<T>;
				group.Init(address, LoadedAssets[address] as T);
				RegisterGroup(group);
				return group;
			}{
				var group = new ObjGroup<T>(address, LoadedAssets[address] as T);
				RegisterGroup(group);
				return group;
			}
		}

		ObjGroup<T> GetLoadingGroup<T>(string address, AsyncOperationHandle<T> load_handle) where T : Object
		{
			if (Live.GroupAddresses.ContainsKey(address)) {
				var group = Live.GroupAddresses[address] as ObjGroup<T>;
				return group;
			} else if (Live.InactiveGroups.TryGetValue(typeof(T), out BaseObjGroup existing)) {
				var group = existing as ObjGroup<T>;
				group.Init(address, load_handle);
				RegisterLoadingGroup(group);
				return group;
			}{
				var group = new ObjGroup<T>(address, load_handle);
				RegisterLoadingGroup(group);
				return group;
			}
		}

		void RegisterGroup(BaseObjGroup group)
		{
			Groups.Add(group);
			GroupAddresses[group.Address] = group;
		}

		void RegisterLoadingGroup(BaseObjGroup group)
		{
			LoadingGroups.Add(group);
			LoadingGroupAdresses[group.Address] = group;
		}

		void Update()
		{
			for (int i = 0; i < LoadingGroups.Count; i++) {
				var grp = LoadingGroups[i];
				grp.Update();
				if (grp.UpdateLoaded(out Object obj)) {
					LoadingGroups.RemoveAt(i--);
					LoadingGroupAdresses.Remove(grp.Address);

					LoadedAssets[grp.Address] = obj;

					Groups.Add(grp);
					GroupAddresses[grp.Address] = grp;
				}
			}

			for (int i = 0; i < Groups.Count; i++)
				Groups[i].Update();
		}

		public abstract class BaseObjGroup
		{
			public string Address;
			public int ID;
			public bool Loading;
			public abstract bool UpdateLoaded(out Object obj);
			public abstract Type AssetType { get; }
			public abstract void Update();
		}

		public class ObjGroup<T> : BaseObjGroup
			where T: Object
		{
			public T 						LoadedAsset;
			public AsyncOperationHandle<T> 	LoadHandle;

			public List<T> 	Active;
			public List<T> 	Inactive;

			public override Type AssetType => typeof(T);

			public List<RefState<T>> 			ActiveStates;
			public List<RefState<T>> 			InactiveStates;
			public Dictionary<int, RefState<T>> ActiveStateRegistry;

			ObjGroup(string address)
			{
				ID             = Guid.NewGuid().GetHashCode();
				Active         = new List<T>();
				Inactive       = new List<T>();

				ActiveStates = new List<RefState<T>>();
				ActiveStateRegistry = new Dictionary<int, RefState<T>>();
				InactiveStates 	= new List<RefState<T>>();

				Address        = address;
				Loading = false;
			}

			public ObjGroup(string address, T asset) : this(address) {
				LoadedAsset = asset;
			}

			public ObjGroup(string address, AsyncOperationHandle<T> load_handle) : this(address) {
				LoadHandle  = load_handle;
				Loading 	= true;
			}

			void Init(string address)
			{
				ID      = Guid.NewGuid().GetHashCode();
				Address = address;
				Active.Clear();
				Inactive.Clear();
				ActiveStateRegistry.Clear();
				ActiveStates.Clear();
				Loading = false;

				//TODO: Reset all states!
			}

			public void Init(string address, T asset)
			{
				Init(address);
				LoadedAsset = asset;
			}

			public void Init(string address, AsyncOperationHandle<T> load_handle)
			{
				Init(address);
				LoadHandle = load_handle;
				Loading = true;
			}

			public override void Update()
			{
				var cnt = ActiveStates.Count;
				for (int i = 0; i < cnt; i++) {
					ActiveStates[i].Update();
				}
			}

			public (T, int) OnGet()
			{
				if (Inactive.Count == 0) AddNew();

				var obj =  Inactive[0];
				Inactive.RemoveAt(0);
				Active.Add(obj);

				var id = Guid.NewGuid().GetHashCode();

				if(obj is GameObject go)
					go.SetActive(true);
				else if (obj is Component com)
					com.gameObject.SetActive(true);
				return (obj, id);
			}

			public PooledRef<T> Get()
			{
				var ref_id = Guid.NewGuid().GetHashCode();
				RefState<T> new_ref = GetNewRefState(ref_id);

				if(Loading)
					new_ref.Init(ref_id, null, true);
				else {
					var (spawned, id) = OnGet();
					new_ref.Init(ref_id, spawned, false);
				}

				ActiveStateRegistry[ref_id] = new_ref;
				//Live.ValidRefIDs.Add(ref_id);

				return new PooledRef<T>(this, ref_id);
			}

			public void Return(int id)
			{
				var state = ActiveStateRegistry[id];
				var obj = state.Obj;
				if (obj == null || !Active.Contains(obj)) return;

				Active.RemoveAt(0);
				Inactive.Add(obj);

				if(obj is GameObject go)
					go.SetActive(false);
				else if (obj is Component com)
					com.gameObject.SetActive(false);

				ReturnRefState(state.ID);
			}

			public void AddNew()
			{
				T obj = Instantiate(LoadedAsset, Live.Root);
				Inactive.Add(obj);
			}

			public override bool UpdateLoaded(out Object loaded_obj)
			{
				loaded_obj = null;

				if (!Loading || !LoadHandle.IsDone) return false;

				LoadedAsset = LoadHandle.Result;
				loaded_obj  = LoadHandle.Result;

				Loading = false;
				return true;
			}

			RefState<T> GetNewRefState(int id)
			{
				if (InactiveStates.Count == 0) {
					var state = new RefState<T>(id, this);
					ActiveStates.Add(state);
					ActiveStateRegistry[id] = state;
					return state;
				} else {
					var state = InactiveStates[0];
					InactiveStates.RemoveAt(0);
					ActiveStates.Add(state);
					ActiveStateRegistry[id] = state;
					return state;
				}
			}

			void ReturnRefState(int id)
			{
				if (ActiveStateRegistry.TryGetValue(id, out var state)) {
					state.Reset();
					ActiveStates.Remove(state);
					ActiveStateRegistry.Remove(id);
					InactiveStates.Add(state);
				}
			}

			public class RefState<T>
				where T:Object
			{
				public int 			  ID;
				public ObjGroup<T>    Group;
				public PooledRefState State;
				public Action<T>      OnInstantiate;
				public bool           OnInstantiateTripped;
				public bool           InstantiateTrigger;

				public T Obj;

				public RefState(int id, ObjGroup<T> group)
				{
					ID = id;
					Group = group;
					Reset();
				}

				public void Update()
				{
					//Don't do any complex processing if the ref is known to be invalid.
					if (State == PooledRefState.Invalid ||
					    State == PooledRefState.Error)
						return;

					if (State == PooledRefState.AssetLoading) {
						if (!Group.Loading) {
							var (spawned, id) = Group.OnGet();
							Obj = spawned;
							State = PooledRefState.Instantiated;

							OnInstantiate?.Invoke(Obj);
							OnInstantiateTripped = true;
							InstantiateTrigger = true;
						}
					}
				}

				public void SetOnInstantiate(Action<T> callback)
				{
					if (State == PooledRefState.Instantiated) {
						if (!OnInstantiateTripped) {
							OnInstantiateTripped = true;
							callback?.Invoke(Obj);
						}
					} else {
						OnInstantiate = callback;
					}
				}

				public void Return()
				{
					if (State == PooledRefState.Instantiated || State == PooledRefState.AssetLoading) {
						Group.Return(ID);

					}
				}

				public void Reset()
				{
					ID = -1;
					State = PooledRefState.Invalid;
					InstantiateTrigger   = false;
					OnInstantiate        = null;
					OnInstantiateTripped = false;
					Obj                  = null;
				}

				public void Init(int id, T obj, bool loading) {
					Reset();
					ID  = id;
					Obj = obj;
					if(obj != null) 	{
						State = PooledRefState.Instantiated;
						InstantiateTrigger = true;
					} else if (loading)
						State = PooledRefState.AssetLoading;
				}


			}
		}
	}
}