using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityUtilities;
using Util.Components.UI;
using Util.Extensions;

namespace Combat.UI
{
	public class Reticle : SerializedMonoBehaviour
	{
		[FormerlySerializedAs("_idlePulseCurve"), Title("Config")]
		[SerializeField] private AnimationCurve IdlePulse = AnimationCurve.Linear(0, 0, 1, 1);

		[FormerlySerializedAs("_baseCornerDistance"), SerializeField]
		private float BaseCornerDistance;

		[FormerlySerializedAs("_confirmAnimationDuration"), SerializeField]
		private float ConfirmAnimationDuration;

		[FormerlySerializedAs("_rtfmCorners"), Title("References")]
		[SerializeField, ChildGameObjectsOnly] private RectTransform[] Corners;

		[SerializeField] public WorldToCanvasRaycast Raycast;

		[Title("Animation")]
		[SerializeField] public Animation Animator;
		[SerializeField] private AnimationClip EnterAnim;
		[SerializeField] private AnimationClip ExitAnim;
		[SerializeField] private AnimationClip ConfirmAnim;

		[Space]
		[SerializeField] public float Opacity = 1; // Animable value

		private bool     _isIdle;
		private float    _elapsedIdleTime;
		private float    animationCountdown;
		private Image[]  _images;
		private bool     _awaitingDestruction;
		private float    _cornerDistance;
		private Corner[] _corners;
		private bool     _entered;

		private bool IsIdle
		{
			get => _isIdle;
			set
			{
				_isIdle = value;

				if (!_isIdle)
				{
					_elapsedIdleTime = 0;
				}
			}
		}

		private void Awake()
		{
			transform.ResetLocal(); // don't ask why

			_images = GetComponentsInChildren<Image>();

			_corners = new Corner[Corners.Length];
			for (var i = 0; i < Corners.Length; i++)
			{
				RectTransform rtfmCorner = Corners[i];

				_corners[i] = new Corner(
					rtfmCorner.anchoredPosition,
					rtfmCorner.anchoredPosition.normalized
				);
			}

			// This is needed because if we render the reticle right from the get-go, it
			// will be centered on the screen incorrectly
			foreach (Image image in _images)
				image.gameObject.SetActive(true);

			Appear();
			Update();
		}

		public void Appear()
		{
			_elapsedIdleTime = 0f;
			Animator.PlayClip(EnterAnim);
		}

		private void BeginExit(AnimationClip anim)
		{
			IsIdle               = false;
			_awaitingDestruction = true;

			Animator.PlayClip(anim);
		}

		public void Disappear(bool confirm)
		{
			BeginExit(ExitAnim);
		}

		public void Confirm()
		{
			BeginExit(ConfirmAnim);
			animationCountdown = ConfirmAnimationDuration;
		}

		private void Update()
		{
			// Idle behavior
			// ----------------------------------------
			IsIdle = !Animator.isPlaying;
			if (IsIdle && _awaitingDestruction)
				Destroy(gameObject);


			// Is playing an animation?
			if (animationCountdown > 0)
			{
				float t              = animationCountdown / ConfirmAnimationDuration;
				float cornerDistance = _cornerDistance * t;

				RefreshCorners(cornerDistance);

				animationCountdown -= Time.deltaTime;
			}
			else
			{
				_cornerDistance = BaseCornerDistance;

				if (IsIdle)
				{
					// The idle animation has a pulse
					_elapsedIdleTime += Time.deltaTime;

					IdlePulse.postWrapMode = WrapMode.Loop;

					float pulseForce = IdlePulse.Evaluate(_elapsedIdleTime);
					_cornerDistance += pulseForce;
				}

				RefreshCorners(_cornerDistance);
			}


			// Update images' alpha
			// ----------------------------------------
			foreach (Image image in _images)
			{
				image.color = image.color.Alpha(Opacity);
			}
		}

		private void LateUpdate()
		{
			// if (_entered && _images[0].gameObject.activeSelf)
			// {
			// foreach (Image image in _images)
			// {
			// image.gameObject.SetActive(false);
			// }
			// }
		}

		private void RefreshCorners(float radius)
		{
			for (var i = 0; i < _corners.Length; i++)
			{
				Corner corner = _corners[i];
				Corners[i].anchoredPosition = corner.position + corner.angle * radius;
			}
		}

		private readonly struct Corner
		{
			public readonly Vector2 position;
			public readonly Vector2 angle;

			public Corner(Vector2 position, Vector2 angle)
			{
				this.position = position;
				this.angle    = angle;
			}
		}
	}
}