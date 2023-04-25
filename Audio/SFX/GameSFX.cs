using System.Collections.Generic;
using Anjin.Audio;
using Anjin.Scripting;
using JetBrains.Annotations;
using UnityEngine;
using Util.Odin.Attributes;

[LuaUserdata(staticAuto:true)]
public class GameSFX : StaticBoy<GameSFX>
{
	private const int SUSPICIOUS_ALLOCATIONS_COUNT = 50;
	private const int MAX_ALLOCATION_COUNT         = 250;

	[ShowInPlay] private List<PoolSource> _all;
	[Space]
	[ShowInPlay] private PoolSource _asMusic;
	[ShowInPlay] private string _musicId;

	protected override void OnAwake()
	{
		_all = new List<PoolSource>();
	}

	/// <summary>
	/// Play an instance of this audiodef at the specified position.
	/// If no sound will play from this call, we return null. (either due to an error or missing data)
	/// </summary>
	/// <param name="position">Where to play the sound.</param>
	/// <param name="overridePitch">A specific pitch to play at.</param>
	/// <param name="overrideVolume">A specific volume to play at.</param>
	/// <returns>AudioSource if we can succesfuly play the sound, otherwise null.</returns>
	[CanBeNull]
	public static AudioSource Play(
		AudioDef def,
		Vector3  position,
		float    overridePitch  = -1,
		float    overrideVolume = -1
	)
	{
		if (!def.clip) return null;

		PoolSource ret = Live.GetNext();

		ret.UpdateSource(def, overridePitch, overrideVolume);
		ret.SetOwnerEmitter(position);
		ret.Play();

		return ret.source;
	}

	/// <summary>
	/// Play an instance of this audiodef at the specified position.
	/// If no sound will play from this call, we return null. (either due to an error or missing data)
	/// </summary>
	/// <param name="position">Where to play the sound.</param>
	/// <param name="overridePitch">A specific pitch to play at.</param>
	/// <param name="overrideVolume">A specific volume to play at.</param>
	/// <returns>AudioSource if we can succesfuly play the sound, otherwise null.</returns>
	[CanBeNull]
	public static AudioSource Play(
		AudioDef def,
		Vector3  position,
		object   owner,
		float    overridePitch  = -1,
		float    overrideVolume = -1
	)
	{
		if (!def.clip) return null;

		PoolSource ret = Live.GetNext();

		ret.UpdateSource(def, overridePitch, overrideVolume);
		ret.SetOwnerEmitter(owner, position);
		ret.Play();

		return ret.source;
	}

	/// <summary>
	/// Play an instance of this audiodef at the position of the transform, and stays attached.
	/// If no sound will play from this call, we return null. (either due to an error or missing data)
	/// </summary>
	/// <param name="transform">Which transform to attach the sound to.</param>
	/// <param name="overridePitch">A specific pitch to play at.</param>
	/// <param name="overrideVolume">A specific volume to play at.</param>
	/// <returns>AudioSource if we can succesfuly play the sound, otherwise null.</returns>
	[CanBeNull]
	public static AudioSource Play(
		AudioDef  def,
		Transform transform,
		float     overridePitch  = -1,
		float     overrideVolume = -1,
		bool	  canReuseExisting = false
	)
	{
		if (!def.clip) return null;

		PoolSource ret = canReuseExisting ? Live.GetNext() : Live.GetNext(def, transform);

		ret.UpdateSource(def, overridePitch, overrideVolume);
		ret.SetOwnerEmitter(transform);
		ret.Play();

		return ret.source;
	}

	/// <summary>
	/// Play an instance of this audiodef at the position of the transform, and stays attached.
	/// If no sound will play from this call, we return null. (either due to an error or missing data)
	/// </summary>
	/// <param name="transform">Which transform to attach the sound to.</param>
	/// <param name="overridePitch">A specific pitch to play at.</param>
	/// <param name="overrideVolume">A specific volume to play at.</param>
	/// <returns>AudioSource if we can succesfuly play the sound, otherwise null.</returns>
	[CanBeNull]
	public static AudioSource Play(
		AudioDef      def,
		MonoBehaviour component,
		float         overridePitch  = -1,
		float         overrideVolume = -1
	)
	{
		return Play(def, component.transform, overridePitch, overrideVolume);
	}


	public static AudioSource PlayGlobal(
		AudioDef def,
		object   owner,
		float    overridePitch  = -1,
		float    overrideVolume = -1
	)
	{
		if (!def.clip) return null;

		PoolSource ret = Live.GetNext();

		ret.UpdateSource(def, overridePitch, overrideVolume);
		ret.SetOwner(owner);
		ret.Play();

		return ret.source;
	}

	public static AudioSource PlayGlobal(
		AudioDef def,
		float    overridePitch  = -1,
		float    overrideVolume = -1
	)
	{
		if (!def.clip) return null;

		PoolSource ret = Live.GetNext();

		ret.UpdateSource(def, overridePitch, overrideVolume);
		ret.ClearOwner();
		ret.Play();

		return ret.source;
	}

	/// <summary>
	/// Get the next available audio source, locking it in the process.
	/// </summary>
	/// <returns></returns>
	public PoolSource GetNext()
	{
		PoolSource ret = null;

		// Find a free source
		// ----------------------------------------
		for (var i = 0; i < _all.Count; i++)
		{
			PoolSource poolee = _all[i];

			if (!poolee.source.isPlaying)
			{
				ret = poolee;
				poolee.Reset();
				_all.RemoveAt(i);
				break;
			}
		}

		// Check if we managed to get a free source
		if (ret == null)
		{
			// We did not.

			if (_all.Count >= MAX_ALLOCATION_COUNT)
			{
				this.LogError($"Reached max number ({MAX_ALLOCATION_COUNT}) of allocated SFX emitters which are all currently busy. This is highly abnormal. The game may break badly.");
				return null;
			}
			else if (_all.Count >= SUSPICIOUS_ALLOCATIONS_COUNT)
			{
				this.LogError($"Suspiciously high number of allocated SFX emitters ({SUSPICIOUS_ALLOCATIONS_COUNT}) currently busy/in use. This is a little strange.");
			}

			// Allocate new AudioSource object
			// ----------------------------------------
			var go = new GameObject($"SFX [{_all.Count}]");
			go.transform.SetParent(Live.transform, false);

			AudioSource src = go.AddComponent<AudioSource>();
			ret = new PoolSource(src);
		}

		_all.Add(ret); // Move to the end.
		return ret;
	}

	public PoolSource GetNext(AudioDef def, object emitter)
	{
		for (var i = 0; i < Live._all.Count; i++)
		{
			PoolSource activeSource = Live._all[i];
			if (activeSource.ownerID == emitter && activeSource.source.clip == def.clip)
			{
				// Re-use the existing source
				activeSource.source.Stop();
				return activeSource;
			}
		}

		return GetNext();
	}

	private void Update()
	{
		foreach (PoolSource registeredSource in _all)
		{
			// Keep the AudioSource positioned onto the attached transform if there is one.
			if (registeredSource.ownerTransform != null)
				registeredSource.sourceTransform.position = registeredSource.ownerTransform.position;
		}
	}


	public class PoolSource
	{
		public AudioSource source;
		public Transform   sourceTransform;
		public object      ownerID;
		public Transform   ownerTransform;
		// public Transform   emitter;

		public PoolSource(AudioSource src)
		{
			source          = src;
			sourceTransform = source.transform;
			ownerID         = src.transform;
		}

		public void Reset()
		{
			source.loop    = false;
			ownerID        = null;
			ownerTransform = null;
		}

		public void UpdateSource(AudioDef def, float overridePitch, float overrideVolume)
		{
			source.clip                  = def.clip;
			source.volume                = overrideVolume >= 0 ? overrideVolume : def.EvaluateVolume();
			source.pitch                 = overridePitch  >= 0 ? overridePitch : def.EvaluatePitch();
			source.outputAudioMixerGroup = AudioManager.Live.MixerGroup_Effects;
		}


		/// <summary>
		/// Configures the audio source for a sound that will be emitted at no particular position.
		/// Volume remains constant regardless of positioning.
		/// Useful for UI and music.
		/// </summary>
		/// <param name="source"></param>
		public void ConfigureGlobalEmitter()
		{
			source.spatialBlend = 0f;
			source.rolloffMode  = AudioRolloffMode.Linear;
			source.maxDistance  = 18.5f;
			source.time         = 0f;
		}

		/// <summary>
		/// Configures the audio source for a sound that will be emitted in the world at a position.
		/// Sound travels to the listener and volume decreases the further away it is.
		/// Useful for world sounds, like footsteps.
		/// </summary>
		/// <param name="source"></param>
		public void ConfigureWorldEmitter()
		{
			source.spatialBlend = 1f;
			source.rolloffMode  = AudioRolloffMode.Linear;
			source.minDistance  = 7.5f;
			source.maxDistance  = 20f;
			source.time         = 0f;
			source.loop         = false;
		}

		public void ClearOwner()
		{
			ownerID        = null;
			ownerTransform = null;

			ConfigureGlobalEmitter();
			sourceTransform.localPosition = Vector3.zero;
		}

		public void SetOwner(object emitter)
		{
			ownerID        = emitter;
			ownerTransform = null;

			ConfigureGlobalEmitter();
			sourceTransform.localPosition = Vector3.zero;
		}

		public void SetOwnerEmitter(Vector3 pos)
		{
			ownerID        = null;
			ownerTransform = null;

			ConfigureWorldEmitter();
			sourceTransform.position = pos;
		}


		public void SetOwnerEmitter(object owner, Vector3 position)
		{
			ownerID        = owner;
			ownerTransform = null;

			ConfigureWorldEmitter();
			sourceTransform.position = position;
		}

		public void SetOwnerEmitter(Transform emitter)
		{
			ownerTransform = emitter;
			ownerID        = emitter;

			ConfigureWorldEmitter();
			sourceTransform.position = emitter.position;
		}

		public void Play()
		{
			source.Play();
		}
	}
}

// public static PoolSource Play(Transform transform, AudioClip clip, float volume = 1, float pitch = 1)
// {
// 	PoolSource registration = Live.GetNext();
//
// 	AudioSource src = registration.source;
//
// 	if (transform != null)
// 	{
// 		registration.ConfigureWorldEmitter(src);
// 		src.transform.position         = transform.position;
// 		registration.attachedTransform = transform;
// 	}
// 	else
// 	{
// 		ConfigureConstantEmitter(src);
// 	}
//
// 	src.volume = volume;
// 	src.pitch  = pitch;
// 	src.clip   = clip;
// 	src.Play();
//
// 	return registration;
// }
//
// public static PoolSource Play(Vector3? position, AudioClip clip, float volume = 1, float pitch = 1)
// {
// 	PoolSource registration = Live.GetNext();
//
// 	AudioSource src = registration.source;
//
// 	if (position.HasValue)
// 	{
// 		ConfigureWorldEmitter(src);
// 		src.transform.position = position.Value;
// 	}
// 	else
// 	{
// 		ConfigureConstantEmitter(src);
// 	}
//
// 	src.volume = volume;
// 	src.pitch  = pitch;
// 	src.clip   = clip;
// 	src.Play();
//
// 	return registration;
// }