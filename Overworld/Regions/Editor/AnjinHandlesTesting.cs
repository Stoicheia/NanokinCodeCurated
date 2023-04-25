using Anjin.CustomHandles;
using Drawing;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Overworld.Regions.Editor {

	[InitializeOnLoad]
	public static class AnjinHandlesTesting_Initializer
	{
		static AnjinHandlesTesting_Initializer() {
			if (AnjinHandlesTesting.Live != null)
				EditorApplication.delayCall += AnjinHandlesTesting.Live.OnInit;
		}
	}

	public class AnjinHandlesTesting : OdinEditorWindow {

		#region Misc
		private static AnjinHandlesTesting _live;
		[ShowInInspector]
		public static AnjinHandlesTesting Live
		{
			get
			{
				if (_live != null) return _live;
				AnjinHandlesTesting[] editor = Resources.FindObjectsOfTypeAll<AnjinHandlesTesting>();

				if (editor == null || editor.Length <= 0) return _live;

				_live = editor[0];
				for (int i = 1; i < editor.Length; i++) DestroyImmediate(editor[i]);

				return _live;
			}

			private set
			{
				_live = value;
			}
		}

		[MenuItem("Anjin/Testing/Anjin Handles")]
		private static void ShowWindow()
		{
			var window = GetWindow<AnjinHandlesTesting>();
			window.titleContent = new GUIContent("Anjin Handles");
			window.Show();
			window.OnInit();
		}

		private void OnDestroy()
		{
			Live                     =  null;
			SceneView.duringSceneGui -= CustomSceneGUI;
			EditorApplication.update -= Update;
		}
		#endregion

		public Vector3 Position;

		public AnjinHandles handles;

		public void OnInit()
		{
			SceneView.duringSceneGui -= CustomSceneGUI;
			SceneView.duringSceneGui += CustomSceneGUI;

			EditorApplication.update -= Update;
			EditorApplication.update += Update;

			handles = new AnjinHandles();

			Live = this;

			Debug.Log("Init");
		}

		private void Update(){
			SceneView.RepaintAll();
			Repaint();
		}

		public void CustomSceneGUI(SceneView scene)
		{
			handles.BeginFrame();

			Draw.Circle(Position, Vector3.up, 0.5f);
			//Position = Handles.PositionHandle(Position, Quaternion.identity);

			//var hsize = HandleUtility.GetHandleSize(Position);

			bool DoArrow(Vector3 pos, Quaternion rot, Color col, Color hoverCol, string ID)
			{
				handles.PushID(ID);

				float   size      = HandleUtility.GetHandleSize(pos);
				float   hsize     = size * 0.14f;
				Vector3 spherePos = pos + rot * Vector3.forward * size;

				bool hover = handles.DoSphere(spherePos, hsize, "sphere", out var ev);

				if(Event.current.OnRepaint()) {
					//Draw.WireSphere(spherePos, hsize, hover ? Color.red : Color.yellow);

					using (Draw.WithLineWidth(2)) {
						Draw.Line(pos, spherePos, hover ? hoverCol : col);
					}

					Handles.color = hover ? hoverCol : col;
					Handles.ConeHandleCap(0, spherePos, rot, hsize * 1.25f, EventType.Repaint);
				}

				handles.PopID();

				return hover;
			}

			void DoTranslationAxisPlane(Vector3 pos, Vector3 offsetDir, Quaternion rot, string ID, Color col, Color hoverCol)
			{
				var size = 0.3f * HandleUtility.GetHandleSize(Position);

				Vector3 plane_pos = pos + offsetDir * (size / 2);

				bool hover = handles.DoPlane(plane_pos, rot.eulerAngles, Vector3.one, new Vector2(size / 2, size / 2), ID, out var ev);

				var alphaCol = col;
				alphaCol.a = 0.1f;

				if(Event.current.OnRepaint()) {
					Draw.SolidPlane(plane_pos, rot, size, hover ? hoverCol : alphaCol);
					Draw.WirePlane(plane_pos, rot, size, hover ? hoverCol : col);
				}
			}

			void DoTransformHandle(Vector3 pos, Quaternion rot, string ID)
			{
				handles.PushID(ID);
				DoArrow(pos, Quaternion.Euler(0,   90,  0), Handles.xAxisColor, Color.white, "xaxis");
				DoArrow(pos, Quaternion.Euler(-90, 0,   0), Handles.yAxisColor, Color.white, "yaxis");
				DoArrow(pos, Quaternion.Euler(0,   0, 0), Handles.zAxisColor, Color.white, "zaxis");

				DoTranslationAxisPlane(pos, new Vector3(1,0,1), Quaternion.identity, "xaxis_plane", Handles.yAxisColor, Color.cyan);

				handles.PopID();
			}

			DoTransformHandle(Position, Quaternion.identity, "test");

			Handles.Label(Position + Vector3.up * 2f, HandleUtility.GetHandleSize(Position).ToString());

			/*Handles.color = Color.green;
			Handles.ArrowHandleCap(0, Position, Quaternion.Euler(-90, 0, 0), hsize, EventType.Repaint);

			var spherePos = Position + Vector3.up * (1 * hsize);
			var sphereRad = hsize * 0.14f;


			bool hover = handles.DoSphere(spherePos, sphereRad, "sphere", out var ev);

			if(Event.current.OnRepaint()) {
				Draw.WireSphere(spherePos, sphereRad, hover ? Color.red : Color.yellow);
			}*/

			//Draw.WireCylinder(Position, Position + Vector3.up * 1f, 0.1f, Color.green);

			handles.EndFrame();
		}
	}
}