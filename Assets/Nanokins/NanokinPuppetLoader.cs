using System;
using System.Collections.Generic;
using System.Linq;
using API.Puppets.Components;
using Cysharp.Threading.Tasks;
using Data.Nanokin;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using UnityEngine.U2D;
using Util.Addressable;
using Util.Odin.Attributes;

namespace Assets.Nanokins
{
	/// <summary>
	/// A component which lets the user load a puppet statically in editor or automaticaly at the start of the game.
	/// </summary>
	[RequireComponent(typeof(PuppetComponent))]
	public class NanokinPuppetLoader : MonoBehaviour
	{
		[SerializeField, Optional] private string _nameToSearch;
		[SerializeField]           private bool   _loadAtGameStart;

		private void Start()
		{
			SpriteAtlasManager.atlasRequested -= OnAtlasRequested;
			SpriteAtlasManager.atlasRequested += OnAtlasRequested;

			Load();
		}

		private async UniTask Load()
		{
			if (_loadAtGameStart)
			{
				await LoadPuppet(_nameToSearch);
			}
		}

		private void OnAtlasRequested(string address, Action<SpriteAtlas> onLoaded)
		{
			OnAtlasRequestedAsync(address, onLoaded);
		}

		private async UniTask OnAtlasRequestedAsync(string address, Action<SpriteAtlas> onLoaded)
		{
			SpriteAtlas atlas = await Addressables.LoadAssetAsync<SpriteAtlas>(address).Task;
			onLoaded(atlas);
		}

		private async UniTask LoadPuppet(string nameToSearch, bool is_managed = true)
		{
			// Load all limbs
			await NanokinLimbCatalogue.Instance.LoadAll();

			// Fetch the limbs by name
			// ----------------------------------------
			List<NanokinLimbAsset> loadedLimbs = NanokinLimbCatalogue.Instance.loadedAssets;

			NanokinLimbAsset body = loadedLimbs.FirstOrDefault(limb => limb.Kind == LimbType.Body && limb.name.ToLower().Contains(nameToSearch));
			NanokinLimbAsset head = loadedLimbs.FirstOrDefault(limb => limb.Kind == LimbType.Head && limb.name.ToLower().Contains(nameToSearch));
			NanokinLimbAsset arm1 = loadedLimbs.FirstOrDefault(limb => limb.Kind == LimbType.Arm1 && limb.name.ToLower().Contains(nameToSearch));
			NanokinLimbAsset arm2 = loadedLimbs.FirstOrDefault(limb => limb.Kind == LimbType.Arm2 && limb.name.ToLower().Contains(nameToSearch));

			AsyncHandles hnd = new AsyncHandles();

			NanokinLimbTree tree = NanokinLimbTree.WithAddressable(body, head, arm1, arm2, hnd);


			// Create the puppet
			// ----------------------------------------
			PuppetComponent monopuppet = GetComponent<PuppetComponent>();
			Assert.IsTrue(monopuppet, "The puppet loader requires a puppet component to go with it.");

			var puppet = new Puppets.PuppetState(tree);
			await puppet.AwaitLoading();

			monopuppet.SetPuppet(puppet);
			monopuppet.puppetState.Play("idle");
			if (!is_managed)
			{
				monopuppet.puppetState.Render();
			}

			hnd.ReleaseAll();
		}

#if UNITY_EDITOR
		[Button]
		private void LoadUnmanaged()
		{
			LoadPuppet(_nameToSearch, false).Forget();
		}
#endif
	}
}