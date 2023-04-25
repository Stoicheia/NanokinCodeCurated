using Drawing;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Components;
using Util.UnityEditor.Odin.Attributes;

namespace Anjin.Nanokin.Map {

	[SelectionBase]
	public class WaterJet : AnjinBehaviour, IMoveable {

		[Title("Stream")]
		[ColoredBack(0, 0.75f, 0.4f)] public float Height;
		[ColoredBack(0, 0.75f, 0.4f)] public float Radius;

		[Title("Head")]
		[LabelText("Radius")]
		[ColoredBack(0.4f, 0.75f, 0.4f)] public float HeadRadius;
		[LabelText("Y Offset")]
		[ColoredBack(0.4f, 0.75f, 0.4f)] public float HeadYOffset;

		[LabelText("Stable Point Offset")]
		[ColoredBack(0.6f, 0.75f, 0.4f)] public float HeadStableYOffset;

		[LabelText("Lowest Point Offset")]
		[ColoredBack(0.75f, 0.75f, 0.4f)] public float HeadLowerPointOffset;

		[FormerlySerializedAs("HeadHighestPoint")]
		[LabelText("Highest Point Offset")]
		[ColoredBack(0.1f, 0.75f, 0.5f)] public float HeadHighestPointOffset;

		public CapsuleCollider StreamCollider;
		public SphereCollider  HeadCollider;

		public Vector3 HeadCenter  => transform.position + new Vector3(0, (Height /** (1 / transform.lossyScale.y)*/ + HeadStableYOffset)      , 0);
		public Vector3 HeadYMinPos => transform.position + new Vector3(0, (Height /** (1 / transform.lossyScale.y)*/ + HeadLowerPointOffset)   , 0);
		public Vector3 HeadYMaxPos => transform.position + new Vector3(0, (Height /** (1 / transform.lossyScale.y)*/ + HeadHighestPointOffset) , 0);

		protected override void OnRegisterDrawer() => DrawingManagerProxy.Register(this);
		private            void OnDestroy()        => DrawingManagerProxy.Deregsiter(this);

		private void InsureColliders()
		{
			if (StreamCollider == null) {
				StreamCollider = gameObject.AddComponent<CapsuleCollider>();
				StreamCollider.hideFlags = HideFlags.NotEditable;
			}

			if (HeadCollider == null) {
				HeadCollider = gameObject.AddComponent<SphereCollider>();
				HeadCollider.hideFlags = HideFlags.NotEditable;
			}
		}

		private void UpdateColliders()
		{
			float height      = Height      / transform.lossyScale.y;
			float headYOffset = HeadYOffset / transform.lossyScale.y;
			float radius      = Radius      / transform.lossyScale.magnitude;

			//Matrix4x4 mat = Matrix4x4.Scale(transform.rotation);

			StreamCollider.height = height;
			StreamCollider.radius = radius;
			StreamCollider.center = /*mat.MultiplyPoint3x4*/(new Vector3(0, height / 2, 0));

			float sphereScale = new Vector2(transform.lossyScale.x, transform.lossyScale.z).magnitude;

			HeadCollider.radius = HeadRadius / sphereScale;
			HeadCollider.center = /*mat.MultiplyPoint3x4*/(new Vector3(0, height + headYOffset, 0));
		}

		private void OnValidate()
		{
			InsureColliders();
			UpdateColliders();
		}

		private void Awake()
		{
			InsureColliders();
			UpdateColliders();

			if (TryGetComponent(out IMover mover)) {
				mover.RegisterMoveable(this);
			}
		}

		public override void DrawGizmos()
		{
			using(Draw.WithMatrix(Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale))) {

				var   yscale = 1 / transform.lossyScale.y;
				float radius = Radius;

				if(StreamCollider)
					Draw.WireCapsule(Vector3.zero, transform.up, StreamCollider.height, StreamCollider.radius, Color.red);

				if(HeadCollider)
					Draw.WireSphere(HeadCollider.center, HeadCollider.radius, Color.green);

				Draw.CircleXZ(new Vector3(0, (Height + HeadStableYOffset)      * yscale, 0), radius * 1.45f, Color.blue);
				Draw.CircleXZ(new Vector3(0, (Height + HeadHighestPointOffset) * yscale, 0), radius * 1.45f, ColorsXNA.Orange);
				Draw.CircleXZ(new Vector3(0, (Height + HeadLowerPointOffset)   * yscale, 0), radius * 1.45f, ColorsXNA.Purple);
			}

		}

		private Vector3 _velocity;
		[ShowInInspector]
		public Vector3 Velocity {
			get => _velocity;
			set {
				_velocity = value;
			}
		}
	}
}