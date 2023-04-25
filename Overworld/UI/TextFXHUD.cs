using System;
using System.Collections.Generic;
using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;

namespace Overworld.UI
{
	[LuaUserdata(staticAuto: true)]
	public class TextFXHUD : StaticBoy<TextFXHUD>
	{
		public Transform UI_Root;

		public ComponentPool<CountdownHUDLabel> Pool_DefaultNumbers;
		public ComponentPool<CountdownHUDLabel> Pool_DefaultText;

		public CountdownHUDLabel Prefab_DefaultNumber;
		public CountdownHUDLabel Prefab_DefaultText;

		public List<CountdownHUDLabel> Active;

		public override void Awake()
		{
			base.Awake();

			Active = new List<CountdownHUDLabel>();

			Pool_DefaultNumbers              = new ComponentPool<CountdownHUDLabel>(UI_Root, Prefab_DefaultNumber);
			Pool_DefaultNumbers.onAllocating = anim => anim.gameObject.SetActive(false);
			Pool_DefaultNumbers.AllocateAdd(5);

			Pool_DefaultText              = new ComponentPool<CountdownHUDLabel>(UI_Root, Prefab_DefaultText);
			Pool_DefaultText.onAllocating = anim => anim.gameObject.SetActive(false);
			Pool_DefaultText.AllocateAdd(5);
		}

		private void Update()
		{
			if (Active.Count > 0)
			{
				for (int i = Active.Count - 1; i >= 0; i--)
				{
					if (!Active[i].IsPlaying)
					{
						Pool_DefaultNumbers.ReturnSafe(Active[i]);
						Pool_DefaultText.ReturnSafe(Active[i]);
						Active.RemoveAt(i);
					}
				}
			}
		}

		public bool AnyActive()    => Active.Count > 0;
		public bool NotAnyActive() => Active.Count == 0;

		[Button]
		public void ShowNumber(int i, CountdownHUDLabel.Animations anim = CountdownHUDLabel.Animations.None)
		{
			CountdownHUDLabel number = Pool_DefaultNumbers.Rent();

			number.DoAnimation(anim, 1);
			number.Text.text = i.ToString();

			Active.Add(number);
		}

		[Button]
		public void ShowText(string message)
		{
			var text = Pool_DefaultText.Rent();

			text.DoAnimation();
			text.Text.text = message;

			Active.Add(text);
		}

		[Button]
		public static async UniTask DefaultCountdown(float delayInSeconds = 1, bool waitTillInactive = false)
		{
			Live.ShowNumber(3);
			await UniTask.Delay(TimeSpan.FromSeconds(delayInSeconds));
			Live.ShowNumber(2);
			await UniTask.Delay(TimeSpan.FromSeconds(delayInSeconds));
			Live.ShowNumber(1);
			await UniTask.Delay(TimeSpan.FromSeconds(delayInSeconds));
			Live.ShowText("Go!");

			if (waitTillInactive)
				await UniTask.WaitUntil(Live.NotAnyActive);
		}

		public static WaitableUniTask default_countdown(float delayInSeconds = 1, bool WaitTillInactive = false)
		{
			var task = DefaultCountdown(delayInSeconds, WaitTillInactive);
			return new WaitableUniTask(task);
		}
	}
}