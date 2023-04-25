using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Anjin.Actors
{
	public class ActorDefinitionDatabase : SerializedScriptableObject
	{
		#region Database Stuff
		public const string DBResourcesPath = "Data/ActorRefs";

		private static ActorDefinitionDatabase _loadedDB;

		[ShowInInspector]
		public static ActorDefinitionDatabase LoadedDB {
			get
			{
				if (_loadedDB == null)
				{
					_loadedDB = Resources.Load<ActorDefinitionDatabase>(DBResourcesPath);
				}

				return _loadedDB;
			}
		}
		#endregion

		public List<ActorReferenceDefinition> 	ReferenceDefinitions;
		public List<ActorTagDefinition> 		TagDefinitions;

		public void AddDef(ActorReferenceDefinition def)
		{
			#if UNITY_EDITOR
			Undo.RecordObject(LoadedDB, "Add new Actor Definition");
			SaveAsset();
			#endif

			ReferenceDefinitions.Add(def);
		}

		public void RemoveDef(ActorReferenceDefinition def)
		{
			#if UNITY_EDITOR
			Undo.RecordObject(LoadedDB, "Remove Actor Definition");
			SaveAsset();
			#endif

			ReferenceDefinitions.Remove(def);
		}

		public void SaveAsset()
		{
		#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(LoadedDB);
			AssetDatabase.SaveAssets();
		#endif
		}

		public ActorReferenceDefinition FindDef(string _ID)
		{
			var def = ReferenceDefinitions.FirstOrDefault(x => x.ID == _ID);
			return def;
		}

		public ActorReferenceDefinition FindDefByPath(string path)
		{
			ActorReferenceDefinition def = null;
			for (int i = 0; i < ReferenceDefinitions.Count; i++)
			{
				def = ReferenceDefinitions[i];
				if (def.Path == path) return def;
			}

			return null;
		}

		public ActorTagDefinition FindTagDef(string _ID)
		{
			var def = TagDefinitions.FirstOrDefault(x => x.ID == _ID);
			return def;
		}
	}
}