using Drawing;
using Sirenix.OdinInspector;

namespace Util.Components
{
	/// <summary>
	/// We want to use SMB everywhere.. but we also want to use ALINE without
	/// boilerplate... enter the AnjinBehaviour
	/// This class extends SerializedMonoBehaviour and re-implements
	/// MonoBehaviourGizmos
	/// </summary>
	public class AnjinBehaviour : SerializedMonoBehaviour, IDrawGizmos
	{
		public AnjinBehaviour()
		{
			OnRegisterDrawer();
		}

		protected virtual void OnRegisterDrawer()
		{
			DrawingManager.Register(this);
		}

		// Why an empty OnDrawGizmos method?
		// This is because only objects with an OnDrawGizmos method will show up in Unity's menu for enabling/disabling
		// the gizmos per object type (upper right corner of the scene view). So we need it here even though
		// we don't use normal gizmos.
		// ReSharper disable once Unity.RedundantEventFunction
		protected virtual void OnDrawGizmos()
		{ }

		public virtual void DrawGizmos()
		{ }
	}

}