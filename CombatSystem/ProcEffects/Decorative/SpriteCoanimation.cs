namespace Combat.Data.Decorative
{
	// public class SpriteCoanimation : ObjectAnimation
	// {
	// 	private readonly SpritesheetAnimationAsset _animation;
	// 	private          string                    _address;
	//
	// 	private bool                             _active;
	// 	private GameObject                       _activeEffect;
	// 	private AsyncOperationHandle<GameObject> _handle;
	// 	private bool                             _loading;
	// 	public  GameObject                       @from;
	//
	// 	public SpriteCoanimation(SpritesheetAnimationAsset animation, GameObject unknown) : base(unknown)
	// 	{
	// 		_animation = animation;
	// 	}
	//
	// 	public SpriteCoanimation(string address, GameObject unknown) : base(unknown)
	// 	{
	// 		_address = address;
	// 	}
	//
	//
	// 	public override float ReportedDuration => 0;
	// 	public override float ReportedProgress => 0;
	// 	public override bool  Active           => _active;
	//
	// 	public override void OnStart()
	// 	{
	// 		base.OnStart();
	//
	// 		void Spawn()
	// 		{
	// 			ActorBase actor = self.GetComponent<ActorBase>();
	//
	// 			_activeEffect                    = new GameObject("ImpactAnimationEffect");
	// 			_activeEffect.transform.position = actor.transform.position + new Vector3(0f, actor.height * 0.5f, 0.01f);
	//
	// 			_activeEffect.AddComponent<SpriteRenderer>();
	//
	// 			Billboard billboard = _activeEffect.AddComponent<Billboard>();
	// 			billboard.Y = true;
	//
	// 			var flipx = false;
	//
	// 			Fighter dealer = coplayer.state.proc?.dealer;
	// 			if (dealer != null && dealer.actor != null)
	// 			{
	// 				flipx = dealer.actor.facing.x > 0;
	// 			}
	//
	// 			SpritesheetAnimation spritesheetAnimation = _activeEffect.AddComponent<SpritesheetAnimation>();
	// 			spritesheetAnimation.animationType = SpritesheetAnimation.AnimationType.AnimateOnceAndDestroy;
	// 			spritesheetAnimation.animation     = _animation;
	// 			spritesheetAnimation.FlipX         = flipx;
	// 		}
	//
	// 		async UniTask LoadAndSpawn()
	// 		{
	// 			_handle  = await Addressables2.LoadHandleAsync<GameObject>(_address);
	// 			_loading = false;
	// 			Spawn();
	// 		}
	//
	// 		if (_animation != null)
	// 		{
	// 			_active = true;
	// 			Spawn();
	// 		}
	// 		else if (_address != null)
	// 		{
	// 			_loading = true;
	// 			_active  = true;
	// 			LoadAndSpawn().Forget();
	// 		}
	// 	}
	//
	//
	// 	public override void OnStop(bool skipped = false)
	// 	{
	// 		Addressables2.Release(_handle);
	// 	}
	//
	// 	public override void OnCoplayerUpdate(float dt)
	// 	{
	// 		base.OnCoplayerUpdate(dt);
	// 		if (_loading) return;
	// 		if (_activeEffect == null)
	// 		{
	// 			_active = false;
	// 		}
	// 	}
	// }
}