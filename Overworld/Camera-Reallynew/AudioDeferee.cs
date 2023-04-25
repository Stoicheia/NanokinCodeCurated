using System;
using System.Collections;
using System.Collections.Generic;
using Anjin.Nanokin;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(AudioListener))]
public class AudioDeferee : MonoBehaviour
{
	public static event Action<AudioDeferee> OnActivate;
	public static event Action<AudioDeferee> OnDeactivate;


	private AudioListener _listener;
	private bool _activated;
	private GameController.GameState _stateFlag;
	private GameController _controller;

	public bool Activated => _activated;
	public AudioListener Listener => _listener;

	[SerializeField] private bool _activateOnEnable = true;
	[SerializeField] private bool _disableInCutscenes = true;
	[SerializeField] private bool _disableInCombat = true; //don't want to make enumflags, might mess something up for demo

	private void Awake()
	{
		_listener = GetComponent<AudioListener>();
	}

	private void Start()
	{
		_controller = GameController.Live;
		_stateFlag = GameController.GameState.Overworld;
		if(_activateOnEnable) ToggleActive(true);
	}

	private void OnEnable()
	{
		if(_activateOnEnable) ToggleActive(true);
	}

	private void OnDisable()
	{
		ToggleActive(false);
	}

	private void Update()
	{
		if (_disableInCutscenes)
		{
			CheckCutsceneActivation();
		}

		if (_disableInCombat)
		{
			CheckCombatActivation();
		}
	}


	private void ToggleActive(bool b)
	{
		_activated = b;
		if(_activated) OnActivate?.Invoke(this);
		if(!_activated) OnDeactivate?.Invoke(this);
	}

	private void CheckCutsceneActivation()
	{
		if (_stateFlag != GameController.GameState.Cutscene && _controller.StateGame == GameController.GameState.Cutscene)
		{
			ToggleActive(false);
			_stateFlag = GameController.GameState.Cutscene;
		}

		else if (_stateFlag == GameController.GameState.Cutscene && _controller.StateGame != GameController.GameState.Cutscene)
		{
			ToggleActive(true);
			_stateFlag = _controller.StateGame;
		}
	}

	private void CheckCombatActivation()
	{
		if (_stateFlag != GameController.GameState.Battle && _controller.StateGame == GameController.GameState.Battle)
		{
			ToggleActive(false);
			_stateFlag = GameController.GameState.Battle;
		}

		else if (_stateFlag == GameController.GameState.Battle && _controller.StateGame != GameController.GameState.Battle)
		{
			ToggleActive(true);
			_stateFlag = _controller.StateGame;
		}
	}
}
