using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Editor;
using Anjin.Nanokin;
using Anjin.Scripting;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
using Sirenix.Utilities.Editor;
using Sirenix.OdinInspector.Editor;
using g = UnityEngine.GUI;
using glo = UnityEngine.GUILayout;
using eg = UnityEditor.EditorGUI;
using eglo = UnityEditor.EditorGUILayout;

#endif

namespace Anjin.Audio
{
	public enum ProfileOverrideMode
	{
		Level,
		Manual
	}

	[Serializable, LuaUserdata]
	public class AudioZone
	{
		// TODO
		public bool Playing = true;

		/// <summary>
		/// The layer this zone should be applied to.
		/// </summary>
		public AudioLayer Layer;

		/// <summary>
		/// Priority of the zone over other zones, used to
		/// select which zone to activate.
		/// </summary>
		public int Priority = 1;

		/// <summary>
		/// A builtin track which will override any current tracks in the game when this zone is active.
		/// Leave null to automatically use the track data.
		/// </summary>
		[CanBeNull]
		public AudioTrack OverrideTrack;

		/// <summary>
		/// Mute all tracks when this zone is active.
		/// </summary>
		public bool MuteAll;

		/// <summary>
		/// Speed at which a track state's volume and pitch will lerp
		/// when this zone is active.
		/// </summary>
		public float LerpSpeed = 0.5f;

		/// <summary>
		/// Fade the volume by distance to the listener.
		/// </summary>
		public bool DistanceFade = false;

		/// <summary>
		/// Range for listener/zone distance based volume fading.
		/// </summary>
		public Vector2 DistanceFadeRange = new Vector2(0, 1);

		/// <summary>
		/// Range of the volume for distance based volume fading.
		/// This is used as a scale onto the track's volume, not an
		/// override.
		/// </summary>
		public Vector2 DistanceFadeVolumeRange = new Vector2(0, 1);

		[FormerlySerializedAs("spatialMode")]
		public SpatialModes SpatialMode = SpatialModes.Off;

		public Vector3 SpatialPosition = Vector3.zero;

		public float SpatialSpread = 0;

		[HideInInspector]
		[FormerlySerializedAs("OverrideSetProfile")]
		[CanBeNull]
		public AudioProfile TrackOverridesProfile;
		[HideInInspector]
		[FormerlySerializedAs("OverrideSet")]
		[CanBeNull]
		public Dictionary<string, AudioTrack.OverrideValues> TrackOverrides;

		// NOTE:
		// These 2 fields are only used for editor drawing
		[FormerlySerializedAs("profileOverrideMode")]
		public ProfileOverrideMode ProfileOverrideMode;
		public AudioProfile ProfileOverriding;

		/// <summary>
		/// Indicates that this zone does not care about spatiality or anything like that.
		/// The volume will always be set to the track's (or overrides) as long as this zone is active.
		/// </summary>
		public bool IsGlobal => !DistanceFade && SpatialMode == SpatialModes.Off;

		/// <summary>
		/// Specify how to retrieve the emission for a spatial zone.
		/// </summary>
		public enum SpatialModes
		{
			/// <summary>
			/// The audio plays anywhere.
			/// </summary>
			Off,
			ZonePosition,
			SpecificPosition,
			TransformPlusSpecific
		}

		/// <summary>
		/// Make a audio zone designed to override the entire music layer
		/// with a specific AudioClip.
		/// </summary>
		/// <param name="clip"></param>
		/// <param name="lerpSpeed"></param>
		/// <returns></returns>
		public static AudioZone CreateMusic(AudioClip clip, float lerpSpeed = 0.25f, int priority = 1337, bool loop = true, float delay = 0)
		{
			return new AudioZone
			{
				Layer     = AudioLayer.Music,
				Priority  = priority,
				LerpSpeed = lerpSpeed,
				OverrideTrack = new AudioTrack
				{
					Clip   = clip,
					Name   = "Cutscene Music",
					Config = {InitialVolume = 0, Loop = loop, Delay = delay}
				}
			};
		}

#if UNITY_EDITOR

		public void UpdateTrackOverrides(AudioProfile profile)
		{
			if (TrackOverrides == null)
				TrackOverrides = new Dictionary<string, AudioTrack.OverrideValues>();

			if (profile == null)
			{
				TrackOverrides.Clear();
				return;
			}

			List<string> keys = TrackOverrides.Keys.ToList();
			for (int i = 0; i < keys.Count; i++)
			{
				if (!profile.Tracks.Exists(x => x.ID == keys[i]))
				{
					TrackOverrides.Remove(keys[i]);
				}
			}

			foreach (AudioTrack profileTrack in profile.Tracks)
			{
				if (!TrackOverrides.ContainsKey(profileTrack.ID))
				{
					TrackOverrides[profileTrack.ID] = new AudioTrack.OverrideValues(profileTrack.Config);
				}
			}
		}
#endif
	}

#if UNITY_EDITOR

	public class AudioZoneDrawer : OdinValueDrawer<AudioZone>
	{
		SimpleLayout layout;
		const float  CONTROL_HEIGHT = 40;

		bool mismatch;

		protected override void DrawPropertyLayout(GUIContent label)
		{
			AudioZone zone = ValueEntry.SmartValue;

			GUIStyle boldWrapping = new GUIStyle(EditorStyles.whiteBoldLabel);
			boldWrapping.wordWrap = true;

			// Draw Zone Properties
			// ----------------------------------------
			GUI.backgroundColor = Color.HSVToRGB(0.2f, 1, 1);
			glo.BeginVertical(SirenixGUIStyles.BoxContainer);
			{
				GUI.backgroundColor = Color.white;

				zone.Layer     = (AudioLayer) eglo.EnumPopup("Layer", zone.Layer);
				zone.Priority  = eglo.IntField("Priority", zone.Priority);
				zone.LerpSpeed = eglo.FloatField("Lerp Speed", zone.LerpSpeed);

				zone.Priority = Mathf.Max(zone.Priority, 0);
			}
			glo.EndVertical();


			GUI.backgroundColor = Color.HSVToRGB(0.7f, 1, 1);
			glo.BeginVertical(SirenixGUIStyles.BoxContainer);
			{
				GUI.backgroundColor = Color.white;

				glo.BeginHorizontal();
				if (glo.Toggle(zone.ProfileOverrideMode == ProfileOverrideMode.Level, "Level", EditorStyles.miniButtonLeft))
					zone.ProfileOverrideMode = ProfileOverrideMode.Level;
				if (glo.Toggle(zone.ProfileOverrideMode == ProfileOverrideMode.Manual, "Manual", EditorStyles.miniButtonRight))
					zone.ProfileOverrideMode = ProfileOverrideMode.Manual;
				glo.EndHorizontal();

				if (zone.ProfileOverrideMode == ProfileOverrideMode.Level) GUI.enabled = false;
				zone.ProfileOverriding = (AudioProfile) SirenixEditorFields.UnityObjectField("Profile", zone.ProfileOverriding, typeof(AudioProfile), false);
				GUI.enabled            = true;
			}
			glo.EndVertical();


			GUI.backgroundColor = Color.HSVToRGB(0.4f, 1, 1);
			glo.BeginVertical(SirenixGUIStyles.BoxContainer);
			{
				GUI.backgroundColor = Color.white;

				zone.DistanceFade = glo.Toggle(zone.DistanceFade, "Distance Fade");

				GUI.enabled                  = zone.DistanceFade;
				zone.DistanceFadeRange       = SirenixEditorFields.MinMaxSlider("Range", zone.DistanceFadeRange, new Vector2(0, 1));
				zone.DistanceFadeVolumeRange = SirenixEditorFields.MinMaxSlider("Volume Range", zone.DistanceFadeVolumeRange, new Vector2(0, 1));
				GUI.enabled                  = true;
			}
			glo.EndVertical();


			GUI.backgroundColor = Color.HSVToRGB(0.6f, 1, 1);
			glo.BeginVertical(SirenixGUIStyles.BoxContainer);
			{
				GUI.backgroundColor = Color.white;

				glo.Label("Note: Spatial currently only works with a RegionShape3D set to sphere.", boldWrapping);

				zone.SpatialMode = (AudioZone.SpatialModes) SirenixEditorFields.EnumDropdown("Spatial Mode", zone.SpatialMode);

				if (zone.SpatialMode != AudioZone.SpatialModes.ZonePosition && zone.SpatialMode != AudioZone.SpatialModes.Off)
					zone.SpatialPosition = SirenixEditorFields.Vector3Field("Position", zone.SpatialPosition);

				if (zone.SpatialMode != AudioZone.SpatialModes.Off)
					zone.SpatialSpread = SirenixEditorFields.RangeFloatField("Spread", zone.SpatialSpread, 0, 360);
			}
			glo.EndVertical();


			// Draw Overrides
			// ----------------------------------------
			if (zone.Priority == 0)
			{
				GUI.backgroundColor = Color.red;
				glo.BeginVertical(SirenixGUIStyles.BoxContainer);
				GUI.backgroundColor = Color.white;

				glo.Label("Zones with priority 0 will be overriden by the base profile, and will have no affect.", boldWrapping);
				glo.EndVertical();
			}


			//Get the profile we should use
			AudioProfile profile = null;
			if (zone.ProfileOverrideMode == ProfileOverrideMode.Manual)
				profile = zone.ProfileOverriding;
			else
			{
				Level lvl = EditorLevelCache.GetLevel();
				if (lvl && lvl.Manifest)
				{
					switch (zone.Layer)
					{
						case AudioLayer.Music:
							profile = lvl.Manifest.MusicProfile;
							break;
						case AudioLayer.Ambience:
							profile = lvl.Manifest.AmbientProfile;
							break;
					}
				}
			}


			if (Event.current.type == EventType.Layout)
			{
				mismatch = false;

				//If we have no override profile, we should initialize it.
				if (zone.TrackOverridesProfile == null && profile != null)
				{
					zone.UpdateTrackOverrides(profile);
					zone.TrackOverridesProfile = profile;
				}
				else
				{
					if (zone.TrackOverridesProfile != profile)
						mismatch = true;
				}
			}

			glo.Space(16);
			SirenixEditorGUI.Title("Profile Override Set:", "", TextAlignment.Left, true);

			if (mismatch)
			{
				GUI.backgroundColor = Color.red;
				glo.BeginVertical(SirenixGUIStyles.BoxContainer);
				GUI.backgroundColor = Color.white;

				glo.Label(profile != null ? "New profile does not match override sets' profile." : "There is an override set for a previous profile, and the new profile is null.", boldWrapping);

				if (glo.Button(profile != null ? "Reset override set" : "Delete override set"))
				{
					zone.UpdateTrackOverrides(profile);
					zone.TrackOverridesProfile = profile;
				}

				glo.EndVertical();

				GUI.enabled = false;
			}

			if (zone.TrackOverridesProfile != null && zone.TrackOverrides != null)
			{
				if (layout == null) layout = new SimpleLayout();

				foreach (KeyValuePair<string, AudioTrack.OverrideValues> trackOverride in zone.TrackOverrides)
				{
					layout.SetupDrawRect(CONTROL_HEIGHT);
					layout.Begin(4, 4);

					if (Event.current.type == EventType.Repaint)
					{
						if (mismatch)
							GUI.backgroundColor = Color.HSVToRGB(0.0f, 0.6f, 0.8f);
						SirenixGUIStyles.BoxContainer.Draw(layout.rect, GUIContent.none, false, false, false, false);
						GUI.backgroundColor = Color.white;
					}

					glo.Space(3);

					AudioTrack track = zone.TrackOverridesProfile.Tracks.FirstOrDefault(x => x.ID == trackOverride.Key);
					if (track != null)
					{
						AudioTrack.OverrideValues OverrideValues = trackOverride.Value;

						bool overriding = OverrideValues.Applies;

						OverrideValues.Applies = g.Toggle(layout.GetRect(16), OverrideValues.Applies, GUIContent.none);
						layout.DoLabel("Override \"" + track.Name + "\" Config");
						layout.NewLine();


						if (!overriding) GUI.enabled = false;

						layout.DoLabel("Mute:");
						if (overriding)
							OverrideValues.Mute = g.Toggle(layout.GetRect(16), OverrideValues.Mute, GUIContent.none);
						else g.Toggle(layout.GetRect(16), track.Config.Mute, GUIContent.none);

						if ((overriding && OverrideValues.Mute) || (!overriding && track.Config.Mute))
							GUI.enabled = false;

						layout.HSpace(6);

						layout.DoLabel("Volume:");
						if (overriding)
							OverrideValues.Volume = eg.FloatField(layout.GetRect(32), OverrideValues.Volume);
						else eg.FloatField(layout.GetRect(32), track.Config.Volume);

						layout.HSpace(6);

						layout.DoLabel("Pitch:");
						if (overriding)
							OverrideValues.Pitch = eg.FloatField(layout.GetRect(32), OverrideValues.Pitch);
						else eg.FloatField(layout.GetRect(32), track.Config.Pitch);

						if (!mismatch)
							GUI.enabled = true;


						OverrideValues.Volume = Mathf.Clamp01(OverrideValues.Volume);
						OverrideValues.Pitch  = Mathf.Clamp(OverrideValues.Pitch, -3, 3);
					}
				}
			}
			else
			{
				glo.Label("Please select a profile to override.");
			}

			GUI.enabled = true;
		}
	}

#endif
}