using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Anjin.CustomHandles;
using Anjin.Editor;
using Anjin.EventSystemNS;
using Anjin.UI;
using Anjin.Util;
using Drawing;
using Sirenix.Utilities.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;
using UnityUtilities;
using Utility.Anjin.Editor.Extensions;
using Object = UnityEngine.Object;
using g = UnityEngine.GUI;
using glo = UnityEngine.GUILayout;
using eg = UnityEditor.EditorGUI;
using eglo = UnityEditor.EditorGUILayout;

namespace Anjin.Nanokin.Map.Editor
{
	// TODO: Scene Visibility & Editability
	// TODO: When adding a point, use the handle types of the previous and next points for each handle of the new point.

	public class AnjinPathEditor : StaticSceneViewEditor<AnjinPathEditor>
	{
		public enum Mode {
			Path,
			Metdata,
		}

		public enum State
		{
			Idle,
			AddPoint,
			SplitSegment,
			DragMetadata,
		}

		const float segmentSelectDistanceThreshold = 10f;
		const float screenPolylineMaxAngleError    = .3f;
		const float screenPolylineMinVertexDst     = .01f;

		private Mode  mode;
		private State state;

		private List<IAnjinPathHolder> _pathHolders = new List<IAnjinPathHolder>();
		private AnjinHandles           _handles;

		private AnjinPathScreenspace           _pathScreenspace;
		private AnjinPathScreenspace.MouseInfo _mouseInfo;
		private bool                           _hasUpdatedScreenspace;
		private Matrix4x4                      _handlesMatrix;

		[SerializeField]
		List<SelectedPath> _selectedPaths = new List<SelectedPath>();

		private AnjinPath.Point _tempPoint;

		public bool Config_PreviewVertices = false;
		[FormerlySerializedAs("Config_PreviewVerticesLength")]
		public float Config_PreviewVerticesScale = 2;

		public Color c_curve_unselected  = ColorsXNA.RoyalBlue;
		public Color c_curve_selected    = ColorsXNA.CornflowerBlue;
		public Color c_curve_hovered     = ColorsXNA.Goldenrod;
		public Color c_curve_hovered_add = ColorsXNA.Goldenrod;

		public Color c_handle_line_unselected = ColorsXNA.DarkOrange.Darken(0.3f);
		public Color c_handle_line_selected   = ColorsXNA.DarkOrange;

		public Color c_freehandle_line_unselected = ColorsXNA.IndianRed.Darken(0.3f);
		public Color c_freehandle_line_selected   = ColorsXNA.IndianRed;

		public Color c_autohandle_line_unselected = ColorsXNA.LimeGreen.Darken(0.3f);
		public Color c_autohandle_line_selected   = ColorsXNA.LimeGreen;

		public Color c_metadata = ColorsXNA.Magenta;

		public LayerMask _raycastMask;

		public struct TempSelected
		{
			public string id;
			public int    point_index;
			public int    handle_index;
		}

		// Don't change to private
		public List<TempSelected> _tempSelected = new List<TempSelected>();

		public override void OnInitialize()
		{
			base.OnInitialize();
			TrackUnityObjects(_pathHolders);
			AddSelectionView(_selectedPaths);

			_handles = new AnjinHandles();

			_raycastMask = Layers.Walkable.mask;

			mode  = Mode.Path;
			state = State.Idle;

			EditorApplication.playModeStateChanged += LogPlayModeState;
		}

		private void LogPlayModeState(PlayModeStateChange obj)
		{
			if (obj == PlayModeStateChange.EnteredPlayMode)
			{
				TriggerRediscover(true);
			}
		}

		public override void OnSceneGUI( SceneView view, Event ev, bool isViewToolActive)
		{
			if (!Camera.current || !view.drawGizmos) return;


			bool mode_path     = mode == Mode.Path;
			bool mode_metadata = mode == Mode.Metdata;

			Profiler.BeginSample("AnjinPathEditor: OnSceneGUI");

			_handles.BeginFrame();

			//Debug.Log(viewToolActive);

			_handlesMatrix = Handles.matrix;

			bool anythingClickedOn = false;
			bool anythingHovered   = false;


			Rect view_rect           = new Rect(Vector2.zero, view.position.size);
			bool mouse_in_scene_view = view_rect.Contains(ev.mousePosition);

			IAnjinPathHolder selected_holder = null;

			(RaycastHit ray_hit, bool ray_did_hit) = RaycastToSceneCollision(view.camera);
			Vector3 camera_forward = view.camera.transform.forward;

			if (_tempSelected.Count > 0)
			{
				for (int j = 0; j < _tempSelected.Count; j++)
				{
					TempSelected sel = _tempSelected[j];

					for (int i = 0; i < _pathHolders.Count; i++)
					{
						if (_pathHolders[i] != null && _pathHolders[i] is IAnjinPathHolder holder && holder.Path.ID == sel.id)
						{
							SelectPath(_pathHolders[i], sel.point_index, sel.handle_index);
							_tempSelected.RemoveAt(j);
							j--;
						}
					}
				}
			}

			// Figure out our current mode;
			if (ev.OnLayout())
			{
				state = State.Idle;

				if (mode_path && mouse_in_scene_view && NumSelected > 0 && !isViewToolActive)
				{
					if (ev.control && !ev.shift)
					{
						if (state != State.AddPoint)
						{
							_tempPoint             = new AnjinPath.Point(Vector3.one * 10);
							_tempPoint.handle_mode_both = AnjinPath.HandleMode.Automatic;
							_tempPoint.auto_scale  = 0.3f;
						}

						state = State.AddPoint;
					}
					else if (ev.shift && !ev.control && !(ev.isMouse && ev.button == 1))
					{
						state = State.SplitSegment;
					}
				}

				if (state != State.AddPoint)
					_tempPoint = null;
			}

			_pathHolders.RemoveAll(x => x == null);

			int segment_hovered = -1;

			foreach (IAnjinPathHolder holder in _pathHolders)
			{
				AnjinPath path = holder?.Path;

				if (holder == null || path == null || !path.Valid())
					continue;

				path.HandleVersionUpgrade();

				_handles.PushID(path.ID);

				bool is_selected = IsPathSelected(holder, out SelectedPath? selection);

				if (is_selected)
					selected_holder = holder;

				Matrix4x4 obj_matrix = path.BaseMatrix;

				Vector3 TO_WORLD(Vector3 pos)
				{
					if (path.CalcSpace == AnjinPath.CalculationSpace.Absolute)
						return pos;

					return obj_matrix.MultiplyPoint3x4(pos);
				}

				AnjinPath.Point p1;
				AnjinPath.Point p2;

				if (is_selected && GUIUtility.hotControl == 0)
				{
					UpdatePathMouseInfo(holder);
				}

				segment_hovered = -1;
				if (is_selected && _mouseInfo.mouseDstToLine < segmentSelectDistanceThreshold)
				{
					segment_hovered = _mouseInfo.closestSegmentIndex;
				}

				// Draw handles
				//-----------------------------------------------------------------------
				for (int i = 0; i < path.Points.Count; i++)
				{
					bool at_end = i >= path.Points.Count - 1;

					bool point_selected      = selection != null && selection.Value.point_index == i;
					bool next_point_selected = selection != null && selection.Value.point_index == i + 1;

					_handles.PushID(i.ToString());

					int p2_ind = i + 1;

					p1 = path.Points[i];
					if (path.Closed && at_end)
					{
						p2     = path.Points[0];
						p2_ind = 0;
					}
					else if (!at_end)
						p2 = path.Points[i + 1];
					else
						p2 = null;

					Vector3 p1_pos = TO_WORLD(p1.position);
					Vector3 p1_rh  = TO_WORLD(p1.position + p1.right_handle);
					Vector3 p2_pos = Vector3.zero, p2_lh = Vector3.zero;

					if (p2 != null)
					{
						p2_pos = TO_WORLD(p2.position);
						p2_lh  = TO_WORLD(p2.position + p2.left_handle);
					}

					bool DoHandle(Vector3 position, int index, int point, out Vector3 newPosition)
					{
						newPosition = position;
						bool selected = is_selected && selection.Value.point_index == point && selection.Value.handle_index == index;

						float size = HandleSize(position) * 0.9f;
						if (index == 0)
							size *= 1.6f;

						bool hover = false;
						if (state == State.Idle && !isViewToolActive && _handles.DoSphere(position, size, index.ToString(), out var e)) {
							if(mode_path) {
								hover           = true;
								anythingHovered = true;
							}

							if (ev.OnMouseUp(0)) {
								SelectPath(holder, point, index);
								if(mode_path) anythingClickedOn = true;
							}
						}

						Color col        = Color.grey;
						float line_width = 0.5f;

						if (is_selected)
						{
							col        =  Color.white;
							line_width *= 1.5f;
						}

						if (hover) col = Color.yellow;

						if (mode_path && is_selected && selected && !isViewToolActive)
						{
							col         = Color.green;
							newPosition = Handles.PositionHandle(position, Quaternion.identity);
						}

						if (state != State.Idle)
						{
							col        =  col.Darken(0.6f).ScaleAlpha(0.5f);
							line_width *= 0.5f;
						}

						if (ev.OnRepaint())
						{
							using (Draw.WithLineWidth(line_width))
							{
								ControlDrawHandle(position, size, col);
							}
						}

						if (position != newPosition)
						{
							return true;
						}

						return false;
					}

					Vector3 newPos;

					// Point 1 Position
					if (DoHandle(p1_pos, 0, i, out newPos)) {
						//_hasUpdatedScreenspace = false;
						RecordUndo(holder, "Move Path Point");
						path.SetPointFromWorldPos(i, newPos);
					}


					// Point 1 Right Handle
					if (p1.handle_mode_right != AnjinPath.HandleMode.Automatic) {
						if (DoHandle(p1_rh, 1, i, out newPos)) {
							//_hasUpdatedScreenspace = false;
							RecordUndo(holder, "Move Right Handle");
							path.SetRightHandleFromWorldPos(i, newPos);
						}
					} else if (ev.OnRepaint()) {
						ControlDrawAutoHandle(p1_rh, HandleSize(p1_rh) * 0.7f, ColorsXNA.ForestGreen.Alpha(0.8f));
					}

					// Point 2 Left Handle
					if (p2 != null) {
						if (p2.handle_mode_left != AnjinPath.HandleMode.Automatic) {
							if (DoHandle(p2_lh, 2, i + 1, out newPos)) {
								//_hasUpdatedScreenspace = false;
								RecordUndo(holder, "Move Left Handle");
								path.SetLeftHandleFromWorldPos(p2_ind, newPos);
							}
						} else if (ev.OnRepaint()) {
							ControlDrawAutoHandle(p2_lh, HandleSize(p2_lh) * 0.7f, ColorsXNA.ForestGreen.Alpha(0.8f));
						}
					}



					// Handle Lines
					if (ev.OnRepaint())
					{
						using (Draw.WithLineWidth(0.8f))
						{
							Draw.Line(p1_pos, p1_rh, HandleLineColor(p1, point_selected, false));

							if (p2 != null)
								Draw.Line(p2_pos, p2_lh, HandleLineColor(p2, next_point_selected, true));
						}
					}

					_handles.PopID();
				}

				// Draw segment lines
				//-----------------------------------------------------------------------
				for (int i = 0; i < path.Points.Count; i++)
				{
					bool at_end = i >= path.Points.Count - 1;

					p1 = path.Points[i];
					if (path.Closed && at_end)
					{
						p2 = path.Points[0];
					}
					else if (!at_end)
						p2 = path.Points[i + 1];
					else
						p2 = null;

					Vector3 p1_pos = TO_WORLD(p1.position);
					Vector3 p1_rh  = TO_WORLD(p1.position + p1.right_handle);
					Vector3 p2_pos = Vector3.zero, p2_lh = Vector3.zero;

					if (p2 != null)
					{
						p2_pos = TO_WORLD(p2.position);
						p2_lh  = TO_WORLD(p2.position + p2.left_handle);
					}

					if (ev.OnRepaint() && p2 != null)
					{
						if (path.CalcSpace != AnjinPath.CalculationSpace.Relative) {
							Color col = SegmentColor(is_selected, segment_hovered == i && !anythingHovered && state == State.SplitSegment);
							DashedBezier(p1_pos, p1_rh, p2_lh, p2_pos, col, col.Brighten(0.3f));
						} else {
							using (Draw.WithColor(SegmentColor(is_selected, segment_hovered == i && !anythingHovered && state == State.SplitSegment))) {
								Draw.Bezier(p1_pos, p1_rh, p2_lh, p2_pos);
							}
						}

					}
				}

				// Draw metadata
				//-----------------------------------------------------------------------
				if (path.Metadata != null) {
					for (int i = 0; i < path.Metadata.Count; i++) {
						var data = path.Metadata[i];
						if (data == null) continue;

						path.GetSegmentPoints(data.Segment.Clamp(0, path.NumSegments), out p1, out p2);

						Vector3 position = Vector3.zero;
						float   t        = Mathf.Clamp01(data.SegmentT);
						;
						if (p1.handle_mode_right == AnjinPath.HandleMode.Linear && p2.handle_mode_left == AnjinPath.HandleMode.Linear) {
							 position = Vector3.Lerp(TO_WORLD(p1.position), TO_WORLD(p2.position), t);
						} else {
							position = GetPointOnbezier(p1, p2, t, path);
						}

						var size = Mathf.Clamp(HandleSize(position) * 2, 0.5f, 2);

						if(ev.OnRepaint()) {
							//Draw.Arrow(position + Vector3.up * 1.0f, position + Vector3.up * 0.15f, Vector3.up, 0.25f, c_metadata);
							Draw.SolidBox(position, size, c_metadata);
						}
					}
				}

				// Splitting segments
				if (mode_path && state == State.SplitSegment && segment_hovered != -1 && !anythingHovered)
				{
					GUIUtility.hotControl = 0;
					if (ev.OnRepaint()) {
						Draw.WireSphere(_mouseInfo.closestWorldPointToMouse, Mathf.Clamp(HandleSize(_mouseInfo.closestWorldPointToMouse) * 0.75f, 0.5f, 2f), Color.red);
					}

					if (ev.OnMouseDown(0)) {
						path.SplitSegment(_mouseInfo.closestSegmentIndex, holder.transform.InverseTransformPoint(_mouseInfo.closestWorldPointToMouse), _mouseInfo.timeOnBezierSegment);
					}

					if (ev.type == EventType.MouseDown && ev.button == 1 || ev.button == 2) {
						ev.Use();
					}
				}

				// Handle adding points to end
				//-----------------------------------------------------------------------
				if (mode_path && state == State.AddPoint && is_selected)
				{
					if (ray_did_hit)
					{
						_tempPoint.position = obj_matrix.inverse.MultiplyPoint3x4(ray_hit.point);

						if (path.Points.Count > 0) {
							AnjinPath.AutoSetHandles(_tempPoint, path.Points[path.Points.Count - 1], path.Closed ? path.Points[0] : null, true, true);
						}

						if (ev.OnMouseDown(0))
						{
							path.AddPointToEnd(_tempPoint);
							state                  = State.Idle;
							_tempPoint            = null;
							GUIUtility.hotControl = 0;
						}

						if (ev.OnRepaint())
						{
							if (path.Points.Count > 0)
							{
								var first_point = path.Points[0];
								var last_point  = path.Points[path.Points.Count - 1];

								using (Draw.WithMatrix(obj_matrix))
								{
									{
										Vector3 p1_pos = last_point.position;
										Vector3 p1_rh  = last_point.position + last_point.right_handle;
										Vector3 p2_pos = _tempPoint.position;
										Vector3 p2_lh  = _tempPoint.position + _tempPoint.left_handle;

										Draw.Bezier(p1_pos, p1_rh, p2_lh, p2_pos);
									}

									if (path.Closed)
									{
										Vector3 p1_pos = _tempPoint.position;
										Vector3 p1_rh  = _tempPoint.position + _tempPoint.right_handle;
										Vector3 p2_pos = first_point.position;
										Vector3 p2_lh  = first_point.position + first_point.left_handle;

										Draw.Bezier(p1_pos, p1_rh, p2_lh, p2_pos);
									}
								}
							}

							ControlDrawHandle(ray_hit.point, HandleSize(ray_hit.point), Color.green);
						}
					}
				}

				// Preview verts
				//-----------------------------------------------------------------------
				if (state == State.Idle && ev.OnRepaint() && (GUIUtility.hotControl == 0 || ev.IsRMB()) && Config_PreviewVertices && path.Vertices != null && is_selected)
				{
					Vector3 p;
					float   size;
					for (int i = 0; i < path.Vertices.Count; i++)
					{
						p    = obj_matrix.MultiplyPoint3x4(path.Vertices[i].position);
						size = Mathf.Clamp(HandleSize(p), 0.01f, 0.2f);

						Draw.SolidBox(p - (Vector3.one * size * 0.4f), obj_matrix.rotation, Vector3.one * size, ColorsXNA.OrangeRed);

						Draw.Arrow(p, p + (obj_matrix.rotation * path.Vertices[i].tangent) * size * Config_PreviewVerticesScale, Vector3.up, size * 0.7f, ColorsXNA.Goldenrod);
					}
				}

				_handles.PopID();

				// Deleting points
				//-----------------------------------------------------------------------
				if (is_selected && ev.OnKeyDown(KeyCode.Delete) /*&& _selectedPaths.Count > 0*/)
				{
					RecordUndo(holder, "Delete Path Point");
					path.DeletePointAtIndex(_selectedPaths[0].point_index);
					ClearSelection();
				}
			}

			_handles.EndFrame();


			Handles.BeginGUI();
			InitStyles();

			/*if(test_object != null) {
				glo.BeginArea(new Rect(128, 128, 1000, 500));
				for (int i = 0; i < _selectedPaths.Count; i++) {
					glo.Label($"{i}: ref: {_selectedPaths[i].holder.unity_obj}, {_selectedPaths[i].holder.Value}");
				}
				//g.Label(new Rect(200, 128, 300, 300), $"test_object: {test_object}" );
				glo.EndArea();
			}*/

			if (selected_holder != null)
				DrawPathGUI(selected_holder as Object, selected_holder.Path, view);
			Handles.EndGUI();

			if (!anythingClickedOn && !anythingHovered && !(state == State.SplitSegment && segment_hovered >= 0) && ev.OnMouseDown(0, false) && NumSelected > 0)
				ClearSelection();

			// TODO: Selective repaint is not working for some reason?
			if (ev.OnMouseMoveDrag(false) && _selectedPaths.Count > 0 /*&& _prevClosest != _handles.ClosestID*/)
			{
				//_prevClosest = _handles.ClosestID;
				SceneView.RepaintAll();
			}


			// FUNCS
			//---------------------------------------------------------------
			void ControlDrawHandle(Vector3 position, float size, Color col)
			{
				Draw.Circle(position, camera_forward, size, col);
				Draw.Circle(position, camera_forward, size * 0.6f, col);
			}

			void ControlDrawAutoHandle(Vector3 position, float size, Color col)
			{
				Draw.Circle(position, camera_forward, size, col);
			}

			Color HandleLineColor(AnjinPath.Point p, bool selected, bool is_left)
			{
				switch (is_left ? p.handle_mode_left : p.handle_mode_right)
				{
					case AnjinPath.HandleMode.Free:
						return selected ? c_freehandle_line_selected : c_freehandle_line_unselected;
					case AnjinPath.HandleMode.Automatic:
						return selected ? c_autohandle_line_selected : c_autohandle_line_unselected;
					default:
						return selected ? c_handle_line_selected : c_handle_line_unselected;
				}
			}

			Color SegmentColor(bool path_selected, bool hovered)
			{
				if (hovered) return c_curve_hovered;
				return path_selected ? c_curve_selected : c_curve_unselected;
			}

			Profiler.EndSample();
		}


		/*public static float GetHandleSize(Vector3 position)
		{
			Camera current = Camera.current;
			position = Handles.matrix.MultiplyPoint(position);
			if (!(bool) (UnityEngine.Object) current)
				return 20f;
			Transform transform = current.transform;
			Vector3 position1 = transform.position;
			float z = Vector3.Dot(position - position1, transform.TransformDirection(new Vector3(0.0f, 0.0f, 1f)));
			return 80f / Mathf.Max((current.WorldToScreenPoint(position1 + transform.TransformDirection(new Vector3(0.0f, 0.0f, z))) - current.WorldToScreenPoint(position1 + transform.TransformDirection(new Vector3(1f, 0.0f, z)))).magnitude, 0.0001f) * EditorGUIUtility.pixelsPerPoint;
		}*/


		float HandleSize(Vector3 position) /*=> HandleUtility.GetHandleSize(position) * 0.1f;*/
		{
			Camera current = Camera.current;
			position = _handlesMatrix.MultiplyPoint(position);

			Transform transform = current.transform;
			Vector3   pos       = transform.position;

			float z = Vector3.Dot(position - pos, transform.TransformDirection(new Vector3(0.0f, 0.0f, 1f)));

			return 80f / Mathf.Max((current.WorldToScreenPoint(pos + transform.TransformDirection(new Vector3(0.0f, 0.0f, z))) - current.WorldToScreenPoint(pos + transform.TransformDirection(new Vector3(1f, 0.0f, z)))).magnitude, 0.0001f) * EditorGUIUtility.pixelsPerPoint * 0.1f;
		}

		public (RaycastHit, bool) RaycastToSceneCollision(Camera camera)
		{
			if (camera.MosuePositionValid(Event.current.mousePosition))
			{
				Ray        ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
				RaycastHit hit;

				if ( /*!mouseWasOverUIWindows &&*/ Physics.Raycast(ray, out hit, 500, _raycastMask))
					return (hit, true);
			}

			return (new RaycastHit(), false);
		}

		void DashedBezier (float3 p0, float3 p1, float3 p2, float3 p3, Color c1, Color c2) {

			CommandBuilder builder = Draw.editor;

			// Dashed Line
			float3 prev = p0;
			bool   alt  = false;

			const float SEGMENTS = 20f;

			for (int i = 1; i <= SEGMENTS; i++) {
				float  t = i /SEGMENTS;
				float3 p = CommandBuilder.EvaluateCubicBezier(p0, p1, p2, p3, t);
				builder.Line(prev, p, alt ? c1 : c2);
				prev = p;
				alt  = !alt;
			}

		}

		Vector3 GetPointOnbezier(AnjinPath.Point p1, AnjinPath.Point p2, float t, AnjinPath path)
		{
			Vector3 TO_WORLD(Vector3 pos)
			{
				if (path.CalcSpace == AnjinPath.CalculationSpace.Absolute)
					return pos;

				return path.BaseMatrix.MultiplyPoint3x4(pos);
			}

			Vector3 p1_pos = TO_WORLD(p1.position);
			Vector3 p1_rh  = TO_WORLD(p1.position + p1.right_handle);
			Vector3 p2_pos = Vector3.zero, p2_lh = Vector3.zero;

			if (p2 != null)
			{
				p2_pos = TO_WORLD(p2.position);
				p2_lh  = TO_WORLD(p2.position + p2.left_handle);
			}

			return CommandBuilder.EvaluateCubicBezier(p1_pos, p1_rh, p2_lh, p2_pos, t);
		}

		// Selection
		//---------------------------------------------------------------
		// TODO: Multiple selection
		[Serializable]
		struct SelectedPath : ISelected
		{
			public UnitySelectionRef<IAnjinPathHolder> holder;

			public int point_index;
			public int handle_index;

			public bool Valid => holder.Value != null;

			public bool AlreadySelected
			{
				get
				{
					for (int i = 0; i < _activeSelections.Count; i++)
					{
						if (_activeSelections[i] is SelectedPath p && p.holder.Value == holder.Value && p.point_index == point_index && p.handle_index == handle_index)
							return true;
					}

					return false;
				}
			}
		}

		void SelectPath(IAnjinPathHolder holder, int point, int handle)
		{
			if (holder == null || point < 0 || handle < 0) return;

			SelectedPath path = new SelectedPath
			{
				holder       = new UnitySelectionRef<IAnjinPathHolder>(holder),
				point_index  = point,
				handle_index = handle
			};

			AddSelected(path);

			if (holder is Object unityObject)
			{
				Selection.activeObject = unityObject;
				//test_object            = unityObject;
			}
		}

		bool IsPathSelected(IAnjinPathHolder holder, out SelectedPath? selected)
		{
			selected = null;
			for (int i = 0; i < _selectedPaths.Count; i++)
			{
				SelectedPath sel = _selectedPaths[i];
				if (sel.Valid && sel.holder.Value == holder)
				{
					selected = sel;
					return true;
				}
			}

			return false;
		}


		// GUI
		//---------------------------------------------------------------

		private bool     styles;
		private GUIStyle titleStyle;
		private GUIStyle labelStyle;

		private void InitStyles()
		{
			if (styles) return;
			styles = true;

			titleStyle = EventStyles.GetTitleWithColor(Color.white);
			labelStyle = EventStyles.GetHeaderWithColor(Color.white);
		}

		public void DrawPathGUI(Object obj, AnjinPath path, SceneView view)
		{
			if (_selectedPaths.Count != 1) return;

			SelectedPath selectedPath = _selectedPaths[0];

			AnjinPath.Point point = path.Points[selectedPath.point_index];

			float w         = 340;
			Rect  sceneRect = view.position;
			Rect  tray      = new Rect(sceneRect.width - w, 0, w, sceneRect.height - 24);

			glo.BeginArea(tray);

			glo.FlexibleSpace();
			glo.BeginVertical(EventStyles.BlueBackground);
			{
				if (this.mode == Mode.Path) {
					glo.BeginHorizontal();
					glo.Label($"Path [{selectedPath.point_index + 1}]", titleStyle);
					glo.FlexibleSpace();
					_raycastMask = SirenixEditorFields.LayerMaskField(GUIContent.none, _raycastMask);
					glo.EndHorizontal();

					if (state != State.Idle)
						GUI.enabled = false;

					AnjinPath.HandleMode mode;

					// Left handle
					if (path.Closed || selectedPath.point_index > 0) {
						glo.BeginHorizontal();

						glo.Label("Left Handle: ", labelStyle);
						mode = point.handle_mode_left;
						AnjinGUILayout.EnumToggleButtons(ref mode);
						if (mode != point.handle_mode_left) {
							RecordUndo(selectedPath.holder.unity_obj, "Change Handle Mode");
							point.handle_mode_left = mode;
							path.InsureLinearHandles();
							path.UpdateAllAutoHandles();
						}

						glo.FlexibleSpace();
						glo.EndHorizontal();

						if (point.handle_mode_left == AnjinPath.HandleMode.Automatic) {
							float prev = point.auto_scale;
							point.auto_scale = eglo.Slider("Left Automatic Scale: ", point.auto_scale, 0.01f, 3f);
							if (Math.Abs(point.auto_scale - prev) > 0.001f) {
								path.UpdateAllAutoHandles();
							}
						}
					}

					// Right handle
					if (path.Closed || selectedPath.point_index < path.Points.Count - 1) {
						glo.BeginHorizontal();

						glo.Label("Right Handle: ", labelStyle);
						mode = point.handle_mode_right;
						AnjinGUILayout.EnumToggleButtons(ref mode);
						if (mode != point.handle_mode_right) {
							RecordUndo(selectedPath.holder.unity_obj, "Change Handle Mode");
							point.handle_mode_right = mode;
							path.InsureLinearHandles();
							path.UpdateAllAutoHandles();
						}

						glo.FlexibleSpace();
						glo.EndHorizontal();

						if (point.handle_mode_right == AnjinPath.HandleMode.Automatic) {
							float prev = point.auto_scale;
							point.auto_scale = eglo.Slider("Right Automatic Scale: ", point.auto_scale, 0.01f, 3f);
							if (Math.Abs(point.auto_scale - prev) > 0.001f) {
								path.UpdateAllAutoHandles();
							}
						}
					}
				}

				//eglo.Separator();
				glo.BeginHorizontal();
				glo.Label("Preview:", labelStyle);
				Config_PreviewVertices = glo.Toggle(Config_PreviewVertices, "Vertices", SirenixGUIStyles.MiniButton);
				if (Config_PreviewVertices)
				{
					Config_PreviewVerticesScale = eglo.FloatField("Scale:", Config_PreviewVerticesScale);
				}

				glo.FlexibleSpace();
				glo.EndHorizontal();

				glo.Space(5);

				GUI.enabled = true;

				AnjinGUILayout.EnumToggleButtons(ref this.mode);
			}
			glo.EndVertical();

			glo.EndArea();
		}

		void UpdatePathMouseInfo(IAnjinPathHolder holder)
		{
			if (!_hasUpdatedScreenspace || _pathScreenspace == null || _pathScreenspace.path != holder.Path || _pathScreenspace.TransformIsOutOfDate())
			{
				_pathScreenspace       = new AnjinPathScreenspace(holder.Path, holder.transform, screenPolylineMaxAngleError, screenPolylineMinVertexDst);
				_hasUpdatedScreenspace = true;
			}

			_mouseInfo = _pathScreenspace.CalculateMouseInfo();
		}

		void RecordUndo(Object obj, string message)
		{
			if (obj == null) return;
			Undo.RegisterCompleteObjectUndo(obj, message);

			if (obj is MonoBehaviour mono)
				EditorSceneManager.MarkSceneDirty(mono.gameObject.scene);
		}

		void RecordUndo<O>(O obj, string message)
		{
			if (obj is Object o)
				RecordUndo(o, message);
		}

		public override void OnRediscover()
		{
			foreach (Object obj in _pathHolders)
			{
				if (obj is IAnjinPathHolder holder && holder.Path != null)
				{
					holder.Path.OnModified += () =>
					{
						_hasUpdatedScreenspace = false;
					};
				}
			}
		}
	}
}