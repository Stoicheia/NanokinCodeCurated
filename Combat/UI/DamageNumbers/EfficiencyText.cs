using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EfficiencyText : MonoBehaviour
{
	[SerializeField] private SpriteRenderer image;

	[SerializeField] private List<Sprite> graphics;

	public void SetLabelText(Data.Combat.ResistanceBracket bracket)
	{
		image.sprite = graphics[(int)bracket];
	}

	public void OnAnimationExit()
	{
		Destroy(gameObject);
	}
}
