using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using Anjin.Scripting;
using UnityEngine;
using Assets.Drawing;
using Combat;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using Vexe.Runtime.Extensions;
using Combat.Toolkit;
using Anjin.Util;

namespace Anjin.Utils
{
	[RequireComponent(typeof(Animator))]
	public class TendrilMotion : MonoBehaviour
	{

		private enum NodeType
		{
			None = -1,
			GameObject,
			Vector3,
			Fighter,
			WorldPoint
		}

		[SerializeField] private Animator _animator;
		//[SerializeField] private ChainScaler _chainScaler;
		[SerializeField] private BezierTransform _bezier;
		[SerializeField] private bool _previewMode;

		private Vector3 _from;
		private Vector3 _to;

		private WorldPoint _fromWP;
		private WorldPoint _toWP;

		private GameObject _fromFollow;
		private GameObject _toFollow;

		private Fighter _fromFighter;
		private Fighter _toFighter;

		private Vector3 _fromOffset;
		private Vector3 _toOffset;

		private static readonly int Replay = Animator.StringToHash("replay");

		private NodeType _nodeType;

		private void Awake()
		{
			_bezier.Render = false;

			_nodeType = NodeType.None;

			if(_animator == null)
				_animator = GetComponent<Animator>();

			//if (_chainScaler == null)
			//	_chainScaler = GetComponent<ChainScaler>();
		}

		private void Update()
		{
			if (!_previewMode)
			{
				switch (_nodeType)
				{
					case NodeType.GameObject:
						_bezier.OriginAnchor = _fromFollow.transform.position + _fromOffset;
						_bezier.DestinationAnchor = _toFollow.transform.position + _toOffset;

						break;
					case NodeType.Vector3:
						_bezier.OriginAnchor = _from + _fromOffset;
						_bezier.DestinationAnchor = _to + _toOffset;

						break;
					case NodeType.Fighter:
						_bezier.OriginAnchor = _fromFighter.offset3(_fromOffset.x, _fromOffset.y, _fromOffset.z);
						_bezier.DestinationAnchor = _toFighter.offset3(_toOffset.x, _toOffset.y, _toOffset.z);

						break;
					case NodeType.WorldPoint:
						_bezier.OriginAnchor = _fromWP + _fromOffset;
						_bezier.DestinationAnchor = _toWP + _toOffset;

						break;
				}
			}

			if (_animator.IsInState(0, "Exit"))
			{
				OnTerminate();
			}
		}

		public void Configure([CanBeNull] Table tbl)
		{
			if (tbl.TryGet("from_offset", out Vector3 from_offset))
			{
				_fromOffset = from_offset;
			}

			if (tbl.TryGet("to_offset", out Vector3 to_offset))
			{
				_toOffset = to_offset;
			}

			if (tbl.TryGet("from", out WorldPoint from_wp) && tbl.TryGet("to", out WorldPoint to_wp))
			{
				_nodeType = NodeType.WorldPoint;

				_fromWP = from_wp;
				_toWP = to_wp;

				_toWP.position.y = _fromWP.position.y;

				_from = from_wp + _fromOffset;
				_to = to_wp.position + _toOffset;

				transform.position = Vector3.zero; //we must spawn tendrils at 0,0,0, renderer takes care of everything
			}
			else if (tbl.TryGet("from_pos", out Vector3 fromVec) && tbl.TryGet("to_pos", out Vector3 toVec))
			{
				_nodeType = NodeType.Vector3;

				_from = fromVec;
				_to = toVec;

				transform.position = Vector3.zero;
			}
			else if (tbl.TryGet("from_obj", out GameObject fromObj) && tbl.TryGet("to_obj", out GameObject toObj))
			{
				_nodeType = NodeType.GameObject;

				_fromFollow = fromObj;
				_toFollow = toObj;

				transform.position = Vector3.zero;
			}
			else if (tbl.TryGet("from_fter", out Fighter fromF) && tbl.TryGet("to_fter", out Fighter toF))
			{
				_nodeType = NodeType.Fighter;

				_fromFighter = fromF;
				_toFighter = toF;

				transform.position = Vector3.zero;
			}
		}

		public void Play()
		{
			if (_previewMode)
			{
				_from = Vector3.zero;
				_to = Vector3.forward;
			}

			_animator.Play("Motion");
			_animator.enabled = true;
			StartCoroutine(DisappearSequence(2));
		}

		public void Retract()
		{
			_animator.SetTrigger("retract");
		}

		public void Pause()
		{
			_animator.enabled = false;
		}

		public void Resume()
		{
			_animator.enabled = true;
		}

		public void Reset()
		{
			_animator.enabled = false;
		}

		private void OnTerminate()
		{
			if(!_previewMode)
				Destroy(gameObject);
		}

		private IEnumerator DisappearSequence(int frames)
		{
			for (int i = 0; i < frames; i++)
			{
				yield return null;
			}

			_bezier.Render = true;
		}

		[ShowIf("_previewMode")]
		[Button("Play")]
		private void ReplayAnimation()
		{
			Play();
		}

	}
}
