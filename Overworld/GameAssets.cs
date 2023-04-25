using System;
using System.Collections.Generic;
using System.Diagnostics;
using Anjin.Actors;
using Anjin.Nanokin;
using Anjin.Nanokin.ParkAI;
using Anjin.UI;
using Anjin.Util;
using Assets.Nanokins;
using Cinemachine;
using Combat;
using Combat.Entry;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Overworld.Rendering;
using Overworld.Shopping;
using SaveFiles.Elements.Inventory.Items;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Profiling;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.Serialization;
using Util.Addressable;
using Util.Odin.Attributes;
using Debug = System.Diagnostics.Debug;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

[DefaultExecutionOrder(-1)]
public class GameAssets : StaticBoy<GameAssets>
{
	[BoxGroupExt("Sprites", 0.3f, 0.8f, 1.0f), Title("Debug")]
	public Sprite Reaction_Question;
	[BoxGroupExt("Sprites")]
	public Sprite Reaction_Exclamation;

	// Core
	public ActorPlaybackCurves ActorPlaybackCurves;
	public NoiseSettings       CamNoiseSettings;
	public TextAsset           Changelog_Internal;

	// Scenes
	[Title("Debug")]
	[BoxGroupExt("Scenes", 0.3f, 0.8f, 1.0f)]
	public SceneReference DebugLevelSelectMenuScene;

	// Prefabs/Actors
	[BoxGroupExt("Prefabs"), Title("Actors")]
	public Actor BaseSpawnableCharacter;
	[BoxGroupExt("Prefabs")] public List<Actor> OtherSpawnableCharacters;

	[BoxGroupExt("Prefabs")]
	public ComponentRef<NPCActor> P_PartyMember_Serio;

	[BoxGroupExt("Prefabs")]
	public ComponentRef<NPCActor> P_PartyMember_Jatz;

	[BoxGroupExt("Prefabs")]
	public ComponentRef<NPCActor> P_PartyMember_Peggie;

	//public List<Actor> PartyNPCPrefabs;

	[FormerlySerializedAs("NPCActorPrefab"), BoxGroupExt("Prefabs")]
	public NPCActor GuestPrefab;

	// ParkAI
	[BoxGroupExt("Prefabs")] public ParkAIAvatar Peep_adult_male;
	[BoxGroupExt("Prefabs")] public ParkAIAvatar Peep_adult_female;
	[BoxGroupExt("Prefabs")] public ParkAIAvatar Peep_adult_male_round;
	[BoxGroupExt("Prefabs")] public ParkAIAvatar Peep_adult_female_round;
	[BoxGroupExt("Prefabs")] public ParkAIAvatar Peep_child;

	// Prefabs/UI
	[BoxGroupExt("Prefabs"), Title("UI")]
	[BoxGroupExt("Prefabs")] public SpeechBubble SpeechBubblePrefab;
	[BoxGroupExt("Prefabs")] public SpritePopup SpritePopupPrefab;
	[BoxGroupExt("Prefabs")] public EmotePopup  EmotePopupPrefab;

	// Prefabs/Effects
	[BoxGroupExt("Prefabs"), Title("Effects")]
	public AfterImageEffect AfterImageEffectPrefab;

	// Prefabs/Cameras
	// public CinemachineVirtualCamera CamPrefab_Base;
	// public VCamStaticOffsetProxy    CamPrefab_Static;
	// public VCamHOrbitProxy          CamPrefab_HOrbit;
	// public VCamFixedOffsetProxy     CamPrefab_FixedOffset;

	// Rendering
	// public Material MatSpritesOpaque;
	// public Material MatSpritesTransparent;

	// public Material GLDebugMaterial;

	[BoxGroupExt("Databases")]
	public PeepSpriteDatabase PeepSpriteDatabase;

	// ScriptableObjects
	[BoxGroupExt("ScriptableObjects")]
	public InputIcons InputIconsObject;

	[NonSerialized, ShowInPlay]
	public Dictionary<Character, NPCActor> LoadedPartyMemberPrefabs;

	// Sounds/SFX
	[BoxGroupExt("Sounds", 0.5f, 0.8f, 1.0f), Title("SFX")]
	public AudioDef Sfx_Typewriter_Tick;

	public AudioDef SFX_Default_Jump_Sound;
	public AudioDef SFX_Default_Land_Sound;

	private bool _isLoaded;

	public struct LoadedAsset
	{
		public AsyncOperationHandle handle;
		public string               address;
		public Object               asset;
	}

	private static Dictionary<string, LoadedAsset> _allCharacters = new Dictionary<string, LoadedAsset>(StringComparer.InvariantCultureIgnoreCase);
	private static Dictionary<string, LoadedAsset> _allLimbs      = new Dictionary<string, LoadedAsset>(StringComparer.InvariantCultureIgnoreCase);
	private static Dictionary<string, LoadedAsset> _allStickers   = new Dictionary<string, LoadedAsset>(StringComparer.InvariantCultureIgnoreCase);
	private static Dictionary<string, LoadedAsset> _allNanokins   = new Dictionary<string, LoadedAsset>(StringComparer.InvariantCultureIgnoreCase);
	private static Dictionary<string, LoadedAsset> _allItems      = new Dictionary<string, LoadedAsset>(StringComparer.InvariantCultureIgnoreCase);
	private static Dictionary<string, LoadedAsset> _allSkills     = new Dictionary<string, LoadedAsset>(StringComparer.InvariantCultureIgnoreCase);

	public static  AsyncLazy loadTask;
	private static Stopwatch _sw = new Stopwatch();

	public static bool IsLoaded => Live != null && Live._isLoaded;

	[NonSerialized]
	public string[] ChangelogLines;

	protected override void OnAwake()
	{
		//loadTask = Load().ToAsyncLazy();
		LevelManifestDatabase.LoadedDB.CatalogIDs();
		ChangelogLines = Changelog_Internal.text.Split('\n');
	}


	public async UniTask InitializeThroughGameController()
	{
		await Load();
	}

	private void OnApplicationQuit()
	{
		// Clean up when exiting playmode
		loadTask = null;
		_allStickers.Clear();
		_allLimbs.Clear();
		_allStickers.Clear();
		_allNanokins.Clear();
		_allItems.Clear();
		_allSkills.Clear();
	}


#region Loading

	private async UniTask Load()
	{
		// Shouldn't need this because GameController already calls it
		//await Addressables.InitializeAsync();

		LoadedPartyMemberPrefabs = new Dictionary<Character, NPCActor>();
		async UniTask LoadPartyMember(Character character, ComponentRef<NPCActor> prefab)
		{
			var loaded = await prefab.LoadAssetAsync();
			LoadedPartyMemberPrefabs[character] = loaded;
		}

		UniTaskBatch batch = UniTask2.Batch();
		LoadPartyMember(Character.Jatz, P_PartyMember_Jatz).Batch(batch);
		LoadPartyMember(Character.Serio, P_PartyMember_Serio).Batch(batch);
		LoadPartyMember(Character.Peggie, P_PartyMember_Peggie).Batch(batch);
		await batch;

		_sw.Restart();

		if (!GameOptions.current.load_on_demand)
		{
			ShopController.EnsureLoaded().Forget();

			// Load Config
			// ----------------------------------------
			_sw.Restart();

			// NOTE: This is a temp hack to get the conifg assets to load at game start.
			ScriptableObject _ = Config.Display;
			this.Log($"Config loaded in {_sw.ElapsedMilliseconds}ms", op: "%%");
		}

		_sw.Restart();
#if UNITY_EDITOR
		LoadEditor();
#else
		await LoadRegular();
#endif

		_isLoaded = true;
	}

	private async UniTask LoadRegular()
	{
		Profiler.BeginSample("GameAssets.GetResourceLocations");
		// TODO this is slow
		IList<IResourceLocation> characterLocs = Addressables2.GetResourceLocations(Addresses.CharacterLabel);
		IList<IResourceLocation> limbLocs      = Addressables2.GetResourceLocations(Addresses.LimbLabel);
		IList<IResourceLocation> stickerLocs   = Addressables2.GetResourceLocations(Addresses.StickerLabel);
		IList<IResourceLocation> nanokinLocs   = Addressables2.GetResourceLocations(Addresses.NanokinLabel);
		IList<IResourceLocation> itemLocs      = Addressables2.GetResourceLocations(Addresses.ItemLabel);
		IList<IResourceLocation> skillLocs     = Addressables2.GetResourceLocations(Addresses.SkillLabel);
		this.Log($"Obtained resource locations in {_sw.ElapsedMilliseconds}ms", op: "%%");

		Profiler.EndSample();
		_sw.Restart();

		using (UniTaskBatch batch = UniTask2.Batch())
		{
			LoadLocations<NanokinLimbAsset>(limbLocs, _allLimbs, "Limbs/").Batch(batch);
			LoadLocations<CharacterAsset>(characterLocs, _allCharacters, "Characters/").Batch(batch);
			LoadLocations<NanokinAsset>(nanokinLocs, _allNanokins, "Nanokins/").Batch(batch);
			LoadLocations<StickerAsset>(stickerLocs, _allStickers, "Stickers/").Batch(batch);
			LoadLocations<ItemAsset>(itemLocs, _allItems, "Items/").Batch(batch);
			LoadLocations<SkillAsset>(skillLocs, _allSkills, "Skills/").Batch(batch);

			Profiler.BeginSample("GameAssets.LoadLocations");
			await batch;
			Profiler.EndSample();
		}

		AssignNanokinLimbRefs();

		this.Log($"Essential assets loaded in {_sw.ElapsedMilliseconds}ms", op: "%%");
	}

	private static void AssignNanokinLimbRefs()
	{
		foreach (LoadedAsset nano in _allNanokins.Values)
		{
			NanokinAsset asset = (NanokinAsset)nano.asset;

			if (asset.Head != null) asset.Head.monster = asset;
			if (asset.Body != null) asset.Body.monster = asset;
			if (asset.Arm1 != null) asset.Arm1.monster = asset;
			if (asset.Arm2 != null) asset.Arm2.monster = asset;
		}
	}

#if UNITY_EDITOR
	private static void LoadEditor()
	{
		// This is a LOT faster for startup
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

		foreach (AddressableAssetGroup group in settings.groups)
		{
			foreach (AddressableAssetEntry entry in group.entries)
			{
				string addr = entry.address;

				if (entry.MainAsset is IAddressable addressable)
				{
					addressable.Address = addr;
				}

				if (entry.labels.Contains(Addresses.LimbLabel))
				{
					Register(addr, _allLimbs, entry);
					// Register(addr.Substring(Addresses.LimbPrefix.Length), _allLimbs, entry);
				}
				else if (entry.labels.Contains(Addresses.NanokinLabel))
				{
					Register(addr, _allNanokins, entry);
					// Register(addr.Substring(Addresses.NanokinPrefix.Length), _allNanokins, entry);
				}
				else if (entry.labels.Contains(Addresses.CharacterLabel))
				{
					Register(addr, _allCharacters, entry);
					// Register(addr.Substring(Addresses.CharacterPrefix.Length), _allCharacters, entry);
				}
				else if (entry.labels.Contains(Addresses.ItemLabel))
				{
					Register(addr, _allItems, entry);
					// Register(addr.Substring(Addresses.ItemPrefix.Length), _allItems, entry);
				}
				else if (entry.labels.Contains(Addresses.StickerLabel))
				{
					Register(addr, _allStickers, entry);
					// Register(addr.Substring(Addresses.StickerPrefix.Length), _allStickers, entry);
				}
				else if (entry.labels.Contains(Addresses.SkillLabel))
				{
					Register(addr, _allSkills, entry);
					// Register(addr.Substring(Addresses.SkillPrefix.Length), _allSkills, entry);
				}
			}
		}

		AssignNanokinLimbRefs();

		UnityEngine.Debug.Log("GameAssets.LoadEditor()");
	}

	private static void Register([NotNull] string addr, [NotNull] Dictionary<string, LoadedAsset> catalogue, [NotNull] AddressableAssetEntry entry)
	{
		catalogue[addr] = new LoadedAsset
		{
			address = addr,
			asset   = entry.MainAsset
		};
	}
#endif

	private async UniTask LoadLocations<TAsset>(
		[NotNull] IList<IResourceLocation> locations,
		Dictionary<string, LoadedAsset>    catalogue,
		[CanBeNull] string                 prefix = null)
		where TAsset : Object
	{
		float startTimestamp = Time.time;

		var handles = new List<AsyncOperationHandle<TAsset>>();

		Profiler.BeginSample("LoadLocations");
		for (var i = 0; i < locations.Count; i++)
		{
			IResourceLocation loc = locations[i];
			(AsyncOperationHandle<TAsset> hnd, TAsset _) = Addressables2.LoadHandleAsyncSlim<TAsset>(loc.PrimaryKey);
			hnd.Completed += handle =>
			{
				Profiler.BeginSample("LoadLocations Handle Completed");
				var asset = new LoadedAsset
				{
					handle  = handle,
					address = loc.PrimaryKey,
					asset   = handle.Result
				};

				catalogue[loc.PrimaryKey] = asset;
				if (prefix != null)
					catalogue[loc.PrimaryKey.Substring(prefix.Length)] = asset;

				Profiler.EndSample();
			};

			handles.Add(hnd);
		}

		Profiler.EndSample();

		foreach (AsyncOperationHandle<TAsset> asyncOperationHandle in handles)
		{
			await asyncOperationHandle;
		}

		this.Log($"Loaded all {typeof(TAsset).Name} loaded in {Time.time - startTimestamp:F}s", op: "%%");
	}

	//	API
	//---------------------------------------------------------------------------

#endregion

#region API

	[NotNull]
	public static Dictionary<string, LoadedAsset>.KeyCollection Characters => _allCharacters.Keys;

	[NotNull]
	public static Dictionary<string, LoadedAsset>.KeyCollection Limbs => _allLimbs.Keys;

	[NotNull]
	public static Dictionary<string, LoadedAsset>.KeyCollection Stickers => _allStickers.Keys;

	[NotNull]
	public static Dictionary<string, LoadedAsset>.KeyCollection Nanokins => _allNanokins.Keys;

	[NotNull]
	public static Dictionary<string, LoadedAsset>.KeyCollection Items => _allItems.Keys;

	[NotNull]
	public static Dictionary<string, LoadedAsset>.KeyCollection Skills => _allSkills.Keys;

	public static AsyncOperationHandle<T> LoadAsset<T>(string address) => Addressables.LoadAssetAsync<T>(address);

	[CanBeNull] public static CharacterAsset   GetCharacter([NotNull] string address) => Get<CharacterAsset>(address, _allCharacters, Addresses.CharacterPrefix);
	[CanBeNull] public static NanokinLimbAsset GetLimb([NotNull]      string address) => Get<NanokinLimbAsset>(address, _allLimbs, Addresses.LimbPrefix);
	[CanBeNull] public static StickerAsset     GetSticker([NotNull]   string address) => Get<StickerAsset>(address, _allStickers, Addresses.StickerPrefix);
	[CanBeNull] public static NanokinAsset     GetNanokin([NotNull]   string address) => Get<NanokinAsset>(address, _allNanokins, Addresses.NanokinPrefix);
	[CanBeNull] public static ItemAsset        GetItem([NotNull]      string address) => Get<ItemAsset>(address, _allItems, Addresses.ItemPrefix);
	[CanBeNull] public static SkillAsset       GetSkill([NotNull]     string address) => Get<SkillAsset>(address, _allSkills, Addresses.SkillPrefix);

	public static bool HasLimb([CanBeNull]      string addr) => Has(addr, _allLimbs, Addresses.LimbPrefix);
	public static bool HasNanokin([CanBeNull]   string addr) => Has(addr, _allNanokins, Addresses.NanokinPrefix);
	public static bool HasCharacter([CanBeNull] string addr) => Has(addr, _allCharacters, Addresses.CharacterPrefix);
	public static bool HasSticker([CanBeNull]   string addr) => Has(addr, _allStickers, Addresses.StickerPrefix);
	public static bool HasItem([CanBeNull]      string addr) => Has(addr, _allItems, Addresses.ItemPrefix);
	public static bool HasSkill([CanBeNull]     string addr) => Has(addr, _allSkills, Addresses.SkillPrefix);

	public static void FindSkills(string search, List<SkillAsset> results = null)
	{
		results = results ?? new List<SkillAsset>();
		results.Clear();

		foreach ((string key, LoadedAsset ass) in _allSkills)
		{
			if (key.Contains(search))
			{
				results.Add((SkillAsset)ass.asset);
			}
		}
	}

	private static TAsset Get<TAsset>([NotNull] string address, [CanBeNull] Dictionary<string, LoadedAsset> dic, string prefix, bool canUseEditorAPI = true)
		where TAsset : Object
	{
		if (dic == null) return null;

		LoadedAsset asset;
		if (dic.TryGetValue(address, out asset)) return (TAsset)asset.asset;
		if (dic.TryGetValue($"{prefix}/{address}", out asset)) return (TAsset)asset.asset;

		return null;
	}

	private static bool Has(string addr, Dictionary<string, LoadedAsset> handles, string prefix)
	{
		if (string.IsNullOrEmpty(addr)) return false;

		if (handles.ContainsKey(addr)) return true;
		if (handles.ContainsKey($"{prefix}/{addr}")) return true;

		return false;
	}

#endregion
}