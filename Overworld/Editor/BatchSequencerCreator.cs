using System.Collections.Generic;
using System.IO;
using System.Linq;
using Anjin.Editor;
using Anjin.Nanokin.ParkAI;
using API.Spritesheet;
using API.Spritesheet.Indexing;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEngine;
using g = UnityEngine.GUI;
using glo = UnityEngine.GUILayout;
using eg = UnityEditor.EditorGUI;
using eglo = UnityEditor.EditorGUILayout;

namespace UnityEditor
{
	public class BatchSequencerCreator : OdinEditorWindow
	{
		[MenuItem("Anjin/Tools/Batch Sequencer Creator")]
		public static void OpenWindow()
		{
			var window = GetWindow<BatchSequencerCreator>(true, "Batch Sequencer Creator");

			window.position = GUIHelper.GetEditorWindowRect().AlignCenter(312, 312);
		}

		private void OnSelectionChange()
		{
			selectedTextures.Clear();
			selectedTextures.AddRange(Selection.assetGUIDs.Select(guid => AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid))));
			selectedTextures.RemoveAll(x => x == null);

			selectedSequencers.Clear();
			selectedSequencers.AddRange(Selection.assetGUIDs.Select(guid => AssetDatabase.LoadAssetAtPath<IndexedSpritesheetAsset>(AssetDatabase.GUIDToAssetPath(guid))));
			selectedSequencers.RemoveAll(x => x == null);
		}

		private List<Texture2D>               selectedTextures   = new List<Texture2D>();
		private List<IndexedSpritesheetAsset> selectedSequencers = new List<IndexedSpritesheetAsset>();

		private int FrameWidth  = 64;
		private int FrameHeight = 64;

		private string prefix  = "Seq_";
		private string postfix = "";

		public PeepSpriteDatabase database;

		private Vector2 ListScrollPos = Vector2.zero;

		private void OnGUI()
		{
			Color inDBTag = new Color(0.16f, 1f, 0.14f, 0.66f);

			//EditorGUIUtility.ShowObjectPicker<IndexedSpritesheetAsset>(null, false, "", 0);
			EditorGUILayout.ObjectField(null, typeof(ScriptableObject), false);

			glo.BeginVertical(SirenixGUIStyles.BoxContainer);
			{
				EditorGUI.indentLevel = 1;
				ListScrollPos         = eglo.BeginScrollView(ListScrollPos, glo.MaxHeight(256));

				if (selectedTextures.Count > 0)
				{
					glo.Label("Selected Textures:", EditorStyles.boldLabel);
					for (int i = 0; i < selectedTextures.Count; i++)
					{
						glo.Label(i.ToString() + ":" + selectedTextures[i].name);
					}
				}

				if (selectedSequencers.Count > 0)
				{
					glo.Label("Selected Sequencers:", EditorStyles.boldLabel);
					for (int i = 0; i < selectedSequencers.Count; i++)
					{
						glo.BeginHorizontal();
						{
							glo.Label(i.ToString() + ":" + selectedSequencers[i].name);
							glo.FlexibleSpace();
							if (database != null)
							{
								if (database.ContainsSequencer(selectedSequencers[i]))
								{
									GUIHelper.PushColor(inDBTag);
									glo.BeginHorizontal(AnjinStyles.BoxContainerNoPadding);
									GUIHelper.PopColor();
									{
										glo.Label("In Database", EditorStyles.miniLabel, glo.Height(14));
									}
									glo.EndHorizontal();
								}
							}
						}
						glo.EndHorizontal();
					}
				}

				eglo.EndScrollView();
			}
			glo.EndVertical();

			glo.BeginHorizontal();
			{
				glo.Label("Frame Size:");

				glo.FlexibleSpace();

				glo.Label("W", glo.Width(20));
				FrameWidth = eglo.IntField(GUIContent.none, FrameWidth, glo.Width(72));

				glo.Label("H", glo.Width(20));
				FrameHeight = eglo.IntField(GUIContent.none, FrameHeight, glo.Width(72));
			}
			glo.EndHorizontal();

			glo.BeginHorizontal();
			{
				glo.Label("Prefix");
				prefix = eglo.TextField(GUIContent.none, prefix, glo.Width(100));
				glo.Label("Postfix");
				postfix = eglo.TextField(GUIContent.none, postfix, glo.Width(100));
			}
			glo.EndHorizontal();

			base.OnGUI();

			if (selectedTextures.Count == 0) GUI.enabled = false;

			if (glo.Button("Create Sequencers"))
			{
				Texture2D tex;
				for (int i = 0; i < selectedTextures.Count; i++)
				{
					tex = selectedTextures[i];
					var path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(tex)).Replace('\\', '/') + "/" +
							   prefix + tex.name +
							   postfix + ".asset";

					Debug.Log(path);

					Spritesheet spritesheet = new Spritesheet
					{
						Texture   = tex,
						CellSize = new Vector2Int(FrameWidth, FrameWidth)
					};

					SpritesheetUtilities.ReadSprites(spritesheet, with_index_in_name: true);
					SpritesheetUtilities.SliceButton(spritesheet);

					IndexedSpritesheetAsset spritesheetAsset = CreateInstance<IndexedSpritesheetAsset>();
					spritesheetAsset.spritesheet = new IndexedSpritesheet(spritesheet);

					AssetDatabase.CreateAsset(spritesheetAsset, path);

					if (database != null)
					{
						database.AddEntryFromSequence(spritesheetAsset);
					}

					//ProjectWindowUtil.StartNameEditingIfProjectWindowExists(asset.GetInstanceID(), ScriptableObject.CreateInstance<EndNameEdit>(), "SpriteAnimationSequencer.asset", AssetPreview.GetMiniThumbnail(asset), null);
				}

				if (database != null)
				{
					EditorUtility.SetDirty(database);
				}

				AssetDatabase.SaveAssets();
			}

			GUI.enabled = true;

			if (selectedSequencers.Count == 0) GUI.enabled = false;
			if (glo.Button("Add sequencer(s) to sprite database"))
			{
				if (database != null)
				{
					IndexedSpritesheetAsset seq;

					for (int i = 0; i < selectedSequencers.Count; i++)
					{
						seq = selectedSequencers[i];

						if (!database.ContainsSequencer(seq))
						{
							database.AddEntryFromSequence(seq);
						}
					}

					EditorUtility.SetDirty(database);
					AssetDatabase.SaveAssets();
				}
			}

			GUI.enabled = true;
		}
	}
}