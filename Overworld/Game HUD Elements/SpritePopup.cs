using System;
using Anjin.UI;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SpritePopup : MonoBehaviour
{


	public enum State { Off, On, Transition }
	public State state = State.Off;

	public float Timer;

	[FoldoutGroup("References")] public HUDElement HudElement;

	public Sprite Sprite;
	public Image  Image;
	public UnityEvent UE_OnDone;

	[Header("Distance Scaling")]
	public static float MinScale;
	public static float MaxScale;
	public static float MinScaleDistance;
	public static float MaxScaleDistance;

	void Awake()
	{
		HudElement = GetComponent<HUDElement>();

		Timer = 0;
	}

	void Update()
	{
		if (!Image) return;
		Image.sprite = Sprite;

		if (state == State.On) {
			Timer -= Time.deltaTime;
			if (Timer <= 0) {
				StartDeactivation();
			}
		}
	}

	public void Show(Sprite sprite, float seconds)
	{
		Sprite = sprite;
		Timer = seconds;
		StartActivation();
	}

	public void Hide(bool instant)
	{
		if(state == State.On || state == State.Transition) {
			if (instant) {
				state                   = State.Off;
				HudElement.Alpha        = 0;
				HudElement.ScreenOffset = Vector2.zero;
			} else {
				StartDeactivation();
			}
		}
	}

	public void StartActivation()
	{
		if(state == State.Off)
		{
			state = State.On;
			HudElement.SetChildrenActive(true);
			HudElement.DoAlphaFade(0, 1, 0.5f);
			HudElement.DoOffset(Vector3.zero, Vector3.up * 0.3f, 0.2f);
		}
	}

	public void StartDeactivation()
	{
		if (state == State.On || state == State.Transition)
		{
			state = State.Transition;
			HudElement.DoAlphaFade(1, 0, 0.8f);
			HudElement.DoOffset(Vector3.up * 0.3f, Vector3.zero, 0.2f).Tween.OnComplete(OnDone);
		}
	}

	void OnDone()
	{
		state = State.Off;
		HudElement.SetChildrenActive(false);
		UE_OnDone.Invoke();
	}
}
