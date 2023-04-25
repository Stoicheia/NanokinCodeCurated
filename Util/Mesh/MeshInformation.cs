using System.Collections.Generic;
using Anjin.Util;
using UnityEngine;
using UnityEngine.Rendering;

namespace Util.RenderingElements.TriangleMenu
{
	/// <summary>
	/// A utility data class used to group useful mesh information and/or components.
	/// Allows efficient modification of the vertex and color attributes.
	/// </summary>
	public struct MeshInformation
	{
		public readonly  MeshRenderer renderer;
		public readonly  Transform    transform;
		private readonly Mesh         _mesh;

		private static readonly List<Vector3> BufferPositions = new List<Vector3>();
		private static readonly List<Color>   BufferColors    = new List<Color>();

		public MeshInformation(MeshFilter filter, MeshRenderer renderer)
		{
			transform     = filter.transform;
			_mesh         = filter.sharedMesh;
			this.renderer = renderer;
		}

		public MeshInformation(GameObject view)
		{
			transform = view.transform;
			renderer  = view.GetComponentInChildren<MeshRenderer>();
			_mesh     = view.GetComponent<MeshFilter>().sharedMesh;
		}

		public List<Vector3> Vertices
		{
			get
			{
				_mesh.GetVertices(BufferPositions);
				return BufferPositions;
			}
		}

		public List<Color> Colors
		{
			get
			{
				_mesh.GetColors(BufferColors);
				return BufferColors;
			}
		}

		public void Save()
		{
			SaveVertices();
			SaveColors();
		}

		public void SaveVertices()
		{
			_mesh.SetVertices(BufferPositions);
		}

		public void SaveColors()
		{
			_mesh.SetColors(BufferColors);
		}

		public static MeshInformation CreateChildObject(GameObject parentGameObject, Mesh mesh, Material material = null, string name = null)
		{
			GameObject go = new GameObject(name ?? "Procedural Mesh");
			go.transform.SetParent(parentGameObject.transform, false);
			go.layer     = parentGameObject.layer;
			go.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

			MeshFilter   filter   = go.AddComponent<MeshFilter>();
			MeshRenderer renderer = go.AddComponent<MeshRenderer>();

			filter.sharedMesh             = mesh;
			renderer.material             = material;
			renderer.lightProbeUsage      = LightProbeUsage.Off;
			renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

			MeshInformation info = new MeshInformation(filter, renderer);

			return info;
		}

		public void CleanUp()
		{
			if (transform)
				transform.Destroy();
		}
	}
}