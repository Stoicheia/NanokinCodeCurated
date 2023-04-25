using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Anjin.Nanokin.Core.Options {
	public class PostProcessVolumeEnabler : MonoBehaviour {

		private static List<PostProcessVolumeEnabler> _all = new List<PostProcessVolumeEnabler>();

		private PostProcessVolume _volume;

		private Bloom _bloom;
		private AmbientOcclusion _ssao;

		private bool _init = false;

		private void Init()
		{
			if (_init) return;
			_init   = true;

			_volume = GetComponent<PostProcessVolume>();
			_volume.profile.TryGetSettings(out _bloom);
			_volume.profile.TryGetSettings(out _ssao);
		}

		private void OnEnable()
		{
			Init();
			_all.Add(this);
			UpdateState();
		}

		private void OnDisable() => _all.Remove(this);

		public static void UpdateAll()
		{
			for (int i = 0; i < _all.Count; i++) {
				_all[i].UpdateState();
			}
		}

		public void UpdateState()
		{
			Init();

			if (Quality.Current == null) return;

			if(_bloom) _bloom.enabled.value = Quality.Current.BloomEnabled;
			if(_ssao) _ssao.enabled.value   = Quality.Current.SSAOEnabled;
		}


	}
}