using Anjin.Scripting;
using Combat;
using Combat.Data;
using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
	[LuaUserdata]
	public class GiantRocAI : UtilityAI
	{
		public int WingFlaps { get; private set; }

		private bool usedIronBeak;

		private int turnCounter;

		private List<BattleSkill> skillsForTurn;

		private Dictionary<string, BattleSkill> skills;

		private System.Random random;

		private List<int> wingFlaps;
		private List<int> selections;

		public GiantRocAI()
		{
			turnCounter = 0;
			WingFlaps   = 0;

			usedIronBeak = false;

			random = new System.Random();

			skillsForTurn = new List<BattleSkill>();
			skills        = new Dictionary<string, BattleSkill>();

			wingFlaps = new List<int>() { 1, 2, 3, 4, 5, 6 };
			selections = new List<int>();
		}

		/*public GiantRocAI([NotNull] Fighter self) : base(self)
		{
		}
		*/

		public override void PopulateSolutions()
		{
			++turnCounter;
			//skillsForTurn.Clear();

			if (solutions.Count > 0)
			{
				Reset();
			}

			if (skills.Count == 0) {
				foreach (BattleSkill skill in self.skills) {
					skills.Add(skill.asset.DisplayName, skill);
				}
			}

			if (skillsForTurn.Count == 0)
			{
				skillsForTurn.Add(skills["Ironbeak"]);

				Debug.Log("Adding skill: " + skillsForTurn[0].scriptName);
				Debug.Log("skillsForTurn Count: " + skillsForTurn.Count);

				if (selections.Count == 0)
				{
					selections.Clear();

					foreach (int flap in wingFlaps)
					{
						selections.Add(flap);
					}
				}

				int selectionIndex = random.Next(0, selections.Count);
				WingFlaps = selections[selectionIndex];
				selections.RemoveAt(selectionIndex);

				self.info.save_int("wing_flaps", WingFlaps);

				if (WingFlaps % 2 == 0)
					skillsForTurn.Add(skills["Hylic Ward"]);
				else
					skillsForTurn.Add(skills["Psychic Ward"]);

				Debug.Log("Adding skill: " + skillsForTurn[1].scriptName);
				Debug.Log("skillsForTurn Count: " + skillsForTurn.Count);
			}

			////The first action will allow for either Ironbeak or a move to occur
			////if (turnCounter == 1)
			//if (!usedIronBeak)
			//{
			//	usedIronBeak = true;

			//	skillsForTurn.Add(skills["Ironbeak"]);

			//	// Formation solutions
			//	// ----------------------------------------
			//	//battle.GetFormationTargets(self, _targeting);

			//	/*foreach (Target target in _targeting.options[0])
			//	{
			//		solutions.Add(new AISolution(AIAction.skill, target));
			//	}*/
			//}
			////The second action will always be a ward skill, and a random number generator will determine which one gets invoked (even number = Hylic for physical, and odd = Psychic for magical)
			//else
			//{
			//	turnCounter = 0;

			//	//A list of numbers 1 - 6 is used to "randomize" the flap amounts. (Basically, RNG is used to select the index of the flap count list, then that element is removed
			//	//so tht it can't be selected again. Sometimes this may result in a skew of wards before things finally balance out, but at least this way, we significantly
			//	//mitigate the chances of one ward being used constantly.)
			//	if (selections.Count == 0)
			//	{
			//		selections.Clear();

			//		foreach (int flap in wingFlaps)
			//		{
			//			selections.Add(flap);
			//		}
			//	}

			//	int selectionIndex = random.Next(0, selections.Count);
			//	WingFlaps = selections[selectionIndex];
			//	selections.RemoveAt(selectionIndex);

			//	self.info.save_int("wing_flaps", WingFlaps);

			//	if (WingFlaps % 2 == 0)
			//		skillsForTurn.Add(skills["Hylic Ward"]);
			//	else
			//		skillsForTurn.Add(skills["Psychic Ward"]);

			//	usedIronBeak = false;
			//}

			// Skill solutions
			// ----------------------------------------
			//foreach (BattleSkill inst in skillsForTurn)
			if (skillsForTurn.Count > 0)
			{
				BattleSkill inst = skillsForTurn[0];

				Debug.Log("Evluating skill: " + inst.scriptName);

				if (inst == null) return;
				if (!battle.CanUse(inst).Item1) return;

				battle.GetSkillTargets(inst, _targeting);
				_targeting.CopyPicksToOptions();

				if (_targeting.options.Count == 0) return;

				List<Target> targets = _targeting.options[0];
				if (targets.Count == 0) return;

				foreach (Target target in targets)
				{
					var sol = new AISolution(inst, target);

					if (inst.HasTag("attack")) sol.tag = AITag.attack;
					if (inst.HasTag("heal")) sol.tag = AITag.heal;
					if (inst.HasTag("state")) sol.tag = AITag.buff;
					if (inst.HasTag("debuff")) sol.tag = AITag.debuff;

					solutions.Add(sol);
				}

				Debug.Log("Removing skill: " + skillsForTurn[0].scriptName);

				skillsForTurn.RemoveAt(0);

				Debug.Log("skillsForTurn Count: " + skillsForTurn.Count);

				OnSolutionsChanged();
			}
		}
	}
}
