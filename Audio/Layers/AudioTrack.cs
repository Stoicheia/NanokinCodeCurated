using System;
using UnityEngine;
using UnityEngine.Serialization;
using Sirenix.OdinInspector;
#if UNITY_EDITOR
using Anjin.Editor;
using Sirenix.Utilities.Editor;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using g = UnityEngine.GUI;
using glo = UnityEngine.GUILayout;
using eg = UnityEditor.EditorGUI;
using eglo = UnityEditor.EditorGUILayout;
#endif

namespace Anjin.Audio
{
	[HideReferenceObjectPicker]
	[Serializable]
	public class AudioTrack
	{
		/// <summary>
		/// The track's ID for referencing purposes.
		/// </summary>
		[ReadOnly]
		public string ID;

		/// <summary>
		/// The track's name, for clean printing in editor.
		/// </summary>
		public string Name;

		/// <summary>
		/// The audio clip played by this track.
		/// </summary>
		public AudioClip Clip;

		/// <summary>
		/// The track's settings.
		/// </summary>
		public ConfigValues Config = new ConfigValues();

		public AudioTrack()
		{
			//System.Random rand = new System.Random();
			ID = DataUtil.MakeShortID(8/*, rand*/);
		}

		[InlineProperty, TitleGroup("Config")]
		[HideReferenceObjectPicker]
		[HideLabel]
		[Serializable]
		public class ConfigValues
		{
			/// <summary>
			/// Whether or not the track is enabled.
			/// If disabled, the track will not play, period.
			/// </summary>
			public bool Enabled = true;

			/// <summary>
			/// Whether or not to play this track immediately.
			/// </summary>
			public bool PlayImmediately = true;

			/// <summary>
			/// Whether or not the track is muted. (will not play)
			/// Can be overriden by a zone.
			///</summary>
			public bool Mute = false;

			/// <summary>
			/// Whether or not the track should loop.
			/// </summary>
			public bool Loop = true;

			/// <summary>
			/// The default volume this track plays at.
			/// Can be overriden by a zone.
			///</summary>
			public float Volume = 1;

			/// <summary>
			/// The default pitch this track plays at.
			/// Can be overriden by a zone.
			///</summary>
			public float Pitch = 1;

			/// <summary>
			/// The initial volume when this track is first added. (automatically lerps to the default)
			/// Leave at -1 to simply start at the desired volume from the get-go.
			/// </summary>
			public float InitialVolume = -1;

			/// <summary>
			/// The amount of seconds to wait before playing this track.
			/// </summary>
			public float Delay = 0;
		}

		/// <summary>
		/// An override for an AudioTrack's settings.
		/// </summary>
		[Serializable]
		public class OverrideValues
		{
			/// <summary>
			/// Indicate that the override should apply.
			/// (Otherwise it is ignored)
			/// </summary>
			[FormerlySerializedAs("Override")]
			public bool Applies = false;

			/// <summary>
			/// Indicate that the track should be muted
			/// </summary>
			public bool Mute = false;
			public float Volume = 1;
			public float Pitch  = 1;

			public OverrideValues(ConfigValues config)
			{
				if (config == null)
					return;

				Mute   = config.Mute;
				Volume = config.Volume;
				Pitch  = config.Pitch;
			}
		}
	}

#if UNITY_EDITOR
	public class AudioTrackDrawer : OdinValueDrawer<AudioTrack>
	{
		SimpleLayout layout;
		const float  CONTROL_HEIGHT = 62;


		protected override void Initialize()
		{
			base.Initialize();
			layout = new SimpleLayout();
		}

		protected override void DrawPropertyLayout(GUIContent label)
		{
			EditorGUI.BeginChangeCheck();


			var track = ValueEntry.SmartValue;
			if (track == null) return;

			layout.SetupDrawRect(CONTROL_HEIGHT);

			if(Event.current.type == EventType.Repaint)
			{
				SirenixGUIStyles.BoxContainer.Draw(layout.rect, GUIContent.none, false, false, false, false);
			}

			layout.Begin(4, 4);

			track.Config.Enabled = g.Toggle(layout.GetRect(16), track.Config.Enabled, GUIContent.none);

			layout.DoLabelWidth("Name:", 48, EditorStyles.boldLabel);
			track.Name = g.TextField(layout.GetRectStretch(), track.Name);

			layout.NewLine(20);
			layout.HSpace(16);

			layout.DoLabelWidth("Clip:", 48, EditorStyles.boldLabel);

			track.Clip = eg.ObjectField(layout.GetRectStretch(), track.Clip, typeof(AudioClip), false) as AudioClip;

			layout.NewLine(20);
			layout.HSpace(16);

			if (!track.Config.Enabled) GUI.enabled = false;

			layout.DoLabelWidth("Config:", 48, EditorStyles.boldLabel);
			layout.HSpace(16);

			//Config
			layout.DoLabel("Mute:");
			track.Config.Mute = g.Toggle(layout.GetRect(16), track.Config.Mute, GUIContent.none);

			if (track.Config.Mute) GUI.enabled = false;

			layout.DoLabel("Volume:");
			track.Config.Volume = eg.FloatField(layout.GetRect(32), track.Config.Volume);

			layout.DoLabel("Initial Volume:");
			track.Config.InitialVolume = eg.FloatField(layout.GetRect(32), track.Config.InitialVolume);

			layout.HSpace(6);

			layout.DoLabel("Pitch:");
			track.Config.Pitch = eg.FloatField(layout.GetRect(32), track.Config.Pitch);

			GUI.enabled = true;

			track.Config.Volume = Mathf.Clamp01(track.Config.Volume);
			track.Config.Pitch  = Mathf.Clamp(track.Config.Pitch, -3, 3);

			//layout.NewLine();

			layout.DoLabel("Play Instant:");
			track.Config.PlayImmediately = g.Toggle(layout.GetRect(16), track.Config.PlayImmediately, GUIContent.none);


			if (EditorGUI.EndChangeCheck())
			{
				UnityEditor.EditorUtility.SetDirty(Property.Tree.UnitySerializedObject.targetObject);
				PrefabUtility.RecordPrefabInstancePropertyModifications(Property.Tree.UnitySerializedObject.targetObject);
			}

			/*glo.BeginVertical(SirenixGUIStyles.BoxContainer);

			glo.BeginHorizontal();
			{
				track.Config.Enabled = glo.Toggle(track.Config.Enabled, GUIContent.none, glo.Width(20));

				glo.Label(track.ID, glo.ExpandWidth(false));
				track.Name = glo.TextField(track.Name);
				track.Clip = eglo.ObjectField(track.Clip, typeof(AudioClip), false) as AudioClip;

			}
			glo.EndHorizontal();

			glo.BeginHorizontal();
			{
				glo.Label("Config:", glo.ExpandWidth(false));
				glo.FlexibleSpace();

				//Config
				glo.Label("Volume:");
				track.Config.Volume = eglo.FloatField(track.Config.Volume, glo.Width(32));

				glo.Label("Pitch:");
				track.Config.Pitch  = eglo.FloatField(track.Config.Pitch,  glo.Width(32));
			}
			glo.EndHorizontal();

			glo.EndVertical();*/
		}
	}

#endif
}