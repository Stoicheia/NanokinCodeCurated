namespace Combat.Toolkit
{
	// public class CombatEffectCoanimation<TEffect> : ObjectAnimation
	// 	where TEffect : CombatEffect
	// {
	// 	private readonly TEffect         _combatEffectPrefab;
	// 	private readonly Vector3         _position;
	// 	private          Handler<TEffect> _onCreate;
	// 	private          TEffect         _combatEffectInstance;
	//
	// 	private float _speed, _elapsed, _duration;
	// 	private bool  _isRunning;
	//
	// 	public CombatEffectCoanimation(GameObject unknown, TEffect combatEffectPrefab, Vector3 position, [CanBeNull] Handler<TEffect> onCreate = null) : base(unknown)
	// 	{
	// 		_combatEffectPrefab = combatEffectPrefab;
	// 		_position           = position;
	// 		_onCreate           = onCreate;
	// 	}
	//
	// 	public override bool Active => _elapsed < _duration;
	//
	// 	public override float ReportedProgress => _elapsed / _duration;
	//
	// 	public override float ReportedDuration => _duration;
	//
	// 	public override void OnCoplayerUpdate(float dt)
	// 	{
	// 		_elapsed += dt * _speed ;
	//
	// 		if (_isRunning)
	// 		{
	// 			if (_combatEffectInstance == null)
	// 			{
	// 				_isRunning = false;
	// 			}
	// 			else
	// 			{
	// 				_isRunning = _combatEffectInstance.IsRunning;
	// 				_duration  = _combatEffectInstance.Duration;
	// 			}
	// 		}
	// 	}
	//
	// 	public override void OnStart()
	// 	{
	// 		base.OnStart();
	//
	// 		_combatEffectInstance = Object.Instantiate(_combatEffectPrefab, _position, Quaternion.identity);
	//
	// 		_onCreate?.Invoke(_combatEffectInstance);
	//
	// 		_duration  = _combatEffectInstance.Duration;
	// 		_isRunning = true;
	// 		_elapsed   = 0;
	// 	}
	// }
}