using System.Text;
using Anjin.Cameras;
using Anjin.Nanokin;
using Anjin.Util;
using Combat.Components;
using Combat.Entities;
using Drawing;
using JetBrains.Annotations;
using UnityEngine;

namespace Combat.Chips
{
	public class DebugVisChip : Chip
	{
		private static StringBuilder _sb = new StringBuilder();

		const float BarHeight = 0.2f;
		const float BarWidth  = 2f;
		const float BarPivotX = 0.5f;
		const float BarPivotY = 0.5f;

		public override void Update()
		{
			if (!GameController.DebugMode) return;

			DrawArena();
			DrawPointBars();
		}

		private void DrawArena()
		{
			CommandBuilder d = Draw.ingame;

			// Center of the arena
			using (d.InLocalSpace(battle.arena.transform))
			using (d.WithColor(Color.black))
			{
				d.WireBox(battle.arena.transform.position, 0.3f);
			}

			// Camera body point
			using (d.WithColor(Color.red))
			{
				d.WireSphere(battle.arena.Camera.bodyPoint.position, 0.25f);
			}

			// Camera look point (usually synced to the body point)
			using (d.WithColor(Color.yellow))
			{
				d.WireSphere(battle.arena.Camera.lookPoint.position, 0.25f);
			}
		}

		private void DrawPointBars()
		{
			foreach (Fighter fighter in battle.fighters)
			{
				if (fighter.actor == null) continue;

				Vector3 pos = fighter.actor.transform.position +
				              fighter.actor.height * (fighter.actor.visualTransform.rotation * Vector3.up) +
				              Vector3.up * .75f;

				var mat = Matrix4x4.TRS(pos, GameCams.Live.UnityCam.transform.rotation, Vector3.one);
				using (Draw.ingame.WithMatrix(mat))
				{
					DrawPointBars(fighter);
				}
			}
		}


		public void DrawPointBars([NotNull] Fighter fighter)
		{
			CommandBuilder d = Draw.ingame;

			Vector3 realPos = fighter.actor.transform.position +
			                  fighter.actor.visualTransform.rotation * Vector3.up * fighter.actor.height;


			const float REAL_SIZE     = 14f;
			const float REAL_SHADOW   = 0.025f;
			const float REAL_DISTANCE = 7.5f;

			float dist  = GameCams.DistanceTo(realPos);
			float dnorm = 1 / (dist / REAL_DISTANCE);

			float bwidth  = BarWidth / dnorm;
			float bheight = BarHeight / dnorm;

			DrawBar(ref d, realPos, new Vector2(0, bheight), fighter.hp, fighter.hp_percent, ColorsXNA.DarkMagenta, ColorsXNA.Magenta, new Vector2(bwidth, bheight));
			DrawBar(ref d, realPos, new Vector2(0, 0), fighter.sp, fighter.sp_percent, ColorsXNA.DarkCyan, ColorsXNA.LightCyan, new Vector2(bwidth, bheight));

			const float INFO_MOUSE_DIST = 135;

			if (Vector2.Distance(GameCams.WorldToScreen(realPos), GameInputs.GetMousePosition()) < INFO_MOUSE_DIST)
			{
				// d.SolidPlane(new Vector3(bheight + bheight / 2f, 0, 0), Vector3.forward, new Vector2(bheight / 2, bwidth), Color.black);
				ShadedLabel2D(
					ref d,
					realPos,
					new Vector3(0, bheight * 2, 0),
					$"Lv: {(fighter.level.HasValue ? fighter.level.Value.ToString() : "-")} OP: {fighter.op}/{fighter.max_points.op} States: {fighter.states.Count}"
				);
			}

			/*if (fighter.states.Count > 0) {
				_sb.Clear();
				foreach (State state in fighter.states) {
					_sb.Append(state.)
				}
			}*/

			/*d.SolidPlane(new Vector3(-(width / 2) * (1 - fighter.hp_percent), 0, 0), Vector3.forward, new Vector2(height, width * fighter.hp_percent), Color.red);

			d.WirePlane(Vector3.zero, Vector3.forward, new Vector2(height, width), ColorsXNA.OrangeRed);
			d.Label2D(new Vector3(-(width / 2), 0, 0), fighter.hp.ToString(), 20f, LabelAlignment.MiddleLeft, Color.white);*/
		}

		private static void DrawBar(
			ref CommandBuilder d,
			Vector3            realPos,
			Vector3            pos,
			float              valueAbs,
			float              valuePercent,
			Color              fillColor,
			Color              edgeColor,
			Vector2            size)
		{
			// pos -= Vector3.Scale(size, new Vector3(BarPivotX, BarPivotY, 0));

			// For some reason, here the X and Y axes are exchanged......
			// pos  = new Vector3(pos.y, pos.x, pos.z);
			size = new Vector2(size.y, size.x);


			// float dshad = (dist / REAL_DISTANCE) * REAL_SHADOW;

			// Edge
			// using (d.WithLineWidth(0.22f))
			// {
			// d.WirePlane(pos, Vector3.forward, size, edgeColor);
			// }

			// Fill
			// d.SolidPlane(pos + new Vector3(0, 0, 0.1f), Vector3.forward, size, Color.black.Alpha(0.65f));
			d.SolidPlane(pos, Vector3.forward, new Vector2(size.x * valuePercent, size.y), fillColor); //

			// Label (abs)
			ShadedLabel2D(ref d, realPos, pos, $"{valueAbs:F0} / {(valueAbs / valuePercent).Floor()}");
		}

		private static void ShadedLabel2D(ref CommandBuilder d, Vector3 realPos, Vector3 pos, string text)
		{
			const float REAL_SIZE     = 14f;
			const float REAL_SHADOW   = 0.025f;
			const float REAL_DISTANCE = 7.5f;

			float dist = GameCams.DistanceTo(realPos);
			float shad = (dist / REAL_DISTANCE) * REAL_SHADOW;

			Vector3 spos = pos + new Vector3(REAL_SHADOW, -REAL_SHADOW, 0);

			d.Label2D(spos, text, REAL_SIZE, LabelAlignment.Center, Color.black);
			d.Label2D(pos, text, REAL_SIZE, LabelAlignment.Center, Color.white);
		}
	}
}