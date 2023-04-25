using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Core.Flags;
using Anjin.EditorUtility;
using Anjin.Regions;
using Sirenix.Utilities.Editor;
using UnityEngine;

using g 	= UnityEngine.GUI;
using glo 	= UnityEngine.GUILayout;
using eg 	= UnityEditor.EditorGUI;
using eglo 	= UnityEditor.EditorGUILayout;
using Event = UnityEngine.Event;

namespace Anjin.Editor {
	public static class MenuTreeStyles {
		public static Color fillColor_normal_dark  = Color.HSVToRGB(0.15f, 0.1f,  0.23f);
		public static Color fillColor_normal_light = Color.HSVToRGB(0.15f, 0.1f,  0.255f);
		public static Color fillColor_foldout      = Color.HSVToRGB(0.05f, 0.15f, 0.27f);
		public static Color fillColor_highlight    = Color.HSVToRGB(0.15f, 0.4f,  0.3f);
		public static Color fillColor_selected     = Color.HSVToRGB(0.6f, 0.8f,  0.8f);
	}

	public class MenuTree<T> {
		public List<MenuTreeItem<T>> roots;

		public Action<T> OnItemClickedCallback;
		public Action<T> OnItemRightClickedCallback;

		public MenuTree(MenuTreeItem<T> firstRoot, Action<T> onItemClickedCallback,
						Action<T>       onItemRightClickedCallback = null)
		{
			roots = new List<MenuTreeItem<T>>();
			if (firstRoot != null) roots.Add(firstRoot);

			OnItemClickedCallback      = onItemClickedCallback;
			OnItemRightClickedCallback = onItemRightClickedCallback;

			currentItemHovering = null;
		}

		public MenuTreeItem<T> currentItemHovering;
		public MenuTreeItem<T> ItemSelected;


		private bool highlightAlternate = false;

		public void Clear()
		{
			roots.Clear();
		}

		public void DrawTree()
		{
			highlightAlternate  = false;
			lines               = 0;
			currentItemHovering = null;

			for (int i = 0; i < roots.Count; i++) {
				DrawTreeItem(roots[i], 0, i, roots.Count);
			}
		}

		int lines;

		public int DrawTreeItem( MenuTreeItem<T> item, int indentLevel, int index, int length)
		{
			float height           = 16;
			float indentPixels     = 12;
			float indentLineOffset = 4;

			Event     current = Event.current;
			EventType type    = Event.current.type;


			//Get Rectangle
			var  rect      = GUILayoutUtility.GetRect(0, height);
			Rect labelRect = Rect.MinMaxRect(rect.xMin + indentLevel * indentPixels, rect.yMin, rect.xMax, rect.yMax);


			bool     hover = false;
			GUIStyle style = ActorRefDrawerStyles.TreeItemText;
			Color col =  (highlightAlternate)
				? MenuTreeStyles.fillColor_normal_dark
				: MenuTreeStyles.fillColor_normal_light;
			highlightAlternate = !highlightAlternate;

			//Check for mouse over
			if (type == EventType.Repaint && rect.Contains(current.mousePosition)) {
				hover               = true;
				currentItemHovering = item;
			}


			if (item.type == ItemType.Foldout || item.type == ItemType.Section) {
				col   = MenuTreeStyles.fillColor_foldout;
				style = ActorRefDrawerStyles.TreeItemText_foldout;
				if (hover) {
					col   = MenuTreeStyles.fillColor_highlight;
					style = ActorRefDrawerStyles.TreeItemText_foldout_hover;
				}
			} else if (hover) {
				col   = MenuTreeStyles.fillColor_highlight;
				style = ActorRefDrawerStyles.TreeItemText_hover;
			}

			if (item == ItemSelected) {
				col = MenuTreeStyles.fillColor_selected;
			}

			eg.DrawRect(rect, col);

			string text = item.text;
			if (item.type == ItemType.Foldout || item.type == ItemType.Section)
				text = text + ":";

			if(item.draw_label)
				g.Label( labelRect, text, style );

			lines++;

			var indentColor = Color.HSVToRGB(0.0f, 0.0f, 0.5f);
			var bulletColor = Color.HSVToRGB(0.0f, 0.0f, 0.6f);


			// DRAW INDENT LINES
			//---------------------------------------------------------------------------

			float xx = ( indentLevel == 0 ? indentPixels : indentPixels + indentLineOffset );

			//Horizontal
			eg.DrawRect(new Rect(labelRect.x - xx, labelRect.center.y, indentPixels, 1), indentColor);

			//Up
			if (index > 0)
				eg.DrawRect(new Rect(labelRect.x - xx, labelRect.yMin, 1, labelRect.height / 2), indentColor);

			//Down
			if (index < length - 1)
				eg.DrawRect(new Rect(labelRect.x - xx, labelRect.center.y, 1, labelRect.height / 2), indentColor);


			HandleMouseEvents(item, rect);

			item.customDrawer?.Invoke(item, rect, indentLevel);

			int children      = 0;
			int drawnChildren = 0;
			if (item.children != null) {
				if (item.type == ItemType.Selectable || item.expand) {
					for (int i = 0; i < item.children.Count; i++) {
						children++;
						drawnChildren++;
						var ch = DrawTreeItem(item.children[i], indentLevel + 1, i, item.children.Count);

						children += ch;
						if (i < item.children.Count - 1)
							drawnChildren += ch;
					}

					if (children > 0)
						//eg.DrawTextureTransparent(new Rect(labelRect.x - (indentLevel == 0 ? 0 : indentLineOffset), labelRect.center.y, 1, height * drawnChildren), AnjinEditorIcons.ExpandArrowOff);
						eg.DrawRect(
							new Rect(labelRect.x - (indentLevel == 0 ? 0 : indentLineOffset), labelRect.center.y, 1,
									 height * drawnChildren), indentColor);
				}
			}

			if (Event.current.OnRepaint()) {
				//Bullets
				if (item.type == ItemType.Foldout) {
					var arrowStyle = ( !item.expand )
						? AnjinEditorIcons.ExpandArrowOffStyle
						: AnjinEditorIcons.ExpandArrowOnStyle;
					arrowStyle.Draw(new Rect(labelRect.x - 4 - 3, labelRect.center.y - 4, 8, 8), GUIContent.none, false,
									false, false, false);
				}
				//eg.DrawRect(new Rect(labelRect.x - 6, labelRect.center.y - 2, 5, 5), bulletColor);
				else
					eg.DrawRect(new Rect(labelRect.x - 5, labelRect.center.y - 1, 3, 3), bulletColor);
			}

			return children;
		}

		void HandleMouseEvents(MenuTreeItem<T> item, Rect rect)
		{
			Event     current = Event.current;
			EventType type    = Event.current.type;

			bool leftDown  = current.OnMouseDown(rect, 0, false);
			bool rightDown = current.OnMouseDown(rect, 1, false);

			if (leftDown) {
				if (item.type == ItemType.Foldout) {
					item.expand = !item.expand;
				} else if (item.type == ItemType.Selectable) {
					OnItemClickedCallback?.Invoke(item.itemValue);
				}

				Event.current.Use();
			} else if (rightDown) {
				OnItemRightClickedCallback?.Invoke(item.itemValue);
				Event.current.Use();
			}

		}
	}

	public enum ItemType {
		Selectable,
		Foldout,
		Section,
	}

	public class MenuTreeItem<T> {

		public delegate void MenuTreeItemDrawerFunc(MenuTreeItem<T> drawer, Rect rect, int indent);

		public MenuTreeItem(string _text)
		{
			type   = ItemType.Foldout;
			text   = _text;
			expand = true;
		}

		public MenuTreeItem(string _text, T value)
		{
			type      = ItemType.Selectable;
			text      = _text;
			itemValue = value;
		}

		public ItemType type;
		public string   text;

		public bool expand;
		public bool draw_label = true;

		public T itemValue;

		public List<MenuTreeItem<T>>  children;
		public MenuTreeItemDrawerFunc customDrawer;

		public MenuTreeItem<T> Add(MenuTreeItem<T> item)
		{
			InsureChildren();
			children.Add(item);
			return item;
		}

		public void InsureChildren()
		{

			if (children == null) children = new List<MenuTreeItem<T>>();
		}
	}

	class ActorDefTree : MenuTree<ActorReferenceDefinition> {
		public ActorDefTree(
			MenuTreeItem<ActorReferenceDefinition> roots,
			Action<ActorReferenceDefinition>       onItemClickedCallback,
			Action<ActorReferenceDefinition>       onItemRightClickedCallback)
			: base(roots, onItemClickedCallback, onItemRightClickedCallback) { }
	}

	class ActorDefItem : MenuTreeItem<ActorReferenceDefinition> {
		public ActorDefItem(string _text) : base(_text) { }
		public ActorDefItem(string _text, ActorReferenceDefinition value) : base(_text, value) { }
	}

	public class GraphObjectTree : MenuTree<RegionObjectRef> {
		public Dictionary<string, GraphObjectTreeItem> RefsToItems;

		public GraphObjectTree(MenuTreeItem<RegionObjectRef> firstRoot,
							   Action<RegionObjectRef>       onItemClickedCallback,
							   Action<RegionObjectRef>       onItemRightClickedCallback = null)
			: base(firstRoot, onItemClickedCallback, onItemRightClickedCallback)
		{
			RefsToItems = new Dictionary<string, GraphObjectTreeItem>();

		}
	}

	public class GraphObjectTreeItem : MenuTreeItem<RegionObjectRef> {
		public GraphObjectSection section = GraphObjectSection.Objects;

		public GraphObjectTreeItem(string _text) : base(_text)
		{
			itemValue = RegionObjectRef.Default;
		}

		public GraphObjectTreeItem(string _text, RegionObjectRef value) : base(_text, value) { }
	}

	class StringTree : MenuTree<string> {
		public StringTree(MenuTreeItem<string> roots, Action<string> onItemClickedCallback) : base(
			roots, onItemClickedCallback) { }
	}

	class StringTreeItem : MenuTreeItem<string> {
		public StringTreeItem(string _text) : base(_text) { }
		public StringTreeItem(string _text, string value) : base(_text, value) { }
	}

	public class FlagDefTree : MenuTree<FlagDefinitionBase> {

		public FlagDefTree(MenuTreeItem<FlagDefinitionBase> firstRoot,
						   Action<FlagDefinitionBase>       onItemClickedCallback,
						   Action<FlagDefinitionBase>       onItemRightClickedCallback = null)
			: base(firstRoot, onItemClickedCallback, onItemRightClickedCallback) { }
	}

	public class FlagDefTreeItem : MenuTreeItem<FlagDefinitionBase> {

		public FlagDefTreeItem(string _text) : base(_text) { }
		public FlagDefTreeItem(string               _text, FlagDefinitionBase value) : base(_text, value) { }
	}

}
