using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Audio;
using Anjin.Cameras;
using Drawing;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;
using Random = UnityEngine.Random;

namespace Anjin.Regions
{
	public class RegionMetadata {
		public RegionObject Parent;
	}

	public interface IRegionMetadataDrawsInEditor
	{
#if UNITY_EDITOR
		void SceneGUI(RegionObject obj, bool selected);
		bool UseObjMatrix { get; }
#endif
	}


	[RegionMetadataAttrib("Point Distribution")]
	public class PointDistributionMetadata : RegionMetadata
#if UNITY_EDITOR
										   , IRegionMetadataDrawsInEditor
#endif
	{
		public int seed   = 0;
		public int number = 10;
		/*[MinMaxSlider(0, 100)]
		public Vector2Int number;*/

		public int GetFor(RegionShape2D shape2D, Vector3[] points, int? num_points = null)
		{
			if (shape2D == null || points == null) return 0;

			/*if (num_points == null)
				num_points = UnityEngine.Random.Range(number.x, number.y);*/

			int num = 0;

			Random.InitState(seed);

			for (int i = 0; i < points.Length && i < number; i++) {
				points[i] = shape2D.GetRandomWorldPointInside();
				num++;
			}

			return num;
		}

#if UNITY_EDITOR

		public bool UseObjMatrix => false;

		static int       numPreviewPoints = 0;
		static Vector3[] previewPoints    = new Vector3[500];

		public void SceneGUI(RegionObject obj, bool selected)
		{
			if (!selected || !(obj is RegionShape2D shape)) return;

			numPreviewPoints = GetFor(shape, previewPoints);

			Handles.color = Color.HSVToRGB(0.6f, 1, 1);

			for (int i = 0; i < numPreviewPoints; i++)
				Handles.DrawWireCube(previewPoints[i], Vector3.one * 0.1f);

			//Handles.SphereHandleCap(0, Vector3.zero, Quaternion.identity, 2, EventType.Repaint);
		}

#endif
	}

	// TODO: Constraints (I.E. min/max circle radius for points to be generated!)
	[RegionMetadataAttrib("Walkable Surface Grid", false)]
	public class WalkableSurfaceGrid : RegionMetadata
		#if UNITY_EDITOR
							, IRegionMetadataDrawsInEditor
		#endif
	{
		public float GridSize    = 1f;
		public float CheckHeight = 2f;

		[MinValue(0)] public float CastHeight        = 5f;
		[MinValue(0)] public float MaxDepth          = 1f;
		[MinValue(0)] public float MaxHeight         = 0.75f;
		[MinValue(0)] public float SphereCheckRadius = 0.5f;

		[Title("Debug")]

		[ShowInInspector] public static bool DrawRaycasts = false;
		[ShowInInspector] public static bool DrawSpherechecks = true;
		[ShowInInspector] public static bool DrawWorldPoints = false;

		public List<Vector3> Points;
		public List<Vector3> WorldPoints;

		#if UNITY_EDITOR
		[Button]
		public void DoBake()
		{
			if (!(Parent is RegionObjectSpatial spatial)) return;

			Vector3 boxSize = new Vector3(GridSize, CheckHeight, GridSize);
			boxSize.Scale(spatial.Transform.Scale);

			Vector3 downDir = spatial.Transform.Rotation * Vector3.down;
			Vector3 upDir   = spatial.Transform.Rotation * Vector3.up;

			bool IsPositionValid(Vector2 areaPos, out Vector3 hitPos)
			{
				bool valid = false;

				Vector3 worldPos  = spatial.Transform.matrix.MultiplyPoint3x4(new Vector3(areaPos.x, 0, areaPos.y));
				Vector3 castPoint = worldPos + upDir * CastHeight;

				hitPos = Vector3.zero;

				Ray   ray  = new Ray(castPoint, downDir);
				float ray_dist = Mathf.Abs(CastHeight + MaxDepth);

				// If we hit something, we need
				if (Physics.Raycast(ray, out RaycastHit hit, ray_dist, Layers.Walkable.mask)) {

					float   radius    = SphereCheckRadius;
					Vector3 spherePos = hit.point + upDir * (radius + 0.1f);

					hitPos = hit.point;

					if (hit.distance >= CastHeight - MaxHeight) {

						if (!Physics.CheckSphere(spherePos, radius, Layers.Walkable.mask)) {
							valid = true;
						} else {
							if(DrawSpherechecks) {
								using (Draw.WithDuration(2))
									Draw.WireSphere(spherePos, radius, Color.red);
							}
						}
					}

					if(DrawRaycasts) {
						using (Draw.WithDuration(2))
							using (Draw.WithLineWidth(valid ? 1.5f : 1))
								Draw.Line(ray.origin, hit.point, valid ? ColorsXNA.GreenYellow : ColorsXNA.AliceBlue);
					}
				}

				return valid;

			}

			if (Points == null) {
				Points = new List<Vector3>();
			} else {
				Points.Clear();
			}

			if (WorldPoints == null) {
				WorldPoints = new List<Vector3>();
			} else {
				WorldPoints.Clear();
			}

			switch (spatial) {
				case RegionShape2D shape2D:
					switch (shape2D.Type) {
						case RegionShape2D.ShapeType.Empty:   break;
						case RegionShape2D.ShapeType.Rect: {

							Vector2Int cellNum = new Vector2Int(Mathf.FloorToInt(shape2D.RectSize.x / GridSize) * 2, Mathf.FloorToInt(shape2D.RectSize.y / GridSize) * 2);

							for (int y = 0; y < cellNum.y; y++) {
								for (int x = 0; x < cellNum.x; x++) {

									var areaPos = new Vector2(x * boxSize.x - shape2D.RectSize.x + (GridSize / 2), y * boxSize.z - shape2D.RectSize.y + (GridSize / 2));
									if (IsPositionValid(areaPos, out Vector3 hitPos)) {
										Points.Add(new Vector3(areaPos.x, 0, areaPos.y));
										WorldPoints.Add(hitPos);
									}
								}
							}

						} break;

						case RegionShape2D.ShapeType.Circle: {

							//Vector2Int cellNum = new Vector2Int(Mathf.FloorToInt(shape2D.RectSize.x / GridSize) * 2, Mathf.FloorToInt(shape2D.RectSize.y / GridSize) * 2);

							int size = (int)(shape2D.CircleRadius / GridSize);

							void plot(float xx, float yy)
							{
								var areaPos = new Vector2(xx, yy) * GridSize;
								if (IsPositionValid(areaPos, out Vector3 hitPos)) {
									Points.Add(new Vector3(areaPos.x, 0, areaPos.y));
									WorldPoints.Add(hitPos);
								}
							}

							bool inside_circle(float xx, float yy, float radius) {
								float distance_squared = xx * xx + yy * yy;
								return distance_squared <= radius * radius;
							}

							int top    = -size;
							int bottom = size;
							int left   = -size;
							int right  = size;

							for (float y = top; y <= bottom; y += (1 / shape2D.Transform.Scale.y)) {
								for (float x = left; x <= right; x+= (1 / shape2D.Transform.Scale.x)) {
									if (inside_circle(x, y, size)) {
										plot(x, y);
									}
								}
							}
						} break;


						case RegionShape2D.ShapeType.Polygon: {
							void plot(float xx, float yy)
							{
								var areaPos = new Vector2(xx, yy);
								if (IsPositionValid(areaPos, out Vector3 hitPos)) {
									Points.Add(new Vector3(areaPos.x, 0, areaPos.y));
									WorldPoints.Add(hitPos);
								}
							}

							Rect bounds = shape2D.GetPolyBounds();

							for (float y = bounds.yMin; y < bounds.yMax; y += (GridSize / shape2D.Transform.Scale.y)) {
								for (float x = bounds.xMin; x < bounds.xMax; x += (GridSize / shape2D.Transform.Scale.x)) {
									if(shape2D.PolygonContainsPoint(new Vector2(x, y)))
										plot(x, y);
								}
							}

						} break;
					}


					break;
			}
		}

		public bool UseObjMatrix => true;
		public void SceneGUI(RegionObject obj, bool selected)
		{
			if (Event.current.type != EventType.Repaint || !selected) return;
			RegionObjectSpatial spatial = obj as RegionObjectSpatial;
			if (spatial == null) return;

			using(Draw.WithMatrix(spatial.Transform.matrix)) {

				if(spatial is RegionShape2D shape) {
					switch (shape.Type) {
						case RegionShape2D.ShapeType.Rect:
							Draw.Label2D(Vector3.up   * MaxHeight,  "Max Height",  12f, LabelAlignment.Center, ColorsXNA.OrangeRed);
							Draw.Label2D(Vector3.down * MaxDepth,   "Max Depth",   12f, LabelAlignment.Center, ColorsXNA.BlueViolet);
							Draw.Label2D(Vector3.up   * CastHeight, "Cast Height", 12f, LabelAlignment.Center, ColorsXNA.YellowGreen);

							Draw.WireRectangleXZ(Vector3.up * MaxHeight, shape.RectSize * 2, ColorsXNA.OrangeRed);
							Draw.WireRectangleXZ(Vector3.down * MaxDepth, shape.RectSize * 2, ColorsXNA.BlueViolet);
							Draw.WireRectangleXZ(Vector3.up * CastHeight, shape.RectSize * 2, ColorsXNA.YellowGreen);
							break;

						case RegionShape2D.ShapeType.Circle:

							Vector3 normal = shape.Transform.Up;

							Draw.Label2D(Vector3.up   * MaxHeight,  "Max Height",  12f, LabelAlignment.Center, ColorsXNA.OrangeRed);
							Draw.Label2D(Vector3.down * MaxDepth,   "Max Depth",   12f, LabelAlignment.Center, ColorsXNA.BlueViolet);
							Draw.Label2D(Vector3.up   * CastHeight, "Cast Height", 12f, LabelAlignment.Center, ColorsXNA.YellowGreen);

							Draw.Circle(Vector3.up   * MaxHeight,  normal, shape.CircleRadius, ColorsXNA.OrangeRed);
							Draw.Circle(Vector3.down * MaxDepth,   normal, shape.CircleRadius, ColorsXNA.BlueViolet);
							Draw.Circle(Vector3.up   * CastHeight, normal, shape.CircleRadius, ColorsXNA.YellowGreen);
							break;

						case RegionShape2D.ShapeType.Polygon:
							Rect       bounds = shape.GetPolyBounds();

							Vector3 center = new Vector3(bounds.center.x, 0, bounds.center.y);

							Draw.WireRectangle(center + Vector3.up   * MaxHeight,  Quaternion.identity, bounds.size, ColorsXNA.OrangeRed);
							Draw.WireRectangle(center + Vector3.down * MaxDepth,   Quaternion.identity, bounds.size, ColorsXNA.BlueViolet);
							Draw.WireRectangle(center + Vector3.up   * CastHeight, Quaternion.identity, bounds.size, ColorsXNA.YellowGreen);


							break;
					}
				}
			}

			if (Points == null) return;

			if(!DrawWorldPoints) {
				using(Draw.WithMatrix(Matrix4x4.TRS(spatial.Transform.Position, spatial.Transform.Rotation, Vector3.one))) {
					for (int i = 0; i < Points.Count; i++) {
						using (Draw.WithLineWidth(1.5f)) {
							Vector3 pt = Points[i];
							pt.Scale(spatial.Transform.Scale);
							Draw.CrossXZ(pt, Mathf.Clamp(0.4f * HandleUtility.GetHandleSize(Points[i]), 0.15f, 0.3f), ColorsXNA.OrangeRed);
						}
					}
				}
			} else {
				for (int i = 0; i < WorldPoints.Count; i++) {
					using (Draw.WithLineWidth(1.5f)) {
						Vector3 pt = WorldPoints[i];
						pt.Scale(spatial.Transform.Scale);
						Draw.CrossXZ(pt, Mathf.Clamp(0.4f * HandleUtility.GetHandleSize(WorldPoints[i]), 0.15f, 0.3f), ColorsXNA.BlueViolet);
					}
				}
			}
		}
		#endif
	}

	[RegionMetadataAttrib("Audio Zone")]
	public class AudioZoneMetadata : RegionMetadata
	{
		[Inline]
		public AudioZone zone;
		public AudioZoneMetadata()
		{
			zone = new AudioZone();
		}
	}

	[RegionMetadataAttrib("Camera Zone")]
	public class GameCameraZoneMetadata : RegionMetadata
#if UNITY_EDITOR
										, IRegionMetadataDrawsInEditor
#endif
	{
		public int Priority;

		[Inline]
		public CamConfig Config;

		public GameCameraZoneMetadata()
		{
			Config = new CamConfig();
		}

#if UNITY_EDITOR
		public bool UseObjMatrix => false;

		public static Camera PreviewCam;

		public void SceneGUI(RegionObject obj, bool selected)
		{
			if (Config == null) return;

			var spawn = Config.SpawnParams;
			var confine = Config.ConfinementParams;

			if(selected) {
				if (PreviewCam == null) {
					var go = new GameObject("Preview Cam");
					go.hideFlags = HideFlags.HideAndDontSave;

					PreviewCam = go.AddComponent<Camera>();
				}

				var sobj = obj as RegionObjectSpatial;

				if(spawn != null) {
					Matrix4x4 mat = Matrix4x4.TRS(
						( ( spawn.Relative.HasFlag(RelativeMode.Position) ) ? sobj.Transform.Position : Vector3.zero ),
						( ( spawn.Relative.HasFlag(RelativeMode.Rotation) ) ? sobj.Transform.Rotation : Quaternion.identity ),
						Vector3.one );

					PreviewCam.renderingPath    = RenderingPath.Forward; // Important or visual errors will occur
					PreviewCam.fieldOfView      = Config.Lens.FieldOfView;
					PreviewCam.orthographicSize = Config.Lens.OrthographicSize;
					PreviewCam.nearClipPlane    = Config.Lens.NearClipPlane;
					PreviewCam.farClipPlane     = Config.Lens.FarClipPlane;

					PreviewCam.transform.position = mat.MultiplyPoint3x4(spawn.Position);
					PreviewCam.transform.rotation = ( Matrix4x4.Rotate(Quaternion.Euler(spawn.Rotation)) * mat ).rotation;

					PreviewCam.orthographic = Config.IsOrthographic;

					CameraEditorUtils.DrawFrustumGizmo(PreviewCam);

					var prev = Handles.matrix;
					Handles.matrix = mat;

					switch(Tools.current) {
						case Tool.Move:
							spawn.Position = Handles.PositionHandle(spawn.Position, Tools.pivotRotation == PivotRotation.Local ? Quaternion.Euler(spawn.Rotation) : Quaternion.identity);
							break;
						case Tool.Rotate:
							spawn.Rotation = Handles.RotationHandle(Quaternion.Euler(spawn.Rotation), spawn.Position).eulerAngles;
							break;
					}

					Handles.matrix = prev;
				}


				if (Config.ConfineToBox && confine != null) {
					/* Matrix4x4 confine_mat = Matrix4x4.TRS(
						((confine.Relative.HasFlag(RelativeMode.Position)) ? sobj.Transform.Position : Vector3.zero),
						((confine.Relative.HasFlag(RelativeMode.Rotation)) ? sobj.Transform.Rotation : Quaternion.identity), Vector3.one ) *
					                        Matrix4x4.Translate(confine.Position) *
					                        Matrix4x4.Rotate(Quaternion.Euler(confine.Rotation)); */

					Handles.color = Color.magenta;
					var prev = Handles.matrix;
					Handles.matrix = Config.GetConfinementMatrix(sobj);

					//RegionDrawingUtil.DrawBox();
					Handles.DrawWireCube(confine.Center, confine.Size);

					Handles.matrix = prev;
					Handles.color = Color.white;
				}

				PreviewCam.enabled = true;
				Handles.BeginGUI();

				GUILayout.BeginArea(new Rect(64, 64, 480 * 0.75f, 270 * 0.75f));
				Handles.SetCamera(PreviewCam);
				Handles.DrawCamera(new Rect(0, 0, 480 * 0.75f, 270 * 0.75f), PreviewCam, DrawCameraMode.Normal, false);
				GUILayout.EndArea();

				Handles.EndGUI();
				PreviewCam.enabled = false;
			}

		}
#endif
	}


	public class RegionMetadataAttrib : Attribute
	{
		public string 	PrettyName;
		public bool 	UsableGlobally;

		public RegionMetadataAttrib(string name, bool usableGlobally = true)
		{
			PrettyName     = name;
			UsableGlobally = usableGlobally;
		}

		static Type[] metadataTypes;
		public static Type[] GetMetadataTypes()
		{
			if (metadataTypes == null) {
				metadataTypes = typeof(RegionMetadata).GetChildren().ToArray();
			}

			return metadataTypes;
		}
	}
}