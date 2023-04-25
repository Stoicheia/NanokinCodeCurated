using System;
using Anjin.Scripting;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

[Serializable]
[DarkBox]
[LuaUserdata]
public struct AudioDef
{
	[Optional, HideLabel]
	[OnValueChanged("OnClipChanged")]
	public AudioClip clip;
	[HorizontalGroup("Volume"), HideLabel, SuffixLabel("vol", true)]
	public float volume;
	[HorizontalGroup("Volume"), HideLabel, SuffixLabel("+-", true)]
	public float volumeMod;
	[HorizontalGroup("Pitch"), HideLabel, SuffixLabel("pitch", true)]
	public float pitch;
	[HorizontalGroup("Pitch"), HideLabel, SuffixLabel("+-", true)]
	public float pitchMod;

	public float Duration         => clip.length;
	public float EvaluatePitch()  => pitch + RNG.Range(-pitchMod, pitchMod);
	public float EvaluateVolume() => volume + RNG.Range(-volumeMod, volumeMod);

	public bool IsValid => clip != null && volume > Mathf.Epsilon;

	private AudioDef Default => new AudioDef
	{
		volume = 1,
		pitch  = 1,
	};

	public static AudioDef None => new AudioDef {
		clip = null,
		volume = 0,
	};

	public static implicit operator AudioDef(AudioClip clip)
	{
		return new AudioDef
		{
			clip   = clip,
			volume = 1,
			pitch  = 1
		};
	}

#if UNITY_EDITOR
	public void OnClipChanged()
	{
		// We can't set default values for a struct, so this little function
		// will automatically set some defaults. A volume or pitch of 0 doesn't
		// really make any sense, so this does the job nicely!
		if (Mathf.Approximately(volume, 0)) volume = 1;
		if (Mathf.Approximately(pitch, 0)) pitch   = 1;
	}
#endif
}