using System;
using Anjin.Cameras;
using Anjin.UI;
using Anjin.Util;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Overworld.Cutscenes.Timeline {

	[Serializable]
	public class WorldSpaceUIBehaviour : AnjinPlayableBehaviour {

		public GameObject Prefab;

		//public Color Tint			= Color.white;
		public float WorldDistance			= 20f;
		[Range(0, 1)]
		public float WeightEffectsDistance	= 1f;

		[Range(0, 1)]
		public float Alpha				= 1.0f;
		[Range(0, 1)]
		public float WeightEffectsAlpha = 1f;

		private Canvas      _canvas;
		private GameObject  _spawnedGameobject;
		private CanvasGroup _group;
		private bool        _canvasSpawned;

		public override bool CanPlay => Application.isPlaying || (GameCams.Exists && CutsceneHUD.Exists);

		protected override void MixerProcessFrame(Playable playable, FrameData frameData, object playerData, ProcessInfo info)
		{
			if (_spawnedGameobject != null) {
				if (_group)
					_group.alpha = scaleByWeight(Alpha, info.weight, WeightEffectsAlpha);

				_spawnedGameobject.SetActive(info.insideClip);
			}
		}

		public override void OnGraphStart(Playable playable)
		{
			if (!CanPlay) return;

			if (Prefab != null) {

				if (!_canvasSpawned) {
					_canvasSpawned = true;
					GameObject go = new GameObject("Spawned World Space Canvas");

					_canvas               = go.AddComponent<Canvas>();
					_canvas.renderMode    = RenderMode.ScreenSpaceCamera;
					_canvas.worldCamera   = GameCams.Live.UnityCam;
					_canvas.planeDistance = WorldDistance;

					CanvasScaler scalar	= go.AddComponent<CanvasScaler>();
					scalar.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
					scalar.scaleFactor = 1;
					scalar.referencePixelsPerUnit = 32;

					go.transform.SetParent(CutsceneHUD.Live.WorldspaceUIRoot);

				}

				_spawnedGameobject           = Prefab.Instantiate(_canvas.transform);
				_group                       = _spawnedGameobject.GetOrAddComponent<CanvasGroup>();
				_spawnedGameobject.hideFlags = HideFlags.DontSave;

				RectTransform transform = _spawnedGameobject.GetComponent<RectTransform>();
				transform.localScale    = Vector3.one;
				transform.localPosition = Vector3.zero;
				transform.localRotation = Quaternion.identity;
			}
		}

		public override void OnBehaviourPlay(Playable  playable, FrameData info)
		{
			if (!_spawnedGameobject) return;
			_spawnedGameobject.SetActive(true);
		}

		public override void Cleanup()
		{
			if(_spawnedGameobject != null) {
				if (Application.isPlaying)
					Object.Destroy(_spawnedGameobject);
				else
					Object.DestroyImmediate(_spawnedGameobject);
			}

			if (_canvasSpawned) {
				_canvas.gameObject.Destroy();
				_canvasSpawned = false;
			}
		}

	}
}