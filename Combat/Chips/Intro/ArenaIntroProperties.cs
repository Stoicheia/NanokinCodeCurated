using System;
using System.Collections.Generic;
using Anjin.Cameras;
using Cinemachine;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Timeline;
using Util.Odin.Attributes;

namespace Combat.Components
{
	/// <summary>
	/// A cutscene driven by a timeline which introduces the battle with a camera motion.
	/// Finished when the finish cutscene timed event is received.
	/// </summary>
	public class ArenaIntroProperties : MonoBehaviour
	{
		public GameObject RootObject;

		[SerializeField]
		public TimelineAsset Timeline;

		[SerializeField]
		public CinemachineVirtualCamera VCam;

		[OdinSerialize]
		public List<ArenaIntroAnim.SpawnEntry> TeamSpawns = new List<ArenaIntroAnim.SpawnEntry>();

		[NonSerialized, ShowInPlay]
		public CamController controller;

		/// <summary>
		/// Prepare the intro so the game is showing the first frame of the intro.
		/// (Vcam active)
		/// </summary>
		/// <returns></returns>
		public bool SetInitialState()
		{
			/*controller = new CamController(VCam, GameCams.Cut);
			GameCams.Push(controller);*/
			return true;
		}
	}
}