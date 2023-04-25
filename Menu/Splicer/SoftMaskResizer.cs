using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoftMaskResizer : MonoBehaviour
{
	private int currentScreenWidth;
	private int currentScreenHeight;

	private Image maskImage;

    // Start is called before the first frame update
    void Awake()
    {
		maskImage = GetComponent<Image>();

		if (Screen.fullScreenMode == FullScreenMode.Windowed)
		{
			currentScreenWidth = Screen.width;
			currentScreenHeight = Screen.height;
		}
		else
		{
			currentScreenWidth = Screen.currentResolution.width;
			currentScreenHeight = Screen.currentResolution.height;
		}
	}

    // Update is called once per frame
    void Update()
    {
		if (Screen.fullScreenMode == FullScreenMode.Windowed)
		{
			if ((currentScreenWidth != Screen.width) || (currentScreenHeight != Screen.height))
			{
				currentScreenWidth = Screen.width;
				currentScreenHeight = Screen.height;

				ToggleMaskImage();
			}
		}
		else
		{
			if ((currentScreenWidth != Screen.currentResolution.width) || (currentScreenHeight != Screen.currentResolution.height))
			{
				currentScreenWidth = Screen.currentResolution.width;
				currentScreenHeight = Screen.currentResolution.height;

				ToggleMaskImage();
			}
		}
	}

	//This is a hack to fix the soft mask issue that hides the portrait when the screen size changes
	public async UniTask ToggleMaskImage()
	{
		maskImage.enabled = false;
		await UniTask.DelayFrame(3);
		maskImage.enabled = true;
	}
}
