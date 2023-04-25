using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Editor;
using Anjin.EditorUtility;
using Anjin.EventSystemNS;
using Anjin.Util;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEngine;

using g 	= UnityEngine.GUI;
using glo 	= UnityEngine.GUILayout;
using eg 	= UnityEditor.EditorGUI;
using eglo 	= UnityEditor.EditorGUILayout;

namespace Anjin.Actors
{
	public static class ActorRefDrawerGlobal
	{
		public static void DrawRef(Rect rect, ActorRef reference, GUIContent label, Action<ActorRef> setter, bool compact = false)
		{
			GUILayout.BeginArea(rect);
			DrawRef(reference, label, setter, compact);
			GUILayout.EndArea();
		}

		static Vector2 lastMousePos;

		public static void DrawRef(ActorRef reference, GUIContent label, Action<ActorRef> setter, bool compact = false)
		{
			if(label == null)
				label = GUIContent.none;

			var    db   = ActorDefinitionDatabase.LoadedDB;
			string id   = reference.ID;
			string path = reference.Path;
			string name = reference.Name;

			bool showPopup = false;

			bool error = false;

			if (db)
			{
				var def = db.FindDef(id);
				if (def != null) {
					name = def.Name;
					path = def.Path;
				}
				else if(!reference.IsNullID) error = true;
			}

			g.backgroundColor = !error ? ActorRefDrawerStyles.backgroundColor : Color.HSVToRGB(0f,0f,0.7f);
			glo.BeginVertical(!error ? ActorRefDrawerStyles.Background : ActorRefDrawerStyles.Background_Error, glo.Height(32));
			{
				g.backgroundColor = Color.white;


				if (!compact)
				{
					glo.BeginHorizontal(glo.Height(14));
					{
						glo.Label(label.text + ":", ActorRefDrawerStyles.Header);
					}
					glo.EndHorizontal();
				}

				glo.BeginHorizontal(glo.Height(14));
				{
					glo.Space(6);

					glo.BeginVertical();
					{
						var rect = GUILayoutUtility.GetRect(0, 14);

						var content = new GUIContent(path);
						var size    = ActorRefDrawerStyles.TreeItemText_hover.CalcSize(content);

						g.Label(rect,                   path, ActorRefDrawerStyles.TreeItemText_hover);
						g.Label(rect.Inset(size.x - 4, 0, 0, 0), name, ActorRefDrawerStyles.TreeItemText_Bold);
					}
					glo.EndVertical();

					glo.Label("[" + id + "]", ActorRefDrawerStyles.ID, glo.ExpandWidth(false), glo.ExpandHeight(true));

					if (glo.Button(GUIContent.none, AnjinEditorIcons.ActorTagButtonStyle, glo.Width(20)))
						showPopup = true;

					glo.Space(3);
				}
				glo.EndHorizontal();

				if (error) {
					glo.BeginHorizontal();
					glo.Label("REFERENCE DOES NOT EXIST IN DATABASE!", ActorRefDrawerStyles.TreeItemText_Bold);
					glo.EndHorizontal();
				}

				glo.FlexibleSpace();
			}
			glo.EndVertical();

			if (Event.current.OnRepaint())
				lastMousePos = Event.current.mousePosition;

			if (showPopup)
			{
				ActorDefSelectorPopup.NewPopup(null, null, null, db, def =>
				{
					setter?.Invoke(
						def != null ?
							new ActorRef(def) :
							ActorRef.NullRef);
				});
			}
		}
	}

	public class ActorRefDrawer : OdinValueDrawer<ActorRef>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var reference = ValueEntry.SmartValue;
			ActorRefDrawerGlobal.DrawRef(reference, label, SetReference);
		}

		public void SetReference(ActorRef reference)
		{
			ValueEntry.SmartValue = reference;
		}
	}

	public class ActorTagDrawer : OdinValueDrawer<ActorTag>
	{
		protected override void DrawPropertyLayout(GUIContent label)
		{
			var db  = ActorDefinitionDatabase.LoadedDB;
			var tag = ValueEntry.SmartValue;

			ActorTagDefinition definition = null;
			if (db) definition            = db.FindTagDef(tag.ID);

			g.backgroundColor = ActorRefDrawerStyles.backgroundColor;
			glo.BeginVertical(ActorRefDrawerStyles.Background);
			{
				g.backgroundColor = Color.white;

				glo.Space(4);

				glo.BeginHorizontal(glo.Height(20));
				{
					if(label != null) glo.Label(label.text + ":", ActorRefDrawerStyles.Header);

					DrawTag(definition);
					glo.FlexibleSpace();

					if (glo.Button(GUIContent.none, AnjinEditorIcons.ActorTagButtonStyle, glo.Width(20)))
					{
						ActorTagSelector.Show( enumerable =>
						{
							var def = enumerable.FirstOrDefault();

							if(def != null)
								SetTag(new ActorTag(def));
							else
								SetTag(ActorTag.NullRef);
						} );
					}

					glo.Space(4);
				}
				glo.EndHorizontal();
			}
			glo.EndVertical();
		}

		public void SetTag(ActorTag tag)
		{
			ValueEntry.SmartValue = tag;
		}

		public static void DrawTag(ActorTagDefinition definition)
		{
			if(definition != null)
			{
				g.backgroundColor = definition.tint;
				glo.Label(definition.Name, ActorRefDrawerStyles.Tag);
			}
			else
			{
				glo.Label("(No Tag)", ActorRefDrawerStyles.ID);
			}

			g.backgroundColor = Color.white;
		}
	}

	public class ActorTagListDrawer : OdinValueDrawer<List<ActorTag>>
	{
		private List<ActorTagDefinition> cachedDefs;

		protected override void Initialize()
		{
			cachedDefs = new List<ActorTagDefinition>();
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			var db   = ActorDefinitionDatabase.LoadedDB;
			var tags = ValueEntry.SmartValue;

			cachedDefs.Clear();
			for (int i = 0; i < tags.Count; i++)
			{
				if(!db) cachedDefs.Add(null);
				cachedDefs.Add(db.FindTagDef(tags[i].ID));
			}

			g.backgroundColor = ActorRefDrawerStyles.backgroundColor;
			glo.BeginVertical(ActorRefDrawerStyles.Background);
			{
				g.backgroundColor = Color.white;

				glo.BeginHorizontal(glo.Height(14));
				{
					glo.Label(label.text + ":", ActorRefDrawerStyles.Header);
				}


				if(cachedDefs.Count > 0)
				{
					glo.EndHorizontal();
					glo.BeginHorizontal(glo.Height(20));
				}
				else
				{
					glo.FlexibleSpace();
				}


				{
					glo.Space(3);
					for (int j = 0; j < cachedDefs.Count; j++)
					{
						ActorTagDrawer.DrawTag(cachedDefs[j]);
						glo.Space(2);
					}

					if (cachedDefs.Count > 0)
						glo.FlexibleSpace();

					if (glo.Button(GUIContent.none, AnjinEditorIcons.ActorTagButtonStyle, glo.Width(20)))
					{
						ActorTagSelector.ShowMultiSelect(
							tags,
							enumerable =>
							{
								if (enumerable != null && enumerable.Any())
								{
									SetTags(enumerable.Select(x=>new ActorTag(x)).ToList());
								}
								else ValueEntry.SmartValue.Clear();
							} );
					}

					glo.Space(4);
				}
				glo.EndHorizontal();
				glo.Space(2);
			}
			glo.EndVertical();
		}

		void SetTags(List<ActorTag> tags)
		{
			ValueEntry.SmartValue = tags;
		}
	}

	//[InitializeOnLoad]
	public static class ActorRefDrawerStyles
	{
		public static Color backgroundColor = Color.HSVToRGB(0.0f, 0.0f, 0.4f);

		/*static ActorRefDrawerStyles()
		{
			init();
		}*/

		private static bool initialized = false;

		private static GUIStyle background;
		private static GUIStyle background_error;
		private static GUIStyle header;
		private static GUIStyle name;
		private static GUIStyle tag;
		private static GUIStyle id;



		public static GUIStyle Background       { get{ init(); return background;}}       //{ get { if (background 	== null) init(); return background; }}
		public static GUIStyle Background_Error { get{ init(); return background_error;}} //{ get { if (background 	== null) init(); return background; }}
		public static GUIStyle Header           { get{ init(); return header;}}           //{ get { if (header 		== null) init(); return header; 	}}
		public static GUIStyle Name             { get{ init(); return name;}}             //{ get { if (name 			== null) init(); return name; 		}}
		public static GUIStyle Tag              { get{ init(); return tag;}}              //{ get { if (tag 			== null) init(); return tag;		}}
		public static GUIStyle ID               { get{ init(); return id;}}               //{ get { if (id 			== null) init(); return id; 		}}



		private static GUIStyle treeItemText;
		private static GUIStyle treeItemText_bold;
		private static GUIStyle treeItemText_hover;

		private static GUIStyle treeItemText_foldout;
		private static GUIStyle treeItemText_foldout_hover;

		public static GUIStyle TreeItemText               {get{ init(); return treeItemText;}}
		public static GUIStyle TreeItemText_Bold          {get{ init(); return treeItemText_bold;}}
		public static GUIStyle TreeItemText_hover         {get{ init(); return treeItemText_hover;}}
		public static GUIStyle TreeItemText_foldout       {get{ init(); return treeItemText_foldout;}}
		public static GUIStyle TreeItemText_foldout_hover {get{ init(); return treeItemText_foldout_hover;}}

		static void init()
		{
			if (initialized) return;
			initialized = true;

			background          = new GUIStyle(EventStyles.GreyBackground);
			background.padding  = new RectOffset(0, 0, 0, 4);
			background.overflow = new RectOffset(7, 7, 4, 7);

			background_error          = new GUIStyle(EventStyles.RedBackground);
			background_error.padding  = new RectOffset(0, 0, 0, 4);
			background_error.overflow = new RectOffset(7, 7, 4, 7);

			header              = EventStyles.GetBoldLabelWithColor(Color.HSVToRGB(0.05f, 0.8f, 0.9f));
			header.padding.left = 4;

			name           = EventStyles.GetBoldLabelWithColor(Color.white);
			name.fontSize  = 12;
			name.alignment = TextAnchor.MiddleLeft;

			id           = EventStyles.GetLabelWithColor(Color.HSVToRGB(0.15f, 0.5f, 0.7f));
			id.fontStyle = FontStyle.Italic;
			id.alignment = TextAnchor.MiddleLeft;

			tag           = new GUIStyle("OL Title");
			tag.fontStyle = FontStyle.Bold;
			tag.fontSize  = 10;

			treeItemText = EventStyles.GetLabelWithColor(Color.white);
			//treeItemText.font = AnjinStyles.LiberationMono;
			//treeItemText.fontSize = 14;

			treeItemText_bold           = new GUIStyle(treeItemText);
			treeItemText_bold.fontSize  = 12;
			treeItemText_bold.fontStyle = FontStyle.Bold;

			treeItemText_hover                  = new GUIStyle(treeItemText);
			treeItemText_hover.normal.textColor = Color.HSVToRGB(0.1f, 0.8f, 1.0f);
			//treeItemText_hover.fontStyle = FontStyle.Bold;

			treeItemText_foldout                  = new GUIStyle(treeItemText);
			treeItemText_foldout.fontSize         = 12;
			treeItemText_foldout.fontStyle        = FontStyle.Bold;
			treeItemText_foldout.normal.textColor = Color.HSVToRGB(0.7f, 0.5f, 1.0f);


			treeItemText_foldout_hover                  = new GUIStyle(treeItemText_foldout);
			treeItemText_foldout_hover.normal.textColor = Color.HSVToRGB(0.1f, 0.8f, 1.0f);
		}
	}


	public class ActorRefSelector : OdinSelector<ActorReferenceDefinition>
	{
		public         Action<ActorRef> OnSelect;
		private static OdinEditorWindow window;

		public static void Show(Action<IEnumerable<ActorReferenceDefinition>> _OnSelect)
		{
			ActorRefSelector selector = new ActorRefSelector();

			window = selector.ShowInPopup(256);
			selector.EnableSingleClickToSelect();
			selector.SelectionChanged += _OnSelect;
			//selector.SelectionChanged += delegate { window.Close(); };
		}

		protected override void BuildSelectionTree(OdinMenuTree tree)
		{
			tree.MenuItems.Add(new OdinMenuItem(tree, "(No Reference)", null));
			tree.AddRange(ActorDefinitionDatabase.LoadedDB.ReferenceDefinitions, x => "Refs/"+x.Name);
			tree.Selection.SupportsMultiSelect = false;

			tree.DefaultMenuStyle.AlignTriangleLeft = true;

			tree.Config.DrawSearchToolbar             = false;
			tree.Config.DefaultMenuStyle.Height       = 18;
			tree.Config.DefaultMenuStyle.IndentAmount = 8;
			tree.Config.DefaultMenuStyle.Offset       = 8;
		}

		protected override void DrawSelectionTree()
		{
			//base.DrawSelectionTree();
			g.backgroundColor = Color.grey;
			glo.BeginVertical(AnjinStyles.BoxContainer);
			g.backgroundColor = Color.white;
			SelectionTree.DrawMenuTree();

			glo.EndVertical();
		}
	}

	public class ActorTagSelector : OdinSelector<ActorTagDefinition>
	{
		//public         Handler<ActorTag> OnSelect;
		private static OdinEditorWindow window;
		private        bool             multiSelect = false;

		private List<ActorTag> selectedTags;

		public static void Show(Action<IEnumerable<ActorTagDefinition>> _OnSelect)
		{
			ActorTagSelector selector = new ActorTagSelector();

			window = selector.ShowInPopup(200);
			selector.EnableSingleClickToSelect();
			selector.SelectionChanged += _OnSelect;
		}

		public static void ShowMultiSelect(List<ActorTag> selectedTags, Action<IEnumerable<ActorTagDefinition>> _OnSelect)
		{
			ActorTagSelector selector = new ActorTagSelector();

			window = selector.ShowInPopup(200);
			//selector.EnableSingleClickToSelect();
			selector.SelectionChanged += _OnSelect;
			selector.selectedTags     =  selectedTags;
		}

		protected override void BuildSelectionTree(OdinMenuTree tree)
		{
			var item = new OdinMenuItem(tree, "(No Tag)", null);
			DrawConfirmSelectionButton = true;
			tree.MenuItems.Add(item);
			tree.AddRange(ActorDefinitionDatabase.LoadedDB.TagDefinitions, x => "Refs/" + x.Name);



			tree.Selection.SupportsMultiSelect = true;

			tree.Config.DrawSearchToolbar             = false;
			tree.Config.DefaultMenuStyle.Height       = 18;
			tree.Config.DefaultMenuStyle.IndentAmount = 0;
			tree.Config.DefaultMenuStyle.Offset       = 0;
		}

		protected override void DrawSelectionTree()
		{
			g.backgroundColor = Color.grey;
			glo.BeginVertical(AnjinStyles.BoxContainer);
			g.backgroundColor = Color.white;
			SelectionTree.DrawMenuTree();

			glo.EndVertical();
		}
	}
}