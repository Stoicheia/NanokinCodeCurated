using System;
using Anjin.Scripting.Waitables;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JetBrains.Annotations;
using Overworld.Cutscenes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Util.Addressable;

namespace Combat.Data.Decorative
{
	public class CoroutineSFX : CoroutineManaged
	{
		public struct State {
			public float volume;
			public float pitch;

			public (float time, float startVolume)? fadeOnSpawn;

			public bool looping;

			public static State Default = new State {
				volume = 1,
				pitch = 1,
				fadeOnSpawn = null,
				looping = false,
			};
		}

		private readonly string      _address;
		private readonly WorldPoint? _position;
		private          AudioDef    _sfx;
		private bool _noStop = false;

		private bool                            _active;
		private AsyncOperationHandle<AudioClip> _handle;
		private AudioSource                     _src;

		private bool _loadingInternal;
		private bool _loading;
		private bool _stopping;

		public State sfxState = State.Default;

		public AudioSource source => _src;

		public CoroutineSFX(string address)
		{
			_address = address;
		}

		public CoroutineSFX(string address, WorldPoint? pos = null)
		{
			_address  = address;
			_position = pos;
		}

		public CoroutineSFX(AudioClip clip, WorldPoint? pos = null)
		{
			_sfx      = clip;
			_position = pos;
		}

		public CoroutineSFX(AudioDef sfx, WorldPoint? pos = null)
		{
			_sfx      = sfx;
			_position = pos;
		}

		public override bool Active => _active;

		public override bool CanContinue(bool justYielded, bool isCatchup)
		{
			if (isCatchup)
				return true; // Don't block catchups

			if (_active && !_loading && (_src == null || !_src.isPlaying))
				_active = false;

			return base.CanContinue(justYielded, isCatchup);
		}

		public override void OnStart()
		{
			base.OnStart();

			if (_address != null)
			{
				_active = true;
				LoadAndPlay().ForgetWithErrors();
			}
			else if (_sfx.IsValid)
			{
				_active = true;
				Play();
			}
		}

		public override async void OnEnd(bool forceStopped, bool skipped = false)
		{
			base.OnEnd(forceStopped, skipped);
			if (_noStop)
			{
				await UniTask.Delay(TimeSpan.FromSeconds(30));
				Unload();
			}
			else
			{
				Unload();
			}
		}

		[NotNull]
		public CoroutineSFX NoStop()
		{
			_noStop = true;
			return this;
		}

		public void ApplyState(bool isOnSpawn = false)
		{
			if (_src != null) {
				_src.loop  = sfxState.looping;
				_src.pitch = sfxState.pitch;

				if (sfxState.fadeOnSpawn.HasValue) {
					_src.volume = sfxState.fadeOnSpawn.Value.startVolume;
					_src.DOFade(sfxState.volume, sfxState.fadeOnSpawn.Value.time);
					sfxState.fadeOnSpawn = null;
				} else {
					_src.volume = sfxState.volume;
				}
			}
		}

	#region API

		private async UniTask LoadAndPlay()
		{
			string full_addr = _address;

			// NOTE: This is more to deal with a naming convention incongruity than anything else.
			if (!_address.StartsWith("sfx_") && !_address.StartsWith("env_") && !_address.StartsWith("mus_"))
				full_addr = $"sfx/{_address}";

			_active          = true;
			_loading         = true;
			_loadingInternal = true;

			try
			{
				_handle = Addressables.LoadAssetAsync<AudioClip>(full_addr);
				_handle.Completed += hnd =>
				{
					if (hnd.IsValid() && hnd.Status == AsyncOperationStatus.Succeeded)
						_sfx = hnd.Result;

					_loadingInternal = false;
				};
			}
			catch
			{
				// it throws an exception if the address is simply invalid, and there's no Addressables API to check first
			}

			await UniTask.WaitUntil(() => !_loadingInternal);


			if (_handle.IsValid() && _handle.Status == AsyncOperationStatus.Succeeded)
			{
				Play();
				_loading = false;
				return;
			}

			DebugLogger.Log($"Invalid SFX address: <b>{full_addr}</b>", LogContext.Coplayer, LogPriority.High);
			_active  = false;
			_loading = false;
		}

		private void Play()
		{
			if (_position.HasValue && _position.Value.TryGet(out Vector3 pos))
				_src = GameSFX.Play(_sfx, pos, overridePitch: sfxState.pitch);
			else
				_src = GameSFX.PlayGlobal(_sfx, overridePitch: sfxState.pitch);

			ApplyState(true);
		}

		private void Unload()
		{
			if (_src != null)
				_src.Stop();

			_loading = false;

			if (_address != null)
			{
				Addressables2.ReleaseSafe(_handle);
				_handle = new AsyncOperationHandle<AudioClip>();
			}
		}

		[NotNull]
		public CoroutineSFX volume(float vol, float time = 0)
		{
			sfxState.volume = vol;

			if(time < Mathf.Epsilon) {
				ApplyState();
			} else {

				if(_src == null) {
					sfxState.fadeOnSpawn = (time, 0);
				} else {
					_src.DOFade(vol, time);
				}
			}

			return this;
		}

		[NotNull]
		public CoroutineSFX pitch(float pitch, float time = 0)
		{
			sfxState.pitch = pitch;

			if(time < Mathf.Epsilon) {
				ApplyState();
			} else {
				if(_src != null) {
					_src.DOPitch(pitch, time);
				}
			}

			return this;
		}

		public void stop() => Stop();

		[CanBeNull]
		public ManagedTween stop(float fade_time = 0)
		{
			if (_stopping) return null;
			_stopping = true;

			if (_src != null) {
				return new ManagedTween(_src.DOFade(0, fade_time).OnComplete(() => Stop()));
			}

			return null;
		}

	#endregion
	}
}