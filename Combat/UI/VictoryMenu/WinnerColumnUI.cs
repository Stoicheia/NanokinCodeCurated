using API.Puppets.Components;
using Assets.Nanokins;
using Combat.Entry;
using Cysharp.Threading.Tasks;
using Data.Nanokin;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Util.Addressable;

namespace Combat.Components.VictoryScreen.Menu
{
	public class WinnerColumnUI : VColumn
	{
		private const int LIMB_INDEX_HEAD = 0;
		private const int LIMB_INDEX_ARM1 = 1;
		private const int LIMB_INDEX_ARM2 = 2;
		private const int LIMB_INDEX_BODY = 3;

		public TextMeshProUGUI   Label_FighterName;
		public TextMeshProUGUI   Label_CoachName;
		public PuppetComponent   FighterPuppet;
		public Image             CoachArt;
		public TextMeshProUGUI[] Label_LimbNames  = new TextMeshProUGUI[4];
		public TextMeshProUGUI[] Label_LimbLevels = new TextMeshProUGUI[4];
		public TextMeshProUGUI[] Label_XPToLevel  = new TextMeshProUGUI[4];
		public AudioDef          SFX_LevelUp;

		private NanokinInstance              _nanokin;
		private AsyncOperationHandle<Sprite> _characterArt;

		public async UniTask Show(CharacterAsset coach, [NotNull] NanokinInstance nano)
		{
			if (nano == _nanokin) return;

			Addressables2.ReleaseSafe(_characterArt);

			_nanokin               = nano;
			Label_FighterName.text = nano.Name;

			Label_LimbNames[LIMB_INDEX_HEAD].text = GetLimbName(_nanokin.Head?.Asset);
			Label_LimbNames[LIMB_INDEX_ARM1].text = GetLimbName(_nanokin.Arm1?.Asset);
			Label_LimbNames[LIMB_INDEX_ARM2].text = GetLimbName(_nanokin.Arm2?.Asset);
			Label_LimbNames[LIMB_INDEX_BODY].text = GetLimbName(_nanokin.Body?.Asset);

			RefreshMasteries();
			RefreshValueLabels();

			// FighterPuppet.Puppet = new Puppet(nano.ToAccessedTree(true));

			if (coach)
			{
				_characterArt = await Addressables2.LoadHandleAsync(coach.Art);

				CoachArt.sprite      = _characterArt.Result;
				Label_CoachName.text = coach.Name ?? "NULL";
			}
		}

		private void RefreshMasteries()
		{
			Label_LimbLevels[LIMB_INDEX_HEAD].text = _nanokin.Head?.Mastery.ToString();
			Label_LimbLevels[LIMB_INDEX_ARM1].text = _nanokin.Arm1?.Mastery.ToString();
			Label_LimbLevels[LIMB_INDEX_ARM2].text = _nanokin.Arm2?.Mastery.ToString();
			Label_LimbLevels[LIMB_INDEX_BODY].text = _nanokin.Body?.Mastery.ToString();
		}

		[NotNull]
		private string GetLimbName(NanokinLimbAsset limbAsset)
		{
			if (limbAsset == null) return string.Empty;
			switch (limbAsset.Kind)
			{
				case LimbType.Head: return "Head";
				case LimbType.Body: return "Body";
				case LimbType.Arm1: return "Main Arm";
				case LimbType.Arm2: return "Off Arm";
				default:            return "Unknown";
			}
		}

		public void RefreshValueLabels()
		{
			Label_XPToLevel[LIMB_INDEX_HEAD].text = _nanokin.Head?.NextMasteryRPLeft.ToString();
			Label_XPToLevel[LIMB_INDEX_ARM1].text = _nanokin.Arm1?.NextMasteryRPLeft.ToString();
			Label_XPToLevel[LIMB_INDEX_ARM2].text = _nanokin.Arm2?.NextMasteryRPLeft.ToString();
			Label_XPToLevel[LIMB_INDEX_BODY].text = _nanokin.Body?.NextMasteryRPLeft.ToString();
		}

		public override void StepGains(ref int currentTotal)
		{
			foreach (LimbInstance instance in _nanokin.Limbs)
			{
				if (currentTotal > 0)
				{
					bool levelup = instance.GainRP(1);
					if (levelup)
					{
						// Gained a level!
						RefreshMasteries();
						GameSFX.PlayGlobal(SFX_LevelUp, null);
					}

					currentTotal--;
				}
			}

			RefreshValueLabels();
		}
	}
}