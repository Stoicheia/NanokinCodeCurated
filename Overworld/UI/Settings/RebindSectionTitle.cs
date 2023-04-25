using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RebindSectionTitle : MonoBehaviour
{
	public RectTransform RT;

	public TMP_Text Label;

	public void Set(string label)
	{
		Label.text = label;
	}
}
