using UnityEngine;
using UnityEngine.Playables;

namespace Util.UnityTimeline
{
	public class PositionMixer : PlayableBehaviour
	{
		private Vector3 _initialPosition;
		private bool    _hasFirstFrame;

		public override void ProcessFrame(Playable playable, FrameData info, object playerData)
		{
			Transform transformBinding = playerData as Transform;

			if (transformBinding == null)
				return;

			if (!_hasFirstFrame)
			{
				_initialPosition = transformBinding.position;
				_hasFirstFrame   = true;
			}

			Vector3 combinedPosition = Vector3.zero;
			float   combinedWeight   = 0;

			int inputCount = playable.GetInputCount();
			for (int i = 0; i < inputCount; i++)

			{
				PositionData position = ((ScriptPlayable<PositionData>) playable.GetInput(i)).GetBehaviour();
				float        weight   = playable.GetInputWeight(i);

				combinedPosition += position.position * weight;
				combinedWeight   += weight;
			}

			float invWeight = 1 - combinedWeight;

			Vector3 a = _initialPosition * invWeight;
			Vector3 b = combinedPosition;

			transformBinding.position = a + b;
		}
	}
}