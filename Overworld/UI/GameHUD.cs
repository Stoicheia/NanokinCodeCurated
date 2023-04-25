using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Util;
using Util.Addressable;
using Util.Odin.Attributes;

namespace Anjin.UI
{
	[LuaUserdata(StaticName = "HUD")]
	public class GameHUD : StaticBoy<GameHUD>
	{
		public Vector2          TargetScreenRes;
		public List<HUDElement> Elements;
		public RectTransform    ElementsRect;

		public RectTransform BubblePoolRoot;
		public RectTransform SpritePoolRoot;
		public RectTransform EmotePoolRoot;

		public RectTransform DebugCrosshairs;
		public RectTransform InteractionHUD;
		public HUDElement    LevelLoadingHUD;

		public int NumSpeechBubbles;
		public int NumSpritePopups;
		public int NumEmotePopups;

		[NonSerialized]
		public ChoiceBubble choiceBubble;
		[NonSerialized]
		public ComponentPool<HUDBubble> bubblePool;
		[NonSerialized]
		public ComponentPool<SpritePopup> spritePopupPool;
		[NonSerialized]
		public ComponentPool<EmotePopup> emotePopupPool;
		[NonSerialized]
		public bool showingInteract;

		[NonSerialized, ShowInPlay]
		public ScriptedHUDElement interactPopupOLD;

		[NonSerialized, ShowInPlay]
		public InteractPopup interactPopup;


		[NonSerialized, ShowInPlay]
		public Dictionary<Emote, Sprite> EmoteSprites;

		private bool _showLoadingHUD = false;
		private bool _loaded;

		private AsyncOperationHandle<GameObject> _hndChoiceBubble;
		private AsyncOperationHandle<GameObject> _hndInteractPopup;

		public static AsyncLazy InitTask;

		private void Start()
		{
			InitTask = Init().ToAsyncLazy();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void StaticInit()
		{
			InitTask = null;
		}

		public override void Reset()
		{
			base.Reset();
			InitTask = null;
		}

		private async UniTask Init()
		{
			bubblePool = new ComponentPool<HUDBubble>(BubblePoolRoot, GameAssets.Live.SpeechBubblePrefab)
			{
				initSize     = GameOptions.current.load_on_demand ? 0 : NumSpeechBubbles,
				allocateTemp = true
			};

			spritePopupPool = new ComponentPool<SpritePopup>(SpritePoolRoot, GameAssets.Live.SpritePopupPrefab)
			{
				initSize     = GameOptions.current.load_on_demand ? 0 : NumSpritePopups,
				allocateTemp = true
			};

			emotePopupPool = new ComponentPool<EmotePopup>(EmotePoolRoot, GameAssets.Live.EmotePopupPrefab)
			{
				initSize     = GameOptions.current.load_on_demand ? 0 : NumEmotePopups,
				allocateTemp = true
			};

			await Addressables.InitializeAsync();


			_hndChoiceBubble  = await Addressables2.LoadHandleAsync<GameObject>("UI/Choice Bubble");
			_hndInteractPopup = await Addressables2.LoadHandleAsync<GameObject>("UI/InteractPopup");

			// Choice bubble
			choiceBubble = Instantiate(_hndChoiceBubble.Result, BubblePoolRoot).GetComponent<ChoiceBubble>();
			choiceBubble.gameObject.SetActive(true);

			// Interact popup
			/*interactPopupOLD = Instantiate(_hndInteractPopup.Result, InteractionHUD).GetComponent<ScriptedHUDElement>();
			interactPopupOLD.gameObject.SetActive(true);
			interactPopupOLD.HudElement.Alpha                                  = 0;*/

			interactPopup = Instantiate(_hndInteractPopup.Result, InteractionHUD).GetComponent<InteractPopup>();
			interactPopup.gameObject.SetActive(true);

			/*foreach (InputButtonLabel label in interactPopupOLD.GetComponentsInChildren<InputButtonLabel>()) {
				label.Button = GameInputs.interact;
			}*/

			// Emotes
			EmoteSprites = new Dictionary<Emote, Sprite>();
			UniTaskBatch batch = UniTask2.Batch();

			LoadEmote(Emote.Neutral,   "Sprites/Emotes[peep_emoji_neutral]").Batch(batch);
			LoadEmote(Emote.Happy,     "Sprites/Emotes[peep_emoji_happy]").Batch(batch);
			LoadEmote(Emote.VeryHappy, "Sprites/Emotes[peep_emoji_very_happy]").Batch(batch);
			LoadEmote(Emote.Sad,       "Sprites/Emotes[peep_emoji_sad]").Batch(batch);
			LoadEmote(Emote.VerySad,   "Sprites/Emotes[peep_emoji_very_sad]").Batch(batch);
			LoadEmote(Emote.Angry,     "Sprites/Emotes[peep_emoji_angry]").Batch(batch);
			LoadEmote(Emote.Confused,  "Sprites/Emotes[peep_emoji_confused]").Batch(batch);
			LoadEmote(Emote.Aloof,     "Sprites/Emotes[peep_emoji_aloof]").Batch(batch);
			LoadEmote(Emote.Tired,     "Sprites/Emotes[peep_emoji_tired]").Batch(batch);
			LoadEmote(Emote.Nauseous,  "Sprites/Emotes[peep_emoji_nauseous]").Batch(batch);
			LoadEmote(Emote.Crying,    "Sprites/Emotes[peep_emoji_crying]").Batch(batch);
			LoadEmote(Emote.Food,      "Sprites/Emotes[peep_emoji_food]").Batch(batch);
			LoadEmote(Emote.Drink,     "Sprites/Emotes[peep_emoji_drink]").Batch(batch);
			LoadEmote(Emote.Music,     "Sprites/Emotes[peep_emoji_music]").Batch(batch);
			LoadEmote(Emote.Star,      "Sprites/Emotes[peep_emoji_star]").Batch(batch);
			LoadEmote(Emote.Rest,      "Sprites/Emotes[peep_emoji_rest]").Batch(batch);
			LoadEmote(Emote.Coaster,   "Sprites/Emotes[peep_emoji_coaster]").Batch(batch);
			LoadEmote(Emote.Sword,     "Sprites/Emotes[peep_emoji_sword]").Batch(batch);


			await batch;

			_showLoadingHUD       = false;
			LevelLoadingHUD.Alpha = 0;

			_loaded = true;

			async UniTask LoadEmote(Emote emote, string address) => EmoteSprites[emote] = (await Addressables2.LoadHandleAsync<Sprite>(address)).Result;
		}

		private void Update()
		{
			if (!_loaded) return;
			if (GameController.Live.CanControlPlayer())
			{
				Interactable interactable = ActorController.playerBrain.nearbyInteractables.objs[0];
				if (interactable != null && interactable.CanInteractWith(ActorController.playerActor))
				{
					interactPopup.Show(interactable);

					/*if (!showingInteract)
					{
						interactPopupOLD.ScriptTable["interactable"] = interactable;
						interactPopupOLD.Show();
						showingInteract = true;
					}

					interactPopupOLD.HudElement.WorldAnchor.mode       = WorldPoint.WorldPointMode.Gameobject;
					interactPopupOLD.HudElement.WorldAnchor.gameobject = interactable.gameObject;
					if (interactable.TryGetComponent(out Actor actor))
					{
						interactPopupOLD.HudElement.WorldAnchorOffset = Vector3.up * actor.height;
					}
					else
					{
						interactPopupOLD.HudElement.WorldAnchorOffset = Vector3.up * 1.5f;
					}*/
				}
				else/* if (showingInteract)*/
				{
					interactPopup.Hide();
					/*interactPopupOLD.Hide();
					showingInteract = false;*/
				}
			}
			else /*if (showingInteract)*/
			{
				interactPopup.Hide();
				/*interactPopupOLD.Hide();
				showingInteract = false;*/
			}

			bool showLoading = GameController.Live.AnyLoading;

			if (!_showLoadingHUD && showLoading)
			{
				_showLoadingHUD = true;
				//LevelLoadingHUD.Alpha = 1;
				LevelLoadingHUD.DoAlphaFade(0, 1, 0.75f);
				LevelLoadingHUD.DoOffset(new Vector3(0, -50, 0), new Vector3(0, 0, 0), 0.75f);
			}
			else if (_showLoadingHUD && !showLoading)
			{
				_showLoadingHUD = false;
				//LevelLoadingHUD.Alpha = 0;
				LevelLoadingHUD.DoAlphaFade(1, 0, 0.5f);
			}
		}

		[Button]
		public static SpeechBubble SpawnSpeechBubble()
		{
			HUDBubble bubble = Live.bubblePool.Rent();

			bubble.GetComponent<RectTransform>().anchorMin = Vector2.zero;
			bubble.GetComponent<RectTransform>().anchorMax = Vector2.zero;

			Live.Elements.Add(bubble.hudElement);

			// TODO must return the bubble to the pool after we are done using it, very important

			return bubble as SpeechBubble;
		}

		public static SpritePopup SpawnSpritePopup()
		{
			SpritePopup popup = Live.spritePopupPool.Rent();

			popup.GetComponent<RectTransform>().anchorMin = Vector2.zero;
			popup.GetComponent<RectTransform>().anchorMax = Vector2.zero;

			Live.Elements.Add(popup.HudElement);

			// TODO must return the popup to the pool after we are done using it, very important

			return popup;
		}

		public static void dismiss_all_bubbles()
		{
			for (int i = 0; i < Live.bubblePool.actives.Count; i++)
			{
				var b = Live.bubblePool.actives[i];
				b.StartDeactivation(false);
			}

			Live.bubblePool.ReturnAll();
			Live.choiceBubble.StartDeactivation(false);
		}

		[Button]
		public void TestChoiceBubble()
		{
			choiceBubble.gameObject.SetActive(true);
			choiceBubble.Show(new List<GameText> { "Test1", "Test2", "Test3" }, 0, null);
		}

		/*public static (ChoiceBubble, bool) GetChoiceBubble()
		{
			return Live.choiceBubble;
		}*/

		public static Vector2 ScreenRatio => new Vector2(Screen.width, Screen.height) / Live.TargetScreenRes;

		public static Vector2 PercentToPixel(Vector2        pixelPos)    => pixelPos * Live.TargetScreenRes;
		public static Vector2 CorrectScreenpointPos(Vector2 screenpoint) => new Vector2(screenpoint.x, screenpoint.y) / ScreenRatio;
	}
}