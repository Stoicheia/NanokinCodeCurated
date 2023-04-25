//using System;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Anjin.Scripting;
using Anjin.Util;
using ImGuiNET;
using Overworld.Controllers;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Vexe.Runtime.Extensions;
#if UNITY_EDITOR
using Undo = UnityEditor.Undo;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;

#endif

namespace Overworld.Tags
{
	/// <summary>
	/// Renamed from GameTags
	/// </summary>
	[LuaUserdata]
	public class Tag : MonoBehaviour
	{
		public const string TAG_NPC         = "NPC";
		public const string TAG_COLLECTABLE = "Collectable";

#if UNITY_EDITOR
		[ListDrawerSettings(Expanded = true, OnTitleBarGUI = nameof(OnTitlebarGUI))]
		[CustomValueDrawer(nameof(TagListDrawer))]
#endif
		[DisableInPlayMode]
		public List<string> Tags;


		[NonSerialized, ShowInInspector]
		public bool state;

		private bool _init;

		// System
		//------------------------------------------------------------------------------

		public static readonly string[] DEFAULT_TAGS =
		{
			TAG_NPC,
			TAG_COLLECTABLE
		};

		private void Awake()
		{
			Init();
		}

		private void Init()
		{
			if (_init) return;
			_init = true;

			Tags  = Tags ?? new List<string>();
			state = true; // TODO Not sure if this is right

			Enabler.Register(gameObject);
			TagController.Register(this);

			// Debug stuff
			// ----------------------------------------
			if (!_guiRegistered)
			{
				_guiRegistered       =  true;
				DebugSystem.onLayout += OnImgui;
			}
		}

		private void OnDestroy()
		{
			TagController.Deregister(this);
			Enabler.Deregister(gameObject);

			// Debug stuff
			// ----------------------------------------
			_keysToDelete.Clear();
			foreach ((string key, List<Tag> value) in TagController.allByName)
			{
				if (value.Count == 0)
					_keysToDelete.Add(key);
			}

			for (int i = 0; i < _keysToDelete.Count; i++)
			{
				TagController.allByName.Remove(_keysToDelete[i]);
			}
		}

		public void SetState(bool state)
		{
			// if (state && TryGetComponent(out Layer layer)) // && layer.IsOverrideGametags()) // TODO potentially simplify with Enabler
			// {
			// 	layer.RefreshActivationState();
			// }
			// else
			// {
			// 	gameObject.SetActive(false);
			// }

			Enabler.Set(gameObject, state, SystemID.Tag);
			this.state = state;
		}

		/// <summary>
		/// Add a tag to the list.
		/// </summary>
		public void AddTag(string tag)
		{
			Init();
			if (Tags.Contains(tag))
				return;

			Tags.Add(tag);

			if (!TagController.allByName.TryGetValue(tag, out List<Tag> set))
				TagController.allByName[tag] = set = new List<Tag>();

			set.AddIfNotExists(this);
		}

		/// <summary>
		/// Remove a tag from the list.
		/// </summary>
		/// <param name="tag"></param>
		public void RemoveTag(string tag)
		{
			Init();
			if (Tags.Contains(tag)) return;

			if (TagController.allByName.TryGetValue(tag, out List<Tag> actives))
			{
				actives.Remove(this);
				if (actives.Count == 0)
				{
					TagController.allByName.Remove(tag);
				}
			}
		}

		/// <summary>
		/// Remove all tags from this list.
		/// </summary>
		public void RemoveAll()
		{
			Init();
			for (int i = 0; i < Tags.Count; i++)
			{
				if (TagController.allByName.TryGetValue(Tags[i], out List<Tag> actives))
					actives.Remove(this);
			}

			Tags.Clear();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		public static void OnInit()
		{
			TagController.all       = new List<Tag>();
			TagController.allByName = new Dictionary<string, List<Tag>>();

			_keysToDelete  = new List<string>();
			_sb            = new StringBuilder();
			_guiRegistered = false;
		}

		// System
		//------------------------------------------------------------------------------

	#region System

	#endregion

		// Debug
		//------------------------------------------------------------------------------

	#region Debug

		private static StringBuilder _sb;
		private static bool          _guiRegistered;
		private static List<string>  _keysToDelete;

		private void OnImgui(ref DebugSystem.State state)
		{
			_sb.Clear();
			if (state.IsMenuOpen("GameTags"))
			{
				if (ImGui.Begin("GameTags"))
				{
					ImGui.BeginTabBar("tabs");

					if (ImGui.BeginTabItem("Tagged Objects"))
					{
						ImGui.Columns(2);

						for (int i = 0; i < TagController.all.Count; i++)
						{
							_sb.Clear();
							ImGui.Text(TagController.all[i].name);
							ImGui.NextColumn();

							for (int j = 0; j < TagController.all[i].Tags.Count; j++)
							{
								_sb.Append(TagController.all[i].Tags[j]);
								_sb.Append(", ");
							}

							ImGui.TextColored(ColorsXNA.Aqua.ToV4(), _sb.ToString());
							ImGui.SameLine();

							ImGui.NextColumn();

							ImGui.Separator();
						}

						ImGui.Columns(1);

						ImGui.EndTabItem();
					}

					if (ImGui.BeginTabItem("Active Tags"))
					{
						foreach (var pair in TagController.allByName)
						{
							ImGui.PushID(pair.Key);
							ImGui.Text(pair.Key);
							ImGui.SameLine();
							if (ImGui.Button("Activate All"))
							{
								TagController.ActivateAll(pair.Key);
							}

							ImGui.SameLine();
							if (ImGui.Button("Deactivate All"))
							{
								TagController.DeactivateAll(pair.Key);
							}

							ImGui.PopID();
						}

						ImGui.EndTabItem();
					}

					ImGui.EndTabBar();
				}

				ImGui.End();
			}
		}

	#endregion


		// Editor
		//------------------------------------------------------------------------------

#if UNITY_EDITOR

		private void OnTitlebarGUI()
		{
			if (GUILayout.Button("Default Tags"))
			{
				var selector = new GenericSelector<string>("", false);
				var window   = selector.ShowInPopup();

				selector.SelectionChanged += list =>
				{
					window.Close();
					var str = list.FirstOrDefault();
					if (!str.IsNullOrWhitespace())
					{
						Undo.RecordObject(this, "Add Tag");
						Tags.AddIfNotExists(str);
						PrefabUtility.RecordPrefabInstancePropertyModifications(this);
						EditorUtility.SetDirty(this);
						EditorSceneManager.MarkSceneDirty(gameObject.scene);
					}
				};

				for (int i = 0; i < DEFAULT_TAGS.Length; i++)
				{
					if (Tags.Contains(DEFAULT_TAGS[i])) continue;
					selector.SelectionTree.Add(DEFAULT_TAGS[i], DEFAULT_TAGS[i]);
				}
			}
		}

		private static GUIStyle _defaultTagStyle;
		private static GUIStyle _customTagStyle;

		private static string TagListDrawer(string value, GUIContent label)
		{
			if (_defaultTagStyle == null)
			{
				_defaultTagStyle                  = new GUIStyle(EditorStyles.textField);
				_defaultTagStyle.fontStyle        = FontStyle.BoldAndItalic;
				_defaultTagStyle.fontSize         = 14;
				_defaultTagStyle.normal.textColor = Color.HSVToRGB(0.5f, 0.6f, 0.8f);
			}

			if (_customTagStyle == null)
			{
				_customTagStyle                  = new GUIStyle(EditorStyles.textField);
				_customTagStyle.fontStyle        = FontStyle.Bold;
				_customTagStyle.fontSize         = 14;
				_customTagStyle.normal.textColor = Color.HSVToRGB(0.3f, 0.6f, 0.8f);
			}

			GUIStyle style = EditorStyles.textField;

			if (DEFAULT_TAGS.Contains(value))
			{
				style               = _defaultTagStyle;
				GUI.backgroundColor = Color.HSVToRGB(0.55f, 1.0f, 1.0f);
			}
			else if (value.IsNullOrWhitespace())
			{
				GUI.backgroundColor = Color.red;
			}
			else
			{
				style               = _customTagStyle;
				GUI.backgroundColor = Color.HSVToRGB(0.25f, 1.0f, 1.0f);
			}

			string result = GUILayout.TextField(value, style);
			GUI.color = Color.white;

			return result;
		}

#endif
	}
}