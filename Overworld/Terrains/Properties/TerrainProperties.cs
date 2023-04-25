using Sirenix.OdinInspector;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Overworld.Terrains
{
	/// <summary>
	/// Reusable set of properties for a surface.
	/// </summary>
	public class TerrainProperties : SerializedScriptableObject
	{
		[FormerlySerializedAs("SteppingSound")]
		public AudioDef StepSound;

		[FormerlySerializedAs("FrictionMultiplier")]
		[Range01]
		public float Friction = 1;
	}
}