using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Anjin.Nanokin.Map
{
	public class CollectGainUI : SerializedMonoBehaviour
	{
		[SerializeField] public TextMeshProUGUI GainHP;
		[SerializeField] public TextMeshProUGUI GainXP;
		[SerializeField] public TextMeshProUGUI GainCredit;

		[NonSerialized] public RectTransform rectTransform;

		private void Awake()
		{
			gameObject.SetActive(false);
		}

		private void Start()
		{
			rectTransform = GetComponent<RectTransform>();
		}

		public void Hide()
		{
			gameObject.SetActive(false);
		}

		public void Show()
		{
			gameObject.SetActive(true);
		}

	}
}