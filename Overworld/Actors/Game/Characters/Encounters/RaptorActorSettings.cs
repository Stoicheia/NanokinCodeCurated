using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Actors
{
	public class RaptorActorSettings : SerializedScriptableObject
	{
		[SerializeField] public float                  DashForce;
		[SerializeField] public TankMoveState.Settings MoveState;
		[SerializeField] public JumpState.Settings     JumpSettings;
		[SerializeField] public FallState.Settings     FallSettings;
	}
}