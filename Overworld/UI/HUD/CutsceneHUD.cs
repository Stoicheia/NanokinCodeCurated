
using System;
using System.Collections.Generic;
using Anjin.Cameras;
using Anjin.Nanokin;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Util;
using Util.Addressable;
using Util.Odin.Attributes;

namespace Anjin.UI {

	[ExecuteInEditMode]
	public class CutsceneHUD : StaticBoy<CutsceneHUD> {

		public enum Mode {
			Main,
			Ambient
		}

		public Mode mode;

		public DialogueTextbox      MainTextbox;
		public DialogueTextbox      MainTextboxAlt;
		public DialogueTextbox      SubtitleTextbox;
		public DialogueTextbox      SubtitleBoxTextbox;
		public ChoiceTextbox        ChoiceTextbox;
		public CharacterBustManager StandaloneBustManager;

		public RectTransform   TimelineUIRoot;
		public Transform	   WorldspaceUIRoot;

		[NonSerialized, ShowInPlay]
		public Dictionary<Character,ComponentPool<CharacterBust>> BustPools;
		[NonSerialized, ShowInPlay]
		public Dictionary<CharacterBust,ComponentPool<CharacterBust>> BustsToPools;
		[NonSerialized, ShowInPlay]
		public List<CharacterBust> AllBusts;
		public List<ComponentPool<CharacterBust>> AllPools;


		public async override void Awake()
		{
			base.Awake();
			if(!Application.isPlaying) return;

			MainTextbox.gameObject.SetActive(true);
			MainTextboxAlt.gameObject.SetActive(true);
			ChoiceTextbox.gameObject.SetActive(true);

			MainTextbox.AdvanceButton    = GameInputs.confirm;
			MainTextboxAlt.AdvanceButton = GameInputs.confirm;

			MainTextbox.SoftAutoButton    = GameInputs.textBoxSoftAuto;
			MainTextboxAlt.SoftAutoButton = GameInputs.textBoxSoftAuto;

			AllBusts     = new List<CharacterBust>();
			AllPools     = new List<ComponentPool<CharacterBust>>();
			BustPools    = new Dictionary<Character, ComponentPool<CharacterBust>>();
			BustsToPools = new Dictionary<CharacterBust, ComponentPool<CharacterBust>>();


			async UniTask LoadBust(string address)
			{
				GameObject prefab = (await Addressables2.LoadHandleAsync<GameObject>(address)).Result;

				if (prefab == null) {
					this.LogError($"Bust at address {address} failed to load");
					return;
				}

				CharacterBust bust = prefab.GetComponent<CharacterBust>();

				if (bust == null) {
					this.LogError($"Bust at address {address} does not have CharacterBust component attached.");
					return;
				}

				if (bust.Character == Character.None) {
					this.LogError($"Bust at address {address} does not have its Character enum set, and therefore cannot be loaded.");
					return;
				}

				var pool = new ComponentPool<CharacterBust>(transform, prefab.GetComponent<CharacterBust>()) {
					throwsOnCantAllocate = false,
					maxSize              = 2,
				};

				pool.allocateTemp = true;
				pool.onAllocating = _bust => {
					BustsToPools[_bust] = pool;
					AllBusts.Add(_bust);
				};

				pool.AllocateAdd(1);

				BustPools[bust.Character] = pool;
				AllPools.Add(pool);
			}

			List<string> bustAddresses = Addressables2.Find(Addresses.BustPrefix);
			UniTaskBatch batch = new UniTaskBatch();

			foreach (string address in bustAddresses) {
				LoadBust(address).Batch(batch);
			}

			await batch;

			/*async UniTask LoadBust(Character character, string address)
			{
				GameObject prefab = (await Addressables2.LoadHandleAsync<GameObject>(address)).Result;
				if (prefab == null) {
					this.LogError($"Bust for character {character} at address {address} failed to load");
					return;
				}

				var pool = new ComponentPool<CharacterBust>(transform, prefab.GetComponent<CharacterBust>()) {
					throwsOnCantAllocate = false,
					maxSize = 2,
				};

				pool.allocateTemp    = true;
				pool.onAllocating = _bust => {
					BustsToPools[_bust] = pool;
					AllBusts.Add(_bust);
				};

				pool.AllocateAdd(1);

				BustPools[character] = pool;
				AllPools.Add(pool);
			}*/



			/*await LoadBust(Character.TestDummy,		"UIBusts/TestDummy");
			await LoadBust(Character.Nas,			"UIBusts/Nas");
			await LoadBust(Character.Jatz,			"UIBusts/Jatz");
			await LoadBust(Character.Serio,			"UIBusts/Serio");
			await LoadBust(Character.Peggie,		"UIBusts/Peggie");
			await LoadBust(Character.ChalkyLeBarron,"UIBusts/Chalky");*/
		}

		private void OnEnable()
		{
			//WorldSpaceCanvas.worldCamera = GameCams.Live.UnityCam;
		}

		private void Start()
		{
			if(!Application.isPlaying) return;
			mode = Mode.Main;
		}


		public static bool TryRentBust(Character character, out CharacterBust bust) => Live._tryRentBust( character, out bust);

		bool _tryRentBust(Character character, out CharacterBust bust)
		{
			bust = null;
			if (BustPools.TryGetValue(character, out var pool) && (bust = pool.Rent()) != null)
				return true;

			return false;
		}

		public static void ReturnBust(CharacterBust bust)
		{
			bust.Reset();
			if (!Live.BustsToPools.TryGetValue(bust, out var pool))
				return;

			pool.Return(bust);
		}
	}
}