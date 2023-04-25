using System;
using UnityEngine;

namespace Anjin.Actors
{
	[Serializable]
	public class GroundState : StateKCC
	{
		public override bool IsGround => true;
		public override bool IsAir    => false;

		public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			currentVelocity *= 0.8f;
		}
	}
}