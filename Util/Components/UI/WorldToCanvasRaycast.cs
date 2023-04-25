using System;
using Anjin.Actors;
using Anjin.Cameras;
using Overworld;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Util.Components.UI
{
	public class WorldToCanvasRaycast : SerializedMonoBehaviour
	{
		[Title("Setup")]
		[SerializeField]
		private Transform WorldTransform;

		[SerializeField]
		private Vector3 WorldPosition;

		[Title("Setup")]
		[SerializeField]
		private ActorBase WorldActor;

		public Vector3 WorldTransformOffset;

		public Vector2 CanvasRaycastOffset;

		public UpdateTiming Timing = UpdateTiming.Normal;

		public bool EnableDistanceScaling;

		/// <summary>
		/// How many units of distance for the scale to be double its base scale.
		/// </summary>
		[ShowIf("@EnableDistanceScaling")]
		public float DistanceRealSize = 10f;

		[NonSerialized]
		private RectTransform _canvasRect;

		private Camera        _camera;
		private RectTransform _rectTransform;
		private bool          _isValid;
		private Vector3       _baseScale;

		public Camera Camera
		{
			set
			{
				_camera = value;
				if (!_camera)
					_camera = GameCams.Live.UnityCam;
			}
		}


		public void SetWorldPos(Vector3 p)
		{
			WorldPosition = p;
			RefreshPos();
		}

		public void SetWorldPos(Transform t)
		{
			WorldTransform = t;
			RefreshPos();
		}

		public void SetWorldPos(ActorBase t)
		{
			WorldActor = t;
			RefreshPos();
		}

		private void Awake()
		{
			_rectTransform = GetComponent<RectTransform>();

			// The position we get when raycasting is relative
			// to the bottom-left of the screen
			_rectTransform.anchorMin = Vector2.zero;
			_rectTransform.anchorMax = Vector2.zero;

			_camera    = GameCams.Live.UnityCam;
			_isValid   = false;
			_baseScale = _rectTransform.localScale;
		}

		private void Start()
		{
			if (_canvasRect == null)
			{
				Canvas canvas = GetComponentInParent<Canvas>();

				if (canvas == null)
				{
					this.LogError("Couldn't find parent canvas.");
					enabled = false;
					return;
				}

				SetCanvasRect(canvas);
			}

			RefreshPos();
		}

		public void SetCanvasRect(Canvas canvas)
		{
			SetCanvasRect(canvas.GetComponent<RectTransform>());
		}

		public void SetCanvasRect(RectTransform canvas)
		{
			if (canvas == null)
			{
				_canvasRect = null;
				_isValid    = false;
			}
			else
			{
				_canvasRect = canvas.GetComponent<RectTransform>();
				_isValid    = true;
			}
		}

		private void Update()
		{
			if ((Timing & UpdateTiming.Normal) == UpdateTiming.Normal)
				RefreshPos();
		}

		private void LateUpdate()
		{
			if ((Timing & UpdateTiming.Late) == UpdateTiming.Late)
				RefreshPos();
		}

		public void RefreshPos()
		{
			if (!_isValid) return;

			if (_camera == null)
				_camera = GameCams.Live.UnityCam;

			Vector3 targetPosition = WorldPosition;

			if (WorldTransform)
				targetPosition = WorldTransform.position + WorldTransformOffset;
			else if (WorldActor)
				targetPosition = WorldActor.transform.position + WorldActor.visualTransform.rotation * WorldTransformOffset;


			Vector2 viewportPoint = _camera.WorldToViewportPoint(targetPosition).xy();
			Vector2 screenPos     = viewportPoint * _canvasRect.rect.size;

			_rectTransform.anchoredPosition = screenPos + CanvasRaycastOffset;

			if (EnableDistanceScaling)
			{
				float x = Vector3.Distance(targetPosition, _camera.transform.position);

				float scale = 1 / (x / DistanceRealSize);

				// if (x < BaseDistance)
				// scale = DeltaDistance / (x - BaseDistance + DeltaDistance);
				// if (x > BaseDistance)
				// scale = (BaseDistance - x) / DeltaDistance + 1; // Increase linearly

				_rectTransform.localScale = scale * _baseScale;
			}
		}

		public Vector2 WorldToCanvas(Vector3 worldPoint)
		{
			if (!_isValid) return Vector3.zero;

			if (_camera == null)
				_camera = GameCams.Live.UnityCam;

			Vector2 viewportPoint = _camera.WorldToViewportPoint(worldPoint).xy();
			Vector2 screenPos     = viewportPoint * _canvasRect.rect.size;

			return screenPos;
		}
	}
}