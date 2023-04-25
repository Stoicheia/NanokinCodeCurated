using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Actors
{
	public class NanokeeperActorSettings : SerializedScriptableObject
	{
		[SerializeField] public TankMoveState.Settings MoveState;
		[SerializeField] public FallState.Settings FallState;
	}
}
