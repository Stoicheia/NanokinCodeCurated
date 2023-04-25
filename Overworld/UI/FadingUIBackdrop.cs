using UnityEngine;

public class FadingUIBackdrop : MonoBehaviour {

	public Color fadeColor;
	public Color currentColor;

	float currentAlpha;

	[Range(0,1)]
	public float targetAlpha;

	[Range(0, 1)]
	public float startLerpValue;

	[Range(0, 1)]
	public float endLerpValue;

	public GameObject controller;

	private new MeshRenderer renderer;

	public int state = 0;

	// Use this for initialization
	void Start () {
		renderer                            = GetComponent<MeshRenderer>();
		renderer.sharedMaterial             = new Material(Shader.Find("Sprites/Default"));
		renderer.sharedMaterial.renderQueue = 2000;
		currentColor                        = fadeColor;
		currentColor.a                      = 0;

		renderer.sharedMaterial.color = currentColor;
	}

	// Update is called once per frame
	void Update () {
		switch(state)
		{
			//Startup
			case 0:
			{
				if (((currentAlpha + 0.001f) < targetAlpha))
				{
					currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, startLerpValue);
				}
				else
				{
					state = 1;
				}
			}
				break;

			//Running
			case 1:
			{
				currentAlpha = targetAlpha;

				if (controller == null)
				{
					state = 2;
				}
			}
				break;

			//Shutdown
			case 2:
			{
				if (((currentAlpha - 0.001f) > 0))
				{
					currentAlpha = Mathf.Lerp(currentAlpha, 0, endLerpValue);
				}
				else
				{
					DestroyImmediate(gameObject,false);
					return;
				}
			}
				break;
		}

		currentColor.a = currentAlpha;

		renderer.sharedMaterial.color = currentColor;
	}
}