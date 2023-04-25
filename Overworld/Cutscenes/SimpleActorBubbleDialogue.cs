using System.Collections.Generic;
using Anjin.Actors;
using Anjin.UI;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Overworld.Cutscenes
{
	public class SimpleActorBubbleDialogue : SerializedMonoBehaviour
	{
		//void OnAddLine() { text.Add(new LocalizedText()); }
		//[ListDrawerSettings(CustomAddFunction = "OnAddLine")]
		public List<string> text;
		public SpeechBubble Bubble;
		[FormerlySerializedAs("textTest")]
		public List<GameText> Lines;

		public bool activated;
		[Min(1)]
		public float range = 3;

		// Start is called before the first frame update
		void Start()
		{
			activated = false;

			//Spawn bubble
			var b = GameHUD.SpawnSpeechBubble();

			Bubble = b;
			Bubble.SetLines(Lines);

			Bubble.hudElement.SetPositionModeWorldPoint(new WorldPoint(transform.position), Vector3.up * 1.5f);
			//Bubble.hudElement.EnableDistanceFade(range,range+1);
		}

		private void Update()
		{
			var player = ActorController.playerActor;
			if (player != null)
			{
				var withinRange = player.transform.position.Distance(transform.position) <= range;

				if (!activated)
				{
					//If the player is close enough and the object is in view, ACTIVATE!
					if (withinRange)
						Activate();
				}
				else
				{
					if (!withinRange)
					{
						Deactivate();
					}
				}
			}
			else Deactivate();
		}

		public void Activate()
		{
			activated = true;
			Bubble.StartActivation();
		}

		public void Deactivate()
		{
			activated = false;
			Bubble.StartDeactivation(true);
		}
	}
}