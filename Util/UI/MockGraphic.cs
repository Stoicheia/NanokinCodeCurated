using UnityEngine;
using UnityEngine.UI;

public class MockGraphic : Image, IMeshModifier
{
	public void ModifyMesh(Mesh mesh)
	{ }

	public void ModifyMesh(VertexHelper vh)
	{
		var r = GetPixelAdjustedRect();
		var v = new Vector4(r.x, r.y, r.x + r.width / 2f, r.y + r.height / 2f);

		Color32 color32 = color;
		vh.Clear();
		vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(0f, 0f));
		vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(0f, 1f));
		vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(1f, 1f));
		vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(1f, 0f));

		vh.AddTriangle(0, 1, 2);
		vh.AddTriangle(2, 3, 0);
	}
}