using System.Collections.Generic;
using Anjin.Regions;
using UnityEngine;

namespace Anjin.Audio
{
	/// <summary>
	/// Runtime state for an audio layer.
	/// </summary>
	public class AudioLayerState
	{
		public readonly AudioLayer                     layer;
		public readonly AudioSourcePool                pool;
		public readonly List<string>                   trackIDs;
		public readonly Dictionary<string, AudioTrack> trackInfos;
		public readonly Dictionary<string, TrackState> trackStates;
		public readonly List<TrackState>               overrideTracks;
		public readonly List<AudioZoneState>           zoneStates;

		public bool         playing;
		public AudioProfile profile;

		private static readonly AnimationCurve ConstantCurve = AnimationCurve.Constant(0, 1, 1);

		public AudioLayerState(AudioSourcePool pool, AudioLayer layer)
		{
			this.pool  = pool;
			this.layer = layer;

			trackIDs       = new List<string>();
			trackInfos     = new Dictionary<string, AudioTrack>();
			trackStates    = new Dictionary<string, TrackState>();
			zoneStates     = new List<AudioZoneState>();
			overrideTracks = new List<TrackState>();

			playing = false;
		}

		public void Setup(AudioProfile profile)
		{
			this.profile = profile;

			if (this.profile == null) return;
			if (this.profile.Tracks == null) return;
			if (this.profile.Tracks.Count == 0) return;

			for (var i = 0; i < this.profile.Tracks.Count; i++)
			{
				AudioTrack track = this.profile.Tracks[i];
				RegisterTrack(track);
			}
		}

		public void Start()
		{
			if (playing) return;
			playing = true;

			foreach (TrackState state in trackStates.Values)
			{
				if (state.source != null)
					state.source.Play();
			}
		}

		public void Stop()
		{
			if (!playing) return;
			playing = false;

			foreach (TrackState state in trackStates.Values)
			{
				if (state.source != null)
					state.source.Stop();
			}
		}

		public void Reset()
		{
			foreach (TrackState state in trackStates.Values)
			{
				pool.ReturnSource(state.source);
				state.source = null;
			}

			trackIDs.Clear();
			trackInfos.Clear();
			trackStates.Clear();

			profile = null;
		}

		private TrackState RegisterTrack(AudioTrack track)
		{
			if (track == null) return null;

			if (trackStates.ContainsKey(track.ID)) return null; // Already registered

			if (track.Clip == null) return null;
			if (!track.Config.Enabled) return null;

			(AudioSource src, bool success) = pool.ReserveSource();

			if (!success)
				return null;

			trackIDs.Add(track.ID);

			var state = new TrackState
			{
				source = src,

				lerpSpeed    = 0.1f,
				targetVolume = track.Config.InitialVolume > -1 + Mathf.Epsilon ? track.Config.InitialVolume : track.Config.Volume,
				targetPitch  = track.Config.Pitch,

				spatial       = false,
				spatialPos    = Vector3.zero,
				spatialRadius = 1,
				spatialSpread = 0
			};

			src.clip   = track.Clip;
			src.loop   = track.Config.Loop;
			src.volume = track.Config.Mute ? 0 : track.Config.Volume;

			if (track.Config.PlayImmediately)
			{
				if (track.Config.Delay < Mathf.Epsilon)
				{
					src.Play();
				}
				else
				{
					src.PlayDelayed(track.Config.Delay);
				}
			}

			trackInfos[track.ID]  = track;
			trackStates[track.ID] = state;
			return state;
		}

		public void UpdateTrackStates()
		{
			// Mute every track if whole layer is not playing
			// ----------------------------------------
			if (!playing)
			{
				foreach (string id in trackIDs)
				{
					trackStates[id].targetVolume = 0;
					trackStates[id].lerpSpeed    = 0.35f;
				}

				return;
			}
			//Debug.Log("XXXXX ZONE STATES COUNT: " + zoneStates.Count);
			// Find the zone with the highest priority
			// ----------------------------------------
			AudioZone zone      = null;
			var       zoneState = new AudioZoneState();

			bool shouldMuteAll = false;

			for (var i = 0; i < zoneStates.Count; i++) {
				var state = zoneStates[i];
				//Debug.Log(zoneStates[i].zone + " has priority " + zoneStates[i].zone.Priority);
				if (state.zone.Priority > 0 && (state.zone.OverrideTrack == null || !trackStates.TryGetValue(state.zone.OverrideTrack.ID, out var track) || track.source.isPlaying) && (zone == null || state.zone.Priority > zone.Priority))
				{

					zoneState = state;
					zone      = state.zone;
				}
			}

			if (zone != null)
			{
				if (zone.MuteAll)
				{
					shouldMuteAll = true;
				}
				else if (zone.OverrideTrack != null)
				{
					TrackState state = RegisterTrack(zone.OverrideTrack);
					if (state != null)
					{
						state.fullOverride = true;
						overrideTracks.Add(state);
					}
				}
			}


			// Update our tracks according to the active zone
			// ------------------------------------------------
			for (var i = 0; i < trackIDs.Count; i++)
			{
				string id = trackIDs[i];

				AudioTrack info  = trackInfos[id];
				TrackState state = trackStates[info.ID];

				if (state.exiting)
				{
					state.targetVolume = 0;
					continue;
				}

				AudioTrack.ConfigValues baseValues = info.Config;
				if (zone == null)
				{
					state.spatial      = false;
					state.targetPitch  = baseValues.Pitch;
					state.targetVolume = baseValues.Mute ? 0 : baseValues.Volume;
					continue;
				}

				AudioTrack.OverrideValues overrideValues = null;
				bool hasOverride = zone.TrackOverrides != null
				                   && zone.TrackOverrides.TryGetValue(info.ID, out overrideValues)
				                   && overrideValues.Applies;


				state.lerpSpeed = zone.LerpSpeed;
				if (!hasOverride)
				{
					// Update track state with the base config of the track
					state.targetVolume = baseValues.Mute ? 0 : baseValues.Volume;
					state.targetPitch  = baseValues.Pitch;
				}
				else
				{
					// Update track state with override config
					state.targetVolume = overrideValues.Mute ? 0 : overrideValues.Volume;
					state.targetPitch  = overrideValues.Pitch;
				}

				if (!baseValues.Enabled || shouldMuteAll || overrideTracks.Count > 0 && !state.fullOverride)
				{
					// The track should not play at all!
					state.targetVolume = 0;
					continue;
				}


				// Calculate volume from distance to listener
				// -------------------------------------------
				if (zone.DistanceFade)
				{
					Vector2 volumeRange = zone.DistanceFadeVolumeRange;
					Vector2 fadeRange   = zone.DistanceFadeRange;

					state.targetVolume *= volumeRange.x + (volumeRange.y - volumeRange.x) * Mathf.Clamp01(zoneState.normalizedRange / Mathf.Abs(fadeRange.y - fadeRange.x));
				}


				// Update spatial emission parameters
				// ----------------------------------------
				state.spatial = false;

				// Only sphere shape are supported
				var sphere = zoneState.shape as RegionShape3D;
				if (sphere == null || sphere.Type != RegionShape3D.ShapeType.Sphere)
					continue;

				state.spatial       = zone.SpatialMode != AudioZone.SpatialModes.Off;
				state.spatialRadius = sphere.SphereRadius;
				state.spatialSpread = zone.SpatialSpread;

				// Find the position for the spatial emitter
				switch (zone.SpatialMode)
				{
					case AudioZone.SpatialModes.Off:
						break;

					case AudioZone.SpatialModes.ZonePosition:
						state.spatialPos = sphere.Transform.Position;
						break;

					case AudioZone.SpatialModes.SpecificPosition:
						state.spatialPos = zone.SpatialPosition;
						break;

					case AudioZone.SpatialModes.TransformPlusSpecific:
						state.spatialPos = sphere.Transform.Position + zone.SpatialPosition;
						break;
				}
			}
		}

		/// <summary>
		/// Update the audio source for each track.
		/// </summary>
		public void UpdateTrackSources()
		{
			for (var i = 0; i < trackIDs.Count; i++)
			{
				string      id    = trackIDs[i];
				TrackState  state = trackStates[id];
				AudioSource src   = state.source;

				src.volume = Mathf.Lerp(src.volume, state.targetVolume, state.lerpSpeed);
				src.pitch  = Mathf.Lerp(src.pitch, state.targetPitch, state.lerpSpeed);

				if (Mathf.Abs(src.volume - state.targetVolume) < 0.04) src.volume = state.targetVolume;
				if (Mathf.Abs(src.pitch - state.targetPitch) < 0.04) src.pitch    = state.targetPitch;

				if (state.spatial)
				{
					src.transform.position = state.spatialPos;

					src.spatialBlend = 1;
					src.spread       = state.spatialSpread;
					src.dopplerLevel = 0;

					src.SetCustomCurve(AudioSourceCurveType.CustomRolloff, ConstantCurve);
					src.rolloffMode = AudioRolloffMode.Custom;
					src.maxDistance = state.spatialRadius;
					src.minDistance = state.spatialRadius;
				}
				else
				{
					src.spatialBlend = 0;
					src.maxDistance  = 0;
					src.minDistance  = 0;
				}

				if (state.exiting && src.volume < Mathf.Epsilon)
				{
					trackIDs.RemoveAt(i--);
					trackStates.Remove(id);
					trackInfos.Remove(id);
					overrideTracks.Remove(state);
				}
			}
		}

		public class TrackState
		{
			public AudioSource source;
			public float       lerpSpeed;

			public float targetVolume;
			public float targetPitch;

			public bool    spatial;
			public Vector3 spatialPos;
			public float   spatialRadius;
			public float   spatialSpread;

			/// <summary>
			/// Indicate that the track state is in the process of exiting. (fading out)
			/// Its targetVolume will be set to 0 and it will be automatically deleted when its source's
			/// volume reaches 0;
			/// </summary>
			public bool exiting;

			/// <summary>
			/// Indicate that this track overrides everything in the layer.
			/// (every other track will be muted)
			/// </summary>
			public bool fullOverride;
		}

		public void ResetTrack(string id)
		{
			if (trackStates.TryGetValue(id, out TrackState state))
			{
				if (state.source != null)
				{
					state.source.time = 0;
				}
			}
		}
	}
}