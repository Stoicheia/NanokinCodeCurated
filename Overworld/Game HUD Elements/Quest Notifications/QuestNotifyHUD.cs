using System;
using System.Collections.Generic;
using Anjin.Nanokin;
using Anjin.Scripting;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.UI
{
	public enum QuestNotificationType
	{
		Discovered,
		ObjectiveUpdated,
		Finished
	}

	public struct QuestNotification
	{
		public QuestNotificationType type;
		public Quests.LoadedQuest    quest;
		public Quests.Objective      objective;
	}

	[LuaUserdata]
	public class QuestNotifyHUD : StaticBoy<QuestNotifyHUD>
	{
		public const float DEFAULT_DELAY = 0.75f;

		public enum State { Idle, Pump, Showing, Delay }

		// References
		public QuestStartedNotification Popup_QuestStarted;

		// Notification mechanism
		[ShowInPlay, NonSerialized] public List<QuestNotification> Notifications;
		[ShowInPlay, NonSerialized] public ICoroutineWaitable      CurrentWaiting;
		[ShowInPlay, NonSerialized] public State                   state;
		[ShowInPlay, NonSerialized] public float                   Timer;
		[ShowInPlay, NonSerialized] public bool                    Forcing;

		protected override void OnAwake()
		{
			state         = State.Idle;
			Timer         = 0;
			Forcing       = false;
			Notifications = new List<QuestNotification>();
			Popup_QuestStarted.gameObject.SetActive(true);
		}

		private void Update()
		{
			if (GameController.IsWorldPaused) return;
			UpdateCycle();
		}

		void UpdateCycle()
		{
			switch (state)
			{
				case State.Idle:
					if (Notifications.Count == 0 && Forcing)
						Forcing = false;

					if (canShowNotifications() && Notifications.Count > 0)
						state = State.Pump;

					break;

				case State.Pump:

					if (Notifications.Count == 0 || !canShowNotifications())
					{
						state   = State.Idle;
						Forcing = false;
						break;
					}

					QuestNotification notification = Notifications[0];
					Notifications.RemoveAt(0);

					switch (notification.type)
					{
						case QuestNotificationType.Discovered:
							CurrentWaiting = doQuestStarted(notification);
							break;

						case QuestNotificationType.ObjectiveUpdated:
							break;

						case QuestNotificationType.Finished:
							break;
					}

					if (CurrentWaiting != null)
					{
						state = State.Showing;
					}

					break;

				case State.Showing:
					if (CurrentWaiting == null || CurrentWaiting.CanContinue(false))
					{
						if (Notifications.Count > 0)
						{
							state = State.Delay;
							Timer = DEFAULT_DELAY;
						}
						else
						{
							state = State.Pump;
						}
					}

					break;

				case State.Delay:
					Timer -= Time.deltaTime;
					if (Timer <= 0)
						state = State.Pump;
					break;
			}
		}

		bool canShowNotifications() => GameController.Live.IsPlayerControlled || Forcing;

		ICoroutineWaitable doQuestStarted(QuestNotification notification)
		{
			Popup_QuestStarted.Show(notification.quest);
			return Popup_QuestStarted;
		}

		// API
		//----------------------------------------------

		/// <summary>
		/// Forces all waiting notifications to be pushed to the screen
		/// </summary>
		[LuaGlobalFunc("quest_show_notifications")]
		[Button]
		public static ICoroutineWaitable ForcePushAllWaiting()
		{
			Live.Forcing = true;
			Live.UpdateCycle();
			return new Waitable();
		}

		public void NotifyStarted(Quests.LoadedQuest quest)
		{
			Notifications.Add(new QuestNotification
			{
				type  = QuestNotificationType.Discovered,
				quest = quest,
			});
			UpdateCycle();
		}

		//----------------------------------------------

		public class Waitable : ICoroutineWaitable
		{
			public bool CanContinue(bool justYielded, bool isCatchup) => Live.state == State.Idle;
		}
	}
}