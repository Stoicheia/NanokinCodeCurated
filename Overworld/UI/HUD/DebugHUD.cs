using System.Collections.Generic;
using Anjin.Nanokin;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;


public class DebugHUD : StaticBoy<DebugHUD>
{
	public enum Pages { Main, Camera}

	private bool _debugHUDOpen;
	[ShowInInspector]
	public bool DebugHUDOpen
	{
		get => _debugHUDOpen;
		set
		{
			CanvasRoot.gameObject.SetActive(value);
			_debugHUDOpen = value;
		}
	}

	public RectTransform                   CanvasRoot;
	public Dictionary<Pages,RectTransform> PageObjects;

	public Pages CurrentPage;

	public void Start()
	{
		//Trigger the setter at start
		DebugHUDOpen = DebugHUDOpen;

		foreach (var o in PageObjects)
		{
			o.Value.gameObject.SetActive(false);
		}
	}

	public void Update()
	{
		if (GameInputs.IsPressed(Key.F1))
		{
			DebugHUDOpen = !DebugHUDOpen;
		}
	}

	//These are here because the button UI doesn't have parametered UnityEvents
	public void OpenPage_Main()   => OpenPage(Pages.Main);
	public void OpenPage_Camera() => OpenPage(Pages.Camera);

	public void OpenPage(Pages page)
	{
		foreach (var o in PageObjects)
		{
			o.Value.gameObject.SetActive(o.Key == page);
		}

		CurrentPage = page;
	}

}