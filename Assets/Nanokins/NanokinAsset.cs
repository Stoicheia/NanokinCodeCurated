using System.Collections.Generic;
using System.Linq;
using Anjin.Util;
using Combat;
using Combat.Startup;
using Data.Overworld;
using Data.Shops;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Addressable;
using Util.Odin.Attributes;
using Util.UnityEditor.Launch;

namespace Assets.Nanokins
{
	public class NanokinAsset : SerializedScriptableObject
	{


		[FormerlySerializedAs("displayName")]
		public string DisplayName;
		[FormerlySerializedAs("description"), Multiline, Optional]
		public string Description;

		[FormerlySerializedAs("area")]
		public Areas Area = Areas.None;
		[FormerlySerializedAs("number")]
		public int Number;

		[ValidateInput("@Body.name.ToLower().Contains(\"body\")", "Assigned body does not appear to be a body limb.", InfoMessageType.Warning)]
		[FormerlySerializedAs("bodyRef"), SerializeField, Required]
		public NanokinLimbAsset Body;

		[ValidateInput("@Head.name.ToLower().Contains(\"head\")", "Assigned head does not appear to be a head limb.", InfoMessageType.Warning)]
		[FormerlySerializedAs("headRef"), SerializeField, Required]
		public NanokinLimbAsset Head;

		[ValidateInput("@Arm1.name.ToLower().Contains(\"arm1\")", "Assigned arm1 does not appear to be a arm1 limb.", InfoMessageType.Warning)]
		[FormerlySerializedAs("arm1Ref"), SerializeField, Required]
		public NanokinLimbAsset Arm1;

		[ValidateInput("@Arm2.name.ToLower().Contains(\"arm2\")", "Assigned arm2 does not appear to be a arm2 limb.", InfoMessageType.Warning)]
		[FormerlySerializedAs("arm2Ref"), SerializeField, Required]
		public NanokinLimbAsset Arm2;

		public string DefaultAI = "balanced";

		/// <summary>
		/// XP for the nanokin at level 1.
		/// </summary>
		public int BaseXPLoot = 50;

		/// <summary>
		/// RP for a monster. (any level)
		/// </summary>
		public int BaseRPLoot = 1;



#if UNITY_EDITOR
		[ShowInInspector, PropertyOrder(-1)]
		public void OpenInPuppetViewer()
		{
			EditorApplication.ExecuteMenuItem("Anjin/Windows/Puppet Viewer");
		}

		[Button]
		private void DetectLimbs()
		{
			List<string> all = Addressables2.FindInEditor("Limbs/", notEndWith: ".spritesheet");

			string tokenizedName = name.ToLower().ToSnakeCase();
			var    myLimbs       = all.Where(x => x.ToLower().ToSnakeCase().Contains(tokenizedName)).ToList();

			if (myLimbs.Count == 0)
			{
				DebugLogger.Log($"Couldn't detect any limbs for {name}..", LogContext.Combat, LogPriority.High);
				return;
			}

			Body = Addressables2.LoadInEditor<NanokinLimbAsset>(myLimbs.FirstOrDefault(x => x.ToLower().Contains("body")) ?? "");
			Head = Addressables2.LoadInEditor<NanokinLimbAsset>(myLimbs.FirstOrDefault(x => x.ToLower().Contains("head")) ?? "");

			Arm1 = Addressables2.LoadInEditor<NanokinLimbAsset>(
				myLimbs.FirstOrDefault(x => x.ToLower().Contains("arm1")) ??
				myLimbs.FirstOrDefault(x => x.ToLower().Contains("main")) ?? myLimbs.FirstOrDefault(x => x.ToLower().Contains("front")) ?? "");

			Arm2 = Addressables2.LoadInEditor<NanokinLimbAsset>(
				myLimbs.FirstOrDefault(x => x.ToLower().Contains("arm2")) ??
				myLimbs.FirstOrDefault(x => x.ToLower().Contains("off")) ?? myLimbs.FirstOrDefault(x => x.ToLower().Contains("back")) ?? "");
		}

		[Button(ButtonSizes.Large), GUIColor(0, 1, 0), PropertyOrder(-1)]
		private void Test()
		{
			NanokinLauncher.LaunchCombat(new PlayerOpponent1V1
			{
				PlayerTeamRecipe = new TeamRecipe(new List<MonsterRecipe>
				{
					new SimpleNanokin { Nanokin  = this, Level = 50, },
					new RandomNanokin(25),
					new RandomNanokin(25),
				}),

				EnemyTeamRecipe = new TeamRecipe(new List<MonsterRecipe>
				{
					new RandomNanokin(25),
					new RandomNanokin(25),
					new RandomNanokin(25),
				}),
				playerBrain = new PlayerBrain(),
				enemyBrain  = new DebugBrain(),
			});
		}



#endif
	}
}