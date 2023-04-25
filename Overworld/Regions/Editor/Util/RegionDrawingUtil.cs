using Drawing;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Anjin.Regions
{
	public static class RegionDrawingUtil
	{
		public static bool     Initalized;
		public static Material Material;

		static Mesh QuadMesh;
		static Mesh DiskMesh;


		//	QUAD
		//------------------------------------------------------

		static Vector3[] QuadVerts = {
			new Vector3(0, 0, 0),
			new Vector3(1, 0, 0),
			new Vector3(0, 1, 0),
			new Vector3(1, 1, 0),
		};

		static int[]     QuadTris    = { 0, 2, 1, 3, 2, 1 };
		static Vector3[] QuadNormals = { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward };


		const int DISK_VERT_COUNT = 64;

		static Vector2[] QuadUVs = {
			new Vector2(0, 0),
			new Vector2(1, 0),
			new Vector2(0, 1),
			new Vector2(1, 1),
		};


		//	DISK
		//------------------------------------------------------

		static Vector3[] DiskVerts;
		static int[]     DiskTris;

		public static void Init()
		{
			if (Initalized) return;
			Initalized = true;

			Material = Resources.Load<Material>("aline_surface");
			Material.DisableKeyword("UNITY_HDRP");

			Debug.Log("RegionDrawingUtil Init: " + Material);

			QuadMesh = new Mesh();
			QuadMesh.SetVertices(QuadVerts);
			QuadMesh.SetTriangles(QuadTris, 0);
			QuadMesh.SetNormals(QuadNormals);
			QuadMesh.SetUVs(0, QuadUVs);

			DiskVerts = new Vector3[DISK_VERT_COUNT + 1];
			DiskTris  = new int[DISK_VERT_COUNT * 3];


			DiskVerts[0] = new Vector3(0, 0, 0);

			float r = ( ( 1 / DISK_VERT_COUNT ) * 360 ) * Mathf.Deg2Rad;
			DiskVerts[1] = new Vector3(Mathf.Sin(r), 0, Mathf.Cos(r));

			r            = ( ( 2 / DISK_VERT_COUNT ) * 360 ) * Mathf.Deg2Rad;
			DiskVerts[2] = new Vector3(Mathf.Sin(r), 0, Mathf.Cos(r));

			DiskTris[0] = 0;
			DiskTris[1] = 1;
			DiskTris[2] = 2;

			for (int i = 0; i < DISK_VERT_COUNT; i++) {
				r = ( ( (float)i / DISK_VERT_COUNT ) * 360 ) * Mathf.Deg2Rad;

				DiskVerts[i + 1] = new Vector3(Mathf.Sin(r), 0, Mathf.Cos(r));

				if (i > 1) {
					var j = (i - 2) * 3;
					DiskTris[j + 0] = 0;
					DiskTris[j + 1] = i - 1;
					DiskTris[j + 2] = i;
				}
			}

			DiskMesh = new Mesh();
			DiskMesh.SetVertices(DiskVerts);
			DiskMesh.SetTriangles(DiskTris, 0);
			DiskMesh.RecalculateBounds();
		}

		/*public void GLDrawRectangle(Vector3 position, Quaternion rotation, Vector2 size)
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
			Init();
			if (!Material) return;

			GL.PushMatrix();
			GL.MultMatrix(matrix);

			//GLDrawingMaterial.SetInt("_HandleZTest", (int) CompareFunction.GreaterEqual);
			Material.SetPass(0);

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
		}*/

		public static void DrawCirlce(Vector3 position, float size, Quaternion rotation, Matrix4x4 matrix, Color fill, Color outline)
		{
			if (Event.current.type != EventType.Repaint) return;
			Init();
			Handles.matrix = matrix;
			Handles.color  = fill;
			Handles.DrawSolidDisc(position, rotation * Vector3.up, size);

			using (Draw.WithMatrix(matrix)){
				Draw.Circle(position,rotation * Vector3.up, size, outline);
			}
		}

		public static void DrawRectangle(Vector3 position, Vector2 size, Quaternion rotation, Matrix4x4 matrix, Color fill, Color outline)
		{
			if (Event.current.type != EventType.Repaint) return;
			Init();
			using(Draw.WithMatrix(matrix)) {
				Draw.SolidPlane(position, rotation, size * 2, fill);
				Draw.WirePlane(position, rotation, size * 2, outline);
			}
		}

		public static void DrawBox(Vector3 position, Vector3 size, Quaternion rotation, Matrix4x4 matrix, Color fill, Color outline)
		{
			if (Event.current.type != EventType.Repaint) return;
			Init();
			using (Draw.WithMatrix(matrix)) {
				Draw.SolidBox(position, rotation, size * 2, fill);
				Draw.WireBox(position, rotation, size * 2, outline);
			}

			/*Init();
			var w = size.x;
			var h = size.y;
			var d = size.z;
			Handles.DrawSolidRectangleWithOutline(new [] { new Vector3(-w, -h, -d), new Vector3(-w,  -h, d), new Vector3(w,   -h, d), new Vector3(w,  -h, -d) }, fill, Color.black);  //-Y
			Handles.DrawSolidRectangleWithOutline(new [] { new Vector3(-w, h,  -d),  new Vector3(-w, h,  d),  new Vector3(w,  h,  d),  new Vector3(w, h,  -d) }, fill, Color.black ); //+Y
			Handles.DrawSolidRectangleWithOutline(new [] { new Vector3(-w, -h, -d), new Vector3(-w,  h,  -d), new Vector3(-w, h,  d), new Vector3(-w, -h, d) },  fill, Color.black ); //-X

			Handles.DrawSolidRectangleWithOutline(new [] { new Vector3(w,  -h, -d),  new Vector3(w, h, -d),  new Vector3(w, h, d),  new Vector3(w, -h, d) },  fill, Color.black ); //+X
			Handles.DrawSolidRectangleWithOutline(new [] { new Vector3(-w, -h, -d), new Vector3(-w, h, -d), new Vector3(w,  h, -d), new Vector3(w, -h, -d) }, fill, Color.black ); //-Z
			Handles.DrawSolidRectangleWithOutline(new [] { new Vector3(-w, -h, d),  new Vector3(-w, h, d),  new Vector3(w,  h, d),  new Vector3(w, -h, d) },  fill, Color.black ); //+Z*/



			//Sides
			//GLDrawLine(new Vector3(-w, -h, -d), new Vector3(-w, h, -d), matrix, Color.black);
			//GLDrawLine(new Vector3( w, -h, -d), new Vector3( w, h, -d), matrix, Color.black);
			//GLDrawLine(new Vector3(-w, -h,  d), new Vector3(-w, h,  d), matrix, Color.black);
			//GLDrawLine(new Vector3( w, -h,  d), new Vector3( w, h,  d), matrix, Color.black);
		}
	}
}