using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.UI;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;
using Util.UniTween.Value;
using Image = UnityEngine.UI.Image;

namespace Overworld.QuestCompass.UI
{
	public class ObjectiveRadarUI : SerializedMonoBehaviour {

		public HUDElement Element;

		[SerializeField] private ObjectiveRadar _referenceRadar;
		[SerializeField] private Dictionary<MapObjective.ObjectiveType, Sprite> _mapIcons;
		[SerializeField] private Dictionary<MapObjective.ObjectiveType, float> _iconScales;

		[SerializeField] private RectTransform _playerArrow;
		[SerializeField] private RectTransform _iconsRoot;
		[SerializeField] private float _iconScale;

		[SerializeField] private float _iconOpacityAtEdge;
		[SerializeField] private float _iconScaleAtEdge;

		[SerializeField] private RectTransform _compass;
		[SerializeField] private RectTransform _fullCompass;

		public Vector3 InactiveScale = new Vector3(1, 1, 1);
		public Vector3 InactiveOffset = new Vector3(0, 0, 0);

		public Easer ShowOffsetEase;
		public Easer ShowScaleEase;
		public Easer ShowAlphaEase;

		public Easer HideOffsetEase;
		public Easer HideScaleEase;
		public Easer HideAlphaEase;

		//[SerializeField] private RectTransform _activePosition; //refactor
		//[SerializeField] private RectTransform _inactivePosition; //refactor

		//[SerializeField] private float _showHideSpeed; //refactor

		//[NonSerialized] public bool IsActive; //refactor

		//private Vector2 _targetPosition; //refactor

		[NonSerialized, ShowInPlay]
		public AsyncTransitioner Transitioner;
		public TransitionStates State => Transitioner.State;

		private void Awake()
		{

			Transitioner = new AsyncTransitioner(
				() => { Element.Alpha.value = 1; },
				() => { Element.Alpha.value = 0; },

				async cts => {
					var batch = UniTask2.Batch();
					Element.DoOffset(InactiveOffset, Vector2.zero, ShowOffsetEase).Token(cts).Batch(batch);
					Element.DoAlpha(0, 1, ShowAlphaEase).Token(cts).Batch(batch);
					Element.DoScale(InactiveScale, Vector2.one, ShowScaleEase).Token(cts).Batch(batch);

					await batch;
				},

				async cts => {
					var batch = UniTask2.Batch();
					Element.DoOffset(Vector2.zero, InactiveOffset, HideOffsetEase).Token(cts).Batch(batch);
					Element.DoAlpha(1, 0, HideAlphaEase).Token(cts).Batch(batch);
					Element.DoScale(Vector2.one, InactiveScale, HideScaleEase).Token(cts).Batch(batch);

					await batch;
				}
			);


			//ToggleActive(false); //refactor
		}

		private void Start()
		{
			Element.Alpha.value = 0;

			for (int i = 0; i < ObjectiveRadar.MAX_TRACKED_OBJECTS; i++)
			{
				var iconObject = new GameObject($"Icon {i}", typeof(RectTransform));
				iconObject.transform.SetParent(_iconsRoot, false);
				iconObject.transform.localScale = _iconScale * Vector3.one;
				iconObject.SetActive(false);
			}
		}

		private void Update()
		{
			if(State != TransitionStates.Off) {
				Draw(_referenceRadar);
				RotateCompass(_referenceRadar);
			}

			/*_fullCompass.anchoredPosition = Vector3.MoveTowards(
				_fullCompass.anchoredPosition,
				_targetPosition,
				_showHideSpeed * Time.deltaTime);
			_targetPosition = IsActive ? _activePosition.anchoredPosition : _inactivePosition.anchoredPosition; //refactor*/
		}

		private void Draw(ObjectiveRadar from)
		{
			//Set arrow rotation
			float arrowRotation = from.RelativePlayerRotation;
			_playerArrow.localRotation = Quaternion.Euler(new Vector3(0, 0, arrowRotation));

			//Draw map icons
			for (int i = 0; i < Math.Min(from.DrawInfo.Count, _iconsRoot.childCount); i++)
			{
				RectTransform icon = _iconsRoot.GetChild(i).GetComponent<RectTransform>();
				if (icon == null) continue;
				if (i >= from.DrawInfo.Count)
				{
					icon.SetActive(false);
					continue;
				}

				RadarObjectInfo info = from.DrawInfo[i];

				Image img = icon.GetOrAddComponent<Image>();
				img.sprite = _mapIcons[info.Type];

				Color color = img.color;
				color.a = Mathf.LerpUnclamped(1, _iconOpacityAtEdge, info.Position.magnitude);
				img.color = color;

				Rect rect = _iconsRoot.rect;
				Vector2 center = rect.center;
				icon.anchoredPosition = center + new Vector2(info.Position.x * rect.width, info.Position.y * rect.height)/2;
				var position = icon.localPosition;
				position = new Vector3(position.x, position.y, 0);
				icon.localPosition = position;
				float scalingFactor = _iconScales.Keys.Contains(info.Type) ? _iconScales[info.Type] : 1;
				icon.localScale = Vector3.one * (_iconScale * scalingFactor * Mathf.Lerp(1, _iconScaleAtEdge, info.Position.magnitude));
				icon.rotation = Quaternion.identity;
				icon.SetActive(true);
			}
		}

		private void RotateCompass(ObjectiveRadar from)
		{
			_compass.rotation = Quaternion.Euler(0, 0, from.CompassRotation);
		}

		/*public void ToggleActive(bool b)
		{
			IsActive = b;
		}*/
	}
}
