using System;
using Anjin.EditorUtility;
using Anjin.Util;
using Combat.Components.VictoryScreen.Menu;
using Cysharp.Threading.Tasks;
using Data.Nanokin;
using SaveFiles;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Util;
using Util.Addressable;
using Util.Components.UI;

namespace Anjin.UI {
	public class HUDXPBar : SerializedMonoBehaviour {

		public Image            BarImage;
		public TextMeshProMulti Label_KidLevel;
		public TextMeshProMulti Label_RemainingXP;

		public Material BarMaterial;
		public AudioDef SFX_LevelUp;

		[NonSerialized] public CharacterEntry character;

		private float                _xpProgress;

		private AsyncHandles         _handles;

		private static readonly int _idProgress      = Shader.PropertyToID("_Progress");
		private static readonly int _idProgressAhead = Shader.PropertyToID("_ProgressAhead");

		private void Awake()
		{
			_handles = new AsyncHandles();
		}

		public async UniTask SetCharacter(CharacterEntry character)
		{
			_handles.ReleaseAll();

			this.character = character;

			BarMaterial       = new Material(await _handles.LoadAssetAsync(character.asset.XPBarMaterial));
			BarImage.material = BarMaterial;

			_xpProgress = character.NextLevelProgress;

			BarMaterial.SetFloat(_idProgress,      _xpProgress);
			BarMaterial.SetFloat(_idProgressAhead, _xpProgress);
		}

		private void Update()
		{
			if(character != null) {
				_xpProgress = MathUtil.LerpDamp(_xpProgress, character.NextLevelProgress, 5);
				if (Mathf.Abs(_xpProgress - character.NextLevelProgress) < 0.001f) {
					_xpProgress = character.NextLevelProgress;
				}
				BarMaterial.SetFloat(_idProgress,      _xpProgress);
				BarMaterial.SetFloat(_idProgressAhead, character.NextLevelProgress);

				Label_KidLevel.Text    = character.Level.ToString();
				Label_RemainingXP.Text = character.NextLevelXPLeft.ToString();
			}
		}

		private static int StripRemainderToStore(ref VictoryUI.TickerValue ticker, ref float remainderStore)
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

			return rateFloor;
		}
	}
}