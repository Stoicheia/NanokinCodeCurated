using System.Collections.Generic;
using System.Text;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Nanokin;
using Anjin.Nanokin.ParkAI;
using Anjin.UI;
using Anjin.Util;
using Drawing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityUtilities;
using Util;
using g = ImGuiNET.ImGui;

namespace Overworld.Controllers
{
	public class TeleportTool : DebugCameraTool
	{
		public override void OnUpdate()
		{
			if (GameInputs.IsPressed(Key.Backspace) || GameInputs.InputsEnabled && Mouse.current.rightButton.wasPressedThisFrame)
			{
				Exit();
				return;
			}

			var (hit, ok) = GameCams.RaycastFromCamera(new Vector2(Screen.width, Screen.height) * 0.5f, Layers.Walkable.mask);
			if (ok)
			{
				Draw.ingame.WireCapsule(hit.point, hit.point + Vector3.up * 1.5f, 0.4f, ColorsXNA.BlueViolet);

				if (GameInputs.IsPressed(Key.Enter) || GameInputs.InputsEnabled && Mouse.current.leftButton.wasPressedThisFrame)
				{
					if (GameInputs.IsDown(Key.LeftCtrl)) {
						ActorController.playerActor.Teleport(hit.point);
					} else {
						foreach (Actor actor in ActorController.partyActors)
							actor.Teleport(hit.point);
					}

					if(!GameInputs.IsDown(Key.LeftShift))
						Exit();
				}
			}
		}

		public override void OnToolbarString(StringBuilder builder) => builder.Append("Enter/LMB: Teleport (LCtrl: Only player, LShift: Don't exit),  Backspace/RMB: Cancel  ");

		public override string Name             => "Teleport";
		public override string ShortcutString   => "T";
		public override bool   ShouldActivate() => Keyboard.current.tKey.wasPressedThisFrame;
	}


	public class ParkAITool : DebugCameraTool
	{
		private ParkAIPeep Inspecting = null;

		public override void OnUpdate()
		{
			var drawer = Draw.ingame;

			if (GameInputs.IsPressed(Key.Backspace))
			{
				Exit();
				return;
			}

			if (ParkAIController.Live.State != ParkAIController.SystemState.Running &&
			    ParkAIController.Live.State != ParkAIController.SystemState.Paused)
			{
				return;
			}

			GameHUD.Live.DebugCrosshairs.SetActive();

			List<ParkAIPeep> peeps = ParkAIController.Live.AllPeeps;

			Ray ray = GameCams.Live.UnityCam.ScreenPointToRay(new Vector2(Screen.width, Screen.height) * 0.5f);

			//Draw.Cross(ray.GetPoint(0.5f), 0.5f, Color.green);

			for (var i = 0; i < peeps.Count; i++)
			{
				var peep = peeps[i];
				var r    = 0.3f;

				Vector3 pos = peep.Agent.Position + Vector3.up * r;

				(bool hit, float dist) = IntersectionUtil.Intersection_RaySphere(ray, pos, r);

				Color col = Color.blue;

				if (peep.Agent.Location.region_obj != null && peep.Agent.Location.region_obj.RequiresPathfinding &&
				    (peep.Agent.Path == null || !peep.Agent.Path.IsDone() || peep.Agent.Path.error))
				{
					col = Color.red;

					if(peep.Agent.Path != null && !peep.Agent.Path.IsDone())
						col = Color.yellow;

					drawer.Arrow(pos + Vector3.up * 5, pos + Vector3.up * 3, Vector3.up, 1, Color.red);
				}

				if (Inspecting == peep)
				{
					drawer.WireSphere(pos, r, col);
					//using(drawer.WithLineWidth(2f))
				}
				else if (hit)
				{
					drawer.WireSphere(pos, r, ColorsXNA.Orange);

					if (Inspecting != peep && Mouse.current.leftButton.wasPressedThisFrame)
					{
						Inspecting = peep;
					}
				}
				else
				{
					drawer.WireSphere(pos, r, Color.white.Alpha(0.25f));
				}

				if (peep.Agent.Path != null && peep.Agent.Path.IsDone())
				{
					drawer.Polyline(peep.Agent.Path.vectorPath, Color.red);
				}
			}

			if (Inspecting != null) { }
		}

		public override void OnGui(ref DebugSystem.State state)
		{
			if (g.Begin("ParkAI Inspector"))
			{
				if (Inspecting != null)
				{
					ParkAIController.Live.ImGuiPeepStats(Inspecting);
				}
			}

			g.End();
		}


		public override void Exit()
		{
			base.Exit();
			Inspecting = null;
			GameHUD.Live.DebugCrosshairs.SetActive(false);
		}

		public override void OnToolbarString(StringBuilder builder) => builder.Append("Backspace: Cancel  ");

		public override string Name             => "ParkAI Inspector";
		public override string ShortcutString   => "P";
		public override bool   ShouldActivate() => Keyboard.current.pKey.wasPressedThisFrame;
	}


	public abstract class DebugCameraTool
	{
		public abstract string Name           { get; }
		public abstract string ShortcutString { get; }
		public abstract bool   ShouldActivate();

		public virtual void OnActivate()   { }
		public virtual void OnDeactivate() { }

		public virtual void OnUpdate()                                   { }
		public virtual void OnToolbarString(StringBuilder       builder) { }
		public virtual void OnGui(ref   DebugSystem.State       state)   { }
		public virtual void OnInput(ref FirstPersonFlightInputs inputs)  { }

		public virtual void Exit()
		{
			OnDeactivate();
			DebugCamera.Live.currentTool = null;
		}
	}
}