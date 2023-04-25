using System.Collections.Generic;
using Anjin.Actors;
using API.Spritesheet.Indexing.Runtime;
using Overworld.Controllers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util;
using Util.Animation;

namespace Anjin.Nanokin.ParkAI
{
	public class AIAvatarOld : SerializedMonoBehaviour
	{
		// OLD:
		public Transform GroundPivotTransform;
		public Transform BillboardTransform;

		public PeepLOD LOD;

		[FormerlySerializedAs("Animator")]
		public SpriteAnim RootAnim;

		public CharacterRig Rig;

		public Vector3 targetPosition;
		public Vector3 LookDirection;

		float ground_y_offset = 0;

		public float starting_y_offset;
		public float sitting_y_offset;

		public PeepDef definition;

		public void PlayOnAnimator(string animation)
		{
			var pos = BillboardTransform.localPosition;
			BillboardTransform.localPosition = new Vector3(pos.x, (animation == "sit") ? sitting_y_offset : starting_y_offset, pos.z);

			PlayOptions opt = PlayOptions.Continue;

			float blending = MathUtil.ToWorldAzimuthBlendable(LookDirection);

			Direction8 ordinal  = MathUtil.ToWorldAzimuthOrdinal(blending);
			Direction8 cardinal = MathUtil.ToWorldAzimuthCardinal(blending);

			// Try playing the animation directly by its exact name.
			bool success = RootAnim.Play((animation, "stand"), opt).IsPlaying(); // TODO

			// Try the animation in various directions. (whichever is found first)
			success = success || TryPlay(ordinal, animation, opt);                   // Ordinal (8 frames)
			success = success || TryPlay(ordinal.FlipHorizontal(), animation, opt);  // Ordinal (5 frames + mirroring on right)
			success = success || TryPlay(cardinal, animation, opt);                  // Cardinal (4 frames)
			success = success || TryPlay(cardinal.FlipHorizontal(), animation, opt); // Cardinal (3 frames + mirroring on right)
			success = success || TryPlay(ordinal, "stand", opt);                     // Cardinal (3 frames + mirroring on right)
			success = success || TryPlay(ordinal.FlipHorizontal(), "stand", opt);    // Cardinal (3 frames + mirroring on right)

			// TODO Verify this
			RootAnim.playSpeed = 0.65f;
		}

		private static Dictionary<(string, Direction8), string> _cachedAnimationNames = new Dictionary<(string, Direction8), string>();
		private static Dictionary<Direction8, string> _cachedStandingAnimations = new Dictionary<Direction8, string>
		{
			{Direction8.Down, "stand_" + DirUtil.ToString(Direction8.Down)},
			{Direction8.DownLeft, "stand_" + DirUtil.ToString(Direction8.DownLeft)},
			{Direction8.Left, "stand_" + DirUtil.ToString(Direction8.Left)},
			{Direction8.UpLeft, "stand_" + DirUtil.ToString(Direction8.UpLeft)},
			{Direction8.Up, "stand_" + DirUtil.ToString(Direction8.Up)},
			{Direction8.UpRight, "stand_" + DirUtil.ToString(Direction8.UpRight)},
			{Direction8.Right, "stand_" + DirUtil.ToString(Direction8.Right)},
			{Direction8.DownRight, "stand_" + DirUtil.ToString(Direction8.DownRight)},
		};

		private bool TryPlay(Direction8 direction, string animation, PlayOptions option)
		{
			(string animName, Direction8 dir) pair = (animation, direction);

			string dirName = DirUtil.ToString(direction);

			if (!_cachedAnimationNames.TryGetValue(pair, out string directionalAnimation))
			{
				// Cache the combination of name and direction.
				_cachedAnimationNames[pair] = directionalAnimation = $"{animation}_{dirName}";
			}

			string standing = _cachedStandingAnimations[direction];

			// Attempt playing this animation name.
			return RootAnim.player.Play((directionalAnimation, standing), option).IsPlaying();
		}

		public void UpdateLOD(PeepLOD lod)
		{
			//if (lod == LOD) return;
			//Profiler.BeginSample("Update LOD");
			LOD = lod;
			//farLOD.SetActive(lod == PeepLOD.Far);
			//nearLOD.SetActive(lod == PeepLOD.Near);

			//Profiler.EndSample();
		}

		void Update()
		{
			if (LOD == PeepLOD.LOD0 && ParkAIController.Config.AvatarGroundSnapping)
			{
				if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out var ray, 4f, Layers.Walkable.mask, QueryTriggerInteraction.Ignore))
				{
					ground_y_offset = ray.point.y - transform.position.y;
				}
				else ground_y_offset = 0;

				GroundPivotTransform.localPosition = new Vector3(0, ground_y_offset, 0);
			}
		}

		/*public ParkAIAgent.Actions CurrentAction() => ( agent.actions != null && agent.actions.Count > 0 )
			? agent.actions.Peek()
			: ParkAIAgent.Actions.Stand;*/

		public static int GetBlendedDirectionIndex(float[] blending, float d)
		{
			int idx = 0;
			for (int i = 0; i < blending.Length; i++)
			{
				if (d < blending[i])
				{
					idx = i;
					break;
				}
			}

			return idx;
		}

		public static string GetBlendedDirection(float[] blending, string[] directions, float d)
		{
			//UnityEngine.Assertions.Assert.AreEqual(blending.Length, directions.Length);
			return directions[GetBlendedDirectionIndex(blending, d)];
		}


		private static string GetLeftFacingDirection(string n)
		{
			switch (n)
			{
				case "upRight":   return "upLeft";
				case "right":     return "left";
				case "downRight": return "downLeft";
				default:          return n;
			}
		}

		/*public bool PlayDirectionalAnimation(SpriteAnimator animator , string anim, bool withError = true)
		{
			if (animator.Player == null)
				return false;

			float azimuth = CharacterAnimatorOld.ToWorldAzimuth(LookDirection);

			// ordinals first
			string direction = GetBlendedDirection(OrdinalUtil.blendingOrdinal, OrdinalUtil.ordinals, azimuth);

			if (animator.Player.Play(anim + "_" + direction) == AnimationPlayStatus.OK) // full Ordinal8
			{
				return true;
			}

			if (animator.Player.Play(anim + "_" + GetLeftFacingDirection(direction)) == AnimationPlayStatus.OK) // reduced Ordinal5+FlipX
			{
				return true;
			}

			// now try cardinals
			direction = GetBlendedDirection(OrdinalUtil.blendingCardinal, OrdinalUtil.cardinals, azimuth);

			if (animator.Player.Play(anim + "_" + direction) == AnimationPlayStatus.OK) // full Cardinal4
			{
				return true;
			}

			if (animator.Player.Play(anim + "_" + GetLeftFacingDirection(direction)) == AnimationPlayStatus.OK) // reduced Ordinal3+FlipX
			{
				return true;
			}

			if (animator.Player.IsAnimationAvailable(anim + "_" + direction))
			{
				return false;
			}

			if (withError)
			{
				//				Debug.LogError("An actor needs at least cardinal directions to face towards (Attempting to play nonexistent `" + anim + "_" + direction + "` sprite animation)");
			}

			return false;
		}*/

		/*public bool PlayDirectionalAnimation(SpriteAnimator _animator, string animationName)
		{
			if (_animator.Player == null)
				return false;

			float azimuth = CharacterAnimator.ToWorldAzimuth(LookDirection);

			// ordinals first
			string direction = GetBlendedDirection(OrdinalUtil.blendingOrdinal, OrdinalUtil.ordinals, azimuth);

			// full Ordinal8
			if (_animator.Player.Play(animationName + "_" + direction).IsPlaying()) return true;

			// reduced Ordinal5+FlipX
			if (_animator.Player.Play(animationName + "_" + GetLeftFacingDirection(direction)).IsPlaying()) return true;

			// now try cardinals
			direction = GetBlendedDirection(OrdinalUtil.blendingCardinal, OrdinalUtil.cardinals, azimuth);

			// full Cardinal4
			if (_animator.Player.Play(animationName + "_" + direction).IsPlaying()) return true;

			// reduced Ordinal3+FlipX
			if (_animator.Player.Play(animationName + "_" + GetLeftFacingDirection(direction)).IsPlaying()) return true;

			if (_animator.Player.IsAnimationAvailable(animationName + "_" + direction)) return false;

			//Debug.LogError("An actor needs at least cardinal directions to face towards
			//(Attempting to play nonexistent `" + anim + "_" + direction + "` sprite animation)");

			return false;
		}*/

		/*public bool TryDirectionalAnimation(SpriteAnimator animator, string anim, params string[] fallbacks)
		{
			return !PlayDirectionalAnimation(animator, anim) && fallbacks.Any(fallback => PlayDirectionalAnimation(animator, fallback));
		}*/
	}
}