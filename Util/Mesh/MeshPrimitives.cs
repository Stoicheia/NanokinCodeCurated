using UnityEngine;
using Util.RenderingElements.TriangleMenu;

namespace Util
{
	public static class MeshPrimitives
	{
		private static Mesh _quad;
		private static Mesh _sphere;
		private static Mesh _invertedSphere;

		public static Mesh Quad
		{
			get
			{
				if (_quad == null) _quad = CreateQuad();
				return _quad;
			}
		}

		public static Mesh Sphere
		{
			get
			{
				if (_sphere == null) _sphere = CreateSphere(25, 50);
				return _sphere;
			}
		}

		public static Mesh InvertedSphere
		{
			get
			{
				if (_invertedSphere == null) _invertedSphere = CreateInvertedSphere(25, 50);
				return _invertedSphere;
			}
		}


		private static Mesh CreateInvertedSphere(int rows, int cols)
		{
			Mesh       sphere = CreateSphere(rows, cols);
			MeshEditor editor = new MeshEditor(sphere);
			editor.InvertNormals();
			editor.SaveNormals();
			return sphere;
		}

		public static Mesh CreateQuad()
		{
			return new Mesh
			{
				vertices = new[]
				{
					Vector3.right + Vector3.down, // DR
					Vector3.left + Vector3.down,  // DL
					Vector3.left + Vector3.up,    // UL
					Vector3.right + Vector3.up    // UR
				},
				triangles = new[]
				{
					0, 1, 2,
					0, 2, 3
				},
				uv = new[]
				{
					Vector2.right,
					Vector2.zero,
					Vector2.up,
					Vector2.up + Vector2.right
				},
				normals = new[]
				{
					-Vector3.forward,
					-Vector3.forward,
					-Vector3.forward,
					-Vector3.forward
				},
				colors = new[]
				{
					Color.white,
					Color.white,
					Color.white,
					Color.white
				}
			};
		}

		private static Mesh CreateSphere(int rows, int cols, float w = 1, float h = 1)
		{
			var mesh = new Mesh();

			var vertices  = new Vector3[(rows + 1) * (cols + 1)];
			var uv        = new Vector2[(rows + 1) * (cols + 1)];
			var normals   = new Vector3[(rows + 1) * (cols + 1)];
			var triangles = new int[6 * rows * cols];

			for (var i = 0; i < vertices.Length; i++)
			{
				float x     = i % (cols + 1);
				float y     = i / (cols + 1);
				float x_pos = x / cols * w;
				float y_pos = y / rows * h;
				vertices[i] = new Vector3(x_pos, y_pos, 0);
				float u = x / cols;
				float v = y / rows;
				uv[i] = new Vector2(u, v);
			}

			for (int i = 0; i < 2 * rows * cols; i++)
			{
				int[] triIndex = new int[3];
				if (i % 2 == 0)
				{
					triIndex[0] = i / 2 + i / (2 * cols);
					triIndex[1] = triIndex[0] + 1;
					triIndex[2] = triIndex[0] + cols + 1;
				}
				else
				{
					triIndex[0] = (i + 1) / 2 + i / (2 * cols);
					triIndex[1] = triIndex[0] + cols + 1;
					triIndex[2] = triIndex[1] - 1;
				}

				triangles[i * 3]     = triIndex[0];
				triangles[i * 3 + 1] = triIndex[1];
				triangles[i * 3 + 2] = triIndex[2];
			}

			mesh.vertices  = vertices;
			mesh.normals   = normals;
			mesh.uv        = uv;
			mesh.triangles = triangles;

			return mesh;
		}
	}
}