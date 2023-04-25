using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Anjin.Editor;
using Anjin.EventSystemNS;
using Anjin.CustomHandles;
using Anjin.Nanokin;
using Anjin.UI;
using Anjin.Util;
using Drawing;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Vexe.Runtime.Extensions;
using Util.Editor;
using Utility.Anjin.Editor.Extensions;
using g = UnityEngine.GUI;
using glo = UnityEngine.GUILayout;
using eg = UnityEditor.EditorGUI;
using eglo = UnityEditor.EditorGUILayout;
using esm = UnityEditor.SceneManagement.EditorSceneManager;
using TypeExtensions = Vexe.Runtime.Extensions.TypeExtensions;

namespace Anjin.Regions
{

	// TODO:
	//	Line width on Circle2D.
	//  Proper dragging of verts with scaled/rotated polygons/paths.
	//	Automatic link plane generation??
	//	Phase during link creation where you actually select the link position, instead of it automatically snapping towards the center.
	//	Select a polygon by clicking on any of its triangles.
	//	Draw polygons properly (requires triangle drawer from ALINE)
	//	Move polygon/path origin without moving points.
	// 	Ensure hover doesn't happen when over UI windows

	public class RegionGraphEditor : ScriptableObject
	{
		#region CLASSES
		[Serializable]
		public class RegionGraphEditorSettings
		{
			public bool ZTestNonCulled              = true;
			public bool ShowNodeAxis                = true;
			public bool AddObjectSpatial_NormalSnap = true;
		}

		#endregion

		#region MEMBERS
		public static RegionGraphEditor Live
		{
			get
			{
				if (_live != null) return _live;
				RegionGraphEditor[] editor = Resources.FindObjectsOfTypeAll<RegionGraphEditor>();

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
		private static RegionGraphEditor         _live;
		public         RegionGraphEditorSettings Settings;

		private List<Rect> UIWindowPanes;


		private static int ObjectIDHash = "RegionGraphEditorObject".GetHashCode();
		private static int AreaIDHash   = "RegionGraphEditorShapeArea".GetHashCode();

		public enum State
		{
			Closed   = 0,
			MainMenu = 1,
			EditingGraph,
			//AddNode,
			AddObject,
			AddLink,
			AddObjectSequence,
			EditPath,
			EditShape,
			DragLinkPoint
		}

		public enum EditVertsState
		{
			Main,
			DragVert,
			AddVerts,
		}

		private AnjinHandles handles;

		public State state;

		public bool  clearNextSelection = false;
		public float clearTimer         = -1;

		public bool initialized = false;

		public RegionGraphAsset       LiveEditAsset;
		public List<RegionGraphAsset> loadedGraphAssets;

		private int selectedAvailableAsset = -1;

		private bool _isViewToolActive = false;

		//Create new asset
		//--------------------------------------------------------------------------------
		private bool   newAssetFoldout = false;
		private string newAssetName    = "";

		public bool anyNodeCLickedOn;

		//Selection
		//--------------------------------------------------------------------------------
		public  List<RegionObject> selectedObjects;
		private List<RegionObject> objectsToSelect;

		//	OUTLINER
		//--------------------------------------------------------------------------------
		public GraphObjectTree OutlinerTree;
		public bool            RebuildOutlinerFlag = false;

		enum ObjectType
		{
			Shape2D,
			Shape3D,
			ParkAI_SpawnNode,
			ParkAI_ExitNode,
			ParkAI_GraphPortal,
			Path
		}

		//Adding objects
		//--------------------------------------------------------------------------------
		ObjectType   addObject_Type;
		RegionObject addObject_TempObj;


		//Adding sequences
		//--------------------------------------------------------------------------------
		RegionObjectSequence addSequence_TempSeq;

		//Adding links
		//--------------------------------------------------------------------------------
		RegionObjectSpatial LinkObjectFirst;

		//Dragging links
		//--------------------------------------------------------------------------------
		RegionSpatialLinkBase dragLink;
		int                   dragLinkPoint;

		Vector3[]     line_drawn_points;
		List<Vector3> line_drawn_points_list;

		// Editing Shapes
		//--------------------------------------------------------------------------------
		RegionShape2D      shapeEditing;
		EditVertsState     shapeEditState;
		(int ind, bool ok) shapeSelectedVert;
		(int ind, bool ok) shapeDraggingVert;

		//Editing Paths
		//--------------------------------------------------------------------------------
		RegionPath         pathEditing;
		EditVertsState     pathEditState;
		(int ind, bool ok) pathSelectedVert;
		(int ind, bool ok) pathDraggingVert;


		// Outliner
		//--------------------------------------------------------------------------------
		public bool    OutlinerOpen;
		public Vector2 OutlinerScrollPos;

		// Styles
		//--------------------------------------------------------------------------------
		GUIStyle WhiteHeader;
		GUIStyle SequenceNumberStyle;


		#endregion

		#region HELPERS
		private bool mouseWasOverUIWindows = false;
		bool MouseOverUIWindows()
		{
			foreach (Rect r in UIWindowPanes)
				if (r.Contains(Event.current.mousePosition))
					return true;
			return false;
		}

		#endregion

		#region INIT_DESTROY

		/// <summary>
		/// Initialize a new instance of the editor if one does not exist
		/// </summary>
		[MenuItem("Anjin/Windows/Region Graph Editor", priority = AnjinMenuItems.USEFUL_PRIORITY)]
		public static void InitGraphEditor()
		{
			if (Live == null)
			{
				Debug.Log($"[DEBUG] RegionGraphEditor: Create from Menu");
				Live                        =  CreateInstance<RegionGraphEditor>();
				Live.hideFlags              =  HideFlags.DontSave;
				EditorApplication.delayCall += Live.OnInit;
			}
			else
			{
				Live.Close(false);
			}
		}

		public void OnInit()
		{
			Live = this;

			if (!initialized)
			{
				state             = State.Closed;
				loadedGraphAssets = new List<RegionGraphAsset>();
				loadedGraphAssets = FindAvailableAssetsForScene(EditorSceneManager.GetActiveScene());

				selectedAvailableAsset = -1;
				initialized            = true;
			}

			//Debug.Log("RegionGraphEditor: OnInit");

			//TODO: Load settings
			Settings = new RegionGraphEditorSettings();

			SceneView.duringSceneGui -= CustomSceneGUI;
			EditorApplication.update -= Update;

			esm.activeSceneChangedInEditMode -= ActiveEditorSceneChanged;
			esm.sceneOpened                  -= SceneOpened;
			esm.sceneClosing                 -= SceneClosed;
			Selection.selectionChanged       -= SceneSelectionChanged;

			Undo.undoRedoPerformed -= UndoRedoPerformed;

			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

			handles = new AnjinHandles();

			SceneView.duringSceneGui += CustomSceneGUI;
			EditorApplication.update += Update;

			esm.activeSceneChangedInEditMode += ActiveEditorSceneChanged;
			esm.sceneOpened                  += SceneOpened;
			esm.sceneClosing                 += SceneClosed;
			Selection.selectionChanged       += SceneSelectionChanged;

			Undo.undoRedoPerformed += UndoRedoPerformed;

			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

			UIWindowPanes     = new List<Rect>();
			OutlinerScrollPos = Vector2.zero;
			activeScene       = esm.GetActiveScene();
			pathSelectedVert  = (0, false);


			InitMaterials();

			//Debug.Log("Active Scene: "+activeScene.name);

			if(selectedObjects == null)
				selectedObjects = new List<RegionObject>();
			objectsToSelect = new List<RegionObject>();

			//Debug.Log("Asset:" + LiveEditAsset);

			if (state == State.EditPath || state == State.EditShape) {
				state          = State.EditingGraph;
				pathEditState  = EditVertsState.Main;
				shapeEditState = EditVertsState.Main;
			}

			//TODO: Move handles test code to AnjinHandles
			/*for (int i = 0; i < MAX_SHAPES; i++) {

				shapes[i] = new HandleShape {
					type       	= shape_types[Random.Range(0, 5)],
					pos        	= new Vector3(Random.value * 100f,    Random.value * 100f,    Random.value * 100f),
					rot        	= new Vector3(Random.Range(0, 360),   Random.Range(0, 360),   Random.Range(0, 360)),
					scale 		= Vector3.one * Random.Range(0.8f, 1.8f),
					//scale 		= Vector3.one * 2,
					box_size   	= new Vector3(3f + Random.value * 5f, 3f + Random.value * 5f, 3f + Random.value * 5f),
					//box_size = Vector3.one,
					radius     	= Random.Range(3, 5),
					plane_size 	= new Vector2(3f + Random.value * 5f, 3f + Random.value * 5f),

					tri_1 = new Vector3(4f + Random.value * 8f, 4f + Random.value * 8f, 4f + Random.value * 8f),
					tri_2 = new Vector3(4f + Random.value * 8f, 4f + Random.value * 8f, 4f + Random.value * 8f),
					tri_3 = new Vector3(4f + Random.value * 8f, 4f + Random.value * 8f, 4f + Random.value * 8f)

					/*type  = ShapeType.Triangle,
					//pos   = new Vector3(0, 2, 0),
					scale = Vector3.one,

					tri_1 = new Vector3(1f, 0, 0),
					tri_2 = new Vector3(0, 0, 0),
					tri_3 = new Vector3(0, 0, 1f)#1#
				};
			}*/

		}

		//private static ShapeType[] shape_types = {ShapeType.Box, ShapeType.Disk, ShapeType.Plane, ShapeType.Sphere, ShapeType.Triangle};

		private void OnDestroy()
		{
			Close(true);
		}

		public void Close(bool isBeingDestroyed)
		{
			//Debug.Log($"RegionGraphEditor: Close, (is being destroyed: {isBeingDestroyed})");

			SceneView.duringSceneGui -= CustomSceneGUI;
			EditorApplication.update -= Update;

			esm.activeSceneChangedInEditMode -= ActiveEditorSceneChanged;
			esm.sceneOpened                  -= SceneOpened;
			esm.sceneClosing                 -= SceneClosed;
			Selection.selectionChanged       -= SceneSelectionChanged;

			Undo.undoRedoPerformed -= UndoRedoPerformed;

			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

			if(!isBeingDestroyed)
			{
				DestroyImmediate(Live);
				DestroyImmediate(this);
				Live = null;
			}
		}

		bool styles = false;

		public void InitStyles() {
			if (styles) return;
			styles              = true;
			WhiteHeader         = EventStyles.GetHeaderWithColor(new Color(1f, 1f, 1f));
			SequenceNumberStyle = EventStyles.GetTitleWithColor(ColorsXNA.GhostWhite);
		}

		#endregion

		#region RESOURCES


		public static Material GLDrawingMaterial;

		public void InitMaterials()
		{
			GLDrawingMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/Editor/BetterHandles.mat");
		}

		/// <summary>
		/// Get the folder that should house the scene assets (a folder with the same name as the scene)
		/// </summary>
		/// <param name="scene"></param>
		/// <returns></returns>
		public string GetSceneAssetFolderPath(Scene scene)
		{
			return ( Path.GetDirectoryName(scene.path) + "/" + scene.name ).Replace('\\', '/');
		}

		/// <summary>
		/// Find the graph asset that is tied to the specific scene, which should always be located in a folder
		/// in the scene path with the name SceneName_AIGraph.
		/// </summary>
		/// <param name="scene">The scene to find the asset for.</param>
		public List<RegionGraphAsset> FindAvailableAssetsForScene(Scene scene)
		{
			List<RegionGraphAsset> potentialAssets = new List<RegionGraphAsset>();

			if (string.IsNullOrEmpty(scene.path))
				return potentialAssets;

			var scenePath = Path.GetDirectoryName(scene.path);

			var sceneSubfolderPath = scenePath + "\\" + scene.name;

			string dataPath   = Application.dataPath;
			string folderPath = dataPath.Substring(0, dataPath.Length - 6) + sceneSubfolderPath;


			if(Directory.Exists(folderPath))
			{
				string[] filesInDir = Directory.GetFiles(folderPath);

				foreach (var file in filesInDir)
				{
					string assetPath = file.Substring(dataPath.Length - 6).Replace('\\', '/');

					RegionGraphAsset asset = AssetDatabase.LoadAssetAtPath<RegionGraphAsset>(assetPath);

					if (asset != null) potentialAssets.Add(asset);
				}
			}

			return potentialAssets;
		}

		public RegionGraphAsset CreateSceneAsset(Scene scene, string name)
		{
			string path = GetSceneAssetFolderPath(scene) + "/" + name + ".asset";

			var prevAsset = AssetDatabase.LoadAssetAtPath<RegionGraphAsset>(path);

			if (prevAsset != null)
			{
				return prevAsset;
			}

			RegionGraphAsset newAsset = ScriptableObject.CreateInstance<RegionGraphAsset>();

			AssetDatabase.CreateFolder(Path.GetDirectoryName(scene.path), scene.name);
			AssetDatabase.CreateAsset(newAsset, path);
			AssetDatabase.SaveAssets();

			Selection.activeObject = newAsset;

			newAsset.OnCreation();

			return newAsset;
		}

		public void DeloadAssets()
		{
			//Debug.Log("[DEBUG] Deload Assets");
			loadedGraphAssets.Clear();
			LiveEditAsset = null;
		}

		public void RefreshAvailableAssets(Scene scene)
		{
			selectedAvailableAsset = -1;
			loadedGraphAssets      = FindAvailableAssetsForScene(scene);
		}

		#endregion

		#region STATE

		public void BeginEditingGraph(RegionGraphAsset asset)
		{
			if (state == State.MainMenu)
			{
				state         = State.EditingGraph;
				LiveEditAsset = asset;
			}
		}

		public void EndEditingGraph()
		{
			if (state == State.EditingGraph)
			{
				state         = State.MainMenu;
				LiveEditAsset = null;
				OutlinerTree  = null;
			}
		}

		void BeginAddingObject(ObjectType type)
		{
			if (state == State.EditingGraph)
			{
				state          = State.AddObject;
				addObject_Type = type;
				switch(type)
				{
					case ObjectType.Shape2D:            addObject_TempObj = new RegionShape2D();   break;
					case ObjectType.Shape3D:            addObject_TempObj = new RegionShape3D();   break;
					case ObjectType.Path:               addObject_TempObj = new RegionPath();      break;
					//case ObjectType.ParkAI_SpawnNode:   addObject_TempObj = new ParkAISpawnNode(); break;
					//case ObjectType.ParkAI_ExitNode:    addObject_TempObj = new ParkAIExitNode();  break;
					case ObjectType.ParkAI_GraphPortal: addObject_TempObj = new ParkAIGraphPortal();  break;
				}
			}
		}

		void EndAddingObject()
		{
			if (state == State.AddObject)
			{
				state               = State.EditingGraph;
				addObject_Type      = ObjectType.Shape2D;
				addObject_TempObj   = null;
				clearNextSelection  = true;
				RebuildOutlinerFlag = true;
			}
		}

		void BeginAddingObjectSequence()
		{
			if (state == State.EditingGraph)
			{
				state = State.AddObjectSequence;
				ClearSelection();
				addSequence_TempSeq = new RegionObjectSequence();
			}
		}

		void EndAddingObjectSequence()
		{
			if (state == State.AddObjectSequence)
			{
				state                           = State.EditingGraph;
				addSequence_TempSeq.ParentGraph = LiveEditAsset.Graph;
				addSequence_TempSeq             = null;
				RebuildOutlinerFlag             = true;
			}
		}

		void BeginAddingLink()
		{
			if (state == State.EditingGraph)
			{
				state = State.AddLink;

				if(selectedObjects.Count > 0 &&
				   selectedObjects[0] is RegionObjectSpatial spatial)
					LinkObjectFirst = spatial;
				else
					LinkObjectFirst = null;
			}
		}

		void EndAddingLink()
		{
			if (state == State.AddLink)
			{
				state               = State.EditingGraph;
				LinkObjectFirst     = null;
				RebuildOutlinerFlag = true;
			}
		}

		void BeginEditingShape(RegionShape2D shape)
		{
			if (shape == null || shape.Type != RegionShape2D.ShapeType.Polygon || state != State.EditingGraph)
				return;

			state             = State.EditShape;
			shapeEditing      = shape;
			shapeEditState    = EditVertsState.Main;
			shapeDraggingVert = (0, false);
		}

		void EndEditingShape()
		{
			if (state != State.EditShape) return;
			state          = State.EditingGraph;
			shapeEditing   = null;
			shapeEditState = EditVertsState.Main;
		}

		void BeginEditingPath(RegionPath path)
		{
			if (path == null || state != State.EditingGraph) return;
			state            = State.EditPath;
			pathEditState    = EditVertsState.Main;
			pathEditing      = path;
			pathSelectedVert = (0, false);
		}

		void EndEditingPath()
		{
			if (state != State.EditPath) return;
			state            = State.EditingGraph;
			pathEditing      = null;
			pathSelectedVert = (0, false);
		}

		void DeleteObject(RegionObject obj)
		{
			RecordAssetUndo("Delete Object");

			selectedObjects.Remove(obj);
			obj.ParentGraph.RemoveObject(obj);

			RebuildOutlinerFlag = true;
		}

		void DeleteSelectedObjects()
		{
			RecordAssetUndo("Delete Selected Objects");

			for (int i = 0; i < selectedObjects.Count; i++)
			{
				selectedObjects[i].ParentGraph.RemoveObject(selectedObjects[i]);
			}

			selectedObjects.Clear();
			RebuildOutlinerFlag = true;
		}

		#endregion

		#region CALLBACKS

		void OnPlayModeStateChanged(PlayModeStateChange obj)
		{
			RegionMetadataWindow.StopInspecting();
		}

		public void ActiveEditorSceneChanged(Scene prev, Scene next)
		{
			SceneChange(next);
		}

		public void SceneOpened(Scene scene, OpenSceneMode mode)    { }
		public void SceneClosed(Scene scene, bool          removed) { }

		public Scene activeScene;

		public void SceneChange(Scene scene)
		{
			//Debug.Log("[DEBUG] Scene Change: " + activeScene.name + " -> " + scene.name);

			if (scene != activeScene)
			{
				DeloadAssets();
				RefreshAvailableAssets(scene);
				activeScene = scene;
			}
		}

		public void SceneSelectionChanged()
		{
			if (clearNextSelection)
			{
				ClearGameObjectSelection();
				clearTimer         = 20;
				clearNextSelection = false;
				return;
			}

			if (Selection.activeGameObject != null)
				SelectGameobject(Selection.activeGameObject);
		}

		void UndoRedoPerformed()
		{
			RebuildOutlinerFlag = true;

			if (LiveEditAsset == null) return;

			//attepmt to reselect after an undo
			for (int i = 0; i < selectedObjects.Count; i++) {
				if (selectedObjects[i] != null)
					selectedObjects[i] = LiveEditAsset.Graph.FindObject<RegionObject>(selectedObjects[i].ID);
			}
		}

		public void ClearGameObjectSelection()
		{
			Selection.objects = new UnityEngine.Object[0];
		}

		#endregion

		#region EDITOR

		#region MAIN

		public void Update()
		{
			if(state != State.Closed)
				RepaintSceneView();
		}

		private bool shouldClearSelection = false;
		private bool AnyHandleSelected    = true;

		private const int           MAX_SHAPES = 60;
		public static HandleShape[] shapes     = new HandleShape[MAX_SHAPES];

		public void CustomSceneGUI(SceneView view)
		{
			Profiler.BeginSample("Region Graph Editor Custom Scene GUI");

			// TODO:
			_isViewToolActive = (bool)typeof(Tools).GetProperty("viewToolActive", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

			int controlID = GUIUtility.GetControlID(FocusType.Passive);

			//Debug.Log("Repaint!");

			handles.BeginFrame();

			Handles.zTest     = (Settings.ZTestNonCulled) ? CompareFunction.Always : CompareFunction.LessEqual ;
			AnyHandleSelected = false;

			if (state != State.Closed && state != State.MainMenu) {
				if (LiveEditAsset == null)
					state = State.MainMenu;
			}

			if (state != State.Closed) {
				if (state == State.MainMenu) {
					for (int i = 0; i < loadedGraphAssets.Count; i++)
						DrawGraph(loadedGraphAssets[i].Graph, false);
				} else {
					//Can't edit the graph when adding a node
					bool canEditGraph = state != State.AddObject;

					if (LiveEditAsset != null) {
						DrawGraph(LiveEditAsset.Graph, canEditGraph);
					}
				}

				//Handle adding new nodes
				if (state == State.AddObject) {
					DrawAddingObject();
				}

				if (state == State.AddObjectSequence) {
					DrawAddingSequence();
				}
			}

			Handles.BeginGUI();
			InitStyles();
			DrawGUI();
			Handles.EndGUI();


			//Insure dragging states have an exit!
			//-----------------------------------------------------------------------------------------------
			var sceneViewRect = SceneView.currentDrawingSceneView.position;

			if (!Event.current.IsMouseContained(new Rect(Vector2.zero, sceneViewRect.size)) || Event.current.IsMouseUp()) {
				if (state == State.DragLinkPoint) {
					state         = State.EditingGraph;
					dragLink      = null;
					dragLinkPoint = 0;
				} else if (state == State.EditPath && pathEditState == EditVertsState.DragVert) {

					state            = State.EditPath;
					pathEditState    = EditVertsState.Main;
					pathDraggingVert = ( 0, false );
				} else if (state == State.EditShape && shapeEditState == EditVertsState.DragVert) {
					state             = State.EditShape;
					shapeEditState    = EditVertsState.Main;
					shapeDraggingVert = ( 0, false );

					shapeEditing.TriangulatePolygon();
				}
			}


			if (!anyNodeCLickedOn && Event.current.OnMouseDown(0, false) && !mouseWasOverUIWindows && !AnyHandleSelected)
				LeftClickOnNothing();


			if (Event.current.type == EventType.Layout) {
				if (mouseWasOverUIWindows)
					HandleUtility.AddDefaultControl(0);
			}

			if (Event.current.type == EventType.Repaint) {
				if (shouldClearSelection) {
					shouldClearSelection = false;
					ClearSelection();
				}

				if (objectsToSelect.Count > 0) {
					selectedObjects.AddRange(objectsToSelect);
					objectsToSelect.Clear();
				}

				mouseWasOverUIWindows = MouseOverUIWindows();

				if(anyNodeCLickedOn) {
					anyNodeCLickedOn      = false;
					GUIUtility.hotControl = 0;
				}
			}

			UIWindowPanes.Clear();

			if (clearTimer > 0) {
				ClearGameObjectSelection();
				clearTimer -= 1;
			}

			if (selectedObjects.Count > 0 && Event.current.OnKeyDown(KeyCode.Delete)) {
				DeleteSelectedObjects();
			}

			if (selectedObjects.Count > 0 && selectedObjects[0] is RegionObjectSpatial spatial) {
				if (Event.current.OnKeyDown(KeyCode.F) && Event.current.modifiers == EventModifiers.Shift) {

					if (spatial is RegionPath path && pathSelectedVert.ok) {
						view.LookAt(path.GetWorldPoint(pathSelectedVert.ind));
					} else {
						view.LookAt(spatial.GetFocusPos());
					}

				}
			}

			handles.EndFrame();

			//if(Event.current.IsMouseUp() || Event.current.IsMouseDown())
			if (!_isViewToolActive && anyNodeCLickedOn && GUIUtility.hotControl == 0)
				GUIUtility.hotControl = controlID;

			Profiler.EndSample();

			/*Handles.BeginGUI();
			GUI.Label(new Rect(20,20, 300, 200), $"GUIUtility.hotControl: {GUIUtility.hotControl}\n HandleUtility.nearestControl: {HandleUtility.nearestControl}", SirenixGUIStyles.BlackLabel);
			Handles.EndGUI();*/

			/*if (Event.current.type == EventType.Layout) {
				handles.StartCollecting();
				for (int i = 0; i < MAX_SHAPES; i++) {
					handles.AddShape(i, shapes[i]);
				}

				handles.StopCollecting();
			}

			if (Event.current.type == EventType.Repaint) {

				var      ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
				ShapeHit hit = handles.GetShapeForRay(ray);

				for (int i = 0; i < MAX_SHAPES; i++) {
					var shape = shapes[i];
					switch (shape.type) {
						case ShapeType.Sphere:
							//Draw.WireSphere(shape.pos, shape.radius, hit.ID == i ? Color.red : Color.blue);
							Handles.color = hit.ID == i ? Color.red : Color.cyan;
							Handles.SphereHandleCap(0, shape.pos, Quaternion.identity, shape.radius * 2, EventType.Repaint);
							break;

						case ShapeType.Box:

							using (Draw.WithMatrix(Matrix4x4.TRS(shape.pos, Quaternion.Euler(shape.rot), shape.scale))) {
								Draw.SolidBox( Vector3.zero, shape.box_size / 2, hit.ID == i ? Color.yellow : Color.blue);
								Draw.WireBox( Vector3.zero, shape.box_size / 2, hit.ID  == i ? Color.red : Color.cyan);
							}
							break;

						case ShapeType.Plane:
							using (Draw.WithMatrix(Matrix4x4.TRS(shape.pos, Quaternion.Euler(shape.rot), shape.scale))) {
								Draw.SolidPlane( Vector3.zero, Vector3.up, shape.plane_size * 2, hit.ID == i ? Color.yellow : Color.blue);
								Draw.WirePlane( Vector3.zero, Vector3.up, shape.plane_size  * 2, hit.ID == i ? Color.red : Color.cyan);
							}

							break;

						case ShapeType.Disk:
							using(Draw.WithMatrix(Matrix4x4.TRS(shape.pos, Quaternion.Euler(shape.rot), shape.scale))) {
								Draw.Circle(Vector3.zero, Vector3.up, shape.radius, hit.ID == i ? Color.yellow : Color.cyan);
							}
							break;

						case ShapeType.Triangle: {
							/*Handles.Label(shape.tri_1, "1");
							Handles.Label(shape.tri_2, "2");
							Handles.Label(shape.tri_3, "3");#1#
							using (Draw.WithMatrix(Matrix4x4.TRS(shape.pos, Quaternion.Euler(shape.rot), shape.scale))) {
								Draw.WireTriangle(shape.tri_1, shape.tri_2, shape.tri_3, hit.ID == i ? Color.yellow : Color.cyan );
							}
						} break;
					}
				}

			}*/
		}

		#endregion

		#region INPUT



		/// <summary>
		/// Returns if the handle with the set ID is hovered over, and also handles clicking on it.
		/// </summary>
		public bool HandleInput(int id, params object[] objs)
		{
			if (mouseWasOverUIWindows || HandleUtility.nearestControl != id) return false;

			if ((Event.current.IsMouseDown() || Event.current.IsMouseUp()) && Event.current.button == 0)
				anyNodeCLickedOn = true;

			if (Event.current.OnMouseDown(0))
				LeftClickOnGraphObject(objs);

			if (Event.current.OnMouseDown(1))
				RightClickOnGraphObject(objs);

			return true;
		}

		public bool HandleInputNew(params object[] objs)
		{
			if (mouseWasOverUIWindows || Event.current.OnRepaint()) return false;

			//Debug.Log($"HandleInputNew: {Event.current.type}");

			if ((Event.current.IsMouseDown() || Event.current.IsMouseUp()) && Event.current.button == 0)
				anyNodeCLickedOn = true;

			if (Event.current.OnMouseDown(0))
				LeftClickOnGraphObject(objs);

			if (Event.current.OnMouseDown(1, false))
				RightClickOnGraphObject(objs);

			return true;
		}

		//@fix
		//The link itself
		//static object[] selectionTypes_Link = new object[] {typeof(RegionNodeLink)};
		public void LeftClickOnGraphObject(params object[] objs)
		{
			if(objs.Length == 1 && objs[0] is RegionObject obj)
				LeftClickOnObject(obj);
			if(objs.Length == 2 && objs[0] is RegionSpatialLinkBase _link && objs[1] is int handle)
				LeftClickOnGraphLinkPosition(_link, handle);
			if(objs.Length == 1 && objs[0] is RegionSpatialLinkBase link)
				LeftClickOnGraphLink(link);
			if(objs.Length == 2 && objs[0] is RegionPath path && objs[1] is int path_vert)
				LeftClickOnPathVert(path, path_vert);
			if(objs.Length == 2 && objs[0] is RegionShape2D shape && objs[1] is int vert)
				LeftClickOnShape2DVert(shape, vert);
		}

		private void RightClickOnGraphObject(object[] objs)
		{
			if (objs.Length == 2 && objs[0] is RegionPath path && objs[1] is int path_vert) {
				RightClickOnPathVert(path, path_vert);
				Event.current.Use();
			}
		}

		public void LeftClickOnObject(RegionObject obj)
		{
			if(state == State.EditingGraph) {
				shouldClearSelection = true;
				SelectGraphObject(obj);
			}
			else if (state == State.AddLink) {
				if(obj is RegionObjectSpatial spatial)
				{
					if (LinkObjectFirst == null)
					{
						LinkObjectFirst = spatial;
					}
					else if(spatial != LinkObjectFirst)
					{
						Debug.Log("[DEBUG] Add link");
						obj.ParentGraph.LinkSpatialObjects(spatial as ILinkableBase, LinkObjectFirst as ILinkableBase);
						state = State.EditingGraph;
					}
				}
			} else if (state == State.AddObjectSequence) {
				if (Event.current.shift)
					addSequence_TempSeq.Objects.Remove(obj);
				else
					addSequence_TempSeq.Objects.Add(obj);
			}
		}

		public void LeftClickOnGraphLink(RegionSpatialLinkBase link)
		{
			if (state != State.EditingGraph) return;

			shouldClearSelection = true;
			SelectGraphObject(link);
		}

		public void LeftClickOnGraphLinkPosition(RegionSpatialLinkBase link, int point)
		{
			//We can only enter the link position dragging state if we're in the normal state
			if (state != State.EditingGraph) return;

			state         = State.DragLinkPoint;
			dragLink      = link;
			dragLinkPoint = point;
		}

		public void LeftClickOnPathVert(RegionPath path, int vert)
		{
			if (state == State.EditingGraph && !selectedObjects.Contains(path)) {
				shouldClearSelection = true;
				SelectGraphObject(path);
				return;
			}

			if (state != State.EditPath && pathEditState != EditVertsState.Main) return;
			if (path != pathEditing) return;

			if (Event.current.shift) {
				pathEditState    = EditVertsState.DragVert;
				pathDraggingVert = ( vert, true );
			}
			else {
				pathSelectedVert = ( vert, true );
			}
		}

		private void RightClickOnPathVert(RegionPath path, int vert)
		{
			if (state != State.EditPath && pathEditState != EditVertsState.Main) return;
			if (path != pathEditing) return;

			if (Event.current.shift) {
				RecordAssetUndo("Delete Vert");
				path.Points.RemoveAt(vert);
				selectedObjects[0] = path;
			}
		}

		public void LeftClickOnShape2DVert(RegionShape2D shape, int vert)
		{
			if (state != State.EditShape && shapeEditState != EditVertsState.Main) return;
			if (shapeEditing != shape) return;

			if (Event.current.shift) {
				shapeEditState    = EditVertsState.DragVert;
				shapeDraggingVert = ( vert, true );
			}
			else {
				shapeSelectedVert = ( vert, true );
			}
		}

		public void LeftClickOnNothing()             => FlagNonSelect();
		public void SelectGameobject(GameObject obj) => FlagNonSelect();

		public void FlagNonSelect()
		{
			if (state == State.EditingGraph)
				shouldClearSelection = true;
			else if (state == State.EditPath)
				pathSelectedVert = ( -1, false );
		}

		#endregion

		#region DRAWING_SCENE

		void DrawGraph(RegionGraph graph, bool editable)
		{
			if (graph == null) return;

			Profiler.BeginSample("DrawGraph");

			for (int i = 0; i < graph.GraphObjects.Count; i++)
			{
				if(graph.GraphObjects[i] is RegionObjectSpatial spatialObj)
				{
					var id = DrawSpatialGraphObject(spatialObj, editable);

					if (editable &&  Event.current.type != EventType.Used && HandleUtility.nearestControl == id)
					{
						AnyHandleSelected = true;
					}
				}
			}

			Handles.color = Color.white;

			//@fix

			RegionSpatialLinkBase link;
			for (int i = 0; i < graph.SpatialLinks.Count; i++)
			{
				link = graph.SpatialLinks[i];
				DrawLink(link, editable);
			}


			if (selectedObjects.Count > 0) {
				if(selectedObjects[0] is RegionObjectSequence seq)
					DrawSequenceObject(seq);
			}

			Profiler.EndSample();
		}

		//Returns handle
		int DrawSpatialGraphObject(RegionObjectSpatial obj, bool editable)
		{

			Profiler.BeginSample("DrawSpatialGraphObject");


			var shapeID = GUIUtility.GetControlID(ObjectIDHash, FocusType.Passive);
			DrawGraphObjectShape(shapeID, obj, editable);

			var objID = GUIUtility.GetControlID(AreaIDHash, FocusType.Passive);
			DrawGraphObjectCenterHandle(objID , obj, editable);

			//Draw metadata
			if(obj.Metadata != null) {
				for (int i = 0; i < obj.Metadata.Count; i++) {
					if (obj.Metadata[i] is IRegionMetadataDrawsInEditor canDraw)
					{

						Profiler.BeginSample("IRegionMetadataDrawsInEditor.SceneGUI");
						if(canDraw.UseObjMatrix) {
							z_Handles.PushMatrix();
							Handles.matrix = Matrix4x4.TRS(obj.Transform.Position, obj.Transform.Rotation, obj.Transform.Scale);
							canDraw.SceneGUI(obj, selectedObjects.Contains(obj));
							z_Handles.PopMatrix();
						}
						else {
							canDraw.SceneGUI(obj, selectedObjects.Contains(obj));
						}
						Profiler.EndSample();
					}
				}
			}

			Profiler.EndSample();


			return objID;
		}

		void DrawGraphObjectCenterHandle(int id, RegionObjectSpatial obj, bool editable)
		{
			Profiler.BeginSample("DrawGraphObjectCenterHandle");
			var nodeTransform = obj.Transform;

			z_Handles.PushMatrix();
			Handles.matrix = Matrix4x4.TRS(nodeTransform.Position, nodeTransform.Rotation, nodeTransform.Scale);

			var nodeDiscRadius = 0.2f;

			if (Event.current.OnRepaint())
			{
				Color ring_col = new Color(1f, 0.9f, 0f);

				for (int i = 0; i < 3; i++)
				{
					using(Draw.WithMatrix(Matrix4x4.TRS(nodeTransform.Position, nodeTransform.Rotation, nodeTransform.Scale))) {
						using(Draw.WithLineWidth(1.25f)) {
							Draw.Circle(Vector3.zero, Vector3.up, nodeDiscRadius * ( 1 - i / 3.0f ), ring_col);
						}
					}

					/*Handles.CircleHandleCap(id, Vector3.zero, Quaternion.Euler(new Vector3(90, 0, 0)),
						nodeDiscRadius * ( 1 - i / 3.0f ), EventType.Repaint);*/
				}

				if(Settings.ShowNodeAxis)
				{
					Handles.color = Color.red;
					Handles.DrawAAPolyLine(4, Vector3.zero, new Vector3(nodeDiscRadius * 1.75f, 0, 0));

					Handles.color = Color.blue;
					Handles.DrawAAPolyLine(4, Vector3.zero, new Vector3(0, 0, nodeDiscRadius * 1.75f));

					Handles.color = Color.green;
					Handles.DrawAAPolyLine(4, Vector3.zero, new Vector3(0, nodeDiscRadius * 1.75f, 0));
				}
			}

			z_Handles.PopMatrix();

			//You can only move or transform a node if it's in the normal state
			if(editable && !(obj is RegionPath && state == State.EditPath) && !(obj is RegionShape2D && state == State.EditShape))
			{
				EditorGUI.BeginChangeCheck();

				if (selectedObjects.Contains(obj))
				{
					switch(Tools.current)
					{
						case Tool.Move:
							nodeTransform.Position = Handles.PositionHandle(nodeTransform.Position, Quaternion.identity);
							break;
						case Tool.Rotate:
							nodeTransform.Rotation = Handles.RotationHandle(nodeTransform.Rotation, nodeTransform.Position);
							break;
						case Tool.Scale:
							nodeTransform.Scale = Handles.ScaleHandle(nodeTransform.Scale, nodeTransform.Position, nodeTransform.Rotation, HandleUtility.GetHandleSize(nodeTransform.Position));
							break;
						/*case Tool.Rect:
							break;
						case Tool.Transform:
							break;*/
					}
				}

				if (EditorGUI.EndChangeCheck())
				{
					RecordAssetUndo("Object Changed");
				}
			}

			Profiler.EndSample();
		}

		void DrawGraphObjectShape(int id, RegionObjectSpatial obj, bool editable)
		{
			Profiler.BeginSample("DrawSpatialGraphObject");
			switch(obj) {
				case RegionShape2D shape: DrawShape2D(id, shape, editable); break;
				case RegionShape3D shape: DrawShape3D(id, shape, editable); break;
				case RegionPath	    path: DrawPath(id, path, editable);     break;
			}

			Profiler.EndSample();
		}

		private static Color SelectedShapeOutlineColor = new Color(0.91f, 0.64f, 0.63f);
		private static Color RectFillColor             = new Color(0.84f, 0.04f, 0.04f, 0.2f);
		private static Color CircleFillColor           = new Color(0.07f, 0.23f, 0.76f, 0.2f);
		private static Color PolyFillColor             = new Color(0.17f, 0.76f, 0.24f, 0.2f);
		private static Color LinkFillColor             = new Color(0.7f, 0.1f, 0.7f, 0.2f);

		void DrawShape2D(int id, RegionShape2D shape, bool editable)
		{
			Profiler.BeginSample("DrawShape2D");
			DrawSpatial_Begin(shape);

			RegionShape2D.ShapeType shapeType    = shape.Type;
			EventType               controlEvent = Event.current.GetTypeForControl(id);
			bool                    hover        = false;

			bool selected = selectedObjects.Contains(shape);

			switch(shapeType)
			{
				case RegionShape2D.ShapeType.Empty:
				{
					float nodeDiscRadius = 0.3f;

					if (editable && controlEvent == EventType.Layout)
						Handles.SphereHandleCap(id, Vector3.zero, Quaternion.Euler(new Vector3(90, 0, 0)), nodeDiscRadius - 0.05f, EventType.Layout);

					hover = editable && HandleInput(id, shape);

					if (controlEvent == EventType.Repaint)
					{
						z_Handles.PushHandleColor();
						Handles.color = ( hover &&  !selectedObjects.Contains(shape)) ? new Color(0.45f, 0.98f, 0f, 0.49f) : new Color(0.02f, 0.65f, 0.01f, 0.4f);
						Handles.SphereHandleCap(id, Vector3.zero, Quaternion.identity, nodeDiscRadius * 2, EventType.Repaint);
						z_Handles.PopHandleColor();
					}
				} break;

				case RegionShape2D.ShapeType.Rect: {


					// ID stack system
					bool is_hover = false;

					if (handles.DoPlane(shape.Transform, shape.RectSize, shape.ID, out var ev)) {
						is_hover = true;
						HandleInputNew(shape);
					}

					Color fillColor    = !selected && is_hover  ? new Color(0.91f, 0.55f, 0f, 0.19f) : RectFillColor;
					Color outlineColor = selected 			 ? SelectedShapeOutlineColor : Color.black;

					if(Event.current.OnRepaint()) {
						using (Draw.WithMatrix(shape.Transform.matrix)) {
							Draw.SolidPlane(Vector3.zero, Quaternion.identity, shape.RectSize * 2, fillColor);
							using(Draw.WithLineWidth(selected ? 1.25f : 1)) {
								Draw.WirePlane(Vector3.zero, Quaternion.identity, shape.RectSize * 2, outlineColor);
							}
						}
					}
				} break;

				case RegionShape2D.ShapeType.Circle: {

					bool is_hover = false;

					if (handles.DoDisk(shape.Transform, shape.CircleRadius, shape.ID, out var ev)) {
						is_hover = true;
						HandleInputNew(shape);
					}

					Color fillColor    = ( is_hover &&  !selectedObjects.Contains(shape) ) ? new Color(0.07f, 0.75f, 0.95f, 0.26f) : CircleFillColor;
					Color outlineColor = selected 			 ? SelectedShapeOutlineColor : Color.black;

					RegionDrawingUtil.DrawCirlce(Vector3.zero, shape.CircleRadius, Quaternion.identity, shape.Transform.matrix, fillColor, outlineColor);

					/*if (editable && controlEvent == EventType.Layout)
						Handles.CircleHandleCap(id, Vector3.zero, Quaternion.Euler(90, 0, 0), shape.CircleRadius - 0.1f, EventType.Layout);

					hover = editable && HandleInput(id, shape);

					if (controlEvent == EventType.Repaint)
					{
						Color fill = ( hover &&  !selectedObjects.Contains(shape) ) ? new Color(0.07f, 0.75f, 0.95f, 0.26f) : CircleFillColor;

						RegionDrawingUtil.DrawCirlce(Vector3.zero, shape.CircleRadius, Quaternion.identity, shape.Transform.matrix, fill, selectedObjects.Contains(shape) ? SelectedShapeOutlineColor : Color.black);

						/*Handles.DrawSolidDisc(Vector3.zero, Vector3.up, shape.CircleRadius);
						Handles.color = selectedObjects.Contains(shape) ? SelectedShapeOutlineColor : Color.black;
						Handles.CircleHandleCap(0, Vector3.zero, Quaternion.Euler(new Vector3(90, 0, 0)), shape.CircleRadius, EventType.Repaint);#1#
					}*/
				} break;

				case RegionShape2D.ShapeType.Polygon: {

					DrawSphereHandle(id, shape, controlEvent, true);

					if (state != State.EditShape || shapeEditing != shape)
						editable = false;

					Plane   areaPlane     = new Plane(shape.Transform.Rotation * Vector3.up, shape.Transform.Position);
					Ray     ray           = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
					bool    ray_ok        = areaPlane.Raycast(ray, out float enter) && !mouseWasOverUIWindows;
					Vector3 ray_point_raw = ray.GetPoint(enter);
					Vector2 ray_point     = ray_point_raw.xz() - shape.Transform.Position.xz();
					//Vector3 ray_point = new Vector3(ray_point_obj.x, 0, ray_point_obj.y);

					Color outlineColor 	= selected ? ColorsXNA.Violet : ColorsXNA.LightSkyBlue;
					Color lineColor 	= selected ? ColorsXNA.Orange : ColorsXNA.LimeGreen;

					var points = shape.PolygonPoints;

					var tris = shape.PolygonTriangulation;

					//handles.PushID(shape.ID);
					//var handlesID = handles.GetCurrentID();
					if(shape.TrianulationValid() && shapeEditState == EditVertsState.Main) {
						for (int i = 0; i < tris.Length; i += 3) {
							Vector2 p1 = shape.PolygonPoints[tris[i]];
							Vector2 p2 = shape.PolygonPoints[tris[i + 1]];
							Vector2 p3 = shape.PolygonPoints[tris[i + 2]];

							Vector3 wp1 = new Vector3(p1.x, 0, p1.y);
							Vector3 wp2 = new Vector3(p2.x, 0, p2.y);
							Vector3 wp3 = new Vector3(p3.x, 0, p3.y);

							if (handles.DoTriangle(shape.Transform, wp1, wp2, wp3, shape.ID, out var ev)) {
								//if (handles.DoShapeWithFullID(new HandleShape {type = ShapeType.Triangle,pos = shape.Transform.Position, rot = shape.Transform.Rotation.eulerAngles, scale = shape.Transform.Scale, tri_1 = p1, tri_2 = p2, tri_3 = p3}, handlesID, out var ev)) {
								HandleInputNew(shape);
								hover = true;
							}
						}

					}

					//handles.PopID();

					if (hover && !selected) {
						outlineColor = ColorsXNA.Aqua;
						lineColor    = ColorsXNA.MediumSpringGreen;
					}

					if (shape.TrianulationValid() && shapeEditState == EditVertsState.Main && Event.current.OnRepaint()) {
						for (int i = 0; i < tris.Length; i += 3) {
							Vector2 p1 = shape.PolygonPoints[tris[i]];
							Vector2 p2 = shape.PolygonPoints[tris[i + 1]];
							Vector2 p3 = shape.PolygonPoints[tris[i + 2]];

							using (Draw.WithMatrix(shape.Transform.matrix)) {
								Draw.WireTriangle(new Vector3(p1.x, 0, p1.y), new Vector3(p2.x, 0, p2.y), new Vector3(p3.x, 0, p3.y), lineColor);
							}
						}
					}

					if (points.Count > 0) {

						if (line_drawn_points == null || line_drawn_points.Length == 0)
							line_drawn_points = new Vector3[2048];

						int num = 0;

						for (int i = 0; i < points.Count; i++) {
							var p = points[i];

							line_drawn_points[i] = new Vector3(p.x, 0, p.y);
							num++;
						}

						if (points.Count > 1) {
							if (ray_ok && editable && shapeEditState == EditVertsState.AddVerts)
								line_drawn_points[num++] = new Vector3(ray_point.x, 0, ray_point.y);

							line_drawn_points[num++] = new Vector3(points[0].x, 0, points[0].y);
						}

						if(Event.current.OnRepaint()) {
							using (Draw.WithMatrix(shape.Transform.matrix)) {
								using (Draw.WithLineWidth(1.15f)) {
									for (int i = 1; i < num; i++) {

										Draw.Line(line_drawn_points[i - 1], line_drawn_points[i], outlineColor);
									}
								}
							}
						}

						/*Handles.color = Color.HSVToRGB(0.5f, 0.8f, 0.8f);
						Handles.DrawAAPolyLine(4, num, line_drawn_points);*/

						handles.PushID("vert");
						for (int i = 0; i < points.Count; i++) {
							var p          = new Vector3(points[i].x, 0, points[i].y);
							var vert_id    = GUIUtility.GetControlID(( shape.ID + "_Vert_" + i ).GetHashCode(), FocusType.Passive);
							var vert_event = Event.current.GetTypeForControl(vert_id);

							var size      = 0.08f;
							var mouseSize =  size;
							if (editable) {
								size      = Mathf.Clamp(0.1f * HandleUtility.GetHandleSize(p), 0.1f, 0.75f);
								mouseSize = size * Mathf.Clamp(100 / (Vector2.Distance(Event.current.mousePosition, HandleUtility.WorldToGUIPoint(p)) * 3), 1, 1.5f);
							}

							bool is_hover = false;

							Handles.color = Color.HSVToRGB(0.0f, 0.6f, 0.9f);
							if (editable) {

								AnjinHandlesEvent ev;

								var e_p=  shape.Transform.matrix.MultiplyPoint3x4(p);

								if (handles.DoSphere(e_p, size, i.ToString(), out ev) ||
									Event.current.shift && handles.DoDisk(p, Vector3.zero, Vector3.one, mouseSize, i.ToString(), out ev)) {

									HandleInputNew(shape, i);
									is_hover = true;
								}

							}

							//hover = editable && HandleInput(vert_id, shape, i);

							if(editable)
							{
								if (is_hover)
									Handles.color = Color.HSVToRGB(0.4f, 0.6f, 0.9f);

								if (shapeSelectedVert.ok && shapeSelectedVert.ind == i)
									Handles.color = Color.HSVToRGB(0.15f, 0.9f, 0.9f);

								if (Event.current.shift)
									Handles.DrawSolidDisc(p, Vector3.up, mouseSize);
								else
									Handles.SphereHandleCap(vert_id, p, Quaternion.identity, size * 1.25f, EventType.Repaint);
							}
						}

						handles.PopID();

						//Drag verts
						if (editable && shapeEditState == EditVertsState.DragVert && Event.current.type == EventType.MouseDrag) {
							if (shapeDraggingVert.ok) {
								if (ray_ok) {
									RecordAssetUndo("Move Shape Vert");
									shape.PolygonPoints[shapeDraggingVert.ind] = ray_point;
								}
							}
						}
					}

					//DrawSpatial_End();

					if (editable && shapeEditState == EditVertsState.AddVerts) {

						if (ray_ok) {
							var pt = shape.Transform.matrix.inverse.MultiplyPoint3x4(ray_point_raw);

							if (points.Count > 0) {
								var last = points.Last();
								Handles.DrawAAPolyLine(2, new Vector3(last.x, 0, last.y), pt);
							}
							else {
								Handles.SphereHandleCap(-1, pt, Quaternion.identity, Mathf.Clamp(0.1f * HandleUtility.GetHandleSize(pt), 0.1f, 0.75f), EventType.Repaint);
							}

							if (Event.current.OnMouseDown(0)) {
								shape.PolygonPoints.Add(ray_point);
								shapeEditing.TriangulatePolygon();
							}
						}

					}

					if(editable && ray_ok && Event.current.control) {
						//var correctedPos = ray_point - path.Transform.Position;
						var (ind, dist, pos) = shape.GetClosestEdge(ray_point);

						Handles.color = Color.green;
						//Handles.CubeHandleCap(0, ray.point, Quaternion.identity, 0.1f, EventType.Repaint);

						if ((ind < shape.PolygonPoints.Count - 1) && dist < 0.5f) {
							if(shape.GetPolyEdge(ind, out Vector2 p1, out Vector2 p2)) {
								var point = Vector2.Lerp(p1, p2, pos);

								Handles.color = Color.yellow;
								using (Draw.WithMatrix(shape.Transform.matrix)) {
									Draw.Circle(new float3(point.x, 0, point.y), Vector3.up, 0.25f, Color.red);
								}

								//Handles.Label(shape.Transform.matrix.MultiplyPoint3x4(point), $"(ind: {ind}, dist: {dist}, pos: {pos})");

								//Handles.SphereHandleCap(0, point, Quaternion.identity, 0.15f, EventType.Repaint);

								if (Event.current.IsLMBDown()) {
									Debug.Log($"[DEBUG] Insert {ind}, {point}");
									shape.InsertPolygonPoint(ind + 1, point);
								}
							}
						}
					}

					/*Vector2 point1, point2;
					Vector3 p1, p2;
					for (int i = 0; i < shape.PolygonPoints.Count; i++) {
						point1 = shape.PolygonPoints[i];
						point2 = shape.PolygonPoints[(i + 1) % shape.PolygonPoints.Count];
						p1 = new Vector3(point1.x, 0, point1.y);
						p2 = new Vector3(point2.x, 0, point2.y);
						Handles.DrawLine(p1, p2);
					}
*/


				} break;
			}

			DrawSpatial_End();
			Profiler.EndSample();
		}

		void DrawShape3D(int id, RegionShape3D shape, bool editable)
		{
			Profiler.BeginSample("DrawShape3D");
			DrawSpatial_Begin(shape);

			RegionShape3D.ShapeType shapeType    = shape.Type;
			EventType               controlEvent = Event.current.GetTypeForControl(id);
			bool                    hover;

			bool selected = selectedObjects.Contains(shape);

			switch (shapeType)
			{
				case RegionShape3D.ShapeType.Empty:
				{
					DrawSphereHandle(id, shape, controlEvent, editable);
				} break;

				case RegionShape3D.ShapeType.Box:
				{
					if (editable && !_isViewToolActive && controlEvent == EventType.Layout)
					{
						RectCapVector2(id, Vector3.up * shape.BoxSize.y,   Quaternion.Euler(90, 0, 0),  shape.BoxSize.xz(), EventType.Layout); //Top
						RectCapVector2(id, Vector3.down * shape.BoxSize.y, Quaternion.Euler(-90, 0, 0), shape.BoxSize.xz(), EventType.Layout); //Bottom

						RectCapVector2(id, Vector3.right * shape.BoxSize.x, Quaternion.Euler(0, 0, 0), shape.BoxSize.yz(), EventType.Layout); //+x
						RectCapVector2(id, Vector3.left * shape.BoxSize.x, Quaternion.Euler(0, 0, 0), shape.BoxSize.yz(), EventType.Layout);  //-x

						RectCapVector2(id, Vector3.forward * shape.BoxSize.z, Quaternion.Euler(0, 90, 0), shape.BoxSize.xy(), EventType.Layout); //+z
						RectCapVector2(id, Vector3.back    * shape.BoxSize.z, Quaternion.Euler(0, 90, 0), shape.BoxSize.xy(), EventType.Layout); //-z
					}

					hover = editable && !_isViewToolActive && HandleInput(id, shape);

					if (controlEvent == EventType.Repaint)
					{
						Color fill = ( hover && !selectedObjects.Contains(shape)) ? Color.HSVToRGB(0.1f, 0.8f, 0.8f) : Color.HSVToRGB(0.05f, 0.6f, 0.7f);
						fill.a = 0.3f;

						RegionDrawingUtil.DrawBox(-shape.BoxSize, shape.BoxSize, Quaternion.identity, shape.Transform.matrix, fill, selected ? SelectedShapeOutlineColor : Color.black );
					}

				} break;

				case RegionShape3D.ShapeType.Cylinder: break;

				case RegionShape3D.ShapeType.Sphere:
				{
					if(editable && controlEvent == EventType.Layout)
						Handles.SphereHandleCap(id, Vector3.zero, Quaternion.identity, shape.SphereRadius - 0.05f, EventType.Layout);

					hover = editable && HandleInput(id, shape);

					if (controlEvent == EventType.Repaint)
					{
						Handles.color = ( hover &&  !selectedObjects.Contains(shape)) ? new Color(0.45f, 0.98f, 0f, 0.49f) : new Color(0.02f, 0.65f, 0.01f, 0.4f);
						Handles.SphereHandleCap(id, Vector3.zero, Quaternion.identity, shape.SphereRadius * 2, EventType.Repaint);

						Handles.color = new Color(0.65f, 0.65f, 0.0f, 0.85f);
						Handles.CircleHandleCap(id, Vector3.zero, Quaternion.Euler(0, 90, 0), shape.SphereRadius, EventType.Repaint);
						Handles.CircleHandleCap(id, Vector3.zero, Quaternion.Euler(90, 0, 0), shape.SphereRadius, EventType.Repaint);
						Handles.CircleHandleCap(id, Vector3.zero, Quaternion.identity,        shape.SphereRadius, EventType.Repaint);
					}
				} break;

				case RegionShape3D.ShapeType.Polygon: break;
			}

			DrawSpatial_End();
			Profiler.EndSample();
		}

		private static Color path_color_unselected = Color.HSVToRGB(0.4f, 0.0f, 0.7f);
		private static Color path_color_hover      = Color.HSVToRGB(0.2f, 0.4f, 0.8f);
		private static Color path_color_selected   = Color.HSVToRGB(0.5f, 0.8f, 0.8f);
		private static Color path_color_editing    = Color.HSVToRGB(0.1f, 0.8f, 0.8f);

		public void DrawPath(int id, RegionPath path, bool editable)
		{
			Profiler.BeginSample("DrawPath");

			var (ray, ray_ok) = RaycastToSceneCollision();

			bool hover = false;

			Color getColor()
			{
				if (pathEditing == path)
					return path_color_editing;

				if (selectedObjects.Contains(path))
					return path_color_selected;

				if(hover)
					return path_color_hover;

				return path_color_unselected;
			}

			Color greyOut(Color input)
			{
				if (pathEditing == path) return input;
				Color.RGBToHSV(input, out var h, out var s, out var v);
				return Color.HSVToRGB(h, 0, v);
			}


			DrawSpatial_Begin(path);
			EventType controlEvent = Event.current.GetTypeForControl(id);
			DrawSphereHandle(id, path, controlEvent, editable);

			if (state != State.EditPath || pathEditing != path)
				editable = false;

			if (path.Points.Count > 0) {

				if (line_drawn_points_list == null)
					line_drawn_points_list = new List<Vector3>();
				else
					line_drawn_points_list.Clear();

				//int num = 0;

				for (int i = 0; i < path.Points.Count; i++) {
					var p = path.Points[i];

					line_drawn_points_list.Add(p.point);
					//line_drawn_points[i] = p.point;
					//num++;
				}

				if (path.Points.Count > 1) {
					if (path == pathEditing && pathEditState == EditVertsState.AddVerts && ray_ok) {
						line_drawn_points_list.Add(ray.point - path.Transform.Position);
						//line_drawn_points[num++] = ray.point - path.Transform.Position;
					}

					/*if (path.Closed) {
						line_drawn_points[num++] = path.Points[0].point;
					}*/
				}

				if (Event.current.OnRepaint()) {
					using (Draw.WithMatrix(path.Transform.matrix)) {
						using (Draw.WithLineWidth(5f)) {
							Draw.Polyline(line_drawn_points_list, path.Closed, getColor());
						}
					}
				}

				/*Handles.color = ;
				Handles.DrawAAPolyLine(4, num, line_drawn_points);*/

				for (int i = 0; i < path.Points.Count; i++) {
					var p          = path.Points[i];
					var vert_id    = GUIUtility.GetControlID(( path.ID + "_Vert_" + i ).GetHashCode(), FocusType.Passive);
					var vert_event = Event.current.GetTypeForControl(vert_id);

					var size      = 0.08f;
					var mouseSize =  size;
					if (editable) {
						size      = Mathf.Clamp(0.1f * HandleUtility.GetHandleSize(p.point), 0.1f, 0.75f);
						mouseSize = size * Mathf.Clamp(100 / (Vector2.Distance(Event.current.mousePosition, HandleUtility.WorldToGUIPoint(p.point)) * 3), 1, 1.5f);
					}

					Handles.color = greyOut(Color.HSVToRGB(0.0f, 0.6f, 0.9f));
					if (vert_event == EventType.Layout) {
						if(editable && Event.current.shift)
							Handles.CircleHandleCap(vert_id, p.point, Quaternion.identity, mouseSize, EventType.Layout);
						else
							Handles.SphereHandleCap(vert_id, p.point, Quaternion.identity, size * 1.25f, EventType.Layout);
					}

					hover = HandleInput(vert_id, path, i) && editable;

					//Handles.Label(p.point + Vector3.up * 0.3f, (i + 1).ToString(), EventStyles.GetTitleWithColor(Color.HSVToRGB(0.7f, 0.6f, 0.7f)));

					if(hover)
						Handles.color = greyOut(Color.HSVToRGB(0.4f, 0.6f, 0.9f));
					if(pathSelectedVert.ok && pathSelectedVert.ind == i)
						Handles.color = greyOut(Color.HSVToRGB(0.15f, 0.9f, 0.9f));

					if(editable && Event.current.shift)
						Handles.DrawSolidDisc(p.point, Vector3.up, mouseSize);
					else
						Handles.SphereHandleCap(vert_id, p.point, Quaternion.identity, size * 1.25f, EventType.Repaint);
				}

				//Drag verts
				if (editable && pathEditState == EditVertsState.DragVert && Event.current.type == EventType.MouseDrag)
				{
					if (pathDraggingVert.ok)
					{
						if (ray_ok) {
							var point = ray.point;

							RecordAssetUndo("Move Path Vert");
							path.SetWorldPoint(pathDraggingVert.ind, point);
						}
					}
				}

				if(editable && ray_ok && Event.current.control) {
					var correctedPos = ray.point - path.Transform.Position;
					var (ind, dist, pos) = path.GetClosestEdge(correctedPos);

					Handles.color = Color.green;
					//Handles.CubeHandleCap(0, ray.point, Quaternion.identity, 0.1f, EventType.Repaint);
					Handles.Label(ray.point, $"(ind: {ind}, dist: {dist}, pos: {pos})");

					if ((ind < path.Points.Count-1 || path.Closed) && dist < 0.5f) {
						if(path.GetPathEdge(ind, out Vector3 p1, out Vector3 p2)) {
							var point = Vector3.Lerp(p1, p2, pos);

							Handles.color = Color.yellow;
							Handles.SphereHandleCap(0, point, Quaternion.identity, 0.15f, EventType.Repaint);

							if (Event.current.IsLMBDown()) {
								Debug.Log($"[DEBUG] Insert {ind}, {correctedPos}");
								path.InsertPoint(ind + 1, correctedPos);
							}
						}
					}
				}

				if (pathSelectedVert.ok) {
					if (Event.current.OnKeyDown(KeyCode.PageDown)) 	pathSelectedVert.ind--;
					if (Event.current.OnKeyDown(KeyCode.PageUp)) 	pathSelectedVert.ind++;

					if(pathEditing == path)
						pathSelectedVert.ind = pathSelectedVert.ind.Wrap(0, path.Points.Count - 1);
				} else if(editable && Event.current.OnKeyDown(KeyCode.PageDown) || Event.current.OnKeyDown(KeyCode.PageUp)) {
					pathSelectedVert = (0, true);
				}
			}
			DrawSpatial_End();


			if (editable && !Event.current.control && !Event.current.shift && (pathEditState == EditVertsState.AddVerts || pathEditState == EditVertsState.Main)) {
				if (ray_ok) {
					Handles.color = Color.white;
					Handles.SphereHandleCap(-1, ray.point, Quaternion.identity, Mathf.Clamp(0.12f * HandleUtility.GetHandleSize(ray.point), 0.1f, 0.75f), EventType.Repaint);

					if (Event.current.OnMouseDown(0))
						path.AddWorldPoint(ray.point);
				}

				// Right click cancels the path editing
				if (pathEditState == EditVertsState.AddVerts && Event.current.OnKeyDown(KeyCode.Escape))
					pathEditState = EditVertsState.Main;
			}

			Profiler.EndSample();
		}

		public void DrawLink(RegionSpatialLinkBase link, bool editable)
		{
			switch (link)
			{
				case RegionShape2DLink regionShape2DLink: DrawLink_Shape2D(regionShape2DLink, editable); break;
			}
		}

		public void DrawLink_Shape2D(RegionShape2DLink link, bool editable)
		{
			if (link == null || !link.Valid) return;

			if (link.Transform == null)
			{
				link.Transform = new GraphObjectTransform();
			}

			bool selected = selectedObjects.Contains(link);

			var idline = GUIUtility.GetControlID(( link.ID + "_Line" ).GetHashCode(), FocusType.Passive);
			var id1    = GUIUtility.GetControlID(( link.ID + "_Control1" ).GetHashCode(), FocusType.Passive);
			var id2    = GUIUtility.GetControlID(( link.ID + "_Control2" ).GetHashCode(), FocusType.Passive);

			var controlEventLine = Event.current.GetTypeForControl(idline);
			var controlEvent1    = Event.current.GetTypeForControl(id1);
			var controlEvent2    = Event.current.GetTypeForControl(id2);

			var pos1 = link.FirstTransform.GetWorldPosition(link.First);
			var pos2 = link.SecondTransform.GetWorldPosition(link.Second);

			var linkRotation = Quaternion.LookRotation(( pos2 - pos1 ).normalized, Vector3.up) * Quaternion.Euler(0, 90, 0);
			var linkMidpoint = Vector3.Lerp(pos1, pos2, 0.5f);

			var drawHandle1 = link.First.Type != RegionShape2D.ShapeType.Empty;
			var drawHandle2 = link.Second.Type != RegionShape2D.ShapeType.Empty;


			bool HoverLine = false;

			handles.PushID(link.ID);
			if (handles.DoPlane(linkMidpoint, linkRotation.eulerAngles, Vector3.one,
								new Vector2(Vector3.Distance(pos1, pos2) / 2, link.Type == RegionShape2DLink.LinkAreaType.Line ? 0.05f : link.PlaneWidth / 2 ),
								"link_line", out var ev)) {
				HandleInputNew(link);
				HoverLine = true;
			}

			handles.PopID();

			//Layout
			if(drawHandle1 && controlEvent1 == EventType.Layout)
				Handles.CubeHandleCap(id1, pos1, link.First.Transform.Rotation, Mathf.Clamp(0.1f * HandleUtility.GetHandleSize(pos1), 0.1f, 0.75f),
					EventType.Layout);

			if(drawHandle2 && controlEvent2 == EventType.Layout)
				Handles.CubeHandleCap(id2, pos2, link.Second.Transform.Rotation, Mathf.Clamp(0.1f * HandleUtility.GetHandleSize(pos2), 0.1f, 0.75f),
					EventType.Layout);

			bool Hover1 = false, Hover2 = false;

			//Hover
			if(drawHandle1) Hover1 = HandleInput(id1, link, 1);
			if(drawHandle2) Hover2 = HandleInput(id2, link, 2);

			//Draw line or rect
			if (controlEventLine == EventType.Repaint)
			{
				if (link.Type == RegionShape2DLink.LinkAreaType.Plane) {
					float length = Vector3.Distance(pos1, pos2);
					RegionDrawingUtil.DrawRectangle(new Vector3(length/2, 0), new Vector2(length, link.PlaneWidth)/2, Quaternion.identity, Matrix4x4.TRS(pos2, linkRotation, Vector3.one), LinkFillColor, Color.black);
				}

				Color lineCol = (HoverLine || selected) ?
					ColorUtil.MakeColorHSVA(0.2f, 0.6f, 0.8f, 0.9f) :
					new Color(0.02f, 0.98f, 0.03f);

				using(Draw.WithLineWidth(!selected ? 2f : 2.75f))
					Draw.Line(pos1, pos2, lineCol);
			}

			//Repaint
			if(drawHandle1 && controlEvent1 == EventType.Repaint)
			{
				Handles.color = (Hover1) ? new Color(1f, 0.96f, 0.15f) : Color.blue;
				Handles.CubeHandleCap(id1, pos1, link.First.Transform.Rotation, Mathf.Clamp(0.1f * HandleUtility.GetHandleSize(pos1), 0.1f, 0.75f), EventType.Repaint);
			}

			if(drawHandle2 && controlEvent2 == EventType.Repaint)
			{
				Handles.color = (Hover2) ? new Color(1f, 0.96f, 0.15f) : Color.blue;
				Handles.CubeHandleCap(id2, pos2, link.Second.Transform.Rotation, Mathf.Clamp(0.1f * HandleUtility.GetHandleSize(pos2), 0.1f, 0.75f), EventType.Repaint);
			}

			//Handles.Label(pos1 + Vector3.up, link.GetLength().ToString());

			if (state == State.DragLinkPoint && controlEvent1 == EventType.MouseDrag)
			{
				if (dragLink == link)
				{
					RegionShape2D       shape = null;
					ShapeLink2DPosition pos   = link.FirstTransform;

					//Drag the point
					if (dragLinkPoint == 1)
						shape = link.First;
					else if (dragLinkPoint == 2)
						shape = link.Second;

					if(shape != null)
					{
						Plane areaPlane = new Plane(shape.Transform.Rotation * Vector3.up, shape.Transform.Position);
						Ray   ray       = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

						if (areaPlane.Raycast(ray, out float enter))
						{
							Vector2 nodePoint = shape.WorldPosToAreaPos(ray.GetPoint(enter));

							RecordAssetUndo("Move link");

							if (dragLinkPoint == 1)
								link.FirstTransform.SetPosition(nodePoint, shape);
							else if (dragLinkPoint == 2)
								link.SecondTransform.SetPosition(nodePoint, shape);

							//Handles.CubeHandleCap(0, nodePoint, quaternion.identity, 0.2f, EventType.Repaint);
						}
					}
				}
			}
		}

		Dictionary<RegionObject, string> _scratchSequenceLabels = new Dictionary<RegionObject, string>();

		void DrawSequenceObject(RegionObjectSequence seq)
		{
			Vector3 last = Vector3.zero;

			if(Event.current.OnRepaint()) {

				_scratchSequenceLabels.Clear();

				for (int i = 0; i < seq.Objects.Count; i++) {
					var obj = seq.Objects[i];

					if (!_scratchSequenceLabels.ContainsKey(obj))
						_scratchSequenceLabels[obj] = "";

					var str = _scratchSequenceLabels[obj];

					if(str.IsNullOrWhitespace())
						_scratchSequenceLabels[obj] = str.Insert(str.Length, (i + 1).ToString());
					else
						_scratchSequenceLabels[obj] = str.Insert(str.Length, ", " + (i + 1));
				}

				for (int i = 0; i < seq.Objects.Count; i++) {
					var obj = seq.Objects[i];

					if (obj is RegionObjectSpatial spatial) {
						Vector3 pos  = spatial.GetFocusPos();
						var     size = HandleUtility.GetHandleSize(pos);

						Handles.Label( pos + Vector3.up * 1.5f, _scratchSequenceLabels[obj], SequenceNumberStyle);

						Draw.WireHexagon(pos, Quaternion.identity, Mathf.Clamp(0.3f * size, 0.5f, 0.9f), ColorsXNA.PaleGoldenrod);

						if (i > 0) {
							using (Draw.WithLineWidth(2.5f)) {
								Draw.Line(last, pos, ColorsXNA.Goldenrod);
							}
						}

						last = pos;
					}
				}
			}
		}

		public void DrawAddingObject()
		{
			var (hit, ok) = RaycastToSceneCollision();

			if (ok) {

				if(addObject_TempObj is RegionObjectSpatial obj)
				{
					obj.Transform.Position = hit.point;

					if (Settings.AddObjectSpatial_NormalSnap)
						obj.Transform.Rotation = Quaternion.LookRotation(Vector3.forward, hit.normal);
					else
						obj.Transform.Rotation = Quaternion.identity;

					var id = GUIUtility.GetControlID(99999, FocusType.Passive);
					DrawGraphObjectCenterHandle(id, obj, false);
					DrawGraphObjectShape(id, obj, false);
				}

				if (Event.current.OnMouseDown(0))
				{
					//Add the node
					RecordAssetUndo("Add Object");

					LiveEditAsset.Graph.GraphObjects.Add(addObject_TempObj);
					addObject_TempObj.ParentGraph = LiveEditAsset.Graph;

					selectedObjects.Clear();
					SelectGraphObject(addObject_TempObj);
					EndAddingObject();
				}
			}
		}

		void DrawAddingSequence() {
			DrawSequenceObject(addSequence_TempSeq);
		}

		public void DrawSphereHandle<S>(int id, S obj, EventType controlEvent, bool editable, float radius = 0.3f) where S : RegionObject
		{
			if (editable && controlEvent == EventType.Layout)
				Handles.SphereHandleCap(id, Vector3.zero, Quaternion.Euler(new Vector3(90, 0, 0)), radius - 0.05f, EventType.Layout);

			var hover = editable && HandleInput(id, obj);

			if (controlEvent == EventType.Repaint)
			{
				z_Handles.PushHandleColor();
				Handles.color = ( hover &&  !selectedObjects.Contains(obj)) ? new Color(0.45f, 0.98f, 0f, 0.49f) : new Color(0.02f, 0.65f, 0.01f, 0.4f);
				Handles.SphereHandleCap(id, Vector3.zero, Quaternion.identity, radius * 2, EventType.Repaint);
				z_Handles.PopHandleColor();
			}
		}

		public void DrawSpatial_Begin<S>(S obj) where S:RegionObjectSpatial
		{
			if (obj.Transform == null)
				obj.Transform = new GraphObjectTransform();

			z_Handles.PushMatrix();
			Handles.matrix = Matrix4x4.TRS(obj.Transform.Position, obj.Transform.Rotation, obj.Transform.Scale);
			Handles.color  = Color.white;
		}

		public void DrawSpatial_End()
		{
			z_Handles.PopMatrix();
		}

		#endregion

		#region DRAWING_GUI
		public void DrawGUI()
		{
			Profiler.BeginSample("DrawGUI");

			Rect  sceneRect  = SceneView.currentDrawingSceneView.position;
			Rect  windowRect = new Rect(0, 0, sceneRect.width, sceneRect.height - 20);
			Color backColor  = new Color(1f, 0f, 0f, 0.76f);


			if (state == State.Closed)
			{
				DrawClosedGUI();
				return;
			}

			bool shouldClose = false;

			glo.BeginArea(windowRect);
			{
				glo.BeginHorizontal();
				{
					//BOTTOM TRAY
					glo.BeginVertical();
					{
						glo.FlexibleSpace();
						glo.BeginHorizontal(glo.ExpandHeight(false));
						{
							BeginGUIWindow(backColor, 224, state == State.MainMenu || state == State.EditingGraph || state == State.AddObject || state == State.AddLink || state == State.AddObjectSequence);
							{
								glo.BeginHorizontal();
								{
									glo.Label("Region Graph Editor", EventStyles.GetTitleWithColor(new Color(0.99f, 0.98f, 1f)));
									if (glo.Button("Close")) shouldClose = true;
								}
								glo.EndHorizontal();

								if (state == State.MainMenu)
									MainGUI();
								else
									MainGUIEditingGraph(LiveEditAsset);
							}
							EndGUIWindow();

							//Object selected window
							if (state != State.MainMenu)
							{
								if (selectedObjects.Count > 0)
								{
									var obj = selectedObjects[0];

									if (obj is RegionObject robj)
									{
										BeginGUIWindow(backColor, 256, state == State.EditingGraph);
										RegionObjectGUI(robj);
										EndGUIWindow();
									}

									if (obj is RegionSpatialLinkBase link)
									{
										BeginGUIWindow(backColor, 192);
										LinkGUI(link);
										EndGUIWindow();
									}

									if (obj is RegionObjectSpatial objSpatial)
									{
										BeginGUIWindow(backColor, 192, state == State.EditingGraph || state == State.EditPath || state == State.EditShape);
										{
											if (objSpatial is RegionShape2D shape2D) 	Shape2DGUI(shape2D);
											if (objSpatial is RegionShape3D shape3D) 	Shape3DGUI(shape3D);
											if (objSpatial is RegionPath path) 			PathGUI(path);
											if (objSpatial is ParkAIGraphPortal portal) PortalGUI(portal);
										}
										EndGUIWindow();
									}

									if (obj is RegionObjectSequence seq) {
										BeginGUIWindow(backColor, 192, state == State.EditingGraph || state == State.EditPath || state == State.EditShape);
										SequenceGUI(seq);
										EndGUIWindow();
									}
								}
							}


							glo.BeginHorizontal();
							glo.FlexibleSpace();
							glo.EndHorizontal();
						}
						glo.EndHorizontal();
					}
					glo.EndVertical();

					if (state != State.MainMenu && OutlinerOpen)
					{
						//Right Tray
						glo.BeginVertical(glo.ExpandWidth(false));
						{
							glo.Space(120);
							BeginGUIWindow(backColor, 256);

							glo.BeginHorizontal();
							glo.Label("Outliner:", EventStyles.GetHeaderWithColor(new Color(0.99f, 0.98f, 1f)));
							glo.FlexibleSpace();

							var arrowRect = GUILayoutUtility.GetRect(24, 24);
							EditorIcons.ArrowRight.Draw(arrowRect);
							if (Event.current.OnLeftClick(arrowRect)) {
								OutlinerOpen = false;
							}


							glo.Space(6);
							glo.EndHorizontal();

							OutlineGUI(LiveEditAsset.Graph);
							EndGUIWindow();
							glo.FlexibleSpace();
						}
						glo.EndVertical();
					}

				}
				glo.EndHorizontal();


			}
			glo.EndArea();

			if (!OutlinerOpen) {
				var areaRect  = new Rect(windowRect.xMax - 32, 120, 32, 32);
				var mouseRect = new Rect(new Vector2(0, 0),    areaRect.size);

				var style = new GUIStyle(EventStyles.GetHeaderWithColor(new Color(0.99f, 0.98f, 1f)));
				//style.alignment = TextAnchor.MiddleCenter;

				glo.BeginArea(areaRect);
				BeginGUIWindow(backColor, 32);
				glo.BeginVertical();

				/*if (Event.current.IsMouseContained(mouseRect)) {
					GUI.color = ColorsXNA.Goldenrod;
				}*/

				//EditorIcons.ArrowLeft.Draw(GUILayoutUtility.GetRect(24, 24));
				glo.FlexibleSpace();
				EditorIcons.GridImageTextList.Draw(GUILayoutUtility.GetRect(24,      24));
				glo.FlexibleSpace();


				GUI.color = Color.white;

				if (Event.current.OnLeftClick(mouseRect)) {
					OutlinerOpen = true;
				}

				glo.EndVertical();
				EndGUIWindow();
				glo.EndArea();
			}

			if (shouldClose)
				state = State.Closed;

			Profiler.EndSample();
		}


		public void DrawClosedGUI()
		{
			Rect  sceneRect  = SceneView.currentDrawingSceneView.position;
			Rect  windowRect = new Rect(0, 0, sceneRect.width, sceneRect.height - 20);
			Color backColor  = new Color(1f, 0f, 0f, 0.76f);

			glo.BeginArea(windowRect); { glo.BeginHorizontal(); { glo.BeginVertical();
				{
					glo.FlexibleSpace();
					glo.BeginHorizontal(glo.ExpandHeight(false));
					{
						BeginGUIWindow(backColor, 128);

						var content = new GUIContent("Region Graphs");
						var style   = WhiteHeader;

						var rect = GUILayoutUtility.GetRect(content, style);

						if (Event.current.IsMouseContained(rect))
						{
							style.normal.textColor = Color.HSVToRGB(0.15f, 0.8f, 0.9f);

							if (Event.current.IsMouseDown() && Event.current.button == 0)
								state = State.MainMenu;
						}

						g.Label(rect, content, style);

						EndGUIWindow();
					}
					glo.EndHorizontal();
				}
				glo.EndVertical(); } glo.EndHorizontal(); } glo.EndArea();
		}



		public void MainGUI()
		{
			var labelStyle         = EventStyles.GetLabelWithColor(Color.white);
			var labelStyleSelected = EventStyles.GetBoldLabelWithColor(Color.white);
			var headerStyle        = EventStyles.GetHeaderWithColor(Color.white);

			var listSelectStyle = new GUIStyle();

			//glo.Label("No asset Loaded", labelStyle);

			glo.Space(4);

			glo.Label("Scene Graphs:", headerStyle);

			glo.BeginVertical(SirenixGUIStyles.BoxContainer);
			{
				Level level = EditorLevelCache.GetLevel();

				if(loadedGraphAssets.Count > 0)
				{
					GUIHelper.PushColor(new Color(1f, 0f, 0.9f));
					glo.BeginVertical("CurveEditorBackground");
					{
						GUIHelper.PopColor();
						for (int i = 0; i < loadedGraphAssets.Count; i++)
						{
							var name = ( loadedGraphAssets[i] != null )
								? loadedGraphAssets[i].name
								: "Null";
							var content   = new GUIContent(name);
							var textRect  = GUILayoutUtility.GetRect(content, labelStyle);
							var clickRect = textRect.Outset(4, 2, 4, 2);

							if (Event.current.OnRepaint()) {
								bool is_enabled_in_manifest = false;

								if (level != null && level.Manifest != null) {
									if (level.Manifest.ParkAIGraphs.Contains(loadedGraphAssets[i])) {
										is_enabled_in_manifest = true;
									}
								}

								if (i == selectedAvailableAsset)
								{
									//SirenixGUIStyles.BoxContainer.Draw(textRect, GUIContent.none, false, false, false, false);
									eg.DrawRect(clickRect, new Color(1f, 0f, 0.92f, 0.5f));
									g.Label(textRect, content, labelStyleSelected);
								} else {
									g.Label(textRect, content, labelStyle);
								}

								if (is_enabled_in_manifest) {
									var width = labelStyleSelected.CalcSize(new GUIContent("(L)")).x;
									g.Label(new Rect(textRect.xMax - width - 4, textRect.y, width, textRect.height), "(L)", labelStyleSelected);
								}
							}

							if (Event.current.OnMouseDown(clickRect, 0) && loadedGraphAssets[i] != null)
								selectedAvailableAsset = i;
						}
					}
					glo.EndVertical();
				}
				else
				{
					glo.Label("No graph assets detected for active scene.",labelStyle);
				}
			}
			glo.EndVertical();



			glo.BeginHorizontal();
			{
				if (selectedAvailableAsset == -1 || loadedGraphAssets[selectedAvailableAsset] == null)
					GUI.enabled = false;

				if (glo.Button("Edit"))
				{
					BeginEditingGraph(loadedGraphAssets[selectedAvailableAsset]);
				}

				GUI.enabled = true;

				if (glo.Button("Refresh"))
				{
					RefreshAvailableAssets(EditorSceneManager.GetActiveScene());
				}
			}
			glo.EndHorizontal();

			glo.Space(6);
			glo.Label("Create Graph:", headerStyle);

			glo.BeginVertical(SirenixGUIStyles.BoxContainer);
			{
				glo.BeginHorizontal();
				{
					glo.Label("Name:", labelStyle, glo.Width(48));
					newAssetName = glo.TextField(newAssetName);
				}
				glo.EndHorizontal();

				if (newAssetName.Replace(" ", "").Length == 0 || loadedGraphAssets.Contains(x=>x != null && x.name == newAssetName))
					GUI.enabled = false;

				if (glo.Button("Add New"))
				{
					if(UnityEditor.EditorUtility.DisplayDialog("Create new graph","Are you sure you want to create a new graph?","Yes","Canel"))
					{
						CreateSceneAsset(EditorSceneManager.GetActiveScene(), newAssetName);
						RefreshAvailableAssets(EditorSceneManager.GetActiveScene());
					}
				}

				GUI.enabled = true;
			}
			glo.EndVertical();

			glo.Space(4);
		}

		bool CancelButton() => glo.Button("Cancel", SirenixGUIStyles.Button);

		public void MainGUIEditingGraph(RegionGraphAsset graph)
		{
			glo.BeginHorizontal();
			{
				switch (state)
				{
					case State.AddObject: if(CancelButton()) EndAddingObject(); 		  break;
					case State.AddLink:   if(CancelButton()) EndAddingLink(); 		  break;
					case State.AddObjectSequence:
					{
						if (glo.Button("Finish", SirenixGUIStyles.Button))
						{
							RecordAssetUndo("Add Sequence");
							LiveEditAsset.Graph.Sequences.Add(addSequence_TempSeq);
							EndAddingObjectSequence();
						}
						if (CancelButton()) EndAddingObjectSequence();
						break;
					}

					default:
					{
						GUIHelper.PushGUIEnabled(state == State.EditingGraph);
						glo.Label("Shapes:", EditorStyles.whiteLabel);

						if (glo.Button("Add 2D", SirenixGUIStyles.ButtonLeft)) BeginAddingObject(ObjectType.Shape2D);
						if (glo.Button("Add 3D", SirenixGUIStyles.ButtonMid))  BeginAddingObject(ObjectType.Shape3D);
						if (glo.Button("Path", SirenixGUIStyles.ButtonRight))  BeginAddingObject(ObjectType.Path);
						if (glo.Button("Link", SirenixGUIStyles.ButtonRight))  BeginAddingLink();

						GUIHelper.PopGUIEnabled();
					}
						break;
				}
			}
			glo.EndHorizontal();


			GUIHelper.PushGUIEnabled(state == State.EditingGraph);
			glo.BeginHorizontal();
			{
				glo.Label("ParkAI Nodes:", EditorStyles.whiteLabel);

				if (glo.Button("Graph Portal", SirenixGUIStyles.Button) && state != State.AddObject)
					BeginAddingObject(ObjectType.ParkAI_GraphPortal);
			}
			glo.EndHorizontal();

			glo.BeginHorizontal();
			{
				glo.Label("Objects:", EditorStyles.whiteLabel);

				if (glo.Button("Add Sequence", SirenixGUIStyles.Button) && state != State.AddObject)
				{
					BeginAddingObjectSequence();
				}
			}
			glo.EndHorizontal();
			GUIHelper.PopGUIEnabled();


			SettingsControlsGUI();

			GUIHelper.PushGUIEnabled(state == State.EditingGraph);
			glo.BeginHorizontal();
			{
				if (selectedObjects.Count > 0)
				{
					if (glo.Button("Delete Selected Objects"))
					{
						DeleteSelectedObjects();
					}
				}

				glo.FlexibleSpace();
				if (glo.Button("Exit"))
				{
					shouldClearSelection = true;
					EndEditingGraph();
				}
			}
			glo.EndHorizontal();
			GUIHelper.PopGUIEnabled();
		}

		public void RegionObjectGUI(RegionObject obj)
		{
			EditorGUI.BeginChangeCheck();

			var title  = EventStyles.GetTitleWithColor(new Color(0.99f, 0.98f, 1f));
			var header = WhiteHeader;

			glo.BeginHorizontal();
			glo.Label("Region Object:", title);
			glo.FlexibleSpace();

			glo.Label("ID: " + obj.ID, header);
			if (glo.Button("Copy", SirenixGUIStyles.MiniButton)) {
				EditorGUIUtility.systemCopyBuffer = obj.ID;
			}
			glo.EndHorizontal();

			glo.BeginHorizontal();
			glo.Label("Name:", header , glo.Width(48));
			glo.Space(8);
			obj.Name = eglo.TextField(obj.Name);
			glo.EndHorizontal();

			switch (obj) {
				case RegionObjectSpatial spatial: RegionObjectSpatialGUI(spatial); break;
			}

			if (EditorGUI.EndChangeCheck())
			{
				RecordAssetUndo("Object Changed");
				RebuildOutlinerFlag = true;
			}
		}

		void RegionObjectSpatialGUI(RegionObjectSpatial obj)
		{
			var rot = obj.Transform.Rotation.eulerAngles;

			DrawVector3Field(ref obj.Transform.Position, "Pos:",   SirenixGUIStyles.WhiteLabel);
			DrawVector3Field(ref rot,                    "Rot:",   SirenixGUIStyles.WhiteLabel);
			DrawVector3Field(ref obj.Transform.Scale,    "Scale:", SirenixGUIStyles.WhiteLabel);

			obj.Transform.Rotation = Quaternion.Euler(rot);

			obj.RequiresPathfinding = DrawSetting(obj.RequiresPathfinding, "Requires Pathfinding");
		}


		public void Shape2DGUI(RegionShape2D shape)
		{
			EditorGUI.BeginChangeCheck();

			glo.BeginHorizontal();
			glo.Label("Shape 2D:", WhiteHeader, glo.Width(72));
			AnjinGUILayout.EnumToggleButtons(ref shape.Type);
			glo.EndHorizontal();

			switch(shape.Type)
			{
				case RegionShape2D.ShapeType.Empty:
					glo.Label("No parameters for empty area type." ,EditorStyles.whiteMiniLabel);
					break;

				case RegionShape2D.ShapeType.Rect:
					glo.BeginHorizontal();
				{
					glo.Label("Size:", EditorStyles.whiteBoldLabel);
					glo.FlexibleSpace();

					glo.BeginVertical();
					{
						var s = shape.RectSize;
						DrawVector2Field(ref s);
						shape.RectSize = s;
					}
					glo.EndVertical();
				}
					glo.EndHorizontal();
					break;

				case RegionShape2D.ShapeType.Circle:
					glo.BeginHorizontal();
				{
					DoDragLabel(new GUIContent("Radius"), ref shape.CircleRadius, EditorStyles.whiteLabel);
					shape.CircleRadius = eglo.FloatField(shape.CircleRadius);
				}
					glo.EndHorizontal();
					break;

				case RegionShape2D.ShapeType.Polygon:

					bool vertsChanged = false;

					if(state == State.EditShape)
					{
						if (Event.current.OnKeyDown(KeyCode.Delete)) {
							Debug.Log("[DEBUG] Try Delete while editing path. (Not implemented yet!)");
						}

						if (shapeSelectedVert.ok) {
							glo.BeginHorizontal();
							glo.Label("Vert #" +(shapeSelectedVert.ind +1));
							if (glo.Button("Delete")) {
								RecordAssetUndo("Delete Vert");
								shape.PolygonPoints.RemoveAt(shapeSelectedVert.ind);
								shape.TriangulatePolygon();
								selectedObjects[0] = shape;
							}
							glo.EndHorizontal();
						}

						if (shapeEditState == EditVertsState.Main) {
							if (glo.Button("Add Vert")) {
								shapeEditState = EditVertsState.AddVerts;
							}
						}
						else if (shapeEditState == EditVertsState.AddVerts) {
							if (glo.Button("Cancel")) {
								shapeEditState = EditVertsState.Main;
							}
						}
					}

					glo.BeginHorizontal();
					switch (state) {
						case State.EditingGraph:
							if (glo.Button("Edit"))
								BeginEditingShape(shape);
							break;

						case State.EditShape:
							if (glo.Button("Cancel"))
								EndEditingShape();
							break;
					}
					glo.FlexibleSpace();
					glo.EndHorizontal();

					if (state == State.EditShape)
						glo.Label("Hold Shift: Drag Verts", SirenixGUIStyles.WhiteLabel);

					if(glo.Button("Triangulate"))
						shape.TriangulatePolygon();

					break;
			}

			glo.Label("Calculated Area: " + shape.GetArea());

			if (EditorGUI.EndChangeCheck())
			{
				RecordAssetUndo("Shape2D Changed");
			}
		}

		public void Shape3DGUI(RegionShape3D shape)
		{
			EditorGUI.BeginChangeCheck();

			glo.BeginHorizontal();
			glo.Label("Shape 3D:", WhiteHeader, glo.Width(72));
			AnjinGUILayout.EnumToggleButtons(ref shape.Type);
			glo.EndHorizontal();

			switch(shape.Type)
			{
				case RegionShape3D.ShapeType.Empty:
					glo.Label("No parameters for empty area type." , EditorStyles.whiteMiniLabel);
					break;

				case RegionShape3D.ShapeType.Box:
					glo.BeginHorizontal();
				{
					glo.BeginVertical();
					DrawVector3Field(ref shape.BoxSize, "Size", SirenixGUIStyles.WhiteLabel);
					glo.EndVertical();
				}
					glo.EndHorizontal();
					break;

				case RegionShape3D.ShapeType.Sphere:
					glo.BeginHorizontal();
				{
					DoDragLabel(new GUIContent("Radius"), ref shape.SphereRadius, EditorStyles.whiteLabel);
					shape.SphereRadius = eglo.FloatField(shape.SphereRadius);
				}
					glo.EndHorizontal();
					break;

				case RegionShape3D.ShapeType.Polygon:
					glo.Label("Polygon not yet implemented." , EditorStyles.whiteMiniLabel);
					break;
			}

			if (EditorGUI.EndChangeCheck())
			{
				RecordAssetUndo("Shape 3D Changed");
			}
		}

		public void PathGUI(RegionPath path)
		{
			EditorGUI.BeginChangeCheck();

			glo.BeginHorizontal();
			glo.Label("Path:", WhiteHeader, glo.Width(72));
			glo.EndHorizontal();

			glo.Label($"{path.Points.Count} vertices");
			path.Closed = DrawSetting(path.Closed, "Closed");

			bool vertsChanged = false;

			if(state == State.EditPath)
			{
				if (Event.current.OnKeyDown(KeyCode.Delete)) {
					if(pathSelectedVert.ok) {
						RecordAssetUndo("Delete Vert");
						path.Points.RemoveAt(pathSelectedVert.ind);
						selectedObjects[0] = path;

						pathSelectedVert.ind--;
						pathSelectedVert.ind = pathSelectedVert.ind.Wrap(0, path.Points.Count-1);
					}
				}

				if (pathSelectedVert.ok) {
					glo.BeginHorizontal();
					glo.Label("Vert #" +(pathSelectedVert.ind +1));
					if (glo.Button("Delete")) {
						//vertsChanged = true;
						RecordAssetUndo("Delete Vert");
						path.Points.RemoveAt(pathSelectedVert.ind);
						selectedObjects[0] = path;

						pathSelectedVert.ind--;
						pathSelectedVert.ind = pathSelectedVert.ind.Wrap(0, path.Points.Count -1);
					}
					glo.EndHorizontal();
				}

				if (pathEditState == EditVertsState.Main) {
					if (glo.Button("Add Vert")) {
						pathEditState = EditVertsState.AddVerts;
					}
				}
				else if (pathEditState == EditVertsState.AddVerts) {
					if (glo.Button("Cancel")) {
						pathEditState = EditVertsState.Main;
					}
				}
			}


			glo.BeginHorizontal();
			switch (state) {
				case State.EditingGraph:
					if (glo.Button("Edit"))
						BeginEditingPath(path);
					break;

				case State.EditPath:
					if (glo.Button("Cancel"))
						EndEditingPath();
					break;
			}
			glo.FlexibleSpace();
			glo.EndHorizontal();

			if (state == State.EditPath) {
				glo.Label("Hold Shift: Drag Verts", SirenixGUIStyles.WhiteLabel);
			}

			if (EditorGUI.EndChangeCheck() || vertsChanged)
				RecordAssetUndo("Path Changed");
		}

		void PortalGUI(ParkAIGraphPortal portal)
		{
			EditorGUI.BeginChangeCheck();

			glo.BeginHorizontal();
			glo.Label("Graph: ");
			glo.FlexibleSpace();
			portal.Graph = SirenixEditorFields.UnityObjectField(portal.Graph, typeof(RegionGraphAsset), false) as RegionGraphAsset;
			glo.EndHorizontal();

			glo.BeginHorizontal();
			glo.Label("Target Portal ID: ");
			glo.FlexibleSpace();
			portal.TargetPortalID = SirenixEditorFields.TextField(GUIContent.none, portal.TargetPortalID);
			glo.EndHorizontal();

			if (EditorGUI.EndChangeCheck())
				RecordAssetUndo("Portal Changed");
		}

		void LinksHeader() => glo.Label("Links:", WhiteHeader, glo.Width(48));
		void LinksHeaderFullLine() {
			glo.BeginHorizontal();
			glo.Label("Links:", WhiteHeader, glo.Width(48));
			glo.EndHorizontal();
		}

		public void LinkGUI(RegionSpatialLinkBase link)
		{
			EditorGUI.BeginChangeCheck();

			//LinksHeader();

			if (link is RegionShape2DLink link2D)
				LinkGUI_Shape2D(link2D);

			if (EditorGUI.EndChangeCheck())
				RecordAssetUndo("Links Changed");

		}

		void LinkGUI_Shape2D(RegionShape2DLink link)
		{
			GUILayout.BeginHorizontal();
			LinksHeader();
			AnjinGUILayout.EnumToggleButtons(ref link.Type);
			GUILayout.EndHorizontal();

			if (link.Type == RegionShape2DLink.LinkAreaType.Plane)
			{
				glo.BeginHorizontal();
				{
					DoDragLabel(new GUIContent("Width:"), ref link.PlaneWidth, EditorStyles.whiteLabel);
					link.PlaneWidth = Mathf.Max(0.1f,
						eglo.FloatField(GUIContent.none, link.PlaneWidth, glo.Width(72)));
				}
				glo.EndHorizontal();
			}
		}

		void SequenceGUI(RegionObjectSequence seq)
		{
			glo.Label("Sequence:", WhiteHeader);


		}


		void GraphObjectLabel(RegionObject obj, GUIStyle style)
		{

			glo.Label(obj.Name + " [" +obj.ID +"]", style);
		}


		public void SettingsControlsGUI()
		{
			glo.BeginVertical();
			{
				Settings.ZTestNonCulled = DrawSetting(Settings.ZTestNonCulled, "Disable Handle Culling");
				Settings.ShowNodeAxis   = DrawSetting(Settings.ShowNodeAxis, "Draw Node Axes");
				if (state == State.AddObject)
					Settings.AddObjectSpatial_NormalSnap = DrawSetting(Settings.AddObjectSpatial_NormalSnap, "Rotate to surface normal");
			}
			glo.EndVertical();
		}

		public bool DrawSetting(bool setting, string label)
		{
			bool newSetting;
			glo.BeginHorizontal();
			{
				newSetting = glo.Toggle(setting, GUIContent.none, glo.Width(16));
				glo.Label(label, SirenixGUIStyles.WhiteLabel);
				glo.FlexibleSpace();
			}
			glo.EndHorizontal();

			return newSetting;
		}

		#endregion

		#region OUTLINER

		public void OutlineGUI(RegionGraph graph)
		{
			if (RebuildOutlinerFlag || OutlinerTree == null)
			{
				RebuildOutlinerFlag = false;
				RebuildOutlinerTree();
			}

			OutlinerTree.ItemSelected = null;
			if (selectedObjects.Count > 0) {
				if (OutlinerTree.RefsToItems.TryGetValue(selectedObjects[0].ID, out var item)) {
					OutlinerTree.ItemSelected = item;
				}
			}

			EditorGUI.BeginChangeCheck();
			OutlinerScrollPos = glo.BeginScrollView(OutlinerScrollPos, false, false);
			{
				if (OutlinerTree != null) {
					if(state == State.EditPath)
						GUI.enabled = false;

					OutlinerTree.DrawTree();

					GUI.enabled = true;
				}
			}
			glo.EndScrollView();
			EditorGUI.EndChangeCheck();
		}

		void RebuildOutlinerTree()
		{
			if (LiveEditAsset == null)
			{
				OutlinerTree = new GraphObjectTree(new GraphObjectTreeItem("No Editing Asset"), OnClickOutlinerItem, OnRightClickOutlinerItem);
				return;
			}

			var graphFolder           = new GraphObjectTreeItem(LiveEditAsset.name) 	{type = ItemType.Section};
			var objectFolder          = new GraphObjectTreeItem("Objects") 			{type     = ItemType.Foldout, itemValue = new RegionObjectRef("__OBJECTS")};
			var spatialLinksFolder    = new GraphObjectTreeItem("Spatial Links") 		{type = ItemType.Foldout, itemValue = new RegionObjectRef("__SPATIALINKS")};
			var objectSequencesFolder = new GraphObjectTreeItem("Object Sequences") 	{type = ItemType.Foldout, itemValue = new RegionObjectRef("__SEQUENCES")};
			var metadataFolder        = new GraphObjectTreeItem("Global Metadata") 	{type     = ItemType.Foldout, itemValue = new RegionObjectRef("__METADATA", -1)};

			graphFolder.Add(objectFolder);
			graphFolder.Add(spatialLinksFolder);
			graphFolder.Add(objectSequencesFolder);
			graphFolder.Add(metadataFolder);


			OutlinerTree = new GraphObjectTree(graphFolder, OnClickOutlinerItem, OnRightClickOutlinerItem);

			//RegionObject obj;
			GraphObjectTreeItem folder;
			foreach (RegionObject obj in LiveEditAsset.Graph.GraphObjects)
			{
				RegionObjectRef objectRef = new RegionObjectRef(obj.ID) { Section = GraphObjectSection.Objects };
				folder = new GraphObjectTreeItem(obj.Name + $" [{obj.ID}]", objectRef)
					{ section = GraphObjectSection.Objects };
				objectFolder.Add(folder);

				OutlinerTree.RefsToItems[obj.ID] = folder;

				BuildMetadataSubtree(obj, folder, GraphObjectSection.Objects);
			}

			foreach (RegionObject obj in LiveEditAsset.Graph.SpatialLinks)
			{
				RegionObjectRef objectRef = new RegionObjectRef(obj.ID) { Section = GraphObjectSection.SpatialLinks };
				folder = new GraphObjectTreeItem(obj.Name + $" [{obj.ID}]", objectRef)
					{ section = GraphObjectSection.SpatialLinks };
				spatialLinksFolder.Add(folder);
				OutlinerTree.RefsToItems[obj.ID] = folder;

				BuildMetadataSubtree(obj, folder, GraphObjectSection.SpatialLinks);
			}

			foreach (RegionObject obj in LiveEditAsset.Graph.Sequences)
			{
				var objectRef = new RegionObjectRef(obj.ID) { Section = GraphObjectSection.Sequences };
				folder = new GraphObjectTreeItem(obj.Name + $" [{obj.ID}]", objectRef)
					{ section = GraphObjectSection.Sequences };
				objectSequencesFolder.Add(folder);
				OutlinerTree.RefsToItems[obj.ID] = folder;

				BuildMetadataSubtree(obj, folder, GraphObjectSection.Sequences);
			}

			if (LiveEditAsset.Graph.GlobalMetadata != null)
			{
				for (var i = 0; i < LiveEditAsset.Graph.GlobalMetadata.Count; i++)
				{
					RegionMetadata metadata = LiveEditAsset.Graph.GlobalMetadata[i];
					var objectRef = new RegionObjectRef("__METADATA", i) { Section = GraphObjectSection.Metadata };
					folder = new GraphObjectTreeItem(metadata.GetType().Name, objectRef)
						{ section = GraphObjectSection.Metadata };
					metadataFolder.Add(folder);
					//OutlinerTree.RefsToItems[obj.ID] = folder;
				}
			}
		}

		void BuildMetadataSubtree(RegionObject obj, GraphObjectTreeItem folder, GraphObjectSection section)
		{
			if (obj.Metadata != null)
			{
				for (int i = 0; i < obj.Metadata.Count; i++)
				{
					if (obj.Metadata[i] != null)
					{
						string name = "";
						var    type = obj.Metadata[i].GetType();

						var attribute = TypeExtensions.GetCustomAttribute<RegionMetadataAttrib>(type);

						if (attribute != null)
							name  = attribute.PrettyName;
						else name = type.Name;

						folder.Add(new GraphObjectTreeItem(name, new RegionObjectRef(obj.ID, i)) { section = section });
					}
				}
			}
		}

		void OnClickOutlinerItem(RegionObjectRef reference)
		{
			Debug.Log("[DEBUG] Click on " + reference.ID + ", "+reference.MetaDataIndex);
			if (LiveEditAsset == null) return;

			if (reference.Section == GraphObjectSection.Metadata) {
				RegionMetadataWindow.InspectMetadata(LiveEditAsset.Graph.GlobalMetadata[reference.MetaDataIndex], LiveEditAsset, "Global Metadata");
				return;
			}

			var obj = LiveEditAsset.Graph.FindObject<RegionObject>(reference.ID);
			if (obj != null)
			{
				selectedObjects.Clear();
				SelectGraphObject(obj);

				if (reference.MetaDataIndex >= 0)
				{
					RegionMetadataWindow.InspectMetadata(obj.Metadata[reference.MetaDataIndex], LiveEditAsset, obj.ID);
				}
			}
		}

		void OnRightClickOutlinerItem(RegionObjectRef reference)
		{
			if (reference.ID == RegionObjectRef.NullID) return;

			GenericMenu menu = new GenericMenu();

			if (reference.ID == "__OBJECTS") { }
			else if (reference.ID == "__SPATIALINKS") { }
			else if (reference.ID == "__SEQUENCES") { }
			else if (reference.ID == "__METADATA") {
				if(reference.MetaDataIndex == -1)
					OutlinerAddMetadataGlobalContextMenuItems(menu);
				else
					menu.AddItem(new GUIContent("Delete Metadata"), false, CMenu_DeleteMetadata, ((RegionObject)null, reference.MetaDataIndex));
			} else {
				RegionObject obj = LiveEditAsset.Graph.FindObject<RegionObject>(reference.ID);
				if (obj == null) return;

				if(reference.MetaDataIndex == -1)
					OutlinerAddMetadataContextMenuItems(menu, obj);
				else
				{
					menu.AddItem(new GUIContent("Delete Metadata"), false, CMenu_DeleteMetadata, (obj, reference.MetaDataIndex));
				}
			}

			menu.ShowAsContext();
		}

		void OutlinerAddMetadataContextMenuItems(GenericMenu menu, RegionObject obj)
		{
			menu.AddItem(new GUIContent("Delete"), false, CMenu_DeleteGraphObject, obj);
			menu.AddItem(new GUIContent("Copy ID"), false, CMenu_CopyGraphObjectID, obj);
			menu.AddSeparator("");

			var types = RegionMetadataAttrib.GetMetadataTypes();
			for (int i = 0; i < types.Length; i++) {
				menu.AddItem(new GUIContent($"Add {types[i].Name}"), false, AddMetadata, (types[i], obj));
			}
		}

		void OutlinerAddMetadataGlobalContextMenuItems(GenericMenu menu)
		{
			/*menu.AddItem(new GUIContent("Add Audio Zone"),  false, CMenu_AddAudioZone, 	null);
			menu.AddItem(new GUIContent("Add Camera Zone"), false, CMenu_AddCamZone, 	null);*/
		}

		void CMenu_DeleteGraphObject(object obj)
		{
			if (obj is RegionObject RO)
				DeleteObject(RO);
		}

		void CMenu_CopyGraphObjectID(object obj)
		{
			if (obj is RegionObject RO)
				EditorGUIUtility.systemCopyBuffer = RO.ID;
		}

		void CMenu_DeleteMetadata(object obj)
		{
			if (obj is ValueTuple<RegionObject, int> tuple)
			{
				RecordAssetUndo("Deleted Object Metadata");
				RegionMetadataWindow.StopInspecting();
				if(tuple.Item1 != null)
					tuple.Item1.Metadata.RemoveAt(tuple.Item2);
				else
					LiveEditAsset.Graph.GlobalMetadata.RemoveAt(tuple.Item2);
				RebuildOutlinerFlag = true;
			}
		}

		void AddMetadata(object input)
		{
			if (!( input is ValueTuple<Type, RegionObject> valid)) return;

			string name = "";

			var metadata = Activator.CreateInstance(valid.Item1) as RegionMetadata;

			if (valid.Item2 is RegionObject RO) {
				metadata.Parent = RO;
				name            = RO.ID;
				RO.InsureMetadataList();
				RO.Metadata.Add(metadata);
			} else if (valid.Item2 is null) {
				name = "Global Metadata";
				LiveEditAsset.Graph.GlobalMetadata.Add(metadata);
			}
			else return;

			RegionMetadataWindow.InspectMetadata(metadata, LiveEditAsset, name);
			RebuildOutlinerFlag = true;
		}

		#endregion

		#region SELECTION

		public void SelectGraphObject(RegionObject obj)
		{
			if(!selectedObjects.Contains(obj))
				objectsToSelect.AddIfNotExists(obj);
		}

		public void ClearSelection()
		{
			//pathSelectedVert = ( -1, false );
			selectedObjects.Clear();
		}

		#endregion

		#region GUI HELPERS

		public void RecordAssetUndo(string name)
		{
			Undo.RegisterCompleteObjectUndo(LiveEditAsset, name);
			UnityEditor.EditorUtility.SetDirty(LiveEditAsset);
		}

		public void BeginGUIWindow(Color backColor, int minWidth, bool enabled = true)
		{
			GUIHelper.PushColor(backColor);
			glo.BeginVertical(EventStyles.RedBackground, glo.MinWidth(minWidth), glo.ExpandHeight(false));
			GUIHelper.PopColor();

			GUI.enabled = enabled;
		}

		public void EndGUIWindow()
		{
			glo.EndVertical();
			UIWindowPanes.Add(GUILayoutUtility.GetLastRect());

			GUI.enabled = true;
		}

		public void DrawVector2Field(ref Vector2 vec)
		{
			glo.BeginHorizontal();
			DoDragLabel(new GUIContent("X"), ref vec.x, EditorStyles.whiteLabel);
			vec.x = Mathf.Max(0.1f , eglo.FloatField(GUIContent.none, vec.x, glo.Width(64)));
			glo.EndHorizontal();

			glo.BeginHorizontal();
			DoDragLabel(new GUIContent("Y"), ref vec.y, EditorStyles.whiteLabel);
			vec.y = Mathf.Max(0.1f , eglo.FloatField(GUIContent.none, vec.y, glo.Width(64)));
			glo.EndHorizontal();
		}

		public void DrawVector3Field(ref Vector3 value, string label, GUIStyle labelStyle)
		{
			glo.BeginHorizontal();
			glo.Label(label, labelStyle);

			DoDragLabel(new GUIContent("X"), ref value.x, labelStyle);
			value.x = eglo.FloatField(GUIContent.none, value.x, glo.Width(64));

			DoDragLabel(new GUIContent("Y"), ref value.y, labelStyle);
			value.y = eglo.FloatField(GUIContent.none, value.y, glo.Width(64));

			DoDragLabel(new GUIContent("Z"), ref value.z, labelStyle);
			value.z = eglo.FloatField(GUIContent.none, value.z, glo.Width(64));
			glo.EndHorizontal();
		}

		private MethodInfo _dragNumberValueMethod = typeof(eg).GetMethod("DragNumberValue",
			BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

		public void DoDragLabel(GUIContent label, ref float value, GUIStyle labelStyle)
		{
			eglo.LabelField(label, labelStyle, glo.Width(labelStyle.CalcSize(label).x));
			var rect = GUILayoutUtility.GetLastRect();
			int id   = GUIUtility.GetControlID(label.text.GetHashCode(), FocusType.Passive);

			double reference = value;

			var parameters = new object[]
			{
				rect,
				id,
				true,
				reference,
				null,
				0.01f
				//Math.Max(1.0, Math.Pow(Math.Abs((double) value.x), 0.5) * 0.0299999993294477)
			};

			_dragNumberValueMethod.Invoke(null, parameters );

			value = (float)(double) parameters[3];
		}

		MethodInfo _drawRectangleCapMethod = typeof(Handles).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static )
			.First(p => p.GetParameters()
				            .Select(q => q.ParameterType)
				            .SequenceEqual(
					            new Type[] { typeof(int), typeof(Vector3), typeof(Quaternion), typeof(Vector2), typeof(EventType) })
			            && p.ReturnType == typeof(void));

		public void RectCapVector2(int controlID, Vector3 position, Quaternion rotation, Vector2 size, EventType eventType)
		{
			object[] parameters = new object[]
			{
				controlID, position, rotation, size, eventType
			};

			_drawRectangleCapMethod.Invoke(null, parameters );
		}

		void RepaintSceneView()
		{
			SceneView.RepaintAll();
		}

		public (RaycastHit, bool) RaycastToSceneCollision()
		{
			Ray        ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
			RaycastHit hit;

			if (!mouseWasOverUIWindows && Physics.Raycast(ray, out hit, 500, LayerMask.GetMask("Walkable")))
				return ( hit, true );

			return ( new RaycastHit(), false );
		}


		#endregion

		#endregion

		public void GLDrawRectangle(Vector3 position, Quaternion rotation, Vector2 size)
		{
			var points = new Vector3[5];

			Vector3 vector3_1 = rotation * new Vector3(size.x, 0.0f,   0.0f);
			Vector3 vector3_2 = rotation * new Vector3(0.0f,   size.y, 0.0f);
			points[0] = position + vector3_1 + vector3_2;
			points[1] = position + vector3_1 - vector3_2;
			points[2] = position - vector3_1 - vector3_2;
			points[3] = position - vector3_1 + vector3_2;
			points[4] = position + vector3_1 + vector3_2;

			GLDrawRectangle(points, Handles.matrix);

		}

		public void GLDrawRectangle(Vector3[] verts, Matrix4x4 matrix)
		{
			if (!GLDrawingMaterial) return;

			GL.PushMatrix();
			GL.MultMatrix(matrix);

			//GLDrawingMaterial.SetInt("_HandleZTest", (int) CompareFunction.GreaterEqual);
			GLDrawingMaterial.SetPass(0);

			Color c = new Color(0.0f, 0.8f, 0.0f, 0.5f);
			GL.Begin(4);
			for (int index = 0; index < 2; ++index)
			{
				GL.Color(c);
				GL.Vertex(verts[index * 2]);
				GL.Vertex(verts[index * 2 + 1]);
				GL.Vertex(verts[(index * 2 + 2) % 4]);
				GL.Vertex(verts[index           * 2]);
				GL.Vertex(verts[(index * 2 + 2) % 4]);
				GL.Vertex(verts[index * 2 + 1]);
			}
			GL.End();

			GL.PopMatrix();
		}

		public void GLDrawLine(Vector3 p1, Vector3 p2, Matrix4x4 matrix, Color color)
		{
			if (!GLDrawingMaterial) return;

			GL.PushMatrix();
			GL.MultMatrix(matrix);

			GLDrawingMaterial.SetPass(0);
			GL.Begin(GL.LINES);

			GL.Color(color);
			GL.Vertex(p1);
			GL.Vertex(p2);

			GL.End();

			GL.PopMatrix();
		}

		[Shortcut("Anjin/Region Graph/Toggle Edit Mode")]
		public static void ToggleEditMode()
		{
			switch (Live.state)
			{
				case State.EditingGraph:
					if (Live.selectedObjects.Count >= 1)
					{
						switch (Live.selectedObjects[0])
						{
							case RegionPath path:
								Live.BeginEditingPath(path);
								break;

							case RegionShape2D shape:
								Live.BeginEditingShape(shape);
								break;
						}
					}

					break;

				case State.EditPath:
					Live.EndEditingPath();
					break;

				case State.EditShape:
					Live.EndEditingShape();
					break;
			}


		}

	}

	/// <summary>
	/// This will insure that the graph editor will reinitialize if it's been open and the editor reloads
	/// </summary>
	[InitializeOnLoad]
	public static class RegionGraphEditor_Initializer
	{
		static RegionGraphEditor_Initializer() {
			if (RegionGraphEditor.Live != null)
				EditorApplication.delayCall += RegionGraphEditor.Live.OnInit;
			else
				EditorApplication.delayCall += RegionGraphEditor.InitGraphEditor;
		}
	}
}


public static class z_Handles
{
	private static Stack<Color>     handleColorStack = new Stack<Color>();
	private static Stack<Matrix4x4> handlesMatrix    = new Stack<Matrix4x4>();

	public static void PushHandleColor()
	{
		handleColorStack.Push(Handles.color);
	}

	public static void PopHandleColor()
	{
		Handles.color = handleColorStack.Pop();
	}

	public static void PushMatrix()
	{
		handlesMatrix.Push(Handles.matrix);
	}

	public static void PopMatrix()
	{
		Handles.matrix = handlesMatrix.Pop();
	}
}