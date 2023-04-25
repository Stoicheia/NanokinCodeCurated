using System;
using System.Linq;
using Anjin.Nanokin.Core;
using Anjin.Scripting;
using Cysharp.Threading.Tasks.Triggers;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Anjin.EditorUtility
{
	/// <summary>
	/// Utility to use several textmeshpro objects as one.
	/// (which is useful to create more interesting typography)
	/// </summary>
	[LuaUserdata]
	public class TextMeshProMulti : MonoBehaviour
	{
		[NonSerialized]
		public TMP_Text[] all;

		[FormerlySerializedAs("_text"), HideInInspector]
		[SerializeField]
		private string SerializedText;
		private Color _color = Color.white;

		[ShowInInspector]
		[DelayedProperty]
		public string Text
		{
			get => SerializedText;
			set
			{
				if (!Application.IsPlaying(gameObject))
				{
					TMP_Text[] labels = GetComponentsInChildren<TMP_Text>();

#if UNITY_EDITOR
					Undo.RecordObjects(new Object[] {this}.Union(labels).ToArray(), "Modified Text");
#endif

					SerializedText = value;
					foreach (TMP_Text label in labels)
					{
						label.text = SerializedText;
					}
				}
				else
				{
					SerializedText = value;

					if (all != null)
					{
						foreach (TMP_Text label in all)
						{
							label.text = SerializedText;
						}
					}
				}
			}
		}

		public Color Color
		{
			get => _color;
			set
			{
				_color = value;
				foreach (TMP_Text txt in all)
				{
					txt.color = _color;
				}
			}
		}

		private void OnValidate()
		{
			if (string.IsNullOrWhiteSpace(SerializedText) && TryGetComponent(out TextMeshPro self) && self.text.Length > 0)
			{
				SerializedText = self.text;
			}
		}

		private void Awake()
		{
			all    = GetComponentsInChildren<TMP_Text>();
			Text   = SerializedText;

			if (all.Length > 0)
				_color = all[0].color;
		}

		public void ForceMeshUpdate()
		{
			foreach (TMP_Text text in all)
			{
				text.ForceMeshUpdate();
			}
		}
	}
}