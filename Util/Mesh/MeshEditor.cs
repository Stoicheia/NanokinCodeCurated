using System;
using System.Collections.Generic;
using UnityEngine;

namespace Util.RenderingElements.TriangleMenu
{
	public struct MeshEditor
	{
		private static readonly List<Vector3> BufferV3  = new List<Vector3>();
		private static readonly List<Color>   BufferCol = new List<Color>();

		public readonly Mesh mesh;

		public MeshEditor(Mesh mesh)
		{
			this.mesh = mesh;
		}

		public List<Vector3> Vertices
		{
			get
			{
				mesh.GetVertices(BufferV3);
				return BufferV3;
			}
		}

		public List<Vector3> Normals
		{
			get
			{
				mesh.GetNormals(BufferV3);
				return BufferV3;
			}
		}


		public List<int> Triangles
		{
			set => throw new NotImplementedException();
		}

		public List<Color> Col
		{
			get
			{
				mesh.GetColors(BufferCol);
				return BufferCol;
			}
		}

		public void InvertNormals()
		{
			for (var index = 0; index < Normals.Count; index++)
			{
				Normals[index] = -Normals[index];
			}
		}

		public void Save()
		{
			SaveVertices();
			SaveColors();
		}

		public void SaveVertices()
		{
			mesh.SetVertices(BufferV3);
		}

		public void SaveNormals()
		{
			mesh.SetNormals(BufferV3);
		}


		public void SaveColors()
		{
			mesh.SetColors(BufferCol);
		}
	}
}