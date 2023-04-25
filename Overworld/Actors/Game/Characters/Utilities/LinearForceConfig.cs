using System;
using Sirenix.OdinInspector;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	[Serializable]
	public class LinearForceConfig
	{
		public                    float gainPerSecond;
		[MinValue(0.001f)] public float decayPerSecond;
		[Range01]          public float scalePerSecond;
		public                    float minForce;
		public                    float maxForce;
	}
}