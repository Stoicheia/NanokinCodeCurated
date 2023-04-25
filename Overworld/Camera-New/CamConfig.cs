using System;
using Anjin.Editor;
using Anjin.Regions;
using Anjin.Scripting;
using Anjin.Util;
using Cinemachine;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

// ReSharper disable UnusedMember.Global

namespace Anjin.Cameras
{
	[Flags, EnumToggleButtons]
	public enum TargetVariation
	{
		None	= 0,
		Follow  = 1,
		LookAt	= 2
	}

	[EnumToggleButtons]
	public enum CameraVariation
	{
		Static 		= 0,
		FixedOffset = 1,
		HOrbit 		= 2,
	}

	public class CamConfig
	{
		[Title("Base Settings")]
		public bool 			IsOrthographic 	= false;
		public LensSettings 	Lens 			= VCamProxyNormal.DefaultLens;
		[InlineBox]
		public CamSpwanParams 	SpawnParams = new CamSpwanParams();

		[ToggleButton]
		public bool ConfineToBox;
		[ShowIf("@this.ConfineToBox"), HideLabel, InlineBox]
		public CamConfinementParams ConfinementParams = new CamConfinementParams();

		[Title("Blends")]
		[ToggleButton]
		public bool OverrideBlendIncoming;
		[ShowIf("@this.OverrideBlendIncoming"), HideLabel]
		public CinemachineBlendDefinition BlendIncoming;

		[ToggleButton]
		public bool OverrideBlendOutgoing;
		[ShowIf("@this.OverrideBlendOutgoing"), HideLabel]
		public CinemachineBlendDefinition BlendOutgoing;

		[Title("Behaviour")]
		[HideLabel]
		public CameraVariation CamVariation = CameraVariation.HOrbit;

		[BoxGroup("Targeting"), LabelText("Type"), LabelWidth(64)]
		public TargetVariation TargetVariation = TargetVariation.Follow | TargetVariation.LookAt;

		[BoxGroup("Targeting"), LabelText("Mode"), LabelWidth(64)]
		public CamTargetType 	Targeting = CamTargetType.Player;

		[BoxGroup("Targeting"), ShowIf("@Targeting == Anjin.Cameras.CamTargetType.WorldPoint")]
		public WorldPoint 		TargetWorldPoint 	= WorldPoint.Default;


		[InlineBox, ShowIf("$WillHaveComposer")] 	public ComposerSettings		Settings_Composer	= new ComposerSettings();
		[InlineBox, ShowIf("$WillHaveTransposer")] 	public TransposerSettings	Settings_Transposer	= new TransposerSettings {FollowOffset = new Vector3(0, 3.5f, -6)};

		[InlineBox, HideInInspector] public OrbitalTransposerSettings Settings_OrbitalTransposer = OrbitalTransposerSettings.Default;

		[Title("Debug")]
		[ShowInInspector] public bool AnyType 				=> Targeting != CamTargetType.None;
		[ShowInInspector] public bool WillHaveComposer 		=> AnyType && TargetVariation.HasFlag(TargetVariation.LookAt);
		[ShowInInspector] public bool WillHaveTransposer	=> AnyType && TargetVariation.HasFlag(TargetVariation.Follow);

		public CinemachineVirtualCamera SpawnVCam(Transform root, object spawnParent)
		{
			var cam = GameCams.New(root);

			RegionObjectSpatial spatial = spawnParent as RegionObjectSpatial;

			if(spatial != null) {
				Matrix4x4 mat = Matrix4x4.TRS(
					((SpawnParams.Relative.HasFlag(RelativeMode.Position)) ? spatial.Transform.Position : Vector3.zero),
					((SpawnParams.Relative.HasFlag(RelativeMode.Rotation)) ? spatial.Transform.Rotation : Quaternion.identity),
					Vector3.one );

				cam.transform.position = mat.MultiplyPoint3x4(SpawnParams.Position);
				cam.transform.rotation = ( Matrix4x4.Rotate(Quaternion.Euler(SpawnParams.Rotation)) * mat ).rotation;
			}

			if (Targeting == CamTargetType.Player) {
				if(WillHaveComposer) 	cam.LookAt = GameCams.Live.playerTarget.transform;
				if(WillHaveTransposer)	cam.Follow = GameCams.Live.playerTarget.transform;
			}

			if (WillHaveComposer) {
				var composer = cam.AddCinemachineComponent<CinemachineComposer>();
				Settings_Composer.Apply(composer);
			}

			if (WillHaveTransposer) {
				var transposer = cam.AddCinemachineComponent<CinemachineTransposer>();

				if(spatial != null)
					Settings_Transposer.Apply(transposer, spatial.Transform.Rotation);
				else
					Settings_Transposer.Apply(transposer);
			}

			if (IsOrthographic)
				cam.AddComponent<OrthoVCam>();

			cam.m_Lens = Lens;

			return cam;
		}

		public void ApplyToVCam(CinemachineVirtualCamera camera)
		{
			camera.m_Lens = Lens;
			switch (CamVariation) {
				case CameraVariation.HOrbit:
					break;

				case CameraVariation.FixedOffset:
					break;

				case CameraVariation.Static:
					break;
			}
		}

		public Matrix4x4 GetConfinementMatrix(RegionObjectSpatial robj)
		{
			return Matrix4x4.TRS(((ConfinementParams.Relative.HasFlag(RelativeMode.Position)) ? robj.Transform.Position : Vector3.zero),
								 ((ConfinementParams.Relative.HasFlag(RelativeMode.Rotation)) ? robj.Transform.Rotation : Quaternion.identity), Vector3.one ) *
			       Matrix4x4.Translate(ConfinementParams.Position) *
			       Matrix4x4.Rotate(Quaternion.Euler(ConfinementParams.Rotation));
		}
	}

	[Flags]
	public enum RelativeMode
	{
		None     = 0,
		Position = 1,
		Rotation = 2,
		Scale    = 4
	}

	public class CamSpwanParams
	{
		public RelativeMode Relative = RelativeMode.Position;
		public Vector3 Position;
		public Vector3 Rotation;
	}

	public class CamConfinementParams
	{
		public RelativeMode Relative = RelativeMode.Position | RelativeMode.Rotation;

		public Vector3 Position;
		public Vector3 Rotation;
		public Vector3 Center = Vector3.zero;
		public Vector3 Size = Vector3.one;
	}

	[MoonSharpUserData]
	public class CamConfigLuaProxy : LuaProxy<CamConfig>
	{
		public CameraVariation cam_variation { get => proxy.CamVariation; set => proxy.CamVariation = value; }

		public WorldPoint target { get => proxy.TargetWorldPoint; set => proxy.TargetWorldPoint = value;  }

		public Vector3 composer_tracked_offset { get => proxy.Settings_Composer.TrackedOffset; set => proxy.Settings_Composer.TrackedOffset = value; }

		public void set_fov(float fov) => proxy.Lens.FieldOfView = fov;

		public void composer_set_tracked_offset(float  x, float y, float z) => proxy.Settings_Composer.TrackedOffset 	= new Vector3(x, y, z);
		public void transposer_set_follow_offset(float x, float y, float z) => proxy.Settings_Transposer.FollowOffset 	= new Vector3(x, y, z);
		public void transposer_set_damping(float       x, float y, float z) => proxy.Settings_Transposer.Damping 		= new Vector3(x, y, z);

		public void orbital_set_axis_value(float value) => proxy.Settings_OrbitalTransposer.SetAxisValue(value);
	}

}