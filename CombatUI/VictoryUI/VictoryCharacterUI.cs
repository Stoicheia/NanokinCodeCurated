using System;
using Anjin.EditorUtility;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using Data.Nanokin;
using SaveFiles;
using UnityEngine;
using UnityEngine.UI;
using Util.Addressable;
using Util.Components.UI;

namespace Combat.Components.VictoryScreen.Menu
{
	public class VictoryCharacterUI : MonoBehaviour
	{
		public const float MIN_ASSUMED_FPS_FOR_TICK = 5.0f;

		public Image            BarImage;
		public TextMeshProMulti Label_KidName;
		public TextMeshProMulti Label_KidLevel;
		public TextMeshProMulti Label_RemainingXP;

		public Material BarMaterial;

		[NonSerialized] public int            index;
		[NonSerialized] public GameObject     characterObject;
		[NonSerialized] public CharacterEntry character;

		private float                _xpRemainder;
		private float                _rpRemainder;
		private WorldToCanvasRaycast _raycast;
		private AsyncHandles         _handles;
		private CanvasGroup          _canvasGroup;

		private static readonly int _idProgress      = Shader.PropertyToID("_Progress");
		private static readonly int _idProgressAhead = Shader.PropertyToID("_ProgressAhead");

		private void Awake()
		{
			_handles = new AsyncHandles();

			_raycast = gameObject.GetOrAddComponent<WorldToCanvasRaycast>();

			_canvasGroup       = gameObject.AddComponent<CanvasGroup>();
			_canvasGroup.alpha = 0;
		}

		public async UniTask Show(CharacterEntry character, GameObject characterObject)
		{
			_handles.ReleaseAll();

			this.character       = character;
			this.characterObject = characterObject;

			Label_KidName.Text = character.asset.Name;

			if (characterObject != null)
			{
				_raycast.enabled = true;
				_raycast.SetWorldPos(characterObject.transform);
			}
			else
			{
				_raycast.enabled = false;
			}

			BarMaterial       = new Material(await _handles.LoadAssetAsync(character.asset.XPBarMaterial));
			BarImage.material = BarMaterial;

			RefreshStats();

			_canvasGroup.alpha = 1;
		}

		private void RefreshStats()
		{
			// IDEA use ZLogger to achieve zero allocation here
			Label_KidLevel.Text    = character.Level.ToString();
			Label_RemainingXP.Text = character.NextLevelXPLeft.ToString();

			BarMaterial.SetFloat(_idProgress, character.NextLevelProgress);
			BarMaterial.SetFloat(_idProgressAhead, character.NextLevelProgress);
		}

		public void UpdateIncome(VictoryUI.TickerValue xpTick,
			VictoryUI.TickerValue rpTick,
			Action<VictoryCharacterUI>                     showLevelUp,
			Action<VictoryCharacterUI, LimbInstance>       showMasteryUp)
		{
			if (xpTick.remaining > 0)
			{
				int gain = StripRemainderToStore(ref xpTick, ref _xpRemainder, (int)(Mathf.Max(xpTick.ratePerSecond, 0.01f) / MIN_ASSUMED_FPS_FOR_TICK));
				if (character.GainXP(gain))
				{
					showLevelUp(this);
				}
			}

			foreach (LimbInstance instance in character.Limbs)
			{
				if (rpTick.remaining > 0)
				{
					int gain = StripRemainderToStore(ref rpTick, ref _rpRemainder, (int)(Mathf.Max(rpTick.ratePerSecond, 0.01f) / MIN_ASSUMED_FPS_FOR_TICK));
					if (instance.GainRP(gain))
					{
						// Gained a level!
						showMasteryUp(this, instance);
					}
				}
			}

			RefreshStats();
		}

		private static int StripRemainderToStore(ref VictoryUI.TickerValue ticker, ref float remainderStore, int max = Int32.MaxValue)
		{
			// Explanation:
			// We store XP as int for simplicity. However, to get accurate durations in the victory menu we may have to
			// tick at a fractional rate (e.g. 1.5xp per tick)
			// So this function takes the fractional rate and stores the remainder.
			// When the store reaches a full unit, we remove that unit from the store and apply it with our rate.

			float rate = ticker.ratePerSecond * Time.deltaTime;
			rate = Mathf.Min(rate, ticker.remaining); // this is to prevent these greasy speedrunners from lagging up their game to inflate deltatime and get nearly a whole second's worth of free xp. I'm onto you

			ticker.remaining -= rate;
			ticker.remaining =  Math.Max(0, ticker.remaining);

			int rateFloor = Mathf.FloorToInt(rate);

			remainderStore += rate - rateFloor;
			if (remainderStore >= 1)
			{
				int remainderFloor = Mathf.FloorToInt(remainderStore);
				remainderStore -= remainderFloor;

				return rateFloor + remainderFloor;
			}

			return Math.Min(rateFloor, max);
		}
	}
}