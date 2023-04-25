using System;
using Anjin.Actors;
using Anjin.Regions;
using Anjin.Scripting.Waitables;
using MoonSharp.Interpreter;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Util;
using Object = UnityEngine.Object;

namespace Anjin.Scripting
{
	[LuaUserdata(StaticName = "Assets")]
	public class AssetsLua {

		public static WaitableAssetLoadLua load_audio_clip(string address) => LoadAssetWaitable<AudioClip>(address);
		public static WaitableAssetLoadLua load_graph(string      address) => LoadGraph(address);


		public static WaitableAssetLoadLua load_gameobject(string address) => LoadAssetWaitable<GameObject>(address);

		public static WaitableAssetLoadLua load_function(string address) => LoadAssetWaitable<EasingFunction>(address);
		// Components
		public static Table load_actor(string address) => LoadGameobject<Actor>(address);

		public static bool TryGetActorAssetAddress(string actor_path, out string path)
		{
			path = null;

			if (actor_path == null) return false;

			Table assets = Lua.envScript.Globals["actor_assets"] as Table;
			return assets.TryGet(actor_path, out path);
		}

		static Table LoadGameobject<TComponent>(string address) where TComponent : Component
		{
			var table  = Lua.glb_get_asset_table();
			var result = GameAssets.LoadAsset<GameObject>(address);

			result.Completed += h =>
			{
				var c = h.Result.GetComponent<TComponent>();
				table["_asset"]      = DynValue.FromObject(Lua.envScript, c);
				table["is_loaded"]   = true;
				table["instantiate"] = (Action) ( () => instantiate(c) );
			};

			return table;
		}

		static TObj instantiate<TObj>(TObj orig) where TObj : Object
		{
			return Object.Instantiate(orig);
		}

		public static DynValue instantiate(DynValue orig)
		{
			if(orig.Type == DataType.UserData) {
				if (orig.UserData.Object is Object obj)
					return DynValue.FromObject(Lua.envScript, Object.Instantiate(obj));
			} /*else if (orig.Type == DataType.Table) {
				var table = orig.Table;
				if(table.get)
			}*/

			return DynValue.Nil;
		}

		static Table LoadAsset<T>(string address)
		{
			var table  = Lua.glb_get_asset_table();
			var result = GameAssets.LoadAsset<T>(address);

			result.Completed += h =>
			{
				table["_asset"]    = DynValue.FromObject(Lua.envScript, h.Result);
				table["is_loaded"] = true;
			};

			return table;
		}

		static WaitableAssetLoadLua LoadGraph(string address)
		{
			var table  = Lua.glb_get_asset_table();
			var result = GameAssets.LoadAsset<RegionGraphAsset>(address);

			result.Completed += h =>
			{
				table["_asset"]    = DynValue.FromObject(Lua.envScript, h.Result.Graph);
				table["is_loaded"] = true;
			};

			return new WaitableAssetLoadLua(table);
		}

		static WaitableAssetLoadLua LoadAssetWaitable<T>(string address)
		{
			Table table  = Lua.glb_get_asset_table();

			AsyncOperationHandle<T> result = GameAssets.LoadAsset<T>(address);

			result.Completed += h =>
			{
				table["_asset"]    = DynValue.FromObject(Lua.envScript, h.Result);
				table["is_loaded"] = true;
			};

			return new WaitableAssetLoadLua(table);
		}
	}
}