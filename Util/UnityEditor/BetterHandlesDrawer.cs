#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Util.Editor
{
// 	[AddComponentMenu("")]
// 	[ExecuteInEditMode]
// 	public class BetterHandlesDrawer : MonoBehaviour
// 	{
// 		#if UNITY_EDITOR
// 		public Material GLDrawingMaterial;
//
// 		public void InitMaterials()
// 		{
// 			GLDrawingMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/Editor/BetterHandles.mat");
// 			GLDrawingMaterial.hideFlags = HideFlags.DontSave;
//
// 			/*// Unity has a built-in shader that is useful for drawing
// 			// simple colored things.
// 			Shader shader = Shader.Find("Hidden/Internal-Colored");
// 			GLDrawingMaterial = new Material(shader) {hideFlags = HideFlags.HideAndDontSave };
//
// 			// Turn on alpha blending
// 			GLDrawingMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
// 			GLDrawingMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
// 			// Turn backface culling off
// 			GLDrawingMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
// 			// Turn off depth writes
// 			//GLDrawingMaterial.SetInt("_ZWrite", 0);*/
// 		}
//
// 		void OnEnable()
// 		{
// 			InitMaterials();
// 		}
//
// 		/*void OnWillRenderObject()
// 		{
// 			GLDrawRectangle(new []{new Vector3(0, 0, 0),new Vector3(0, 1, 0),new Vector3(0, 1, 1),new Vector3(0, 0, 1) }, transform.localToWorldMatrix);
// 		}*/
//
//
//
// 		public void GLDrawRectangle(Vector3[] verts, Matrix4x4 matrix)
// 		{
// 			if (!GLDrawingMaterial) return;
//
// 			GL.PushMatrix();
// 			GL.MultMatrix(matrix);
//
// 			//GLDrawingMaterial.SetInt("_HandleZTest", (int) CompareFunction.GreaterEqual);
// 			//GLDrawingMaterial.SetPass(0);
//
// 			Color c = new Color(0.0f, 0.8f, 0.0f, 0.5f);
// 			GL.Begin(4);
// 			for (int index = 0; index < 2; ++index)
// 			{
// 				GL.Color(c);
// 				GL.Vertex(verts[index * 2]);
// 				GL.Vertex(verts[index * 2 + 1]);
// 				GL.Vertex(verts[(index * 2 + 2) % 4]);
// 				GL.Vertex(verts[index           * 2]);
// 				GL.Vertex(verts[(index * 2 + 2) % 4]);
// 				GL.Vertex(verts[index * 2 + 1]);
// 			}
// 			GL.End();
//
// 			GL.PopMatrix();
// 		}
// 		#endif
//
// 	}


}