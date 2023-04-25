using System;
using System.Reflection.Emit;
using Anjin.Actors;
using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using Anjin.UI;
using Anjin.Util;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Data.Shops;
using DG.Tweening;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using Util.Odin.Attributes;
using Util.UniTween.Value;

namespace Overworld.Cutscenes
{
	[LuaUserdata(staticAuto: true)]
	public class ItemGetCutscene : StaticBoy<ItemGetCutscene>
	{
		public bool Showing;

		public Cutscene Cutscene;

		public LabelElement Label_Bottom;
		public LabelElement Label_Top;

		public Transform DisplayBillboard3D;
		public Transform DisplayBillboard2D;
		public Transform DisplayRoot2D;
		public Transform DisplayRoot3D;

		public ParticleSystem Part_Sparkles;
		public ParticleSystem Part_Glow;

		public CinemachineVirtualCamera Camera;


		private Transform _displayRoot;

		private float _startingDisplayScale = 0.6f;
		private float _displayScale         = 1;

		private bool _showTop;
		private bool _showBottom;
		private bool _controlCam;

		[DebugVars]
		private Vector3 _cameraInitialPosition;
		private float          _cameraInitialFOV;
		private float          _defaultCameraDistance;
		private TweenableFloat _currentDisplayScale;
		private GameObject     _spawnedDisplayPrefab;
		private GameObject     _spawnedDisplaySprite;
		private SpriteRenderer _spawnedDisplaySpriteRend;

		public Vector2 SpawnedDisplayPrefabBoundsSize;

		private LootDisplayHandle _display;

		private Table _currentOptions;

		protected override void OnAwake()
		{
			if (Camera)
			{
				_cameraInitialPosition = Camera.transform.localPosition;
				_cameraInitialFOV      = Camera.m_Lens.FieldOfView;
				_defaultCameraDistance = Camera.transform.localPosition.xz().magnitude;
			}
		}

		private void Start()
		{
			_currentDisplayScale = new TweenableFloat();
			Showing              = false;

			Label_Top.gameObject.SetActive(true);
			Label_Bottom.gameObject.SetActive(true);

			Label_Top.Alpha    = 0;
			Label_Bottom.Alpha = 0;

			DisplayRoot3D.SetActive(false);
			DisplayBillboard2D.SetActive(false);

			//await Cutscene.initTask;
		}

		[UsedImplicitly]
		public static Cutscene get_cut()
		{
			return Live.Cutscene;
		}

		private void Update()
		{
			if (ActorController.playerActor != null)
			{
				var pos = ActorController.playerActor.transform.position;
				DisplayBillboard3D.transform.position = pos;
				DisplayRoot2D.transform.position      = pos;
			}

			if (Showing)
			{
				if (_display.method == LootDisplayMethod.Sprite && _spawnedDisplaySprite != null)
				{
					_spawnedDisplaySpriteRend.SetWorldScale(Vector2.one * _currentDisplayScale);
				}
				else if (_display.method == LootDisplayMethod.Prefab && _spawnedDisplayPrefab)
				{
					float ratio = _currentDisplayScale / SpawnedDisplayPrefabBoundsSize.y;
					_spawnedDisplayPrefab.transform.localScale = Vector3.one * ratio;
				}
			}
		}

		public static void Setup(Table options)
		{
			Live.setup(options);
			//Live._currentOptions = options;
		}

		private void setup(Table options)
		{
			Reset(); // Reset from last state

			if (options.TryGet("controls_cam", out bool controls))
				Cutscene.runningScript["cam_control"] = controls;

			float cam_dist = _defaultCameraDistance;

			if (options.TryGet("cam_dist", out float num))
			{
				cam_dist = num;
			}

			if (options.TryGet("cam_alignment", out DynValue val))
			{
				if (val.AsUserdata(out Vector2 xzAlignment))
				{
					Vector2 final_xz = xzAlignment * cam_dist;

					var pos = Camera.transform.localPosition;
					Camera.transform.localPosition = new Vector3(final_xz.x, pos.y, final_xz.y);
				}
			}

			if (options.TryGet("cam_fov", out float fov))
			{
				Camera.m_Lens.FieldOfView = fov;
			}

			if (options.TryGet("blend_in", out CinemachineBlendDefinition blend_in))
				Cutscene.runningScript["blend_in"] = blend_in;

			if (options.TryGet("blend_out", out CinemachineBlendDefinition blend_out))
				Cutscene.runningScript["blend_out"] = blend_out;
		}

		public static void Reset() => Live.reset();

		public void reset()
		{
			Cutscene.runningScript["blend_in"]    = DynValue.Nil;
			Cutscene.runningScript["blend_out"]   = DynValue.Nil;
			Cutscene.runningScript["cam_control"] = false;

			if (Camera)
			{
				Camera.transform.localPosition = _cameraInitialPosition;
				Camera.m_Lens.FieldOfView      = _cameraInitialFOV;
			}
		}

		[UsedImplicitly]
		public static void show() => Live.Show();

		[UsedImplicitly]
		public static void hide() => Live.Hide();

		[Button]
		public void Show()
		{
			//setup(_currentOptions);
			//_currentOptions = null;

			Showing = true;

			if (_displayRoot)
			{
				_displayRoot.SetActive();

				_displayRoot.transform.localPosition = new Vector3(0, 1, 0);
				_displayRoot.transform.DOLocalJump(new Vector3(0, 2.75f, 0), 0.75f, 0, 0.5f).onComplete += () =>
				{
					Part_Sparkles.Play();
					Part_Glow.Play();
				};
			}

			_currentDisplayScale.FromTo(_startingDisplayScale, _displayScale, new EaserTo(0.5f, Ease.InElastic));

			if (_showBottom)
			{
				Label_Bottom.DoAlphaFade(0, 1, 0.5f);
				Label_Bottom.SequenceOffset.FromTo(new Vector3(0, -100, 0), Vector3.zero, new JumperTo(0.5f, 80));
				Label_Bottom.Scale.FromTo(Vector3.one * 0.7f, Vector3.one, new EaserTo(0.25f, Ease.OutSine));
			}

			if (_showTop)
			{
				Label_Top.DoAlphaFade(0, 1, 0.5f);
				Label_Top.SequenceOffset.FromTo(new Vector3(0, 100, 0), Vector3.zero, new EaserTo(1.0f, Ease.OutBounce));
				Label_Top.Scale.FromTo(Vector3.one * 0.7f, Vector3.one, new EaserTo(0.25f, Ease.OutSine));
			}
		}

		[Button]
		public async void Hide()
		{
			if (_showBottom)
			{
				Label_Bottom.DoAlphaFade(1, 0, 0.5f);
				Label_Bottom.DoOffset(new Vector3(0, -50, 0), 0.5f);
			}

			if (_showTop)
			{
				Label_Top.DoAlphaFade(1, 0, 0.5f);
				Label_Top.DoOffset(new Vector3(0, 50, 0), 0.5f);
			}

			if (_displayRoot)
				_displayRoot.transform.DOLocalMoveY(20, 0.9f);

			await UniTask.Delay(TimeSpan.FromSeconds(1));

			if (_displayRoot)
				_displayRoot.SetActive(false);

			Destroy(_spawnedDisplayPrefab);
			Destroy(_spawnedDisplaySprite);
			_displayRoot = null;
			Showing      = false;

			Part_Sparkles.Stop();
			Part_Glow.Stop();

			_display.ReleaseSafe();

			// Note:
			// We used to call Reset() at the end here. It was error prone and lead to bugs because the behavior
			// of Reset() camera tries to reset the vcam, however it could still be busy animating which leads to
			// camera jerks. Therefore we just leave the old state as is and reset right at the start of Show().
		}

		public static WaitableUniTask set_loot(LootEntry item)
		{
			return new WaitableUniTask(Live.setLoot(item));
		}

		[Button, ShowInInspector]
		[MoonSharpHidden]
		public static void SetLoot(LootEntry item, string customTextTop = "", string customTextBottom = "")
		{
			Live.setLoot(item, customTextTop, customTextBottom);
		}

		private async UniTask setLoot(LootEntry item, string customTextTop = "", string customTextBottom = "")
		{
			_display.ReleaseSafe();

			_startingDisplayScale = 0.6f;
			_displayScale         = 1f;

			Label_Top.Label.text    = "";
			Label_Bottom.Label.text = "";

			_showTop    = false;
			_showBottom = true;

			if (item != null)
			{
				switch (item.Type)
				{
					case LootType.Limb:
						_showTop = true;
						Label_Top.Label.text = "You obtained a limb!";
						Label_Bottom.Label.text = $"{item.GetName()}!";

						_startingDisplayScale = 1.5f;
						_displayScale = 3f;
						break;

					case LootType.Item:
						Label_Bottom.Label.text = $"You got a {item.GetName()}!";
						break;

					case LootType.Sticker:
						Label_Bottom.Label.text = $"You got a {item.GetName()} sticker!";
						_startingDisplayScale = 0.8f;
						_displayScale = 1.5f;
						break;
				}
				_display = await item.LoadDisplay();
			}

			if (customTextTop != "")
			{
				_showTop = true;
				Label_Top.Label.text = customTextTop;
			}

			if (customTextBottom != "")
			{
				_showBottom = true;
				Label_Bottom.Label.text = customTextBottom;
			}



			switch (_display.method)
			{
				case LootDisplayMethod.None:
					break;

				case LootDisplayMethod.Sprite when _display.sprite != null:
					_spawnedDisplaySprite = new GameObject("Display Sprite (spawned)");
					_spawnedDisplaySprite.transform.SetParent(DisplayBillboard2D, false);
					_spawnedDisplaySpriteRend                                 = _spawnedDisplaySprite.AddComponent<SpriteRenderer>();
					_spawnedDisplaySpriteRend.sprite                          = _display.sprite;
					_spawnedDisplaySpriteRend.gameObject.transform.localScale = Vector3.one * 0.1f;

					_spawnedDisplaySpriteRend.SetWorldScale(Vector2.one * _currentDisplayScale);

					_displayRoot = DisplayBillboard2D;
					break;

				case LootDisplayMethod.Prefab when _display.prefab != null:
					_spawnedDisplayPrefab                      = Instantiate(_display.prefab, DisplayRoot3D);
					_spawnedDisplayPrefab.transform.localScale = Vector3.one * 1.5f;

					MeshRenderer[] meshes = _spawnedDisplayPrefab
						.GetComponentsInChildren<MeshRenderer>();

					Bounds largest = new Bounds(Vector3.zero, Vector3.zero);

					foreach (MeshRenderer mesh in meshes)
					{
						mesh.shadowCastingMode = ShadowCastingMode.Off;
						if (mesh.bounds.size.x > largest.size.x) largest.size = new Vector3(mesh.bounds.size.x, largest.size.y, largest.size.z);
						if (mesh.bounds.size.y > largest.size.y) largest.size = new Vector3(largest.size.x, mesh.bounds.size.y, largest.size.z);
					}

					SpawnedDisplayPrefabBoundsSize = Vector2.one;

					if (largest.size.magnitude > 0)
					{
						SpawnedDisplayPrefabBoundsSize = largest.size.xy();

						float ratio = _currentDisplayScale / SpawnedDisplayPrefabBoundsSize.y;
						_spawnedDisplayPrefab.transform.localScale = Vector3.one * ratio;
					}

					_displayRoot = DisplayRoot3D;


					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}