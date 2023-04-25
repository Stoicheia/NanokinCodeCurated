using System;
using System.Collections;
using Anjin.Actors;
using Anjin.Util;
using Cinemachine;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Util;
using Util.Components.Cinemachine;
using Object = UnityEngine.Object;

namespace Anjin.Cameras
{
	public class CharacterInteractCamera : MonoBehaviour, IRecyclable
	{
		[NonSerialized] public CinemachineVirtualCamera     vcam;
		[NonSerialized] public CinemachineTargetGroup       gotarget;
		[NonSerialized] public VCamTarget					wptarget;
		[NonSerialized] public CinemachineOrbitalTransposer orbit;

		private float     _bias;
		private Transform _interaction;
		private Actor     _interactionActor;

		public LensSettings baseLens;
		public bool ForceActiveAlways = true;

		private void Awake()
		{
			// Group Target
			// ----------------------------------------
			GameObject gotarget = new GameObject("Interact Cam Gameobject Target");
			gotarget.transform.SetParent(GameCams.Live.CharInteractRoot);
			this.gotarget = gotarget.AddComponent<CinemachineTargetGroup>();
			this.gotarget.m_UpdateMethod = CinemachineTargetGroup.UpdateMethod.Update;


			// WorldPoint Target
			// ----------------------------------------
			GameObject wptarget = new GameObject("Interact Cam WorldPoint Target");
			wptarget.transform.SetParent(GameCams.Live.CharInteractRoot);
			this.wptarget = wptarget.AddComponent<VCamTarget>();

			// Camera
			// ----------------------------------------
			vcam     = gameObject.GetComponent<CinemachineVirtualCamera>();
			baseLens = vcam.m_Lens;
			//SetTargetToGroup();

			orbit = vcam.GetCinemachineComponent<CinemachineOrbitalTransposer>();

			_bias = Mathf.Abs(orbit.m_Heading.m_Bias);
		}

		private void LateUpdate()
		{

			if (ReferenceEquals(GameCams.Live.Brain.ActiveVirtualCamera, vcam))
			{
				UpdateForward();

				orbit.m_XAxis.Value = 0;
			}
			else
			{
				//vcam.m_LookAt = null;
				//vcam.m_Follow = null;
				vcam.ForceCameraPosition(GameCams.Live.UnityCam.transform.position, GameCams.Live.UnityCam.transform.rotation);
			}
		}

		public void UpdateForward()
		{
			if (_interactionActor != null)
			{
				gotarget.transform.forward = _interactionActor.targetFacing;
			}
			else if (_interaction != null)
			{
				gotarget.transform.forward = _interaction.forward;
			}
		}

		public void SetTargetToGroup()
		{
			vcam.m_Follow = gotarget.transform;
			vcam.m_LookAt = gotarget.transform;
		}

		public void SetInteractionFocus(Transform interaction)
		{
			SetTargetToGroup();
			gotarget.m_RotationMode = CinemachineTargetGroup.RotationMode.Manual;
			_interaction          = interaction;
			_interactionActor     = interaction.gameObject.GetComponent<Actor>();

			UpdateForward();
		}

		public void SetPlayerInteraction(Transform interaction)
		{
			Transform player = ActorController.playerActor.transform;
			//GameCams.Live.ForceSetPosition(vcam, GameCams.Live.UnityCam.transform);

			SetPlayerInteraction(player, interaction);
		}

		public void SetInteraction(Transform interaction)
		{
			Array.Resize(ref gotarget.m_Targets, 2);
			gotarget.m_Targets[0] = new CinemachineTargetGroup.Target { target = interaction, weight = 1, radius = 1 };

			Vector2 spInteraction = GameCams.WorldToViewport(interaction.transform.position);

			orbit.m_Heading.m_Bias = spInteraction.x > 0.5f ? -_bias : _bias;

			SetInteractionFocus(interaction);

			UpdateLens();
			//GameCams.Live.ForceSetPosition(vcam, GameCams.Live.UnityCam.transform);
		}

		/// <summary>
		/// Makes the camera look to focus on an interaction.
		/// </summary>
		/// <param name="player">The player.</param>
		/// <param name="interaction">The source of the interaction, or npc.</param>
		public void SetPlayerInteraction(Transform player, Transform interaction)
		{
			Array.Resize(ref gotarget.m_Targets, 2);
			gotarget.m_Targets[0] = new CinemachineTargetGroup.Target { target = player, weight      = 1, radius = 1 };
			gotarget.m_Targets[1] = new CinemachineTargetGroup.Target { target = interaction, weight = 1, radius = 1 };

			Vector2 spPlayer      = GameCams.WorldToScreen(player.transform.position);
			Vector2 spInteraction = GameCams.WorldToScreen(interaction.transform.position);

			orbit.m_Heading.m_Bias = spPlayer.x < spInteraction.x ? -_bias : _bias;

			SetInteractionFocus(interaction);

			UpdateLens();


			vcam.ForceCameraPosition(GameCams.Live.UnityCam.transform.position, GameCams.Live.UnityCam.transform.rotation);
			//GameCams.Live.ForceSetPosition(vcam, GameCams.Live.UnityCam.transform);
		}

		public void LookAtTarget(Transform target)
		{
			vcam.m_Follow = null;
			vcam.m_LookAt = target.transform;

			UpdateLens();

			vcam.ForceCameraPosition(GameCams.Live.UnityCam.transform.position, GameCams.Live.UnityCam.transform.rotation);
			//GameCams.Live.ForceSetPosition(vcam, GameCams.Live.UnityCam.transform);
		}

		public void LookAtTarget(WorldPoint target)
		{
			wptarget.Point = target;
			wptarget.Type  = CamTargetType.WorldPoint;

			vcam.m_Follow = null;
			vcam.m_LookAt = wptarget.transform;

			UpdateLens();

			vcam.ForceCameraPosition(GameCams.Live.UnityCam.transform.position, GameCams.Live.UnityCam.transform.rotation);
			//GameCams.Live.ForceSetPosition(vcam, GameCams.Live.UnityCam.transform);
		}

		public void UpdateLens(LensSettings? lens = null)
		{
			if (lens == null)
				vcam.m_Lens = baseLens;
			else
				vcam.m_Lens = lens.Value;
		}

		public void Recycle()
		{
			_interaction = null;
			vcam.m_LookAt = null;
			vcam.m_Follow = null;
		}
	}
}