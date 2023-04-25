using System;
using UnityEngine;

namespace Anjin.Utils
{
	public abstract class CustomMotion : MonoBehaviour
	{
		[NonSerialized]
		public MotionBehaviour motion;

		public abstract void OnMotionUpdate();

		public abstract void OnMotionStart();
	}
}