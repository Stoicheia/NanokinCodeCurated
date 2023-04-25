using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Nanokin;
using Drawing;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using Util;

namespace Anjin.UI
{
	public enum Emote
	{
		None,
		Neutral,
		Happy,
		VeryHappy,
		Sad,
		VerySad,
		Angry,
		Confused,
		Aloof,
		Tired,
		Nauseous,
		Crying,
		Food,
		Drink,
		Music,
		Star,
		Rest,
		Coaster,
		Sword
	}

	public class EmotePopup : SerializedMonoBehaviour
	{
		public enum State { Off, Transition, On }

		public ComponentPool<EmotePopup> ParentPool;

		public HUDElement                Element;
		public Image                     UI_Image;
		//public Dictionary<Emote, Sprite> Sprites;

		public Emote CurrentEmote;

		public float Timer;

		public State state;

		public bool AutoReturn;

		private async void Start()
		{
			await GameHUD.InitTask;
			SetEmote(Emote.None);
			Timer = 0;
		}

		private void Update()
		{
			Profiler.BeginSample("EmotePopup Update");
			if (state >= State.Transition)
			{
				Profiler.BeginSample("Visibility Calculation");
				UpdateVisibility();
				Profiler.EndSample();

				if(state == State.On) {
					Timer += Time.deltaTime;
					if (Timer > 3) {
						state = State.Off;
						Timer = 0;
						if (AutoReturn) {
							ParentPool.ReturnSafe(this);
						}
					}
				}
			}
			Profiler.EndSample();
		}

		public void UpdateVisibility()
		{
			bool can_show =  GameController.Live.IsPlayerControlled                                 &&
							 ActorController.playerActor.activeBrain == ActorController.playerBrain &&
							 !SplicerHub.menuActive;

			var pos    = Element.WorldAnchor.Get();
			var camPos = GameCams.Live.UnityCam.transform.position;

			var ray = new Ray(camPos, (pos - camPos).normalized);
			var len = Vector3.Distance(camPos, pos);

			if (Physics.Raycast(ray, len, Layers.Walkable.mask)) {
				can_show = false;
			}

			Element.Alpha = can_show ? 1 : 0;
		}

		[Button]
		public void SetEmote(Emote emote)
		{
			CurrentEmote = emote;
			if (GameHUD.Live.EmoteSprites.TryGetValue(emote, out Sprite sprite))
			{
				UI_Image.gameObject.SetActive(true);
				UI_Image.sprite = sprite;
			}
			else
			{
				UI_Image.gameObject.SetActive(false);
				UI_Image.sprite = null;
			}
		}
	}
}