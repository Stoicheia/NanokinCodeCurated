using System;
using System.Diagnostics;
using Anjin.EventSystemNS;
using Drawing;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityUtilities;
using Util;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(BoxCollider)), ExecuteInEditMode]
public class EdgeTransitionVolume : TransitionVolume, IShouldDrawGizmos
{

	public Vector3 Size = Vector3.one;
	[EnumToggleButtons]
	public Axis2D BoxAxis;

	public bool AutoSetLayer = true;

	[ReadOnly]
	public BoxCollider Collider;

	void OnEnable()
	{
		Collider = GetComponent<BoxCollider>();
		if (AutoSetLayer)
			gameObject.layer = Layers.TriggerVolume;
	}

	void OnDisable()
	{
		Collider = null;
	}

	[Conditional("UNITY_EDITOR")]
	void Update()
	{
		Collider.size   = Size;
		Collider.center = new Vector3(0, Size.y / 2, 0);

		Collider.isTrigger = true;
	}

	public override Vector3 GetSideA(float extrusion = 0) => Orientation == TransitionOrientation.Positive ? GetPositiveEdge(extrusion) : GetNegativeEdge(extrusion);
	public override Vector3 GetSideB(float extrusion = 0) => Orientation == TransitionOrientation.Positive ? GetNegativeEdge(extrusion) : GetPositiveEdge(extrusion);

	public override TransitionOrientation GetOrientationFromPos(Vector3 pos)
	{
		Matrix4x4 mat           = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
		var       corrected_pos = mat.inverse.MultiplyPoint3x4(pos);

		if (BoxAxis == Axis2D.x) {
			if (corrected_pos.x < 0) 	  return Orientation.Opposite();
			else if(corrected_pos.x >= 0) return Orientation;
		}
		else {
			if (corrected_pos.z < 0) 	   return Orientation.Opposite();
			else if (corrected_pos.z >= 0) return Orientation;
		}

		return TransitionOrientation.Positive;
	}

	public Vector3 GetPositiveEdge(float extrusion = 0)
	{
		if(BoxAxis == Axis2D.x) return (transform.TransformPoint(new Vector3(Size.x / 2 + extrusion, 0, 0)));
		else 					return (transform.TransformPoint(new Vector3(0, 0, Size.z / 2 + extrusion)));
	}

	public Vector3 GetNegativeEdge(float extrusion = 0)
	{
		if(BoxAxis == Axis2D.x) return (transform.TransformPoint(new Vector3(-(Size.x / 2) - extrusion, 0, 0)));
		else 					return (transform.TransformPoint(new Vector3(0,   0, -(Size.z / 2) - extrusion)));
	}

	public override Collider GetCollider() => Collider;
	public override Vector3 GetTargetPositionFromHit(Vector3 HitPosition)
	{
		if(Collider == null) return HitPosition;

		Vector3 targetPos = Vector3.zero;

		Vector3 scale = new Vector3(transform.localScale.x, 0, transform.localScale.z);
		Vector3 size  = Collider.size / 2;

		Matrix4x4 mat = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

		Vector3 relative_pos = mat.inverse.MultiplyPoint3x4(HitPosition);

		if (BoxAxis == Axis2D.x) {
			if (relative_pos.x < 0)			targetPos.x = (size.x * scale.x) + 2;
			else if(relative_pos.x >= 0)	targetPos.x = -(size.x * scale.x) - 2;
		}
		else if (BoxAxis == Axis2D.y) {
			if (relative_pos.z < 0)			targetPos.z = (size.z * scale.z) + 2;
			else if(relative_pos.z >= 0)	targetPos.z = -(size.z * scale.z) - 2;
		}

		return mat.MultiplyPoint3x4(targetPos);
	}

	public override NavMeshPath GetPath(Vector3 HitPosition)
	{
		var targetPos = GetTargetPositionFromHit(HitPosition);

		var path = new NavMeshPath();
		NavMesh.CalculatePath(HitPosition, targetPos, 0, path);
		return path;
	}

	private static bool     _stylesMade = false;
	private        GUIStyle stlye1;
	private        GUIStyle stlye2;

	public override void DrawGizmos()
	{
		if (!_stylesMade) {
			_stylesMade = true;

			/*var style1 = EventStyles.GetTitleWithColor(ColorsXNA.CornflowerBlue);
			var style2 = EventStyles.GetTitleWithColor(ColorsXNA.LimeGreen);
			style1.alignment = TextAnchor.MiddleCenter;
			style2.alignment = TextAnchor.MiddleCenter;*/
		}

		Color c = Color.magenta;
		Vector3 center = /*transform.position + */new Vector3(Collider.center.x, Collider.center.y - Collider.size.y / 2 + 0.1f, Collider.center.z);

		Vector3 size  = Vector3.zero;
		Vector3 csize = Collider.size;
		Vector3 ccenter = Collider.center;

		if (BoxAxis == Axis2D.y)
			size = new Vector3(Mathf.Min(1, csize.x * 0.2f), 0.1f, csize.z);
		else if (BoxAxis == Axis2D.x)
			size = new Vector3(csize.x, 0.1f, Mathf.Min(1, csize.z * 0.2f));

		using (Draw.WithMatrix(transform.localToWorldMatrix)) {
			Draw.SolidBox(center, size, Color.magenta.Alpha(0.3f));
			Draw.WireBox(center, size, Color.magenta);

			Draw.SolidBox(ccenter, csize, Color.red.Alpha(0.1f));
			Draw.WireBox(ccenter, csize, Color.red.Alpha(0.8f));

		}

		Draw.Label2D(GetSideA() + Vector3.up * 0.5f, "Outgoing (A)", 14f, LabelAlignment.Center, ColorsXNA.CornflowerBlue);
		Draw.Label2D(GetSideB() + Vector3.up * 0.5f, "Incoming (B)", 14f, LabelAlignment.Center, ColorsXNA.LimeGreen);


		/*var prev = Gizmos.matrix;
		Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);

		Gizmos.DrawCube(center, size);
		c.a = 0.8f;
		Gizmos.DrawWireCube(center, size);


		c            = Color.red;
		c.a          = 0.2f;
		Gizmos.color = c;

		Gizmos.DrawCube(Collider.center /*transform.position+Collider.center#1#, Collider.size);
		c.a          = 0.9f;
		Gizmos.color = c;

		Gizmos.DrawWireCube(Collider.center /*transform.position +Collider.center#1#, Collider.size);


		Handles.Label(, , style1);
		Handles.Label(GetSideB() + Vector3.up * 0.5f, "Incoming (B)", style2);


		Gizmos.matrix = prev;*/


		/*var rot = Matrix4x4.Rotate(transform.rotation);

			void test_draw(Vector3 vec, Color color)
			{
				var pos = transform.position + rot.MultiplyPoint3x4(vec);

				Gizmos.color = color;
				Gizmos.DrawWireSphere(pos, 0.3f);

				DebugDraw.DrawMarker(GetTargetPositionFromHit(pos), 1, color, 0);
			}

			if (BoxAxis == Axis2D.x) {
				test_draw( Vector3.right, Color.red);
				test_draw( Vector3.right + Vector3.forward * 1, Color.red);
				test_draw( Vector3.right + Vector3.forward * 2, Color.red);
				test_draw( Vector3.right + Vector3.forward * 3, Color.red);
				test_draw( Vector3.right + Vector3.forward * 4, Color.red);

				test_draw( Vector3.right - Vector3.forward * 1, Color.red);
				test_draw( Vector3.right - Vector3.forward * 2, Color.red);
				test_draw( Vector3.right - Vector3.forward * 3, Color.red);
				test_draw( Vector3.right - Vector3.forward * 4, Color.red);

				test_draw(-Vector3.right, Color.yellow);
				test_draw( -Vector3.right + Vector3.forward * 1, Color.yellow);
				test_draw( -Vector3.right + Vector3.forward * 2, Color.yellow);
				test_draw( -Vector3.right + Vector3.forward * 3, Color.yellow);
				test_draw( -Vector3.right + Vector3.forward * 4, Color.yellow);

				test_draw( -Vector3.right - Vector3.forward * 1, Color.yellow);
				test_draw( -Vector3.right - Vector3.forward * 2, Color.yellow);
				test_draw( -Vector3.right - Vector3.forward * 3, Color.yellow);
				test_draw( -Vector3.right - Vector3.forward * 4, Color.yellow);
			} else {
				test_draw( Vector3.forward, Color.red);
				test_draw(-Vector3.forward, Color.yellow);
			}*/

	}

	public bool ShouldDrawGizmos()
	{

		if (Collider == null) return false;

		Vector3 size  = Vector3.zero;
		Vector3 csize = Collider.size;

		if (BoxAxis == Axis2D.y)
			size = new Vector3(Mathf.Min(1, csize.x * 0.2f), 0.1f, csize.z);
		else if (BoxAxis == Axis2D.x)
			size = new Vector3(csize.x, 0.1f, Mathf.Min(1, csize.z * 0.2f));

		Bounds bounds = new Bounds(transform.position, size);


		Vector3 cpos = Camera.current.transform.position;
		return Vector3.Distance(bounds.ClosestPoint(cpos), cpos) < 50f;
	}

	#if UNITY_EDITOR



	/*private void OnDrawGizmos()
	{

	}*/
#endif
}