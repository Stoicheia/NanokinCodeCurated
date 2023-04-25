using Anjin.EventSystemNS.Actions;

namespace Anjin.Nanokin.Actions
{
	public class NanokinActionImpl<T> : ActionImpl<T> where T : EventAction
	{
		public override bool Blocking => false;
		public override bool Done     => true;

		public override void OnDestroy() { }
		public override void OnEnter()   { }
		public override void OnExit()    { }
		public override void OnUpdate()  { }
	}

	/// <summary>
	/// Generic container for holding multiple localizations of pieces of text.
	/// </summary>


	/*[EventActionMetadata("Set Actor Animation")]
	public class ActorSetAnimationAction : EventAction
	{
		public ActorReferencePointer actor;

		public string AnimationName;
		public float  AnimationSpeed;
		public bool   SetSpeed;

		//Will we block?
		public bool Blocking;

		//The duration of the blocking
		public float BlockingDuration;


		public bool  ReturnToPreviousAnimation;
		public float AnimDuration;

		public bool EffectAll;

		//[Newtonsoft.Json.JsonIgnore]
		public bool EditorSettingsExpand;

		public override void Init()
		{
			AnimationName             = "";
			Blocking                  = false;
			BlockingDuration          = 0;
			SetSpeed                  = false;
			ReturnToPreviousAnimation = false;
			AnimationSpeed            = 1;
			AnimDuration              = 1;
			actor                     = new ActorReferencePointer();
		}

		public override void InitSockets()
		{
			actor = new ActorReferencePointer();
		}
	}

	[EventActionImplementation(typeof(ActorSetAnimationAction), "Nanokin")]
	public class ActorSetAnimationImpl : NanokinActionImpl<ActorSetAnimationAction>
	{
		public List<GameObject> actors;
		public List<string>     prevForcedActions = new List<string>();
		public List<float>      prevSpeeds        = new List<float>();

		bool  done         = false;
		bool  doneBlocking = false;
		float BlockTimer   = 0;
		float ReturnTimer  = 0;
		bool  returned     = false;

		public override bool Blocking => Handler.Blocking && !doneBlocking;
		public override bool Done     => done;

		public override void OnEnter()
		{
			var _actors = GetActor(Handler.actor);
			if (_actors.Count > 0)
			{
				actors = _actors;

				if (Handler.EffectAll)
				{
					for (int i = 0; i < actors.Count; i++)
					{
						var view = actors[i].GetComponent<Actor>().View;

						if (Handler.ReturnToPreviousAnimation)
						{
							prevForcedActions.Add(view.forcedAnimation);
							if (Handler.SetSpeed)
								prevSpeeds.Add(view.animationSpeed);
						}

						view.forcedAnimation = Handler.AnimationName;
						if (Handler.SetSpeed)
							view.animator.core.Speed = Handler.AnimationSpeed;
					}
				}
				else
				{
					var view = _actors[0].GetComponent<Actor>().View;

					if (Handler.ReturnToPreviousAnimation)
					{
						prevForcedActions.Add(view.forcedAnimation);
						if (Handler.SetSpeed)
							prevSpeeds.Add(view.animationSpeed);
					}

					view.forcedAnimation = Handler.AnimationName;
					if (Handler.SetSpeed)
						view.animator.core.Speed = Handler.AnimationSpeed;
				}

				if (!Handler.Blocking && !Handler.ReturnToPreviousAnimation) done = true;
			}
			else
			{
				//Log error/warning
				done = true;
			}
		}

		public override void OnUpdate()
		{
			//Block Timer
			if (BlockTimer >= Handler.BlockingDuration)
				doneBlocking = true;
			else
				BlockTimer += Time.deltaTime;

			if (Handler.ReturnToPreviousAnimation)
			{
				if (ReturnTimer >= Handler.AnimDuration)
				{
					ReturnToPrev();
					if (doneBlocking)
					{
						done = true;
					}
				}
				else
					ReturnTimer += Time.deltaTime;
			}
			else if (doneBlocking)
			{
				done = true;
			}
		}

		public void ReturnToPrev()
		{
			if (!returned && Handler.ReturnToPreviousAnimation)
			{
				if (Handler.EffectAll)
				{
					for (int i = 0; i < actors.Count; i++)
					{
						var view             = actors[i].GetComponent<Actor>().View;
						view.forcedAnimation = prevForcedActions[i];
						if (Handler.SetSpeed)
							view.animator.core.Speed = Handler.AnimationSpeed;
					}
				}
				else
				{
					var view = actors[0].GetComponent<Actor>().View;

					view.forcedAnimation = prevForcedActions[0];
					if (Handler.SetSpeed)
						view.animator.core.Speed = Handler.AnimationSpeed;
				}

				returned = true;
			}
		}

		public override void OnExit() { }

		public override void OnDestroy()
		{
			ReturnToPrev();
		}
	}

	[EventActionMetadata("Actor Look At")]
	public class ActorLookAtAction : EventAction
	{

		public ActorReferencePointer actor;
		public WorldPoint            position;

		public override void Init()
		{
			position = new WorldPoint();
			actor    = new ActorReferencePointer();
		}

		public override void InitSockets()
		{
			position = new WorldPoint();
			actor    = new ActorReferencePointer();
		}
	}

	[EventActionImplementation(typeof(ActorLookAtAction), "Nanokin")]
	public class ActorLookAtActionImpl : NanokinActionImpl<ActorLookAtAction>
	{
		private Actor actor;

		public override void OnEnter()
		{
			base.OnEnter();

			var a = GetActor(Handler.actor);
			if (a.Count > 0)
			{
				actor = a[0].GetComponent<Actor>();
				Vector3? pos = Handler.position.Get();
				if(pos != null)
				{
					Vector3 target = (Vector3)pos;
					if (Handler.position.positionLocal)
						target += actor.transform.position;

					actor.LookAt(target);
				}
				else
				{
					ErrorOut("Failed to find position");
				}
			}
			else
			{
				ErrorOut("Failed to find actor");
			}
		}
	}

	[EventActionMetadata("Actor Walk To")]
	public class ActorWalkToAction : EventAction
	{
		public ActorReferencePointer actor;
		public WorldPoint position;

		public bool overideSpeed;
		public float speed;
		public float stoppingDistance;

		public bool waitUntilDone;

		public override void Init()
		{
			actor = new ActorReferencePointer();
			position = new WorldPoint();
			speed = 1;
			overideSpeed = false;
			stoppingDistance = 0;
		}

		public override void InitSockets()
		{
			position = new WorldPoint();
		}
	}

	[EventActionImplementation(typeof(ActorWalkToAction), "Nanokin")]
	public class ActorWalkToActionImpl : NanokinActionImpl<ActorWalkToAction>
	{
		public override bool Done => reachedDestination;
		public override bool Blocking => Handler.waitUntilDone;

		public Actor actor;
		private bool reachedDestination;
		Vector3 destination;

		public override void OnEnter()
		{
			var objs = GetActor(Handler.actor);
			if (objs.Count > 0)
				actor = objs[0].GetComponent<Actor>();

			var pos = Handler.position.Get();

			if (pos == null)
			{
				ErrorOut("Failed to find position");
				return;
			}

			if (actor != null)
			{
				destination = (Vector3) pos;
				if (Handler.position.positionLocal)
					destination = actor.gameObject.transform.position + (Vector3) pos;

				actor.Pathfinder.WalkTo(destination, -1,Handler.waitUntilDone ? OnMoveDone : (Handler<Actor>)null);
			}
			else
			{
				ErrorOut("Failed to find actor");
			}
		}

		public void OnMoveDone(Actor a)
		{
			reachedDestination = true;
		}
	}

	[EventActionMetadata("Actor Set Position")]
	public class ActorSetPositionAction : EventAction
	{
		public ActorReferencePointer actor;
		public WorldPoint position;

		public float delay;

		public bool lerp;
		public float lerpAmount;

		public override void Init()
		{
			position = new WorldPoint();
			actor = new ActorReferencePointer();
			delay = 0;
			lerp = false;
		}

		public override void InitSockets()
		{
			position = new WorldPoint();
			actor = new ActorReferencePointer();
		}
	}

	[EventActionImplementation(typeof(ActorSetPositionAction),"Nanokin")]
	public class ActorSetPositionActionImpl : NanokinActionImpl<ActorSetPositionAction>
	{

		public override bool Done => reachedPos;
		public override bool Blocking => true;

		public bool reachedPos = false;
		private Vector3 targetPos;
		public GameObject actor;

		public override void OnEnter()
		{
			var p = Handler.position.Get();
			//If the position actually exists
			if (p != null)
			{
				targetPos = (Vector3)p;
			}
			else
			{
				ErrorOut("Failed to find position");
				return;
			}

			//If the actor exists.
			var actors = GetActor(Handler.actor);
			if (actors.Count > 0)
			{
				actor = actors[0];
			}
			else
			{
				ErrorOut("Failed to find actor");
				return;
			}

			if (!Handler.lerp)
			{
				if (Handler.position.positionLocal)
					actor.transform.position = actor.transform.position + targetPos;
				else
					actor.transform.position = targetPos;

				reachedPos = true;
			}
			else if (Handler.position.positionLocal)
				targetPos = actor.transform.position + targetPos;
		}

		public override void OnUpdate()
		{
			if (Handler.lerp)
			{
				actor.transform.position = Vector3.Lerp(actor.transform.position, targetPos, Handler.lerpAmount);
				if (actor.transform.position.Distance(targetPos) <= 0.07f)
				{
					actor.transform.position = targetPos;
					reachedPos = true;
				}
			}
		}

		public override void OnExit()
		{
			actor.transform.position = targetPos;
		}
	}

	[EventActionMetadata("Spawn Prefab")]
	public class PrefabSpawnAction : EventAction
	{
		public GameObject prefab;

		public float delay;

		public WorldPoint position;


		public float amount = 1;

		public override void Init()
		{
			position = new WorldPoint();
		}

		public override void InitSockets()
		{
			position = new WorldPoint();
		}
	}*/
}