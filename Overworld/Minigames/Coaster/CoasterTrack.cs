using System;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;

namespace Anjin.Minigames
{
	public class CoasterTrack : SerializedMonoBehaviour
	{
		public SplineComputer Spline;

		public enum EndBehaviors
		{
			None	= -1,
			Voided	= 0,
			JumpTo	= 2,
			Eject	= 3,
		}


		public Option<float> BaseSpeed;
		public Option<float> MaxSpeed;

		[EnumToggleButtons]
		public EndBehaviors EndBehavior = EndBehaviors.Voided;

		public CoasterTrack JumpToTarget;
		public float		JumpToDistance;

		public Transform EjectPointForwards;
		public Transform EjectPointBackwards;

		private void Awake()
		{
			if(!Spline)
				Spline = GetComponent<SplineComputer>();

			//Spline.AddTrigger(0, )
		}
	}
}