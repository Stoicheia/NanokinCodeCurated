using Anjin.UI;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OverworldNotifyUI : StaticBoy<OverworldNotifyUI>
{
	public const float GENERAL_NOTIFICATION_POPUP_DURATION = 3f;

	public HUDElement GeneralNotificationPopup;

	public TextMeshProUGUI TMP_GeneralNotification;

	// Start is called before the first frame update
	void Start()
    {
		GeneralNotificationPopup.gameObject.SetActive(false);

		GeneralNotificationPopup.Alpha = 0;
	}

	public static async UniTask DoGeneralNotificationPopup(string text, float duration = GENERAL_NOTIFICATION_POPUP_DURATION)
	{
		if (!Exists) return;

		if (string.IsNullOrEmpty(text)) return;

		if (Live.GeneralNotificationPopup.Alpha > 0)
		{
			if (Live.GeneralNotificationPopup.gameObject.activeSelf)
			{
				return;
			}
			else
			{
				Live.GeneralNotificationPopup.Alpha = 0;
			}
		}

		Live.TMP_GeneralNotification.text = text;

		Live.GeneralNotificationPopup.gameObject.SetActive(true);

		Live.GeneralNotificationPopup.DoOffset(new Vector3(0, -75), new Vector3(0, 0), 0.25f);
		Live.GeneralNotificationPopup.DoAlphaFade(0, 1, 0.25f);

		await UniTask.Delay(System.TimeSpan.FromSeconds(duration));

		Live.GeneralNotificationPopup.DoOffset(new Vector3(0, 0), new Vector3(0, -75), 0.25f);
		Live.GeneralNotificationPopup.DoAlphaFade(1, 0, 0.25f);
	}

	public static void InstantlyHideNotificationPopup()
	{
		Live.GeneralNotificationPopup.gameObject.SetActive(false);
	}
}
