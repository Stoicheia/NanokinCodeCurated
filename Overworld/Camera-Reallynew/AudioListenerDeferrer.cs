	using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Nanokin;
using Anjin.Util;
using UnityEngine;

public class AudioListenerDeferrer : MonoBehaviour
{
	private GameController _controller;
	private AudioListener  _originalListener;
	private AudioListener  _newListener;
	private AudioDeferee   _carrier;

	private bool _outOfCutsceneFlag;

	[SerializeField] private AudioListener DefaultListener;
	[SerializeField] private Transform StartPosition;

	private void Awake()
	{
		_outOfCutsceneFlag = false;
	}

	private void Start()
	{
		_controller = GameController.Live;
	}

	private void OnEnable()
	{
		_originalListener = DefaultListener;
		_originalListener.enabled = true;
		_originalListener.transform.position = StartPosition.transform.position;
		if (DefaultListener == null) DefaultListener = this.GetOrAddComponent<AudioListener>();
		AudioDeferee.OnActivate += DeferTo;
		AudioDeferee.OnDeactivate += Undefer;
	}

	private void OnDisable()
	{
		AudioDeferee.OnActivate -= DeferTo;
		AudioDeferee.OnDeactivate -= Undefer;
	}

	private void DeferTo(AudioDeferee a)
	{
		_carrier                  = a;
		_newListener              = a.Listener;
		_newListener.enabled      = true;
		_originalListener.enabled = false;
	}

	private void Undefer(AudioDeferee a)
	{
		if (a == _carrier)
		{
			_carrier = null;
			if (_newListener != null)
			{
				_newListener.enabled = false;
			}
			_newListener              = null;
			_originalListener.enabled = true;
			_originalListener.transform.position = StartPosition.transform.position;
		}
	}
}