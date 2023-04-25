using System;
using Anjin.Scripting;
using Anjin.Util;
using Combat;
using Combat.Launch;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Anjin.Nanokin.Park
{
	public enum CombatIntroAnimation
	{
		None,
		ZoomToWorldPoint,
	}

	[Serializable]
	public struct EncounterSettings
	{
		[Optional] public GameObject        MonsterPrefab;
		[Optional] public BattleRecipeAsset Recipe;
		[Optional] public Arena             Arena;
		[Optional] public SceneReference    ArenaAddress;
		[Optional] public string			ArenaID;
	}

	[LuaUserdata]
	public struct CombatIntroAnimationSettings
	{
		public CombatIntroAnimation Type;

		public float Duration;

		public float AdvanceTimeNorm;

		public AnimationCurve ZoomCurve;
		public AnimationCurve ZoomLookCurve;
		public AnimationCurve FOVCurve;

		/*public float Duration {
			get {
				switch (Type) {
					case CombatIntroAnimation.ZoomToWorldPoint:

						float duration = Mathf.Max(
							ZoomCurve[zoomAnim.length     - 1].time,
							ZoomLookCurve[lookAnim.length - 1].time
						);
						break;
				}

				return 0;
			}
		}*/
	}

	[LuaUserdata]
	[Serializable]
	public struct CombatTransitionSettings
	{
		public CombatIntroAnimationSettings? Animation;

		[FormerlySerializedAs("ZoomTarget")]
		public WorldPoint? Target;
		public EncounterMonster Enemy;

		public bool NoPostImmunity;

		public Closure Lua_AfterIntro;
		public Closure Lua_BeforeReturn;

		public void SetFromTable(Table table)
		{
			if (table.TryGet("target", out DynValue val) && val.Type == DataType.UserData && val.UserData != null)
			{
				switch (val.UserData.Object)
				{
					case GameObject target:
						Target = target;
						break;
					case Transform transform:
						Target = transform;
						break;
					case DirectedActor actor:
						Target = actor.gameObject;
						break;
					case WorldPoint wp:
						Target = wp;
						break;
				}
			}

			if (Target != null && table.TryGet("target_offset", out Vector3 offset))
			{
				WorldPoint wp = Target.Value;
				wp.position = offset;
				Target      = wp;
			}

			if (table.TryGet("animation", out DynValue anim))
			{
				if (anim.Type == DataType.UserData)
				{
					switch (anim.UserData.Object)
					{
						case CombatIntroAnimationSettings settings:
							Animation = settings;
							break;
					}
				}
			}


			if (table.TryGet("immunity", out bool immunity)) NoPostImmunity = !immunity;

			if (table.TryGet("after_intro", out Closure func_after_intro))
				Lua_AfterIntro = func_after_intro;

			if (table.TryGet("before_return", out Closure func_return))
				Lua_BeforeReturn = func_return;
		}

		public static CombatTransitionSettings Default => new CombatTransitionSettings
		{
			Target    = null,
			Enemy     = null,
			Animation = null
		};
	}
}