using Anjin.Actors;
using Anjin.Util;
using Combat.Data;
using Drawing;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Components;

namespace Util.Math.Splines
{
	public class LineShape : PlotShape, IShouldDrawGizmos
	{
		[Title("Setup")]
		[FormerlySerializedAs("WorldP1")]
		public Vector3 LocalP1;

		[FormerlySerializedAs("WorldP2")]
		public Vector3 LocalP2;

		[FormerlySerializedAs("snapToGround"), Space]
		[FormerlySerializedAs("isSnapToGround")]
		public bool GroundSnap = true;

		[FormerlySerializedAs("groundHoverDistance"), ShowIf("GroundSnap")]
		public float GroundHover;

		[FormerlySerializedAs("angleForward"), FormerlySerializedAs("_angleForward")]
		public float AngleForward;

		[FormerlySerializedAs("nPlots"), FormerlySerializedAs("_nPlots")]
		public int PlotCount = 0;

		[Title("Gizmo")]
		public bool GizmoLine = true;

		[InfoBox("The first slot is the colored one.")]
		[FormerlySerializedAs("gizmoColor")]
		[FormerlySerializedAs("_gizmoColor")]
		public Color GizmoColor = Color.green;

		[FormerlySerializedAs("gizmoPoints"), FormerlySerializedAs("nGizmoPlots"), FormerlySerializedAs("_nGizmoPlots")]
		public int GizmoCount = -1;

		public Vector3 WorldP1 => transform.TransformPoint(LocalP1);
		public Vector3 WorldP2 => transform.TransformPoint(LocalP2);
		public Vector3 Facing  => Quaternion.Euler(0, AngleForward, 0) * transform.forward;

		public override Plot Get(int index) => Get(index, PlotCount);

		public override Plot Get(int index, int max)
		{
			if (max == 1) return new Plot(Vector3.Lerp(WorldP1, WorldP2, 0.5f), Facing);

			float inc = 1 / (float) (max - 1);

			float t = index * inc;
			if (index == max - 1)
				t = 1;

			var p = Vector3.Lerp(WorldP1, WorldP2, t);
			if (GroundSnap)
			{
				p =  p.DropToGround();
				p += Vector3.up * GroundHover;
			}

			return new Plot(p, Facing);
		}

		protected override void OnRegisterDrawer() => DrawingManagerProxy.Register(this);
		private            void OnDestroy()        => DrawingManagerProxy.Deregsiter(this);

		public override void DrawGizmos()
		{
			if (GizmoLine)
			{
				Vector3 worldP1 = WorldP1;
				Vector3 worldP2 = WorldP2;

				if (GroundSnap)
				{
					worldP1 = worldP1.DropToGround();
					worldP2 = worldP2.DropToGround();
				}

				Draw.Line(worldP1, worldP2);
			}

			int plotCount = GizmoCount > -1
				? GizmoCount
				: PlotCount;

			if (PlotCount > 0)
			{
				for (var idx = 0; idx < plotCount; idx++)
				{
					Plot plot = Get(idx, plotCount);

					using (Draw.WithLineWidth(2.0f))
					{
						Draw.CircleXZ(plot.position, 0.25f, GizmoColor);
						Draw.ArrowheadArc(
							plot.position,
							plot.facing,
							0.5f,
							GizmoColor);
					}
				}
			}
		}



		[ShowInInspector]
		public void ReversePoints()
		{
			// ReSharper disable once InconsistentNaming
			Vector3 p1_ = LocalP2;

			LocalP2 = LocalP1;
			LocalP1 = p1_;
		}

		[Button]
		private void Test1()
		{
			Plot plot = Get(0, 1);

			ActorController.playerActor.Reorient(plot.facing);
			ActorController.playerCamera.ReorientForwardInstant();
		}

		[Button]
		private void Test2()
		{
			Plot plot = Get(0, 1);

			ActorController.playerActor.Teleport(plot.position);
			ActorController.playerActor.Reorient(plot.facing);
			ActorController.playerCamera.ReorientInstant(plot.facing);
		}

		[ShowInInspector]
		public static float EditorViewDistance = 50;

		public bool ShouldDrawGizmos()
		{
			Vector3 cpos = Camera.current.transform.position;

			if (Vector3.Distance(cpos, transform.position) > EditorViewDistance) return false;

			return true;
		}
	}
}