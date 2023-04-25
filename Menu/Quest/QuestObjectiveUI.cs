using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Menu.Quest
{
	public class QuestObjectiveUI : SerializedMonoBehaviour
	{
		public TextMeshProUGUI TMP_Label;
		public Transform       Root_Quota;
		public Transform       Root_Checkbox;
		public TextMeshProUGUI TMP_QuotaMin;
		public TextMeshProUGUI TMP_QuotaMax;

		public Color Col_Active   = Color.white;
		public Color Col_Complete = Color.grey;

		[NonSerialized] public RectTransform rect;

		private void Awake()
		{
			rect = GetComponent<RectTransform>();
		}
	}
}