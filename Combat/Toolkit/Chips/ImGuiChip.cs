using Anjin.Util;
using Combat.Components;
using Combat.Data;
using Combat.UI.TurnOrder;
using Cysharp.Threading.Tasks;
using Data.Combat;
using ImGuiNET;
using UnityEditor;
using UnityEngine;
using g = ImGuiNET.ImGui;

namespace Combat.Toolkit
{
	public class ImGuiChip : Chip, IDebugDrawer
	{
		public override UniTask InstallAsync()
		{
			DebugSystem.Register(this);

#if UNITY_EDITOR
			Selection.selectionChanged += DebugBrain.OnEditorSelectionChanged;
#endif

			return UniTask.CompletedTask;
		}

		public override void Uninstall()
		{
			base.Uninstall();

			DebugSystem.Unregister(this);
#if UNITY_EDITOR
			Selection.selectionChanged -= DebugBrain.OnEditorSelectionChanged;
#endif
		}

		private bool _compact = false;
		private bool _asTeams = false;

		private int      _procAmount  = 1;
		private Elements _procElement = Elements.none;

		private int      _stateAmount  = 1;
		private int      _stateLife    = -1;
		private Elements _stateElement = Elements.none;


		private int _guiHpChangeAmount = 1;
		private int _guiSpChangeAmount = 1;
		private int _guiOpChangeAmount = 1;

		public void OnLayout(ref DebugSystem.State state)
		{
			DebugBrain.OnLayout(ref state, battle);

			if (!state.DebugMode || runner.step != BattleRunner.States.Playing)
				return;

			if (g.Begin("state"))
			{
				DrawWindowContent();
			}
		}

		private void DrawWindowContent()
		{
			if (g.Button("Shutdown"))
				runner.Stop();

			g.SameLine();

			if (g.Button("Win"))
			{
				runner.CancelBrains();
				runner.CancelActions();
				runner.Submit(CoreOpcode.WinBattle);
			}

			g.SameLine();

			if (g.Button("Lose"))
			{
				runner.CancelBrains();
				runner.CancelActions();
				runner.Submit(CoreOpcode.LoseBattle);
			}


			// ----------------------------------------
			if (g.BeginTabBar("tabs"))
			{
				DrawTabs();
			}

			g.End();
		}

		private void DrawTabs()
		{
			if (g.BeginTabItem("core"))
			{
				runner.DrawImGui();
				runner.DrawImGui();
				g.EndTabItem();
			}

			if (g.BeginTabItem("Logging"))
			{
				g.EndTabItem();
			}

			if (g.BeginTabItem("state"))
			{
				g.EndTabItem();
			}

			if (g.BeginTabItem("Fighters"))
			{
				g.Checkbox("Compact", ref _compact);
				g.SameLine();
				g.Checkbox("As Teams", ref _asTeams);

				if (_asTeams)
				{
					for (int i = 0; i < battle.teams.Count; i++)
					{
						Team team = battle.teams[i];
						g.PushID(i);
						g.Text($"Team #{i} (id:{team.id})");
						g.Separator();

						for (int j = 0; j < team.fighters.Count; j++)
							DoFighter(team.fighters[j], j);

						AImgui.VSpace(32);

						g.PopID();
					}
				}
				else
				{
					for (var i = 0; i < battle.fighters.Count; i++)
						DoFighter(battle.fighters[i], i);
				}


				g.EndTabItem();
			}

			// ----------------------------------------
			if (g.BeginTabItem("States"))
			{
				for (var i = 0; i < battle.states.Count; i++)
				{
					State state = battle.states[i];

					g.PushID(i);
					if (g.CollapsingHeader(state.ID))
					{
						DrawBuffControls(state);
					}

					g.PopID();
				}

				g.EndTabItem();
			}

			// ----------------------------------------
			if (g.BeginTabItem("Triggers"))
			{
				for (var i = 0; i < battle.triggers.Count; i++)
				{
					Trigger trigger = battle.triggers[i];

					g.PushID(i);
					if (g.CollapsingHeader(trigger.ID))
					{
						g.LabelText("ID", trigger.ID);
						g.DragInt("Life", ref trigger.state.life);
						g.LabelText("Signal", trigger.signal.ToString());
						g.LabelText("Filter", trigger.filter != null ? trigger.filter.ToString() : "null");

						bool isAlive = trigger.IsAlive;
						g.Checkbox("Alive", ref isAlive);
					}

					g.PopID();
				}

				g.EndTabItem();
			}

			if (g.BeginTabItem("Turn Order"))
			{
				int round = 0;
				foreach (TurnInfo info in TurnUI.CurrentInfos)
				{
					if (info.roundIndex != round)
					{
						g.Text("");
						round = info.roundIndex;
					}

					g.Text(
						$"{info.listIndex.ToString().PadLeft(2, '0')}:{info.roundIndex.ToString().PadLeft(2, '0')}:{info.turnIndex.ToString().PadLeft(4, '0')}:{info.action.id.ToString().PadLeft(3, '0')}:{info.marker}:{(info.acter?.TurnName ?? info.trigger?.ID):-75}");
				}
			}

			g.EndTabBar();
		}

		private void DoFighter(Fighter fighter, int index)
		{
			if (fighter == null || fighter.info == null) return;

			g.PushID(index);

			string label = $"{fighter.info.Name,-16}\t(HP {fighter.points.hp}/{fighter.info.Points.hp}\tSP {fighter.points.sp}/{fighter.info.Points.sp}\tOP {fighter.points.op}/{fighter.info.Points.op})";
			// string label = $"{fighter.info.Name}: "                                  +
			// $"\t(HP: {fighter.points.hp}/{fighter.info.Points.hp}), " +
			// $"(SP: {fighter.points.sp}/{fighter.info.Points.sp}) "    +
			// $"(OP: {fighter.points.op}/{fighter.info.Points.op})";

			if (_compact)
			{
				if (g.Selectable(label))
				{
					g.OpenPopup(fighter.info.Name);
				}

				if (g.BeginPopup(fighter.info.Name))
				{
					DrawFighterControls(fighter);
					g.EndPopup();
				}
			}
			else
			{
				if (g.CollapsingHeader(label))
				{
					if (g.Selectable("Menu"))
						g.OpenPopup(fighter.info.Name);

					if (g.CollapsingHeader($"Base Stats ##basestats_{fighter.LogID}"))
					{
						g.LabelText("Points (start)", fighter.info.StartPoints.ToString());
						g.LabelText("Points (max)", fighter.info.Points.ToString());
						g.LabelText("Stats", fighter.info.Stats.ToString());
						g.LabelText("Efficiency", fighter.info.Resistances.ToString());
						g.LabelText("Handler Bonus", fighter.info.Actions.ToString());
						g.LabelText("Handler Priority", fighter.info.Priority.ToString());
						g.LabelText("DNA", fighter.info.DNA);
						g.LabelText("Level", fighter.info.Level.ToString());
						g.LabelText("XP", fighter.info.XPLoot.ToString());
						g.LabelText("RP", fighter.info.RPLoot.ToString());
						g.LabelText("Movement", fighter.info.FormationMethod);
					}

					g.LabelText("Points", fighter.points.ToString());
					g.LabelText("Points (max)", fighter.max_points.ToString());
					g.LabelText("Stats", fighter.stats.ToString());
					g.LabelText("Efficiency", fighter.resistance.ToString());
					g.LabelText("Efficiency", fighter.resistance.ToString());
					g.LabelText("Efficiency", fighter.resistance.ToString());

					if (g.CollapsingHeader($"Status ##stateing_{fighter.LogID}"))
					{
						g.Text("this is not done, i dont need it right now, this is gonna suck ass to write");
						g.Text("help");
						// TODO fill this out
					}

					if (g.CollapsingHeader($"States ##stateing_{fighter.LogID}"))
					{
						foreach (State b in fighter.states)
						{
							g.Text($"{b.LogID,-16}\tlife {b.life}\t{b.status.cmds.Count} cmds");
						}
					}
				}
			}

			g.PopID();
		}


		void DrawFighterControls(Fighter fighter)
		{
			bool PointField(string label, float input, float max, out float output)
			{
				g.PushID(label);
				g.Text(label);
				g.SameLine();

				output = 0;

				int ioutput = input.Floor();

				//g.InputInt("", ref ioutput, 1);
				g.DragInt("", ref ioutput, 1);
				g.SameLine();
				g.Text($"/{max.Floor()}");

				g.PopID();

				output = ioutput;

				return input.Floor() != ioutput;
			}


			//AImgui.Text("Points:", ColorsXNA.Goldenrod);


			if (g.CollapsingHeader("Points"))
			{
				var points = fighter.points;

				if (PointField("HP", fighter.points.hp, fighter.max_points.hp, out float hp_out))
					battle.SetPoints(fighter, new Pointf(hp_out, points.sp, points.op));

				if (PointField("SP", fighter.points.sp, fighter.max_points.sp, out float sp_out))
					battle.SetPoints(fighter, new Pointf(points.hp, sp_out, points.op));

				if (PointField("OP", fighter.points.op, fighter.max_points.op, out float op_out))
					battle.SetPoints(fighter, new Pointf(points.hp, points.sp, op_out));

				if (g.Button("To Zero"))
				{
					battle.SetPoints(fighter, Pointf.Zero);
					runner.Submit(CoreOpcode.FlushDeaths);
				}

				g.SameLine();

				if (g.Button("To Max"))
				{
					battle.SetPoints(fighter, fighter.max_points);
				}
			}


			//g.Separator();

			//AImgui.Text("Stats:", ColorsXNA.Goldenrod);

			void _stat(string name, params float[] items)
			{
				g.Text(name);
				g.NextColumn();

				for (int i = 0; i < items.Length; i++)
				{
					g.Text(items[i].ToString());
					g.NextColumn();
				}
			}


			if (g.CollapsingHeader("Stats"))
			{
				Statf baseStats = fighter.info.Stats;
				Statf stats     = fighter.stats;

				g.Columns(3);

				// Headers
				g.NextColumn();
				g.Text("Base");
				g.NextColumn();
				g.Text("Current");
				g.NextColumn();

				_stat("Power", baseStats.power, stats.power);
				_stat("Will", baseStats.will, stats.will);
				_stat("Speed", baseStats.speed, stats.speed);
				_stat("AP (Actions)", baseStats.ap, stats.ap);


				g.Columns(1);
			}


			if (g.CollapsingHeader("Efficiencies"))
			{
				Elementf baseEff    = fighter.info.Resistances;
				Elementf currentEff = fighter.resistance;
				Elementf atk        = fighter.atk;
				Elementf def        = fighter.def;

				g.Columns(5);

				// Headers
				g.NextColumn();
				g.Text("Base");
				g.NextColumn();
				g.Text("Current");
				g.NextColumn();
				g.Text("Atk");
				g.NextColumn();
				g.Text("Def");
				g.NextColumn();

				_stat("Blunt", baseEff.blunt, currentEff.blunt, atk.blunt, def.blunt);
				_stat("Pierce", baseEff.pierce, currentEff.pierce, atk.pierce, def.pierce);
				_stat("Slash", baseEff.slash, currentEff.slash, atk.slash, def.slash);
				_stat("Gaia", baseEff.gaia, currentEff.gaia, atk.gaia, def.gaia);
				_stat("Astra", baseEff.astra, currentEff.astra, atk.astra, def.astra);
				_stat("Oida", baseEff.oida, currentEff.oida, atk.oida, def.oida);
				_stat("Physical", baseEff.physical, currentEff.physical, atk.physical, def.physical);
				_stat("Magical", baseEff.magical, currentEff.magical, atk.magical, def.magical);

				g.Columns(1);
			}

			//g.Separator();

			if (g.CollapsingHeader("Procs"))
			{
				//AImgui.Text("Procs:", ColorsXNA.Goldenrod);

				g.DragInt("Amount:", ref _procAmount);

				if (g.Button("Damage"))
				{
					Proc proc = new Proc(battle);
					proc.AddEffect(new HurtHP(_procAmount, _procElement, true));
					proc.AddVictim(fighter);

					battle.Proc(proc);
				}

				g.SameLine();
				AImgui.EnumDrawer("Element", ref _procElement);

				if (g.Button("Heal"))
				{
					Proc proc = new Proc(battle);
					proc.AddEffect(new HealHP(_procAmount));
					proc.AddVictim(fighter);

					battle.Proc(proc);
				}
			}

			if (g.CollapsingHeader("States"))
			{
				g.DragInt("Amount:", ref _stateAmount);
				g.DragInt("Life:", ref _stateLife);
				AImgui.EnumDrawer("Element", ref _stateElement);

				if (g.Button("DOT"))
				{
					State state = battle.globalASM.state();
					state.life = _stateLife;
					//state.tags.Add();

					Trigger trigger = battle.globalASM.trigger();

					trigger.signal = Signals.end_turns;
					trigger.life   = _stateLife;
					trigger.AddHandlerDV(new Trigger.Handler(Trigger.HandlerType.action, x =>
					{
						x.state.Decay();

						Proc proc = new Proc(battle);
						proc.AddEffect(new HurtHP(_stateAmount, _stateElement, true));
						proc.AddVictim(fighter);

						battle.Proc(proc);
					}));
					//trigger.

					state.AddTrigger(trigger);
					battle.AddState(state);
				}

				if (g.Button("Root"))
				{
					State state = battle.globalASM.state();
					state.life = _stateLife;
					//state.tags.Add();
					state.ID = "root";
					state.status.Add(new StateCmd(EngineFlags.unmovable));

					state.AddTrigger(decayTrigger(_stateLife));
					battle.AddState(state);
				}


				Trigger decayTrigger(int life)
				{
					Trigger trigger = battle.globalASM.trigger();

					trigger.signal = Signals.end_turns;
					//trigger.life   = life;
					trigger.AddHandlerDV(new Trigger.Handler(Trigger.HandlerType.action, x =>
					{
						x.state.Decay();
					}));

					return trigger;
				}
			}
		}


		void DrawBuffControls(State state)
		{
			AImgui.Text($"ID: {state.ID}", ColorsXNA.CornflowerBlue);
			g.Indent(16);
			AImgui.Text($"Dealer: {state.dealer}");
			//g.SameLine();
			g.DragInt("Life", ref state.life);

			g.DragInt("Stack Max", ref state.stackMax);
			g.SameLine();
			g.Checkbox("Stack Refresh", ref state.stackRefresh);

			AImgui.Text($"Commands:", ColorsXNA.Goldenrod);
			g.Indent(16);
			g.PushID("commands");
			AImgui.DrawList(state.status.cmds);
			g.PopID();
			g.Unindent(16);

			AImgui.Text($"Triggers:", ColorsXNA.Goldenrod);
			g.Indent(16);
			g.PushID("triggers");
			AImgui.DrawList(state.triggers);
			g.PopID();
			g.Unindent(16);

			AImgui.Text($"Bufees:", ColorsXNA.Goldenrod);
			g.Indent(16);
			g.PushID("statees");
			for (int i = 0; i < state.statees.Count; i++)
			{
				g.PushID(i);
				g.Text($"{i}: {state.statees.Count}");
				AImgui.DrawList(state.statees);
				g.PopID();
			}

			g.PopID();
			g.Unindent(16);

			/*int modcount  = state.stats.cmds.Count;
			int trigcount = state.triggers.Count;
			g.DragInt("Effects",  ref modcount);
			g.DragInt("Triggers", ref trigcount);*/
		}
	}
}