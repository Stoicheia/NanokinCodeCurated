using TMPro;
using UnityEngine;


[ExecuteInEditMode]
public class UIStatBar : MonoBehaviour
{
	public RectTransform travel;
	public RectTransform innerBar;

	public TextMeshProUGUI statText;

	public float value;
	public float valueMax;
	public float valueMin;

	private float prevValue;

	private void OnEnable()
	{
		UpdateBar();
		prevValue = value;
	}

	// Update is called once per frame
	void Update () {

		//TODO: Figure out why this doesn't update the bar size properly on game start.
		/*if (prevValue != value)
		{*/
			UpdateBar();
			//prevValue = value;
		//}
	}

	public void UpdateBar()
	{
		//TODO: Figure out handling lerp animation for this, disabling for now.
		innerBar.sizeDelta = new Vector2(
			Mathf.Lerp(innerBar.sizeDelta.x,

				travel.rect.width /
				Mathf.Max(1,
					Mathf.Abs(valueMax - valueMin)
				) * value

				,1),
			innerBar.sizeDelta.y);

		if (statText)
		{
			statText.text = $"{value}/{valueMax}";
		}
	}

	public void SetValues(float _value, float _min, float _max)
	{
		value 	 = _value;
		valueMin = _min;
		valueMax = _max;

	}
}
