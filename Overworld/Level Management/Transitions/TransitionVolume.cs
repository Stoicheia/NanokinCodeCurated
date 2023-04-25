using Anjin.Cameras;
using Anjin.Nanokin;
using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using Util.Components;

public enum TransitionSide {
	A, B
}


public enum TransitionOrientation {
	Positive, Negative
}


public static class TransformOrientationExtentions
{
	public static TransitionOrientation Opposite(this TransitionOrientation orientation)
	{
		return ( orientation == TransitionOrientation.Negative ) ? TransitionOrientation.Positive : TransitionOrientation.Negative;
	}
}

public abstract class TransitionVolume : AnjinBehaviour
{
	public bool Detectable      = true;
	public bool GenerateCameras = true;

	public CinemachineVirtualCamera SideACam;
	public CinemachineVirtualCamera SideBCam;

	[HideInInspector] public UnityEvent OnEndTransition;
	[HideInInspector] public UnityEvent OnStartTransition;

	[EnumToggleButtons]
	public TransitionOrientation Orientation;

	public bool                       OverrideBlendOutgoing;
	public CinemachineBlendDefinition BlendOutgoing = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseIn, 1);

	public bool                       OverrideBlendIncoming;
	public CinemachineBlendDefinition BlendIncoming = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut,    0);

	void Start()
	{
		if (!Application.isPlaying) return;
		GameController.Live.OnFinishLoadLevel_1 += OnFinishLoadLevel1;
	}

	private void Awake()
	{
		if (SideACam) SideACam.Priority = -1;
		if (SideBCam) SideBCam.Priority = -1;
	}

	void OnDestroy()
	{
		if (!Application.isPlaying) return;
		if (GameController.Live == null) return;
		GameController.Live.OnFinishLoadLevel_1 -= OnFinishLoadLevel1;
	}

	void OnFinishLoadLevel1(Level level)
	{
		if (GenerateCameras) {
			if (SideACam == null) {
				SideACam                    = GameCams.New(transform);
				SideACam.transform.position = GetSideA(3) + Vector3.up * 4;
				SideACam.transform.LookAt(GetSideB(0));
			}

			if (SideBCam == null) {
				SideBCam                    = GameCams.New(transform);
				SideBCam.transform.position = GetSideB(-1) + Vector3.up * 2.5f;
				SideBCam.transform.LookAt(GetSideA(1) + Vector3.up * 1.5f);
			}
		}
	}

	public Vector3 GetSide(TransitionSide side)
	{
		switch (side) {
			case TransitionSide.A: return GetSideA();
			case TransitionSide.B: return GetSideB();
		}
		return Vector3.zero;
	}

	public Matrix4x4 GetMatrix() => Matrix4x4.TRS(Vector3.zero, transform.rotation, transform.localScale);

	public abstract Vector3               GetSideA(float                extrusion = 0);
	public abstract Vector3               GetSideB(float                extrusion = 0);
	public abstract TransitionOrientation GetOrientationFromPos(Vector3 pos);

	public virtual Collider    GetCollider()                                 => null;
	public virtual Vector3     GetTargetPositionFromHit(Vector3 HitPosition) => HitPosition;
	public virtual NavMeshPath GetPath(Vector3                  HitPosition) => new NavMeshPath();

	public bool ActiveOverride { get; }

	public void UpdateCams(int activePriority, int inactivePriority)
	{
	}

	public void DeactivateCams(int inactivePriority)
	{
	}

	public bool                       OverridesBlends { get; }
	public CinemachineBlenderSettings Blends          { get; }
	public CinemachineBlendDefinition DefaultBlend    { get; }
}