using System;
using System.Collections.Generic;
using System.Linq;
using Overworld.Controllers;
using UnityEngine;
using Util.Odin.Attributes;

namespace Util.Components.VFXs
{
	/// <summary>
	/// A simple thing to help control effects in the overworld. (this really needs to be unified with the stuff combat uses)
	/// </summary>
	public class OverworldFX : MonoBehaviour {

		public enum States {
			Idle,
			Playing
		}

		[NonSerialized, ShowInPlay] public States               State;
		[NonSerialized, ShowInPlay] public List<ParticleSystem> Systems;

		public bool IsPooled;

		private void Awake()
		{
			Systems = GetComponentsInChildren<ParticleSystem>().ToList();
		}

		private void Update()
		{
			if (State == States.Playing) {
				bool any_playing = false;
				for (int i = 0; i < Systems.Count; i++) {
					if (Systems[i].isPlaying) {
						any_playing = true;
						break;
					}
				}

				if (!any_playing) {
					Despawn();
				}

			}
		}

		public void Play()
		{
			State = States.Playing;
			Reset();
			for (int i = 0; i < Systems.Count; i++) {
				Systems[i].Play();
			}
		}

		public void Reset()
		{
			for (int i = 0; i < Systems.Count; i++) {
				Systems[i].Clear();
			}
		}

		public void Despawn()
		{
			State    = States.Idle;

			Reset();

			if (IsPooled) {
				PrefabPool.ReturnSafe(this);
			} else {
				Destroy(gameObject);
			}
		}
	}
}