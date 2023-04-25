using System;
using Anjin.Nanokin;
using Anjin.UI;
using Anjin.Util;
using Cinemachine;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Overworld.Cutscenes.Timeline.UI {

	[Serializable]
	public class UIBehaviour : AnjinPlayableBehaviour {

		public enum SpawnMode {
			Sprite,
			Texture,
			Prefab,
			Text,
		}

		public SpawnMode  Mode;
		public GameObject Prefab;
		public Sprite     Sprite;
		public Texture    Texture;

		public GameText   Text;
		public GameObject TMPPrefab;

		public bool PrefabAsTemplate;

		[SerializeField]
		public Vector2 NormalizedPosition	= new Vector2(0.5f, 0.5f);
		[SerializeField]
		public Vector2 Scale				= Vector2.one;
		[SerializeField]
		public Vector3 Rotation				= Vector3.zero;

		[SerializeField]
		public Vector2 AnchorMin = Vector2.one * 0.5f;

		[SerializeField]
		public Vector2 AnchorMax = Vector2.one * 0.5f;

		[SerializeField]
		public Vector2 Pivot = Vector2.one * 0.5f;

		[SerializeField]
		[Range(0, 1)] public float Alpha = 1;

		[SerializeField]
		[Range(0, 1)] public float WeightEffectsAlpha = 1f;

		[SerializeField]
		[Range(0, 1)]
		public float	NoiseWeight		= 0;
		public float	NoiseAmplitude	= 10;
		public float	NoiseFrequency	= 1;
		public Vector2	NoiseScaling	= Vector2.one;

		public NoiseSettings NoiseSettings = null;

		private GameObject    _spawnedGameobject;
		private Image         _image;
		private RawImage      _rawImage;
		private CanvasGroup   _group;
		private RectTransform _rt;
		private TMP_Text[]	  _textObjects;

		public override bool CanPlay => Application.isPlaying || CutsceneHUD.Exists;

		public override async void OnGraphStart(Playable playable)
		{
			if (!CanPlay) return;

			await CutsceneHUD.TillLiveExists();

			Rebuild();
		}

		protected override void MixerProcessFrame(Playable playable, FrameData frameData, object playerData, ProcessInfo info)
		{
			if (_spawnedGameobject != null) {
				if (_group)
					_group.alpha = scaleByWeight(Alpha, info.weight, WeightEffectsAlpha);

				_spawnedGameobject.SetActive(info.insideClip);

				UpdateRectTransform((float)info.globalClipTime);

				switch (Mode) {
					case SpawnMode.Sprite:  _image.sprite     = Sprite; break;
					case SpawnMode.Texture: _rawImage.texture = Texture; break;
					case SpawnMode.Text:
						if(_textObjects != null) {
							for (int i = 0; i < _textObjects.Length; i++) {
								_textObjects[i].text = Text.GetString();
							}
						}
						break;
				}
			}
		}

		public void Rebuild()
		{
			Cleanup();

			switch (Mode) {
				case SpawnMode.Sprite:
					if(Sprite != null) {
						if (PrefabAsTemplate && Prefab != null) {
							_spawnedGameobject = Prefab.Instantiate(CutsceneHUD.Live.TimelineUIRoot);
							_image             = _spawnedGameobject.GetOrAddComponent<Image>();
							_image.sprite      = Sprite;
						} else {
							_spawnedGameobject = new GameObject("Spawned Sprite");
							_image             = _spawnedGameobject.AddComponent<Image>();
							_image.sprite      = Sprite;

							_spawnedGameobject.transform.parent = CutsceneHUD.Live.TimelineUIRoot;
						}
					}
					break;

				case SpawnMode.Texture:
					if(Texture != null) {
						_spawnedGameobject = new GameObject("Spawned Texture");
						_rawImage          = _spawnedGameobject.AddComponent<RawImage>();
						_rawImage.texture  = Texture;

						_spawnedGameobject.transform.parent = CutsceneHUD.Live.TimelineUIRoot;
					}
					break;

				case SpawnMode.Prefab:
					if (Prefab != null) {
						_spawnedGameobject = Prefab.Instantiate(CutsceneHUD.Live.TimelineUIRoot);
					}
					break;

				case SpawnMode.Text:
					if (TMPPrefab != null) {
						_spawnedGameobject = TMPPrefab.Instantiate(CutsceneHUD.Live.TimelineUIRoot);
						_textObjects       = _spawnedGameobject.GetComponentsInChildren<TMP_Text>();
					}
					break;
			}

			if (_spawnedGameobject != null) {
				_group                       = _spawnedGameobject.GetOrAddComponent<CanvasGroup>();
				_spawnedGameobject.hideFlags = HideFlags.DontSave;

				_rt               = _spawnedGameobject.GetComponent<RectTransform>();
				UpdateRectTransform(0);
			}
		}

		private void UpdateRectTransform(float time)
		{
			if (_rt == null) return;

			Vector2 noise = Vector2.zero;

			if (NoiseSettings && NoiseWeight > Mathf.Epsilon) {
				noise = NoiseSettings.GetCombinedFilterResults(NoiseSettings.PositionNoise, time * NoiseFrequency, Vector3.zero).xy() * NoiseScaling * NoiseAmplitude * NoiseWeight;
			}

			_rt.localScale       = Scale;
			_rt.anchoredPosition = NormalizedToRTPos(NormalizedPosition) + noise;
			_rt.localRotation    = Quaternion.Euler(Rotation);

			_rt.anchorMin = AnchorMin;
			_rt.anchorMax = AnchorMax;

			_rt.pivot = Pivot;
		}

		public Vector2 NormalizedToRTPos(Vector2 norm) => norm * CutsceneHUD.Live.TimelineUIRoot.rect.size;

		public override void OnBehaviourPlay(Playable playable, FrameData info)
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

				_group       = null;
				_rt          = null;
				_textObjects = null;
			}
		}
	}
}