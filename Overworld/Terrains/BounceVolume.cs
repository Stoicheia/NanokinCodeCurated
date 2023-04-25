using Drawing;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Overworld.Terrains
{
	[AddComponentMenu("Anjin: Level Building/Bounce Volume")]
	public class BounceVolume : MonoBehaviourGizmos
	{
		private static readonly int ANIM_Bounce = Animator.StringToHash("Bounce");

		[Title("Design"), FormerlySerializedAs("_force")]
		[SerializeField] private float Height = 8;
		[FormerlySerializedAs("_direction"), SerializeField]
		private Vector3 Direction = Vector3.up;
		[FormerlySerializedAs("_energyConservation"), Tooltip("Defines the fraction of energy that will be conserved when landing with downward velocity.")]
		[SerializeField, Range01] private float EnergyConservation = 0.15f;
		[FormerlySerializedAs("_maxEnergyConservation"), Tooltip("Max amount of energy that can be conserved.")]
		[SerializeField] private float EnergyConservationMax = 1;

		[Title("Animation")]
		[FormerlySerializedAs("_animator")]
		[SerializeField] private Animator Animator;

		[FormerlySerializedAs("_adBounce")]
		[SerializeField] private AudioDef SFX;

		[Title("Gizmo")]
		[LabelText("Color")]
		private Color GizmoColor = Color.cyan;

		private Collider _collider;

		private void Awake()
		{
			if (Animator) Animator.SetBool(ANIM_Bounce, true);
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.TryGetComponent(out BounceInfo.IHandler bounceable))
			{
				if (bounceable.CanBounce)
				{
					bounceable.OnBounce(new BounceInfo(0, Direction, Height, EnergyConservation, EnergyConservationMax));

					if (Animator) Animator.SetTrigger(ANIM_Bounce);

					GameSFX.Play(SFX, transform.position);
				}
			}
		}

		public override void DrawGizmos()
		{
			Vector3 pos = Vector3.zero;

			if (_collider == null)
				_collider = GetComponent<Collider>();

			switch (_collider)
			{
				case BoxCollider box:
					pos = box.center;
					break;

				case SphereCollider sphere:
					pos = sphere.center;
					break;
			}

			using (Draw.WithMatrix(Matrix4x4.TRS(transform.TransformPoint(pos), transform.rotation, Vector3.one)))
			using (Draw.WithLineWidth(2f))
			{
				Draw.Arrow(float3.zero, Direction * Height, Vector3.up, 0.3f, GizmoColor);
				Draw.CircleXZ(float3.zero, 1, GizmoColor);
			}
		}
	}
}