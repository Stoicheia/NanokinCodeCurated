using System.Linq;
using Combat.Components;
using Core.Debug;
using Data.Combat;

namespace Combat.Toolkit
{
	public class DebugConsoleChip : Chip
	{
		private object _commandGroup;

		public override void Install()
		{
			base.Install();

			_commandGroup = DebugConsole.BeginGroup();
			// DebugConsole.AddCommand("entities", (_, io) => io.output.AddRange(battle.entities));
			DebugConsole.AddCommand("fighters", (_, io) => io.output.AddRange(battle.fighters));
			DebugConsole.AddCommand("teams", (_,    io) => io.output.AddRange(battle.teams));
			DebugConsole.AddCommand("slots", (_,    io) => io.output.AddRange(battle.slots));

			DebugConsole.AddCommand("allies", (_, io) =>
			{
				io.input.RemoveAll(input =>
				{
					Team team = battle.GetTeam(input);
					return team.isPlayer;
				});

				io.output.AddRange(io.input);
			});

			DebugConsole.AddCommand("enemies", (_, io) =>
			{
				io.input.RemoveAll(input =>
				{
					Team team = battle.GetTeam(input);
					return !team.isPlayer;
				});

				io.output.AddRange(io.input);
			});


			DebugConsole.AddCommand("kill", (_, io) =>
			{
				var fighters = io.input.OfType<Fighter>().ToList();

				foreach (Fighter fighter in fighters)
				{
					battle.SetPoints(fighter, Pointf.Zero);
					runner.Submit(CoreOpcode.FlushDeaths);
				}
			});

			DebugConsole.AddCommand("heal", (_, io) =>
			{
				var pointHolders = io.input.OfType<Fighter>().ToList();

				foreach (Fighter fighter in pointHolders)
				{
					battle.AddPoints(fighter, new Pointf());
				}
			});

			DebugConsole.AddCommand("skip", (_, io) =>
			{
				// if (core.processor.CurrentInstruction.Blocker is BattleActionBlocker blocker)
				// {
				// blocker.Insert(new Trace.Log("Skip Handler"));
				// }
			});
			DebugConsole.EndGroup();
		}

		public override void Uninstall()
		{
			base.Uninstall();
			DebugConsole.RemoveGroup(_commandGroup);
		}
	}
}