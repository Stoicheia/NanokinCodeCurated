using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Anjin.Editor;
using Anjin.EventSystemNS;
using Anjin.Util;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using g 	= UnityEngine.GUI;
using glo 	= UnityEngine.GUILayout;
using eg 	= UnityEditor.EditorGUI;
using eglo 	= UnityEditor.EditorGUILayout;
using Event = UnityEngine.Event;
using Undo = UnityEditor.Undo;

namespace Anjin.Actors
{
	public class ActorDefSelectorPopup
	{
		Action<ActorReferenceDefinition> OnItemChangeCallback 	= null;
		Action<ActorReferenceDefinition> OnRightClick 			= null;

		Vector2      treeScroll = Vector2.zero;
		ActorDefTree actorTree;

		ActorDefinitionDatabase 	db;
		ActorReferenceDefinition 	editing;

		StringBuilder builder;


		enum Mode
		{ Viewing, Adding, Editing}


		Mode mode;
		//bool adding = false;

		public const int WINDOW_WIDTH = 300;

		public static void NewPopup(ActorRef? selected, float? x, float? y, ActorDefinitionDatabase database, Action<ActorReferenceDefinition> OnChangeCallback)
		{
			var popup = new ActorDefSelectorPopup();
			popup.OnItemChangeCallback = OnChangeCallback;
			popup.db = database;
			popup.Show(x, y);
		}

		public ActorDefSelectorPopup()
		{
			mode = Mode.Viewing;
			Undo.undoRedoPerformed += UndoRedoPerformed;
		}

		public void OnClose()
		{
			Undo.undoRedoPerformed -= UndoRedoPerformed;
		}

		void UndoRedoPerformed()
		{
			UpdateTree();
		}

		public void Show(float? x, float? y)
		{
			var win = OdinEditorWindow.InspectObjectInDropDown(this, WINDOW_WIDTH, 350);
			/*win.position = new Rect(
				x.GetValueOrDefault(win.position.x),
				y.GetValueOrDefault(win.position.y),
				win.position.width,
				win.position.height );*/

			SetupWindow(win, EditorWindow.focusedWindow);
		}

		public void SetupWindow(OdinEditorWindow window, EditorWindow prevSelectedWindow)
		{
			builder = new StringBuilder();

			//winInstance = window;
			window.WindowPadding =  Vector4.zero;//new Vector4(5,5,5,5);

			int prevFocusId      = GUIUtility.hotControl;
			int prevKeybaordFocus = GUIUtility.keyboardControl;
			window.UseScrollView = false;

			CreateTree();
			UpdateTree();

			window.OnClose += OnClose;

			window.OnBeginGUI += (() =>
			{
				GUIHelper.PushColor(ActorRefDrawerStyles.backgroundColor);
				glo.BeginVertical(EventStyles.GreyBackground);
				GUIHelper.PopColor();
				{
					glo.Label("Select Reference:", ActorRefDrawerStyles.Header);

					//tree.DrawMenuTree();
					DrawTree();
				}

				if (Event.current.type != EventType.KeyDown || Event.current.keyCode != KeyCode.Escape)
					return;

				UnityEditorEventUtility.DelayAction(window.Close);

				if (prevSelectedWindow)
					prevSelectedWindow.Focus();

				Event.current.Use();
			});

			window.OnEndGUI += () =>
			{
				glo.FlexibleSpace();
				DrawFooter();
				glo.EndVertical();
			};

			window.OnClose += (() =>
			{
				GUIUtility.hotControl      = prevFocusId;
				GUIUtility.keyboardControl = prevKeybaordFocus;
			});
		}

		void DrawTree()
		{
			treeScroll = glo.BeginScrollView(treeScroll, true, false, GUIStyle.none, GUI.skin.verticalScrollbar);
			{
				glo.BeginVertical();
				{
					actorTree.DrawTree();
					glo.FlexibleSpace();
				}
				glo.EndVertical();
			}
			glo.EndScrollView();
		}

		string new_ref_input;

		string new_ref_path;
		string new_ref_name;

		void StartAdding()
		{
			if (mode == Mode.Viewing)
			{
				new_ref_input 	= "";
				new_ref_path 	= "";
				new_ref_name 	= "";

				mode = Mode.Adding;
			}
		}

		void DrawFooter()
		{
			var style = ActorRefDrawerStyles.TreeItemText_hover;
			var loadedLevel = EditorLevelCache.GetLevel();
			var levelPath = ( loadedLevel != null && loadedLevel.Manifest != null )
				? loadedLevel.Manifest.ActorReferencePath
				: "";

			glo.BeginHorizontal();
			{
				glo.BeginVertical();
				{
					var height = GetFooterHeight(style);
					var footerRect = GUILayoutUtility.GetRect(0, height);
					EditorGUI.DrawRect(footerRect.MoveBottom(16), MenuTreeStyles.fillColor_normal_light);

					if (mode == Mode.Adding)
					{
						DrawPathEditing(footerRect, height);

						glo.BeginHorizontal();
						{
							glo.FlexibleSpace();

							if (glo.Button("Cancel"))
								mode = Mode.Viewing;

							if (glo.Button("Add")) {
								var def = new ActorReferenceDefinition(new_ref_name, new_ref_path);
								ActorDefinitionDatabase.LoadedDB.AddDef(def);
								UpdateTree();
								OnItemChangeCallback?.Invoke(def);
								mode = Mode.Viewing;
							}
						}
						glo.EndHorizontal();
					}
					else if (mode == Mode.Editing) {

						DrawPathEditing(footerRect, height);

						glo.BeginHorizontal();
						{
							glo.FlexibleSpace();

							if (glo.Button("Cancel"))
								mode = Mode.Viewing;

							if (glo.Button("Save")) {
								editing.Name = new_ref_name;
								editing.Path = new_ref_path;
								Undo.RecordObject(db, "Edit Actor Ref Definition");
								db.SaveAsset();
								UpdateTree();
								mode = Mode.Viewing;
							}
						}
						glo.EndHorizontal();
					}
					else if(mode == Mode.Viewing)
					{
						if (actorTree.currentItemHovering != null)
						{
							var def = actorTree.currentItemHovering.itemValue;
							if (def != null)
							{
								builder.Clear().Append("Path: ").Append(def.Path);

								if (!def.Path.EndsWith("/"))
									builder.Append("/");

								var pathContent = new GUIContent(builder.ToString());
								var size        = style.CalcSize(pathContent);

								g.Label(footerRect,                            pathContent,     style);
								g.Label(footerRect.Inset(size.x - 4, 0, 0, 0), def.Name,        ActorRefDrawerStyles.TreeItemText_foldout);
								g.Label(footerRect.Move(0, 16),                "ID: " + def.ID, style);
							}
						}

						builder.Clear();
						if (loadedLevel != null && loadedLevel.Manifest != null)
							builder.Append("Level Path: ")
							       .Append("(\"").Append(loadedLevel.Manifest.ActorReferencePath).Append("/\")");

						g.Label(footerRect.Inset(0, 32, 0, 0), builder.ToString(), style);

						glo.BeginHorizontal();
						{
							glo.FlexibleSpace();
							if (glo.Button("Add"))
								StartAdding();
						}

					}
				}
				glo.EndVertical();
			}
			glo.EndHorizontal();
		}

		public float GetFooterHeight(GUIStyle style)
		{
			if (mode != Mode.Viewing) return 48;
			else {
				var pathContent = new GUIContent(new_ref_path);
				return Mathf.Max(style.CalcHeight(pathContent, WINDOW_WIDTH), 16) + 32;
			}
		}

		void CreateTree()
		{
			actorTree = new ActorDefTree( null, OnItemChangeCallback, OnItemRightClickedCallback);
		}

		void DrawPathEditing(Rect rect, float height)
		{
			var style = ActorRefDrawerStyles.TreeItemText_hover;
			var loadedLevel = EditorLevelCache.GetLevel();
			var levelPath = ( loadedLevel != null && loadedLevel.Manifest != null )
								? loadedLevel.Manifest.ActorReferencePath
								: "";

			string prev = new_ref_input;
			new_ref_input = g.TextField(rect.Resize(null, 16).MoveRight(16), new_ref_input);

			var pathContent = new GUIContent(new_ref_path);
			var size        = style.CalcSize(pathContent);

			g.Label(rect.MoveTop(16),                            new_ref_path,                                  style);
			g.Label(rect.MoveTop(16).Inset(size.x - 4, 0, 0, 0), new_ref_name,                                  ActorRefDrawerStyles.TreeItemText_foldout);
			g.Label(rect.MoveTop(height - 16),         "(use ../ for the current level's directory)", ActorRefDrawerStyles.ID);

			if (prev != new_ref_input)
			{
				builder.Clear();
				if(new_ref_input.Length > 0)
				{
					int dirIndex = new_ref_input.LastIndexOf("/");

					if (dirIndex == -1)
					{
						new_ref_path = String.Empty;
						new_ref_name = new_ref_input;
					}
					else if (new_ref_input.Length >= 2 && new_ref_input.Substring(0, 2) == ".." && levelPath != "")
					{
						builder.Append(levelPath).Append(new_ref_input.Substring(2));
						var str = builder.ToString();
						var ind = str.LastIndexOf("/");
						new_ref_path = str.Substring(0, ind + 1);
						new_ref_name = str.Substring(ind + 1).Replace("/", "");
					}
					else
					{
						new_ref_path = new_ref_input.Substring(0, dirIndex + 1);
						new_ref_name = new_ref_input.Substring(dirIndex + 1).Replace("/", "");
					}
				}
				else
				{
					new_ref_path = "";
					new_ref_name = "";
				}
			}
		}

		/// <summary>
		/// Transforms the ActorDef database into a tree.
		/// </summary>
		void UpdateTree()
		{
			actorTree.Clear();
			actorTree.roots.Add(new ActorDefItem("Actor Definitions") { type = ItemType.Section });
			var db = ActorDefinitionDatabase.LoadedDB;

			if (db == null)
			{
				actorTree.roots.Add(new ActorDefItem("Error: Loaded DB can't be found.", null));
				return;
			}


			List<ActorReferenceDefinition> References = new List<ActorReferenceDefinition>(db.ReferenceDefinitions);

			References.Sort((x, y)=>
			{
				if (x.Path.Length > 0 && y.Path.Length == 0) return -1;
				else if (x.Path.Length == 0 && y.Path.Length > 0) return 1;

				var a = x.Path + x.Name;
				var b = y.Path + y.Name;
				return String.Compare(a, b, StringComparison.Ordinal);
			});

			//If any definitions have invalid paths, we need to display them on a separate root.
			List<ActorReferenceDefinition> InvalidPaths = new List<ActorReferenceDefinition>();

			//Validate all paths, sort the invalid ones into the invalid root
			for (int i = 0; i < References.Count; i++)
			{
				//If the path is empty it just gets added to the root
				if(References[i].Path.Replace(" ","").Length > 0 &&
				  !ActorReferenceDefinition.IsPathValid(References[i].Path))
				{
					InvalidPaths.Add(References[i]);
					References.RemoveAt(i);
					i--;
				}
			}

			var actorSection = actorTree.roots[0];

			actorSection.Add(new ActorDefItem("[None]", null));

			for (int i = 0; i < References.Count; i++)
			{
				var reference = References[i];
				var item = new ActorDefItem(reference.Name, reference);

				var folders = reference.Path.Split('/');
				string folderName;
				MenuTreeItem<ActorReferenceDefinition> folder = actorSection;
				for (int j = 0; j < folders.Length; j++)
				{
					folderName = folders[j];
					if (folderName.Replace(" ", "").Length == 0) continue;

					folder.InsureChildren();

					var found = folder.children.FirstOrDefault(x=>x.type == ItemType.Foldout && x.text == folderName);

					if (found != null)
						folder = found;
					else
					{
						var newFolder = new MenuTreeItem<ActorReferenceDefinition>(folderName);
						newFolder.expand = false;
						folder.Add(newFolder);
						folder = newFolder;
					}
				}

				if (folder != null)
					folder.Add(item);
			}



			if(InvalidPaths.Count > 0)
			{
				var invalidSection = new ActorDefItem("Invalid");
				actorTree.roots.Add(invalidSection);

				for (int i = 0; i < InvalidPaths.Count; i++)
				{
					var reference = InvalidPaths[i];
					invalidSection.Add(new ActorDefItem(reference.Name, reference));
				}
			}
		}

		public void OnItemRightClickedCallback(ActorReferenceDefinition def)
		{
			if (def == null || db == null || !db.ReferenceDefinitions.Exists(D => D.ID == def.ID)) return;

			GenericMenu menu = new GenericMenu();

			menu.AddItem(new GUIContent("Edit"), false, onContext_edit, def);
			menu.AddItem(new GUIContent("Delete"), false, onContext_delete, def);

			menu.ShowAsContext();

		}

		void onContext_delete(object user_data)
		{
			var def = user_data as ActorReferenceDefinition;
			if (def == null || db == null) return;

			db.RemoveDef(def);
			UpdateTree();
		}

		void onContext_edit(object user_data)
		{
			var def = user_data as ActorReferenceDefinition;
			if (def == null || db == null) return;

			if (mode == Mode.Viewing) {
				editing = def;
				new_ref_input = def.Path + def.Name;
				new_ref_name = def.Name;
				new_ref_path = def.Path;

				mode = Mode.Editing;
			}
		}
	}
}