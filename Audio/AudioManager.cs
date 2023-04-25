using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Regions;
using Anjin.Scripting;
using Anjin.Util;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using Util.Odin.Attributes;
using g = ImGuiNET.ImGui;

namespace Anjin.Audio
{

	[LuaEnum]
	public enum AudioSnapshot {
		Main,
		CutscenesDefault,
	}

	public class AudioManager : StaticBoy<AudioManager>, IDebugDrawer
	{
		//	CONFIG
		//-------------------------------------------------------------------------------------

		public const int TEST_POOL_SIZE    = 20;
		public const int MUSIC_POOL_SIZE   = 40;
		public const int AMBIENT_POOL_SIZE = 40;

		public const float AUDIO_LEVEL_DEFAULT     = 6;
		public const float AUDIO_LEVEL_UPPER_BOUND = 10;

		[Title("Config")]
		public AudioListener Listener;
		public AudioMixer      MixerAsset;
		public AudioMixerGroup MixerGroup_Master;
		public AudioMixerGroup MixerGroup_Music;
		public AudioMixerGroup MixerGroup_Ambient;
		public AudioMixerGroup MixerGroup_Effects;
		public AudioMixerGroup MixerGroup_Voice;

		public AudioMixerSnapshot Snapshot_Main;
		public AudioMixerSnapshot Snapshot_Cutscene;

		[Space]
		public Transform TestPoolRoot;
		public Transform MusicPoolRoot;
		public Transform AmbientPoolRoot;

		// SIMULATION / RUNTIME
		// ------------------------------------------------------------------------------------

		private static List<AudioZone> allZones = new List<AudioZone>();

		[NonSerialized] public AudioSourcePool testPool;
		[NonSerialized] public AudioSourcePool sfxPool;
		[NonSerialized] public AudioSourcePool musicPool;
		[NonSerialized] public AudioSourcePool ambientPool;

		[Title("Runtime")]
		[NonSerialized, ShowInPlay] public static AudioLayerState musicLayer;
		[NonSerialized, ShowInPlay] public static AudioLayerState ambienceLayer;

		private List<AudioLayerState> _layers;

		protected override void OnAwake()
		{
			base.OnAwake();

			DebugSystem.Register(this);

			testPool    = new AudioSourcePool(TEST_POOL_SIZE, null, TestPoolRoot);
			musicPool   = new AudioSourcePool(MUSIC_POOL_SIZE, MixerGroup_Music, MusicPoolRoot);
			ambientPool = new AudioSourcePool(AMBIENT_POOL_SIZE, MixerGroup_Ambient, AmbientPoolRoot);

			testPool.InstantiateSources();
			musicPool.InstantiateSources();
			ambientPool.InstantiateSources();

			_layers = new List<AudioLayerState>();
			_layers.Add(musicLayer    = new AudioLayerState(musicPool, AudioLayer.Music));
			_layers.Add(ambienceLayer = new AudioLayerState(ambientPool, AudioLayer.Ambience));

			GameOptions.current.audio_speaker_mode.AddHandler(x => _audioWasChanged = true);

			AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			allZones.Clear();
			ambienceLayer = null;
			musicLayer    = null;
		}

		[LuaGlobalFunc("audio_add_music_zone")]
		public static AudioZone AddMusic(AudioClip music, float lerpSpeed = 0.25f, int priority = 200, bool loop = true, float delay = 0)
		{
			AudioZone zone = AudioZone.CreateMusic(music, lerpSpeed, priority, loop, delay);
			AddZone(zone);
			return zone;
		}

		[LuaGlobalFunc("audio_add_mute_zone")]
		public static AudioZone AddMute(AudioLayer layer, float lerpSpeed = 0.25f, int priority = 100)
		{
			AudioZone zone = new AudioZone
			{
				Layer     = layer,
				LerpSpeed = lerpSpeed,
				MuteAll   = true
			};
			AddZone(zone);
			return zone;
		}

		public static void AddZone(AudioZone zone)
		{
			if (zone == null)
			{
				DebugLogger.LogWarning("Passed in a null AudioZone.", LogContext.Audio, LogPriority.Low);
				return;
			}

			allZones.Add(zone);
		}

		[LuaGlobalFunc("audio_remove_zone")]
		public static void RemoveZone(AudioZone zone, bool immediate = false)
		{
			if (zone == null)
			{
				DebugLogger.LogWarning("Passed in a null AudioZone.", LogContext.Audio, LogPriority.Low);
				return;
			}

			allZones.Remove(zone);

			if (zone.OverrideTrack == null) return;
			if (Live == null) return;
			if (Live._layers == null) return;

			foreach (AudioLayerState layer in Live._layers)
			{
				if (layer.trackStates.TryGetValue(zone.OverrideTrack.ID, out AudioLayerState.TrackState track))
				{
					track.exiting = true;
					if (immediate)
						track.lerpSpeed = 1;
				}
			}
		}

		[LuaGlobalFunc("audio_manager_play")]
		public static void Play()
		{
			List<AudioLayerState> states = Live._layers;
			for (int i = 0; i < states.Count; i++)
			{
				AudioLayerState state = states[i];
				state.Start();
			}
		}


		[LuaGlobalFunc("audio_manager_stop")]
		public static void Stop(bool reset = true)
		{
			List<AudioLayerState> states = Live._layers;
			for (int i = 0; i < states.Count; i++)
			{
				AudioLayerState state = states[i];
				state.Stop();

				if (reset)
				{
					state.Reset();
				}
			}

			allZones.Clear();
		}

		[LuaGlobalFunc("audio_manager_reset_all_tracks")]
		public static void ResetAllTracks()
		{
			List<AudioLayerState> states = Live._layers;
			for (int i = 0; i < states.Count; i++)
			{
				foreach (AudioLayerState.TrackState state in states[i].trackStates.Values) {
					state.source.time = 0;
				}
			}
		}

		/// <summary>
		/// Reset a track so it's playing from the start.
		/// </summary>
		/// <param name="zone"></param>
		public static void ResetTrack(AudioZone zone)
		{
			if (zone == null)
			{
				DebugLogger.LogWarning("Passed in a null AudioZone.", LogContext.Audio, LogPriority.Low);
				return;
			}

			if (zone.TrackOverrides == null)
				return;

			// We should introduce a setting on GameAudioTracks to deactivate and reset when not playing.
			foreach ((string id, AudioTrack.OverrideValues @override) in zone.TrackOverrides)
			{
				if (!@override.Mute)
				{
					foreach (AudioLayerState layer in Live._layers)
					{
						layer.ResetTrack(id);
					}
				}
			}
		}

		private void Update()
		{
#if UNITY_EDITOR
			if (Keyboard.current.mKey.isPressed)
			{
				if (musicLayer.playing) musicLayer.Stop();
				else musicLayer.Start();
			}
#endif



			testPool?.OnTick();
			musicPool?.OnTick();
			ambientPool?.OnTick();

			// TODO use a different global component like a AudioListener so it's more flexible (Actually it seems there was already a reference for that, should we use it?)
			// For example we might have mini-games and such where the player is controlling a completely separate object
			// In that case the playerActor object would stay at the entrance most likely
			UpdateZonesInRange(ActorController.playerActor);

			for (int i = 0; i < _layers.Count; i++)
			{
				AudioLayerState state = _layers[i];

				state.UpdateTrackStates();
				state.UpdateTrackSources();
			}

			void UpdateGroupVolume(string param, float volumeNormalized)
			{
				MixerAsset.SetFloat(param, Mathf.Log10(Mathf.Clamp(volumeNormalized, 0.00001f, 1)) * 20);
			}

			UpdateUnityAudioSettings();

			UpdateGroupVolume("Master_Volume", GameOptions.current.audio_level_master.Value / AUDIO_LEVEL_UPPER_BOUND);
			UpdateGroupVolume("Effects_Volume", GameOptions.current.audio_level_sfx.Value / AUDIO_LEVEL_UPPER_BOUND);
			UpdateGroupVolume("Music_Volume", GameOptions.current.audio_level_music.Value / AUDIO_LEVEL_UPPER_BOUND);
			UpdateGroupVolume("Voice_Volume", GameOptions.current.audio_level_voice.Value / AUDIO_LEVEL_UPPER_BOUND);
			UpdateGroupVolume("Ambient_Volume", !SplicerHub.menuActive ? GameOptions.current.audio_level_ambient.Value / AUDIO_LEVEL_UPPER_BOUND : 0);
		}

		/// <summary>Finds blend points in range of a specific actor and fills a list with them.</summary>
		/// <param name="positionActor"></param>
		public void UpdateZonesInRange(Actor positionActor)
		{
			int                   numRegions   = RegionController.Live.numAudioZones;
			AudioZoneMetadata[]   regionZones  = RegionController.Live.audioZonesInside;
			RegionObjectSpatial[] regionShapes = RegionController.Live.audioShapesInside;

			foreach (AudioLayerState layer in _layers)
			{
				layer.zoneStates.Clear();

				// Apply AudioController registered zones
				// ----------------------------------------
				foreach (AudioZone zone in allZones)
				{
					if ((zone.Layer & layer.layer) == layer.layer)
					{
						if (zone.IsGlobal)
						{
							layer.zoneStates.Add(new AudioZoneState(zone));
						}
					}
				}

				// Apply RegionController zones
				// ----------------------------------------
				for (var i = 0; i < numRegions; i++)
				{
					AudioZone           zone  = regionZones[i].zone;
					RegionObjectSpatial shape = regionShapes[i];

					if ((zone.Layer & layer.layer) == layer.layer)
					{
						if (shape == null || zone.IsGlobal)
						{
							// Global zone, it's active everywhere at all time
							layer.zoneStates.Add(new AudioZoneState(zone));
						}
						else if (positionActor != null)
						{
							float distance     = Vector3.Distance(positionActor.transform.position, shape.Transform.Position);
							float distanceNorm = 1;

							if (shape is RegionShape3D shape3D && shape3D.Type == RegionShape3D.ShapeType.Sphere)
								distanceNorm = 1 - distance / shape3D.SphereRadius;

							layer.zoneStates.Add(new AudioZoneState(zone, regionShapes[i], distance, distanceNorm));
						}
					}
				}
			}
		}

		// UNITY AUDIO SETTINGS
		//====================================================================

		private bool _audioWasChanged;

		private static int[] _validSpeakerModes =
		{
			(int)AudioSpeakerMode.Mono,
			(int)AudioSpeakerMode.Stereo,
			(int)AudioSpeakerMode.Quad,
			(int)AudioSpeakerMode.Surround,
			(int)AudioSpeakerMode.Mode5point1,
			(int)AudioSpeakerMode.Mode7point1
		};

		private void OnAudioConfigurationChanged(bool deviceWasChanged)
		{
			if (deviceWasChanged)
			{
				_audioWasChanged = true;
			}
		}

		private void UpdateUnityAudioSettings()
		{
			// TODO: Figure out if this is useful
			/*if (_audioWasChanged) {
				_audioWasChanged = false;
				AudioConfiguration config = AudioSettings.GetConfiguration();
				config.speakerMode = (AudioSpeakerMode)GameOptions.current.audio_speaker_mode.Value;
				AudioSettings.Reset(config);
			}*/
		}

		[LuaGlobalFunc("aud_set_snapshot")]
		public static void SetSnapshot(AudioSnapshot snapshot, float transition = 0)
		{
			switch (snapshot) {
				case AudioSnapshot.Main:             Live.Snapshot_Main.TransitionTo(transition); break;
				case AudioSnapshot.CutscenesDefault: Live.Snapshot_Cutscene.TransitionTo(transition); break;
			}
		}


		// DEBUG
		//====================================================================
		private AudioZone zoneSelected = null;

		public void OnLayout(ref DebugSystem.State state)
		{
			if (state.IsMenuOpen("Audio Controller"))
			{
				if (g.Begin("Audio Controller"))
				{
					if(g.Button("Reset All Tracks"))
						ResetAllTracks();

					if (g.BeginTabBar("tabs"))
					{
						if (g.BeginTabItem("Zones"))
						{
							g.BeginChild("list", new Vector2(86, 0), true);

							int i = 0;
							foreach (AudioZone zone in allZones)
							{
								if (g.Selectable("Zone " + i + " (" + zone.Priority + ")"))
								{
									zoneSelected = zone;
								}

								i++;
							}

							g.EndChild();

							g.SameLine();

							g.BeginGroup();
							if (zoneSelected != null)
							{
								AudioZone zone = zoneSelected;

								g.Text("Layer: " + zone.Layer);
								g.Text("Priority: " + zone.Priority);


								//g.InputFloat3("Spatial Position: ", ref zone.SpatialPosition);

								/*AnjinGui.DrawObj(ref zone.SpatialMode);
								AnjinGui.DrawObj(ref zone.SpatialPosition);
								AnjinGui.DrawObj(ref zone.SpatialSpread);*/

								/*g.Text("Layer: " 	+ zone.Layer);
								g.Text("Priority: " + zone.Priority);*/
							}

							g.EndGroup();

							g.EndTabItem();
						}

						/*if (g.MenuItem("Layers")) {

						}

						if (g.MenuItem("Sources")) {

						}*/

						g.EndMenuBar();
					}
				}

				g.End();
			}
		}
	}

	/// <summary>
	/// Current state for an audio zone.
	/// </summary>
	[Inline]
	public readonly struct AudioZoneState
	{
		public readonly float distance;
		public readonly float normalizedRange;

		public readonly AudioZone           zone;
		public readonly RegionObjectSpatial shape;

		public AudioZoneState(AudioZone zone)
		{
			this.zone = zone;

			shape           = null;
			distance        = 0;
			normalizedRange = 0;
		}

		public AudioZoneState(AudioZone zone, RegionObjectSpatial shape, float distance, float normalizedDistance)
		{
			this.zone = zone;

			this.shape      = shape;
			this.distance   = distance;
			normalizedRange = Mathf.Clamp01(normalizedDistance);
		}
	}
}