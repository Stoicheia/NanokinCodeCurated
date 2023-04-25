using System;
using Anjin.Cameras;
using Anjin.Nanokin;
using Shapes;
using UnityEngine;

namespace Anjin.Minigames
{
	public class MinecartMinigameUI : ImmediateModeShapeDrawer
	{
		private MinecartMinigame _game;

		private Quaternion rot;

		private float _leftScale;
		private float _rightScale;

		private void Awake()
		{
			_game = GetComponent<MinecartMinigame>();
		}

		private void Update()
		{
			rot = Quaternion.Slerp(rot, GameCams.Live.UnityCam.transform.rotation, 0.4f);
		}

		public override void DrawShapes(Camera cam)
		{
			if (!_game || !_game.IsRunning || _game.Cart == null) return;

			CoasterUIInputs inputs = _game.Cart.UIInputs;

			using (Draw.Command(cam))
			{
				Draw.PushMatrix();
				Draw.SetMatrix(Matrix4x4.TRS(_game.Cart.transform.position + Vector3.up * 3, rot, Vector3.one));

				// set up static parameters. these are used for all following Draw.Line calls
				Draw.LineGeometry   = LineGeometry.Volumetric3D;
				Draw.ThicknessSpace = ThicknessSpace.Pixels;
				Draw.Thickness      = 4; // 4px wide
				Draw.LineEndCaps    = LineEndCap.Round;

				bool left  = inputs.left_arrow == CoasterUIInputs.State.Selected;
				bool right = inputs.right_arrow == CoasterUIInputs.State.Selected;

				_leftScale  = Mathf.Lerp(_leftScale,	left	? 0.75f : 0.5f, 0.2f);
				_rightScale = Mathf.Lerp(_rightScale, right ? 0.75f : 0.5f, 0.2f);

				if(inputs.left_arrow != CoasterUIInputs.State.Off)
					Arrow(new Vector3(-1.5f,  0, 0), Vector3.left, _leftScale);

				if(inputs.right_arrow != CoasterUIInputs.State.Off)
					Arrow(new Vector3(1.5f, 0, 0), Vector3.right,  _rightScale);

				Draw.PopMatrix();

				void Arrow(Vector3 offset, Vector3 forwards, float scale)
				{
					Draw.Triangle(
						offset                    + forwards     * scale,
						offset - forwards * scale + Vector3.up   * scale,
						offset - forwards * scale + Vector3.down * scale,
						ColorsXNA.MediumPurple
					);

					Draw.TriangleBorder(
						offset - forwards * scale + Vector3.up   * scale,
						offset - forwards * scale + Vector3.down * scale,
						offset                    + forwards     * scale,
						ColorsXNA.DeepPink
					);
				}

			}

		}
	}
}