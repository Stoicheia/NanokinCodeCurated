using Anjin.Actors;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;


namespace Anjin.Cameras
{
	[EnumToggleButtons]
	public enum CamTargetType
	{
		None,
		Player,
		WorldPoint
	}

	public class VCamTarget : MonoBehaviour
	{
		public CamTargetType Type;

		[ShowIf("@Type == Anjin.Cameras.CamTargetType.WorldPoint")]
		public WorldPoint Point;
		public Vector3    Offset;

		[FormerlySerializedAs("lerpSpeed")]
		[Range(0, 1)]
		public float LerpSpeed = 0.5f;

		void LateUpdate()
		{
			Vector3 targetPos = transform.position;

			switch(Type) {
				case CamTargetType.Player when ActorController.playerActor != null: {
					targetPos = ActorController.playerActor.transform.position;
					break;
				}

				case CamTargetType.WorldPoint: {
					if(Point.TryGet(out var pos))
						targetPos = pos;

					break;
				}
			}

			transform.position = Vector3.Lerp(transform.position, targetPos, LerpSpeed);
		}
	}
}