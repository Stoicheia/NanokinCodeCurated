using UnityEngine.Playables;

namespace Util.Components.Cinemachine
{
	public class OrbitExtensionMixer : PlayableBehaviour
	{
		private SphereCoordinate _initialCoordinates;
		private bool             _hasFirstFrame;

		public override void ProcessFrame(Playable playable, FrameData info, object playerData)
		{
			CinemachineOrbit orbitBinding = playerData as CinemachineOrbit;

			if (orbitBinding == null)
				return;

			if (!_hasFirstFrame)
			{
				_initialCoordinates = orbitBinding.Coordinates;
				_hasFirstFrame      = true;
			}


			int   inputCount  = playable.GetInputCount();
			float totalWeight = 0;

			SphereCoordinate target = new SphereCoordinate();

			for (int i = 0; i < inputCount; i++)
			{
				OrbitExtensionData input  = ((ScriptPlayable<OrbitExtensionData>) playable.GetInput(i)).GetBehaviour();
				float              weight = playable.GetInputWeight(i);

				target.azimuth   += input.azimuth   * weight;
				target.elevation += input.elevation * weight;
				target.distance  += input.distance  * weight;

				totalWeight += weight;
			}

			float invWeight = 1 - totalWeight;

			orbitBinding.Coordinates = target + _initialCoordinates * invWeight;
		}
	}
}