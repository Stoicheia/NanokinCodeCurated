using System;
using System.Collections.Generic;
using Anjin.UI;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Combat.UI.Notifications
{
	public class CombatNotifyUI : StaticBoy<CombatNotifyUI>
	{
		public const float SKILL_USED_POPUP_DURATION           = 0.75f; //1.5f;
		public const float GENERAL_NOTIFICATION_POPUP_DURATION = 3f;

		public HUDElement SkillNamePopup;
		public HUDElement GeneralNotificationPopup;

		public TextMeshProUGUI[] TMP_SkillName;
		public TextMeshProUGUI   TMP_GeneralNotification;

		private void Start()
		{
			SkillNamePopup.gameObject.SetActive(true);
			GeneralNotificationPopup.gameObject.SetActive(true);

			SkillNamePopup.Alpha           = 0;
			GeneralNotificationPopup.Alpha = 0;
		}

		[Button, ShowInInspector]
		public static async UniTask DoSkillUsedPopup(string name, float duration = SKILL_USED_POPUP_DURATION)
		{
			if (!Exists) return;

			Live.TMP_SkillName.SetTextMulti(name);

			Live.SkillNamePopup.DoOffset(new Vector3(0, 20), new Vector3(0, 0), 0.15f);
			Live.SkillNamePopup.DoAlphaFade(0, 1, 0.15f);

			await UniTask.Delay(TimeSpan.FromSeconds(duration));

			Live.SkillNamePopup.DoOffset(new Vector3(0, 0), new Vector3(0, 20), 0.15f);
			Live.SkillNamePopup.DoAlphaFade(1, 0, 0.15f);
		}

		[Button, ShowInInspector]
		public static async UniTask DoGeneralNotificationPopup(string text, float duration = GENERAL_NOTIFICATION_POPUP_DURATION)
		{
			if (!Exists) return;

			if (string.IsNullOrEmpty(text)) return;

			if (Live.GeneralNotificationPopup.Alpha > 0) return;

			Live.TMP_GeneralNotification.text = text;

			Live.GeneralNotificationPopup.DoOffset(new Vector3(0, -75), new Vector3(0, 0), 0.25f);
			Live.GeneralNotificationPopup.DoAlphaFade(0, 1, 0.25f);

			await UniTask.Delay(TimeSpan.FromSeconds(duration));

			Live.GeneralNotificationPopup.DoOffset(new Vector3(0, 0), new Vector3(0, -75), 0.25f);
			Live.GeneralNotificationPopup.DoAlphaFade(1, 0, 0.25f);
		}
	}
}