using System;
using System.Collections.Generic;
using System.IO;
using MoonSharp.Interpreter;
using UnityEditor;
using UnityEngine;
#if UNITY_EDITOR
using Anjin.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor.Experimental.AssetImporters;

#endif

namespace Anjin.Scripting
{
#if UNITY_EDITOR


	[ScriptedImporter(1, "lua")]
	public class LuaAssetImporter : ScriptedImporter
	{
		public override void OnImportAsset(AssetImportContext ctx)
		{
			if (ctx == null) return;

			LuaAsset asset = AssetDatabase.LoadAssetAtPath<LuaAsset>(ctx.assetPath);
			if (asset == null)
			{
				asset           = ScriptableObject.CreateInstance<LuaAsset>();
				asset.functions = new List<string>();

				ctx.AddObjectToAsset("main obj", asset);
				ctx.SetMainObject(asset);
			}

			asset.Path = ctx.assetPath;
			asset.functions.Clear();

			asset.hideFlags     = HideFlags.None;
			asset.ErrorMessage  = "";
			asset.ErrorOnImport = false;

			asset.UpdateText(File.ReadAllText(ctx.assetPath));

			UnityEditor.EditorUtility.SetDirty(asset);
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			AssetDatabase.SaveAssets();
			// PopulateInfo(asset);
		}

		private static void PopulateInfo(LuaAsset asset)
		{
			//Experimental, try compiling
			List<string> functions = new List<string>();
			//List<string> variables = new List<string>();

			try
			{
				Script env = new Script();
				env.Options.DebugPrint = Debug.Log;

				Table temp_table = new Table(env);

				temp_table["get_actor"] = env.LoadString("");

				env.DoString(asset.Text, temp_table);

				DynValue val;
				foreach (var pair in temp_table.Pairs)
				{
					val = pair.Value;

					switch (val.Type)
					{
						case DataType.Boolean:
						case DataType.Number:
						case DataType.String:
							//variables.Add(pair.Key.CastToString());
							break;

						case DataType.Function:
							functions.Add(pair.Key.CastToString());
							break;

						case DataType.Table: break;
						case DataType.Tuple: break;
					}
				}

				asset.functions.AddRange(functions);

				asset.ErrorOnImport = false;
				asset.ErrorMessage  = "";
			}
			catch (Exception e)
			{
				asset.ErrorOnImport = true;
				asset.ErrorMessage  = e.Message;
			}
		}
	}

#endif

#if UNITY_EDITOR
	/*[CustomEditor(typeof(LUAAssetImporter))]
	public class LUAAssetImporterEditor : ScriptedImporterEditor
	{
		protected override bool needsApplyRevert => false;

		public override void OnInspectorGUI()
		{
			GUILayout.Label("TEST");

		}
	}*/
#endif
}