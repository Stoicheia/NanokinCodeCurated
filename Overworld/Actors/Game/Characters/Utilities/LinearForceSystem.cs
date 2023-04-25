using System;
using Anjin.Util;
using UnityEngine;

namespace Anjin.Actors
{
	[Serializable]
	public class LinearForceSystem
	{
		public float gainPerSecond;
		public float decayPerSecond;
		public float scalePerSecond;
		public float minimum;
		public float maximum;

		public float Force { get; private set; }

		public void Decay()
		{
			if (Force > 0) Force -= decayPerSecond * Time.deltaTime;
			else Force           += decayPerSecond * Time.deltaTime;

			Force *= scalePerSecond;

			ClampForce();
		}


		public void Gain(float force)
		{
			if (Force > 0) Force += gainPerSecond * force * Time.deltaTime;
			else Force           -= gainPerSecond * force * Time.deltaTime;

			ClampForce();
		}

		private void ClampForce()
		{
			Force = Force.Clamp(minimum, maximum);
		}
	}
}