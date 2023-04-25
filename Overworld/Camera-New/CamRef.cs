using Anjin.Actors;

namespace Anjin.Cameras
{
	/// <summary>
	/// RUNTIME reference for a cinemachine VCAM
	/// </summary>
	public struct CamRef
	{
		/// <summary>
		/// Cams can either be a spawned cam (with a simple numerical ID), or an actor reference.
		/// </summary>
		public enum Type { Spawned, Actor, Null }

		public Type type;

		/// <summary> Starts at zero, counts up. </summary>
		public int ID;

		/// <summary> This should resolve to a camera actor </summary>
		public ActorRef CamActor;

		public const int NULL_ID = -1;

		public CamRef(int id)
		{
			type = Type.Spawned;
			ID = id;
			CamActor = ActorRef.NullRef;
		}

		public CamRef(ActorRef actor)
		{
			type     = Type.Actor;
			ID       = NULL_ID;
			CamActor = actor;
		}

		public static CamRef NullRef = new CamRef()
		{
			CamActor = ActorRef.NullRef,
			ID = NULL_ID,
			type = Type.Null
		};
	}
}