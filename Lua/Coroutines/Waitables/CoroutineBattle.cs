using Anjin.Actors;
using Anjin.Nanokin.Park;
using Combat;
using Combat.Launch;
using Combat.Startup;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Overworld.Controllers;
using Overworld.Cutscenes;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Util.Addressable;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class CoroutineBattle : CoroutineManaged
	{
		[CanBeNull] public Arena                     arena;
		[CanBeNull] public BattleRecipe              recipe;
		[CanBeNull] public CombatTransitionSettings? transition;
		[CanBeNull] public GameObject				 sharedCoachPrefab;

		[CanBeNull] public string battleAddress = "Combat/Default Core";
		[CanBeNull] public string recipeAddress;
		[CanBeNull] public string arenaAddress;
		[CanBeNull] public string musicAddress;

		public bool retryDisabled = false;
		public bool fleeDisabled  = false;
		public bool? immunity     = null;

		private bool _started;
		private bool _canceled;
		private bool _active;

		private AsyncOperationHandle<BattleRecipeAsset> _loadedRecipe;
		private AsyncOperationHandle<AudioClip>			_loadedMusic;

		public override bool Active    => !_started || _active;
		public override bool Skippable => false;

		public override void OnStart()
		{
			base.OnStart();
			Launch().Forget();
		}

		public async UniTask Launch()
		{
			Vector3 origin = ActorController.playerActor.Position;

			ArenaReference arena_ref;
			if (arena != null)
				arena_ref = arena;
			else
				arena_ref = arenaAddress ?? EncounterLayer.GetArena(origin);


			BattleRecipe recipe       = this.recipe ?? EncounterLayer.GetRecipe(origin)?.Value;
			if (!string.IsNullOrEmpty(recipeAddress))
			{
				_loadedRecipe = await Addressables2.LoadHandleAsync<BattleRecipeAsset>(recipeAddress);
				recipe        = _loadedRecipe.Result.Value;
			}

			AudioClip music = null;
			if (!string.IsNullOrEmpty(musicAddress)) {
				_loadedMusic = await Addressables2.LoadHandleAsync<AudioClip>(musicAddress);
				music        = _loadedMusic.Result;
			}

			if (recipe == null)
			{
				_canceled = true;
				return;
			}

			// RECIPE
			// ----------------------------------------

			recipe.sharedCoachPrefab = sharedCoachPrefab;

			_active  = true;
			_started = true;

			CombatTransitionSettings _transition = transition ?? new CombatTransitionSettings { Target = coplayer.sourceObject.transform };

			if (immunity.HasValue)
				_transition.NoPostImmunity = immunity.Value;

			BattleOutcome outcome = await BattleController.LaunchEncounter(arena_ref, recipe, _transition, music: music, retryDisabled: retryDisabled, fleeDisabled: fleeDisabled);

			coplayer.state.combatOutcome = outcome;
			_active = false;
		}

		public override bool CanContinue(bool justYielded, bool isCatchup)
		{
			bool @continue = base.CanContinue(justYielded);
			if (@continue)
			{
				// CLEANUP
				Addressables2.ReleaseSafe(_loadedRecipe);
				Addressables2.ReleaseSafe(_loadedMusic);
			}

			return @continue;
		}
	}
}