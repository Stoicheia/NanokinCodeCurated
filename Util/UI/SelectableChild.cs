using UnityEngine;

public class SelectableChild : MonoBehaviour
{
	private void Awake()
	{
		SelectableRoot root = GetComponentInParent<SelectableRoot>();
		if (root == null)
		{
			this.LogError("Is a SelectableChild, but no SelectableRoot could be found in the hierarchy above.");
			return;
		}

		if (TryGetComponent(out CanvasRenderer crenderer))
			root.AddChild(crenderer);
		else
			this.LogError("Is a SelectableChild, but no CanvasRenderer which is needed!");
	}
}