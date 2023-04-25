using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Anjin.Actors;
using Anjin.Nanokin;
using Anjin.Scripting;
using Cinemachine;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Util;
using Util.Components;
using Util.Odin.Attributes;
using g = ImGuiNET.ImGui;

namespace Anjin.Cameras
{
	[DefaultExecutionOrder(-15000)]
	public class GameCams : StaticBoy<GameCams>
	{
		public const int PRIORITY_INACTIVE = -1;
		public const int PRIORITY_ACTIVE   = 10;

		[Title("References")]
		public CinemachineBrain Brain;
		public Camera           UnityCam;
		public AudioListener    Listener;
		public Transform        TargetsRoot;
		public Transform        CharInteractRoot;
		public int              BlendLock;
		public GameObject       VCamCharacterInteractPrefab;
		public PostProcessLayer PostProcess;

		[Title("Design")]
		public CinemachineBlenderSettings DefaultBlendSettings;
		public CinemachineBlendDefinition DefaultBlend;

		public bool ForceVirtualCamerasActive = true;

		// Blend Override
		[NonSerialized, ShowInPlay] private bool                        _hasBlendOverride;
		[NonSerialized, ShowInPlay] private ICamController              _blendOverridecontroller;
		[NonSerialized, ShowInPlay] private CinemachineBlendDefinition? _blendOverride;
		[NonSerialized, ShowInPlay] private bool                        _blendOverrideReleaseFlag;

		[DebugVars]
		[NonSerialized, ShowInPlay] public VCamTarget playerTarget;
		[NonSerialized, ShowInPlay] public  ICamController                   _controller;
		[NonSerialized, ShowInPlay] public  List<StackController>            _controllerStack;
		[NonSerialized, ShowInPlay] private List<CinemachineBlendDefinition> _nextBlends;
		[NonSerialized, ShowInPlay] public  CinemachineBlenderSettings       _debug_currentBlendSettings;
		[NonSerialized, ShowInPlay] public  CinemachineBlendDefinition       _debug_currentBlend;
		[NonSerialized, ShowInPlay] public  GObjectPool                      _characterInteractionPool;


		[NonSerialized]
		public static Vector3 currentEuler;

		public bool InputAffectsCamera => GameController.Live.CanControlPlayer();

		public static bool IsBlending => Live.Brain.IsBlending;

		public static readonly CinemachineBlendDefinition Cut = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0);
		private                CinemachineVirtualCamera   _activeVirtualCamera;

		protected override void OnAwake()
		{
			DebugSystem.onLayout += OnDebugLayout;

			_controllerStack = new List<StackController>();
			_nextBlends      = new List<CinemachineBlendDefinition>();

			_characterInteractionPool = new GObjectPool(CharInteractRoot, VCamCharacterInteractPrefab)
			{
				allocateTemp = true,
				maxSize      = 16,
				initSize     = GameOptions.current.pool_on_demand ? 0 : 6
			};

			// We only need one target for the player
			playerTarget      = NewTarget(transform);
			playerTarget.Type = CamTargetType.Player;

			OccluderSystem.SetReferencePoint(Live.UnityCam.transform);

			currentEuler = transform.eulerAngles;
		}

		[LuaGlobalFunc("cam_release_controller")]
		public static void ReleaseController() => Live.setController();

		[LuaGlobalFunc("cam_set_controller")]
		public static void SetController(ICamController newController = null) => Live.setController(newController);

		public static void SetController(CinemachineVirtualCamera newController, CinemachineBlendDefinition cut)
		{
			SetController(new CamController(newController, cut));
		}

		/// <summary>
		/// Allows making a camera
		/// </summary>
		/// <param name="next"></param>
		[LuaGlobalFunc("cam_push")]
		public static void Push(ICamController next)
		{
			if (next == null) return;

			Live._controllerStack.Add(new StackController(next, Live._controller));
			SetController(next);
		}

		[LuaGlobalFunc("cam_push")]
		public static void Pop(ICamController source = null)
		{
			List<StackController> stack = Live._controllerStack;

			if (stack.Count == 0)
			{
				Live.LogError("Cannot pop because stack is empty.");
				return;
			}

			// Find the last
			int last = stack.Count - 1;

			StackController ctrl = stack[last];
			if (ctrl.source == source || source == null)
			{
				stack.RemoveAt(last);
				SetController(ctrl.previous);
			}
			else if (stack.Count >= 2)
			{
				// Search for the last of this source, it may have been overriden
				for (int i = stack.Count - 1; i >= 0; i--)
				{
					StackController c = stack[i];
					if (c.source == source)
					{
						stack.RemoveAt(i);
						return;
					}
				}

				// didn't find anything
				// uhhh probably no good
			}
		}

		private void setController([CanBeNull] ICamController newController = null)
		{
			ICamController              prevController = _controller;
			CinemachineBlendDefinition? exitBlend      = null;
			CinemachineBlendDefinition? enterBlend     = null;

			if (newController == prevController)
				return;

			if (newController == null && ActorController.playerCamera != null)
			{
				setController(ActorController.playerCamera);
				return;
			}

			if (newController is PlayerCameraRig camrig)
			{
				camrig.Teleport(UnityCam.transform.position, UnityCam.transform.forward);
			}

			if (prevController != null)
				prevController.OnRelease(ref exitBlend);

			if (newController != null)
			{
				_controller = newController;
				newController.OnActivate();
				newController.GetBlends(ref enterBlend, ref _debug_currentBlendSettings); // Need this if we want to animate into a cam controller with its custom blend
				newController.ActiveUpdate();
			}

			CinemachineBlendDefinition blend = exitBlend ?? enterBlend ?? DefaultBlend;
			if (BlendLock <= 0)
			{
				if (Math.Abs(blend.m_Time) > 0.0001 || blend.m_Style != CinemachineBlendDefinition.Style.Cut) {
					Brain.m_DefaultBlend = blend;
				} else {
					Brain.m_DefaultBlend = Cut;
				}

				BlendLock = 2;
			}

			Brain.ManualUpdate();
		}

		[LuaGlobalFunc("cam_new")]
		public static CinemachineVirtualCamera New(Transform root = null)
		{
			var go = new GameObject("VCam");
			go.transform.parent = root;

			CinemachineVirtualCamera cam = go.AddComponent<CinemachineVirtualCamera>();
			cam.Priority = PRIORITY_INACTIVE;

			return cam;
		}

		[LuaGlobalFunc("cam_new_target")]
		public static VCamTarget NewTarget(Transform root = null)
		{
			var go = new GameObject("VCam Target");
			go.transform.parent = root;
			VCamTarget target = go.AddComponent<VCamTarget>();
			return target;
		}

		public static CharacterInteractCamera RentCharacterInteraction()
		{
			CharacterInteractCamera cam = Live._characterInteractionPool.Rent<CharacterInteractCamera>();
			return cam;
		}

		public static void ReturnCharacterInteraction(CharacterInteractCamera interactCamera)
		{
			Live._characterInteractionPool.Return(interactCamera);
		}

		/*private void OnEnable()
		{
			Brain.m_UpdateMethod      = CinemachineBrain.UpdateMethod.SmartUpdate;
			Brain.m_BlendUpdateMethod = CinemachineBrain.BrainUpdateMethod.FixedUpdate;
			Debug.Log("Brain Cam is being updated from GameCams.cs.");
		}

		private void OnDisable()
		{
			Brain.m_UpdateMethod      = CinemachineBrain.UpdateMethod.LateUpdate;
			Brain.m_BlendUpdateMethod = CinemachineBrain.BrainUpdateMethod.LateUpdate;
		}*/

		public void Update()
		{
			// Update Blends
			//----------------------------------------------------------------------

			if (BlendLock <= 0 && !Brain.IsBlending)
			{
				CinemachineBlendDefinition? blend    = DefaultBlend;
				CinemachineBlenderSettings  settings = DefaultBlendSettings;

				if (_controller != null)
				{
					_controller.ActiveUpdate();
					_controller.GetBlends(ref blend, ref _debug_currentBlendSettings);
				}

				if (_hasBlendOverride) {
					if (_blendOverrideReleaseFlag || _blendOverridecontroller == null || !_blendOverride.HasValue) {
						_blendOverrideReleaseFlag     = false;
						Live._blendOverridecontroller = null;
						Live._hasBlendOverride        = false;
						Live._blendOverride           = null;
					} else {
						blend    = _blendOverride;
						settings = null;

						BlendLock = 2;
					}
				}

				// ReSharper disable once PossibleInvalidOperationException
				Brain.m_DefaultBlend = blend.Value;
				Brain.m_CustomBlends = settings;

				_debug_currentBlendSettings = settings;
				_debug_currentBlend         = blend.Value;
			}

			if(ForceVirtualCamerasActive)
				_characterInteractionPool.ActivateAll(); //this is garbage, need to refactor after build


			//TODO: Disable game audio when loading level.
			//Listener.enabled = !GameController.Live.IsLoading;

			currentEuler = UnityCam.transform.eulerAngles;
		}

		private void LateUpdate()
		{
			if (!IsBlending && BlendLock > 0)
			{
				BlendLock--;
			}

			UnityCam.orthographic = false;
			if (CinemachineCore.Instance.GetVirtualCamera(0) is var cam)
			{
				if (cam.gameObject.gameObject.gameObject.GetComponent<OrthoVCam>() != null)
				{
					UnityCam.orthographic = true;
				}
			}

			currentEuler = UnityCam.transform.eulerAngles;
			//Brain.ManualUpdate();


		}

	#region Utilities

		public static float DistanceTo(Vector3 pos)
		{
			return Vector3.Distance(pos, Live.UnityCam.transform.position);
		}

		public static Vector2 WorldToScreen(Vector3 worldPoint)
		{
			if (!Live.UnityCam)
				return Vector2.zero;

			return Live.UnityCam.WorldToScreenPoint(worldPoint);
		}

		public static Vector2 WorldToViewport(Vector3 worldPoint)
		{
			if (!Live.UnityCam)
				return Vector2.zero;

			return Live.UnityCam.WorldToViewportPoint(worldPoint);
		}

		public static (RaycastHit, bool) RaycastFromCamera(Vector2 screenPoint, int layerMask, float maxDistance = 10000)
		{
			if (!Live.UnityCam) return (new RaycastHit(), false);

			RaycastHit hit;
			var        ray = Live.UnityCam.ScreenPointToRay(screenPoint);

			return Physics.Raycast(ray, out hit, maxDistance, layerMask) ? (hit, true) : (new RaycastHit(), false);
		}

	#endregion

		private void OnDebugLayout(ref DebugSystem.State state)
		{
			if (!state.IsMenuOpen("Game Cams"))
				return;

			if (g.Begin("Cameras"))
			{
				g.Text("Controller: " + _controller);
				//AnjinGui.DrawEnum("Blend Type:", ref Brain.m_DefaultBlend.m_Style);
				g.Text("Blend Time:" + Brain.m_DefaultBlend.m_Time);
				g.InputInt("Blend Lock", ref BlendLock);
				//AnjinGui.EditStruct(ref Debug_CurrentBlend);
			}

			g.End();
		}

		public struct StackController
		{
			public ICamController source;
			public ICamController previous;

			public StackController(ICamController source, ICamController previous)
			{
				this.source   = source;
				this.previous = previous;
			}
		}

		public static void NextBlend(CinemachineBlendDefinition cut)
		{
			Live._nextBlends.Add(cut);
		}

		public static void SetBlendOverride(CinemachineBlendDefinition def, ICamController controller)
		{
			DebugLogger.Log("Set Blend Override", LogContext.Overworld, LogPriority.Low);
			Live._blendOverridecontroller = controller;
			Live._hasBlendOverride        = true;
			Live._blendOverride           = def;
		}

		public static void ReleaseBlendOverride()
		{
			DebugLogger.Log("Release Blend Override", LogContext.Overworld, LogPriority.Low);
			Live.BlendLock                 = 2;
			Live._blendOverrideReleaseFlag = true;
			/*Live._blendOverridecontroller = null;
			Live._hasBlendOverride        = false;
			Live._blendOverride           = null;*/
		}

		/*public static void FinishBlend()
		{
			Live.Brain.ActiveBlend.
		}*/

		private static FieldInfo _mFrameStackInfo = typeof(CinemachineBrain).GetField("mFrameStack", BindingFlags.Instance | BindingFlags.NonPublic);
		[Button]
		public void MessyHackToResetCinemachineBrainStack()
		{
			IList list = (IList)_mFrameStackInfo.GetValue(Brain);

			while (list.Count > 1) {
				list.RemoveAt(list.Count - 1);
			}
		}



	}
}