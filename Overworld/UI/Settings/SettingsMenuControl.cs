using System;
using Anjin.EditorUtility;
using Anjin.EditorUtility.UIShape;
using Anjin.Util;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Util.Extensions;
using EventTrigger = UnityEngine.EventSystems.EventTrigger;
using Random = UnityEngine.Random;

namespace Overworld.UI.Settings {

	public interface ISettingsMenuControl {
		TMP_Text              Label       { get; }
		RectTransform         RT          { get; }
		Selectable            Selectable  { get; }
		RectTransform         HighlightRT { get; }
		Action<BaseEventData> OnSelected  { get; set; }
		Action<BaseEventData> OnDeselected  { get; set; }

		void Interact();

		void SnapUIRectTo(UIRectangleShape shape);
	}

	public abstract class SettingsMenuControlBase : SerializedMonoBehaviour, ISettingsMenuControl {
		[SerializeField, ShowInInspector]
		private TMP_Text _label;
		public TMP_Text Label => _label;
		//public T        Value;

		[ShowInInspector]
		public RectTransform _rt;
		public RectTransform RT => _rt;

		public RectTransform _highlightRT;
		public RectTransform HighlightRT => _highlightRT;

		public abstract Selectable Selectable { get; }

		protected UIEventListener Listener;

		public Action<BaseEventData> OnSelected { get; set; }
		public Action<BaseEventData> OnDeselected { get; set; }

		public virtual void Interact() { }

		public virtual void UpdateUI() { }

		public virtual void Awake()
		{
			//OnChanged += obj => Debug.Log("Changed: " + obj);
			_rt = GetComponent<RectTransform>();
		}

		public virtual void Start()
		{
			if (Selectable != null)
			{
				Listener = Selectable.GetOrAddComponent<UIEventListener>();
				Listener.onSelect += OnSelect;
				Listener.onDeselect += OnDeselect;
			}
		}

		public void OnSelect(BaseEventData   ev) => OnSelected?.Invoke(ev);
		public void OnDeselect(BaseEventData ev) => OnDeselected?.Invoke(ev);

		public void SnapUIRectTo(UIRectangleShape shape)
		{
			if (shape == null) return;

			RectTransform rect     = _highlightRT;
			if (rect == null) rect = _rt;
			if (rect == null) return;

			shape.rectTransform.pivot = rect.pivot;

			shape.rectTransform.position  = rect.position;
			shape.rectTransform.sizeDelta = rect.rect.size;

			shape.HorizontalSkew = Random.Range(-4, 4);
			shape.VerticalSkew   = Random.Range(-4, 4);

			shape.Redraw();
		}
	}

	public abstract class SettingsMenuControl<T> : SettingsMenuControlBase {
		public Action<T> OnChanged;
		public abstract void Set(T val);
	}
}