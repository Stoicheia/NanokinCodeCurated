using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Anjin.Scripting;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Actors
{
	[LuaUserdata]
	public class ActorPlaybackData : ScriptableObject
	{
		public float FPS = 60;

		[ListDrawerSettings(ShowIndexLabels = true, DraggableItems = false, IsReadOnly = true)]
		public List<ActorKeyframe> Keyframes = new List<ActorKeyframe>();

		public int FrameCount => Keyframes.Count;

		public void Clear()
		{
			Keyframes.Clear();
		}

		/// <summary>
		/// Get the disk space for this playback data.
		/// </summary>
		/// <returns></returns>
		public string GetHumanReadableSize()
		{
			string[] sizes = {"B", "KB", "MB", "GB", "TB"};

			int bytes = 0;

			bytes += Marshal.SizeOf<ActorKeyframe>() * Keyframes.Count;

			int order = 0;

			while (bytes >= 1024 && order < sizes.Length - 1)
			{
				order++;
				bytes /= 1024;
			}

			// Adjust the format string to your preferences. For example "{0:0.#}{1}" would
			// show a single decimal place, and no space.
			return $"{bytes:0.##} {sizes[order]}";
		}
	}

	[Serializable]
	public struct ActorKeyframe
	{
		public float      Time;
		public AnimID     State;
		public Vector3    Position;
		public Vector3    Facing;
		public Vector3Int PositionCurve;
		public int        FacingCurve;
	}
}