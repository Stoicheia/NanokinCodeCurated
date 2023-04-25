using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.UI;
using Anjin.Util;
using Anjin.Utils;
using Combat.Components.VictoryScreen.Menu;
using Combat.UI;
using Cysharp.Threading.Tasks;
using Data.Nanokin;
using Overworld.Controllers;
using SaveFiles;
using TMPro;
using UnityEngine;
using Util.Components.UI;
using Vexe.Runtime.Extensions;

namespace Anjin.Nanokin.Map
{
	public class CollectibleSystem : StaticBoy<CollectibleSystem>
	{
		public static List<Collectable> respawnUpdate;

		[SerializeField] private float          MultiplierDuration;
		[SerializeField] private float          MultiplierIncrement = 0.25f;
		[SerializeField] private float          MultiplierMaximum   = 10f;
		[SerializeField] private SceneReference SceneHUD;
		[SerializeField] private AudioClip[]    Sounds;
		[SerializeField] private GameObject     NumberParticlePrefab;

		[Space]
		public GameObject LevelUpPanelPrefab;
		public AudioDef LevelUpSFX;

		// private AudioSource[] _audioSources;

		// [NonSerialized] public List<StatCollectable> all;

		private float _bufferedGainHP;
		private float _bufferedGainSP;
		private float _bufferedGainOP;
		private float _bufferedGainXP;
		private float _bufferedGainCredit;

		private float _multiplier = 1;
		private int   _multiplierIndex;
		private float _multiplierTimer;

		private List<NotificationRectStack> _notificationStacks;
		[SerializeField] private NotificationRectStack NotifStackPrefab;
		[SerializeField] private List<RectTransform> UIOrderTransforms;

		protected override void OnAwake()
		{
			base.OnAwake();

			respawnUpdate = new List<Collectable>();
		}

		private async UniTaskVoid Start()
		{
			/*if (!GameOptions.current.load_on_demand)
			{
				await SceneLoader.GetOrLoadAsync(SceneHUD);
			}*/

			_notificationStacks = new List<NotificationRectStack>(UIOrderTransforms.Count);
			for (int i = 0; i < UIOrderTransforms.Count; i++)
			{
				_notificationStacks.Add(Instantiate(NotifStackPrefab, UIOrderTransforms[i].transform, false));
			}
		}

		public void Update()
		{
			if (_multiplierTimer <= 0)
				return;

			_multiplierTimer -= Time.deltaTime;
			if (_multiplierTimer <= 0)
			{
				_multiplier      = 1;
				_multiplierIndex = 0;

				_bufferedGainHP     = 0;
				_bufferedGainSP = 0;
				_bufferedGainOP = 0;
				_bufferedGainXP     = 0;
				_bufferedGainCredit = 0;

				ComboUI.EndCombo();
				OverworldHUD.Live.CollectGains.Hide();
			}

			foreach (Collectable collectable in respawnUpdate)
			{
				collectable.respawnTimer -= Time.deltaTime;

				if (collectable.respawnTimer <= 0 || collectable.respawnFlag)
				{
					collectable.spawned     = true;
					collectable.respawnFlag = false;
					for (int i = 0; i < collectable.transform.childCount; i++)
					{
						collectable.transform.GetChild(i).gameObject.SetActive(true);
					}
				}
			}
		}

		public async UniTask OnDamageDealtFlat(float damage)
		{
			PlayerActor player = ActorController.playerActor as PlayerActor;

			if (player != null)
			{
				await SceneLoader.GetOrLoadAsync(SceneHUD);

				float hpLoss = Mathf.Round(damage);

				OverworldHUD.Live.CollectGains.Show();
				OverworldHUD.Live.CollectGains.GainHP.text = hpLoss.ToString("F0");

				OverworldHUD.ShowStatsTimed(5);
				/*if (!OverworldHUD.StatsShowing) {
				}*/

				// Number particle
				// ----------------------------------------
				GameObject numberObject = PrefabPool.Rent(NumberParticlePrefab);
				numberObject.transform.position = player.Position;

				TextMeshPro numberText = numberObject.GetComponentInChildren<TextMeshPro>();

				numberText.text = $"-{hpLoss:F0} HP";

				ParticleSystemCallbacks numberCallbacks = numberObject.GetComponent<ParticleSystemCallbacks>();
				numberCallbacks.SystemEnded = () =>
				{
					PrefabPool.Return(numberObject);
				};

				float startOfDistribution = 0;
				// Distribute HP
				// ----------------------------------------
				float remainingHp = hpLoss;
				int hpCut = (int)(hpLoss / (SaveManager.current.Party.Count));

				while (remainingHp > 0)
				{
					startOfDistribution = remainingHp;

					foreach (CharacterEntry nano in SaveManager.current.Party)
					{
						float normalizedHPCut = hpCut / nano.MaxPoints.hp;
						//float hgain = Mathf.Min(normalizedHPCut, 1 - nano.nanokin.Points.hp);

						nano.nanokin.Points.hp = Mathf.Max(nano.nanokin.Points.hp - normalizedHPCut, 1);
						remainingHp -= (normalizedHPCut * nano.MaxPoints.hp);
					}

					if (Mathf.Abs(startOfDistribution - remainingHp) < Mathf.Epsilon)
						break;
				}
			}
		}

		public async UniTask OnDamageDealtPercentage(float percentage)
		{
			PlayerActor player = ActorController.playerActor as PlayerActor;

			if (player != null)
			{
				await SceneLoader.GetOrLoadAsync(SceneHUD);

				//float hpLoss = Mathf.Round(damage);

				//OverworldHUD.Live.CollectGains.Show();
				//OverworldHUD.Live.CollectGains.GainHP.text = hpLoss.ToString("F0");

				OverworldHUD.ShowStatsTimed(5);

				// Number particle
				// ----------------------------------------
				//GameObject numberObject = PrefabPool.Rent(NumberParticlePrefab);
				//numberObject.transform.position = player.Position;

				//TextMeshPro numberText = numberObject.GetComponentInChildren<TextMeshPro>();

				//numberText.text = $"-{hpLoss:F0} HP";

				//ParticleSystemCallbacks numberCallbacks = numberObject.GetComponent<ParticleSystemCallbacks>();
				//numberCallbacks.SystemEnded = () =>
				//{
				//	PrefabPool.Return(numberObject);
				//};

				//float startOfDistribution = 0;
				//// Distribute HP
				//// ----------------------------------------
				//float remainingHp = hpLoss;
				//int hpCut = (int)(hpLoss / (SaveManager.current.Party.Count));

				//while (remainingHp > 0)
				//{
				//	startOfDistribution = remainingHp;

				//	foreach (CharacterEntry nano in SaveManager.current.Party)
				//	{
				//		float normalizedHPCut = hpCut / nano.MaxPoints.hp;
				//		//float hgain = Mathf.Min(normalizedHPCut, 1 - nano.nanokin.Points.hp);

				//		nano.nanokin.Points.hp = Mathf.Max(nano.nanokin.Points.hp - normalizedHPCut, 1);
				//		remainingHp -= (normalizedHPCut * nano.MaxPoints.hp);
				//	}

				//	if (Mathf.Abs(startOfDistribution - remainingHp) < Mathf.Epsilon)
				//		break;
				//}

				foreach (CharacterEntry nano in SaveManager.current.Party)
				{
					nano.nanokin.Points.hp = Mathf.Max(nano.nanokin.Points.hp - percentage, (1f / nano.nanokin.MaxPoints.hp));
				}
			}
		}

		public async UniTask OnCollect(StatCollectable statCollectable)
		{
			await SceneLoader.GetOrLoadAsync(SceneHUD);

			float hpGain     = Mathf.Round(statCollectable.points.hp * _multiplier);
			float spGain     = Mathf.Round(statCollectable.points.sp * _multiplier);
			float opGain     = Mathf.Round(statCollectable.points.op * _multiplier);
			float xpGain     = Mathf.Round(statCollectable.xp * _multiplier);
			float creditGain = Mathf.Round(statCollectable.credit * _multiplier);

			_bufferedGainHP     += hpGain;
			_bufferedGainSP     += spGain;
			_bufferedGainOP     += opGain;
			_bufferedGainXP     += xpGain;
			_bufferedGainCredit += creditGain;

			_multiplier += MultiplierIncrement;
			_multiplierIndex++;

			_multiplier      = _multiplier.Clamp(1, MultiplierMaximum);
			_multiplierIndex = _multiplierIndex.Clamp(0, Sounds.Length - 1);

			_multiplierTimer = MultiplierDuration;

			// UI
			// ----------------------------------------
			ComboUI.UpdateCombo(_multiplier).Forget();

			OverworldHUD.Live.CollectGains.Show();
			OverworldHUD.Live.CollectGains.GainHP.text     = _bufferedGainHP.ToString("F0");
			OverworldHUD.Live.CollectGains.GainXP.text     = _bufferedGainXP.ToString("F0");
			OverworldHUD.Live.CollectGains.GainCredit.text = _bufferedGainCredit.ToString("F0");

			if (hpGain > Mathf.Epsilon || xpGain > Mathf.Epsilon) {
				OverworldHUD.ShowStatsTimed(5);
			}

			if (creditGain > Mathf.Epsilon) {
				OverworldHUD.ShowCreditsTimed(5);
			}

			// Number particle
			// ----------------------------------------

			void SpawnNumber(string text)
			{
				GameObject numberObject = PrefabPool.Rent(NumberParticlePrefab);
				numberObject.transform.position = statCollectable.transform.position;
				TextMeshPro numberText = numberObject.GetComponentInChildren<TextMeshPro>();
				numberText.text = text;

				// TODO: We probably want to make this more efficient
				ParticleSystemCallbacks numberCallbacks = numberObject.GetComponent<ParticleSystemCallbacks>();

				numberCallbacks.SystemEnded = () => {
					PrefabPool.Return(numberObject);
				};

			}

			if (hpGain     > 0) SpawnNumber($"{hpGain:F0} HP");
			if (xpGain     > 0) SpawnNumber($"{xpGain:F0} XP");
			if (creditGain > 0) SpawnNumber($"{creditGain:F0} ¢");

			/*GameObject numberObject = PrefabPool.Rent(NumberParticlePrefab);
			numberObject.transform.position = statCollectable.transform.position;

			TextMeshPro numberText = numberObject.GetComponentInChildren<TextMeshPro>();
			if (hpGain > 0) numberText.text          = $"{hpGain:F0} HP";
			else if (xpGain > 0) numberText.text     = $"{xpGain:F0} XP";
			else if (creditGain > 0) numberText.text = $"{creditGain:F0} ¢";

			ParticleSystemCallbacks numberCallbacks = numberObject.GetComponent<ParticleSystemCallbacks>();
			numberCallbacks.SystemEnded = () =>
			{
				PrefabPool.Return(numberObject);
			};*/

			// SFX
			// ----------------------------------------
			GameSFX.Play(Sounds[_multiplierIndex % Sounds.Length], statCollectable.transform.position, this);

			// Add to player's data
			// ----------------------------------------
			SaveManager.current.Money += (int)statCollectable.credit;

			float startOfDistribution = 0;
			// Distribute HP
			// ----------------------------------------
			float remainingHp = statCollectable.points.hp;
			int   hpCut       = (int)(statCollectable.points.hp * _multiplier / (SaveManager.current.Party.Count));
			float remainingSp = statCollectable.points.sp;
			int   spCut       = (int)(statCollectable.points.sp * _multiplier / (SaveManager.current.Party.Count));
			float remainingOp = statCollectable.points.op;
			int   opCut       = (int)(statCollectable.points.op * _multiplier / (SaveManager.current.Party.Count));

			while (remainingHp > 0)
			{
				startOfDistribution = remainingHp;

				foreach (CharacterEntry nano in SaveManager.current.Party)
				{
					float normalizedHPCut = hpCut / nano.MaxPoints.hp;
					float hgain            = Mathf.Min(normalizedHPCut, 1 - nano.nanokin.Points.hp);

					nano.nanokin.Points.hp += hgain;
					remainingHp            -= hgain * nano.MaxPoints.hp;
				}

				if (Mathf.Abs(startOfDistribution - remainingHp) < Mathf.Epsilon)
					break;
			}

			while (remainingSp > 0)
			{
				startOfDistribution = remainingSp;

				foreach (CharacterEntry nano in SaveManager.current.Party)
				{
					float normalizedSPCut = spCut / nano.MaxPoints.sp;
					float sgain            = Mathf.Min(normalizedSPCut, 1 - nano.nanokin.Points.sp);

					nano.nanokin.Points.sp += sgain;
					remainingSp            -= sgain * nano.MaxPoints.sp;
				}

				if (Mathf.Abs(startOfDistribution - remainingSp) < Mathf.Epsilon)
					break;
			}

			while (remainingOp > 0)
			{
				startOfDistribution = remainingOp;

				foreach (CharacterEntry nano in SaveManager.current.Party)
				{
					float normalizedOPCut = opCut / nano.MaxPoints.op;
					float ogain            = Mathf.Min(normalizedOPCut, 1 - nano.nanokin.Points.op);

					nano.nanokin.Points.op += ogain;
					remainingOp            -= ogain * nano.MaxPoints.op;
				}

				if (Mathf.Abs(startOfDistribution - remainingOp) < Mathf.Epsilon)
					break;
			}



			// Distribute XP
			// ----------------------------------------
			float remainingXp = statCollectable.xp;

			// Cut if we're adding to limb mastery
			//int   xpCut       = (int) (statCollectable.xp * _multiplier / (SaveManager.current.Party.Count * 4));

			// Cut if we're adding to player level
			int xpCut = (int)(statCollectable.xp * _multiplier / (SaveManager.current.Party.Count));

			while (remainingXp > 0)
			{
				startOfDistribution = remainingXp;

				foreach (CharacterEntry nano in SaveManager.current.Party)
				{
					int gain = xpCut;
					if (nano.GainXP(gain))
					{
						ShowLevelUp(nano);
					}
					remainingXp -= gain;

					// Limb mastery
					/*foreach (LimbInstance limb in nano.Limbs) {
						int gain = xpCut;

						if (limb.Mastery >= StatConstants.MAX_MASTERY) continue;
						if (limb.Mastery == StatConstants.MAX_MASTERY - 1)
							gain = Mathf.Min(xpCut, limb.NextMasteryRPLeft);

						if (limb.GainRP(gain))
							nano.nanokin.RecalculateStats();

						remainingXp -= gain;
					}*/
				}

				if (Mathf.Abs(startOfDistribution - remainingXp) < Mathf.Epsilon)
					break;
			}


			// float multiplierProgress = (_multiplier - 1) / (MultiplierMaximum - 1);
			// for (int i = 0; i < _audioSources.Length; i++)
			// {
			// 	AudioSource source = _audioSources[i];
			//
			// 	// Formula to mix N audio sources
			// 	//
			// 	// linear mix
			// 	// 1 - |2x - i|
			// 	//
			// 	// sine mix
			// 	// (cos x*tau-i*pi) / 2 + 0.5
			// 	// x clamped to [1/(n-1)*i, 1/(n-1)*(i+2)]
			//
			// 	AudioPlayer.ConfigureForSFX(source);
			//
			// 	source.transform.position = collectible.transform.position;
			// 	source.volume             = 1 - Mathf.Abs(2 * multiplierProgress - i);
			// 	source.Play();
			// }

			// AudioPlayer.ConfigureForSFX(_audioSource);
		}


		//TODO: since party order can change, create some system to keep track of this; create system where we can spawn notification stack notifications easily

		private readonly Dictionary<string, int> CHARACTER_ORDER = new Dictionary<string, int>() {{"Nas", 0}, {"Serio", 1}, {"Jatz", 2 } };

		public void ShowLevelUp(CharacterEntry character)
		{
			DebugLogger.Log($"{character.Level} reached by {character.Name} through pickups!", LogContext.Data, LogPriority.Low);
			NotificationRectStack.Notif notif = _notificationStacks[CHARACTER_ORDER[character.Name]].PushPrefab(new NotificationRectStack.Notif
			{
				prefab = LevelUpPanelPrefab,
				id     = character
			});

			LevelUpPanel panel = notif.go.GetComponent<LevelUpPanel>();
			panel.SetValue1(character.Level);
			panel.Enter();

			GameSFX.PlayGlobal(LevelUpSFX);
		}
	}
}