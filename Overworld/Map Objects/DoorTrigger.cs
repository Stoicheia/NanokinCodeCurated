using System;
using Animancer;
using Anjin.Actors;
using Anjin.Scripting;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.Nanokin.Map
{
	[RequireComponent(typeof(AnimancerComponent))]
	public class DoorTrigger : MonoBehaviour
	{
		//[Required]
		//public AnimationClip OpenAnim, CloseAnim;

		public enum States {
			Closed,
			Open,
			Closing,
			Opening,
		}

		[Required]
		public ClipTransition Anim_Open, Anim_Close;

		private AnimancerComponent _anim;
		//private Animation          _anim;

		[ShowInPlay]
		private States _state;

		[DebugVars]
		private int _num;

		private AnimancerState _open;
		private AnimancerState _close;

		private void Awake()
		{
			_state = States.Closed;

			_anim  = GetComponent<AnimancerComponent>();
			_open  = _anim.States.GetOrCreate(Anim_Open);
			_close = _anim.States.GetOrCreate(Anim_Close);

			_close.Play();
			_close.NormalizedTime = 1;
		}

		private void OnTriggerEnter(Collider other)
		{
			if(other.TryGetComponent(out PlayerActor p))
				Open();
		}

		private void OnTriggerExit(Collider other)
		{
			if(other.TryGetComponent(out PlayerActor p))
				Close();
		}

		private void Update()
		{
			if (_num <= 0) {
				if (_state == States.Open) {
					_state                = States.Closing;
					_anim.Play(_close);
					_close.NormalizedTime = 0;
				}/* else if(_state == States.Opening) {
					_close.NormalizedTime = _open.NormalizedTime;
					_anim.Play(_close);
				}*/
			} else {
				if (_state == States.Closed) {
					_state = States.Opening;
					_anim.Play(_open);
					_open.NormalizedTime = 0;
				}/* else if(_state == States.Closing) {
					_anim.Play(_open);
					_open.NormalizedTime = _close.NormalizedTime;
				}*/
			}

			switch (_state) {

				case States.Open:
					if (_num <= 0) {
					}
					break;

				case States.Closed:
					if (_num > 0) {
						//_open.Play();
					}
					break;

				case States.Closing:
					if (_close.NormalizedTime >= 1)
						_state = States.Closed;
					break;

				case States.Opening:

					if (_open.NormalizedTime >= 1)
						_state = States.Open;
					break;
			}
		}

		[Button]
		[UsedImplicitly]
		[HorizontalGroup]
		[HideInEditorMode]
		public void Open()
		{
			//if (_state == States.Open || _state == States.Opening) return;

			/*if (_num == 0) {
			}*/

			_num++;
		}

		[Button]
		[UsedImplicitly]
		[HorizontalGroup]
		[HideInEditorMode]
		public void Close()
		{
			//if (_state == States.Closed || _state == States.Closing) return;

			if (_num == 0) return;
			/*if (_num == 1)
			{
			}*/

			_num--;
		}

		public class DoorTriggerProxy : MonoLuaProxy<DoorTrigger> {
			public void open()	=> proxy.Open();
			public void close() => proxy.Close();
		}
	}
}