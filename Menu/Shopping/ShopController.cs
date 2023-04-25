using System;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Nanokin;
using Anjin.Utils;
using Cinemachine;
using Combat.Data.VFXs;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;
using Data.Shops;
using UnityEngine;
using Util.UniTween.Value;

namespace Overworld.Shopping
{
	public class ShopController : StaticBoy<ShopController>, ICamController
	{
		[SerializeField]
		public AnimationCurve CameraBlend;
		[SerializeField]
		public CharacterInteractCamera DefaultCamera;
		[SerializeField]
		public Vector3 DefaultNPCOffset;

		private static TweenableFloat _cameraProgress = 0;
		private static FadeVFX   _playerFadeVFX;

		[NonSerialized]
		public CinemachineVirtualCamera vcam;

		// private TweenableFloat _playerOpacity = 1;
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{
			ShopMenu.shop = null;
			_cameraProgress = new TweenableFloat();
		}

		public override void Awake()
		{
			base.Awake();
			_cameraProgress = new TweenableFloat();
			_playerFadeVFX  = new FadeVFX(1, EaserTo.Linear, EaserTo.Linear);
		}

		// public static async UniTask Open(ShopNPC npc)
		// {
		// 	// Load the scene
		// 	await EnsureLoaded();
		//
		// 	ShopMenu.Live.MenuCenterObject       = npc.transform;
		// 	ShopMenu.Live.MenuCenterObjectOffset = npc.NPCMenuOffset;
		//
		// 	Live.vcam = npc.VCam;
		//
		// 	OpenAsync(npc.Shop).Forget();
		// }

		public static async UniTask Open(Shop shop, Transform npc)
		{
			// Load the scene
			await EnsureLoaded();

			CinemachineVirtualCamera vcam   = null;
			Vector3                  offset = Live.DefaultNPCOffset;

			if (npc.TryGetComponent(out ShopNPC shopnpc))
			{
				offset = shopnpc.NPCMenuOffset;
				vcam   = shopnpc.VCam;
			}

			if (vcam == null)
			{
				Live.DefaultCamera.SetPlayerInteraction(ActorController.playerActor.transform, npc);
				vcam = Live.DefaultCamera.vcam;
			}

			ShopMenu.Live.MenuCenterObject       = npc.transform;
			ShopMenu.Live.MenuCenterObjectOffset = offset;
			Live.vcam                            = vcam;

			GameCams.Push(Live);

			await OpenAsync(shop);
		}

		public void Open(Transform npc, CinemachineVirtualCamera vcam, Shop shop)
		{
			ShopMenu.Live.MenuCenterObject       = npc;
			ShopMenu.Live.MenuCenterObjectOffset = Live.DefaultNPCOffset;

			GameCams.Push(Live);
			Live.vcam = vcam;
			OpenAsync(shop).Forget();
		}

		/// <summary>
		/// Enter the UI with animations and full-screen effects. Sick bloom, subdued vignette atmosphere, the full 4K synthwave performance!
		/// </summary>
		public static async UniTask OpenAsync(Shop shop)
		{
			if (ShopMenu.menuActive) return;

			GameController.Live.StateApp = GameController.AppState.Menu;

			Actor playerActor = ActorController.playerActor;

			// Auto-walk the player to the predetermined position for UI. We will give this a shot again in the future.
			// MoveTowardsBrain moveTowardsBrain = playerActor.transform.AddComponent<MoveTowardsBrain>();
			// moveTowardsBrain.Destination = PositionPlayer;
			// moveTowardsBrain.ExitBrain   = ActorController.playerBrain;
			// playerActor.SetExternalBaseBrain(moveTowardsBrain);

			// Fade out the player with a VFX.
			VFXManager vfxman = playerActor.GetComponentInChildren<VFXManager>();
			vfxman.Add(_playerFadeVFX);

			_cameraProgress.FromTo(0, 1, EaserTo.Default);


			GameCams.SetController(Live);

			ShopMenu.shop = shop;
			ShopMenu.exitHandler = menu =>
			{
				CloseAsync().Forget();
			};
			await ShopMenu.EnableMenu();
		}

		public static async UniTask EnsureLoaded()
		{
			if (!ShopMenu.Exists)
			{
				await SceneLoader.GetOrLoadAsync("MENU_Shop");
			}
		}

		/// <summary>
		/// Exit the UI with animations. Revert back to player playable.
		/// </summary>
		public static async UniTask CloseAsync()
		{
			if (!ShopMenu.menuActive) return;

			_cameraProgress.To(0, EaserTo.Default);

			_playerFadeVFX.Leave();

			GameCams.Pop(Live);
			await ShopMenu.DisableMenu();

			GameController.Live.StateApp = GameController.AppState.InGame;
		}

		public void OnActivate()
		{
			vcam.Priority = GameCams.PRIORITY_ACTIVE;
		}

		public void OnRelease(ref CinemachineBlendDefinition? blend)
		{
			vcam.Priority = GameCams.PRIORITY_INACTIVE;
		}

		public void ActiveUpdate() { }

		public void GetBlends(ref CinemachineBlendDefinition? blend, ref CinemachineBlenderSettings settings)
		{
			blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Custom, CameraBlend.keys[CameraBlend.length - 1].time)
			{
				m_CustomCurve = CameraBlend
			};
		}
	}
}