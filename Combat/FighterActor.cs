using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace Combat
{
	public abstract class FighterActor : BattleActor
	{
		// ReSharper disable once UnusedMember.Global
		// ReSharper disable once NotAccessedField.Global
		public Fighter fighter;

		[CanBeNull] public virtual AudioClip HurtSFX    => null;
		[CanBeNull] public virtual AudioClip GruntSFX   => null;
		[CanBeNull] public virtual AudioClip ScreechSFX => null;
		[CanBeNull] public virtual AudioClip DeathSFX   => null;

		[SerializeField] protected Assets.Scripts.Utils.ParticlePrefab FX_ActionSignal;

		public virtual GameObject TurnPrefab => null;

		//[SerializeField] protected HealthbarHUD healthbarHUD;

		public virtual void SignalForAction()
		{
			FX_ActionSignal.Instantiate(transform, parent: false);
		}

		public virtual UniTask<Sprite> GetEventSprite()
		{
			return UniTask.FromResult<Sprite>(null);
		}

		public virtual       void                SetPuppetAnim(API.PropertySheet.PuppetAnimation anim) { }
		public virtual async UniTask<GameObject> CreateSilhouette()                                    { return null; }
	}
}