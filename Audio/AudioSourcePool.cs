using System;
using System.Collections.Generic;
using Anjin.Util;
using UnityEngine;
using UnityEngine.Audio;
using Vexe.Runtime.Extensions;

namespace Anjin.Audio
{
	public class AudioSourcePool
	{
		public int PoolSize;

		public AudioMixerGroup MixerGroup;

		public List<AudioSource> FreePool;
		public List<AudioSource> ActivePool;
		public List<AudioSource> ReservedPool;

		public Transform Root;
		public Transform FreeRoot;
		public Transform ActiveRoot;
		public Transform ReservedRoot;

		bool instantiated;

		public AudioSourcePool(int poolSize, AudioMixerGroup mixerGroup, Transform root)
		{
			instantiated = false;
			PoolSize     = poolSize;
			MixerGroup   = mixerGroup;
			Root         = root;

			FreePool     = new List<AudioSource>();
			ActivePool   = new List<AudioSource>();
			ReservedPool = new List<AudioSource>();
		}

		public void InstantiateSources()
		{
			if (instantiated) return;

			FreeRoot     = Root.gameObject.AddChild("Free Pool").transform;
			ActiveRoot   = Root.gameObject.AddChild("Active Pool").transform;
			ReservedRoot = Root.gameObject.AddChild("Reserved Pool").transform;

			GameObject  obj;
			AudioSource source;

			//Instantiate global sources
			for (int i = 0; i < PoolSize; i++)
			{
				obj = new GameObject("Global Source [" + Convert.ToString(i) + "]");

				source                       = obj.AddComponent<AudioSource>();
				source.outputAudioMixerGroup = MixerGroup;
				obj.transform.SetParent(FreeRoot);

				FreePool.Add(source);
			}
		}

		public void OnTick()
		{
			for (int i = 0; i < ActivePool.Count; i++)
			{
				if (!ActivePool[i].isPlaying)
				{
					FreePool.Add(ActivePool[i]);
					ActivePool[i].transform.SetParent(FreeRoot);
					ActivePool.RemoveAt(i);
					if (i > 0) i--;
				}
			}
		}

		public (AudioSource, bool) ActivateSource()
		{
			if (FreePool.Count == 0) return ( null, false );

			AudioSource src = FreePool[FreePool.Count - 1];
			FreePool.RemoveAt(FreePool.Count - 1);
			ActivePool.Add(src);
			src.transform.SetParent(ActiveRoot);

			return (src, true);
		}

		public (AudioSource, bool) ReserveSource()
		{
			if (FreePool.Count == 0) return ( null, false );

			AudioSource src = FreePool[FreePool.Count - 1];
			FreePool.RemoveAt(FreePool.Count - 1);
			ReservedPool.Add(src);
			src.transform.SetParent(ReservedRoot);

			return (src, true);
		}

		public bool ReturnSource(AudioSource source)
		{
			int index = ReservedPool.IndexOf(source);
			if (index == -1) return false;

			if(!FreePool.Contains(source))
				FreePool.Add(ReservedPool[index]);

			ReservedPool.RemoveAt(index);
			source.transform.SetParent(FreeRoot);

			source.Stop();

			return true;
		}
	}
}