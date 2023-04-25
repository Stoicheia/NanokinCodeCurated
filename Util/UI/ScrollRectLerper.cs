using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollRectLerper : MonoBehaviour
{
	[SerializeField] private float scrollDuration = 0.075f;

	private bool isScrolling;

	private float target;

	private ScrollRect scrollRect;

	public void SetScrollDestination(float destination)
	{
		target = Mathf.Clamp(destination, 0, 1);
		isScrolling = true;
	}

	public void StopLerping()
	{
		isScrolling = false;
	}

	private void Awake()
	{
		scrollRect = GetComponent<ScrollRect>();
	}

	// Update is called once per frame
	private void Update()
    {
        if (isScrolling)
		{
			scrollRect.verticalNormalizedPosition = Mathf.Lerp(scrollRect.verticalNormalizedPosition, target, (Time.deltaTime / scrollDuration));

			if (scrollRect.verticalNormalizedPosition == target)
			{
				isScrolling = false;
			}
		}
    }
}
