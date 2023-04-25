using System;
using Anjin.Util;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Util;
using Util.Odin.Attributes;

namespace Anjin.EditorUtility {
	[RequireComponent(typeof(VideoPlayer))]
	public class UIVideoPlayer : MonoBehaviour {

		public VideoPlayer Player;
		public RawImage    TargetImage;

		[NonSerialized, ShowInPlay]
		private RenderTexture _tex;

		public Vector2Int? RenderRes;

		private void Awake()
		{
			if(!Player)
				Player = GetComponent<VideoPlayer>();

			if (!TargetImage)
				TargetImage = transform.GetOrAddComponent<RawImage>();

			#if UNITY_EDITOR
			gameObject.AddComponent<VideoPlayerFixForUnityRecorder>();
			#endif


			GenTexture();
		}

		private void OnDestroy()
		{
			if(_tex) _tex.Release();
		}

		private void OnDisable()
		{
			if(_tex) _tex.Release();
		}

		private void OnEnable()
		{
			GenTexture();
		}

		public void GenTexture()
		{
			if (!Player) return;

			if (_tex != null) {
				_tex.Release();
			}

			int width  = (int)Player.clip.width;
			int height = (int)Player.clip.height;
			_tex = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
			_tex.Create();

			Player.renderMode    = VideoRenderMode.RenderTexture;
			Player.targetTexture = _tex;

			if (TargetImage) {
				TargetImage.texture = _tex;
			}
		}

		private void Update()
		{
			if (_tex == null || !_tex.IsCreated())
				GenTexture();
		}
	}
}