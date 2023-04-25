using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.OleDb;
using Anjin.UI;
using UnityEngine;
using Util.UniTween.Value;

[RequireComponent(typeof(HUDElement))]
public class ViewScaledHUDElement : MonoBehaviour
{
	private Camera _viewCamera;

	private HUDElement _hudElement;
	private Transform _scalingReference;

	public float MinScale;
	public float MaxScale;
	public float MinScaleDist;
	public float MaxScaleDist;

	public bool BindScaling;

	private TweenableVector3 _originalScale;

	private float _scale;
	private bool _hibernate;

	private void Awake()
	{
		_hudElement = GetComponent<HUDElement>();
		_originalScale = _hudElement.Scale;
		_hibernate = false;
	}

	private void Start()
	{
		_viewCamera = Camera.main;
	}

	private void Update()
	{
		if (_scalingReference != null)
		{
			if (_hibernate)
			{
				gameObject.SetActive(true);
				_hibernate = false;
			}
			ApplyScaling();
		}
		else if (!_hibernate)
		{
			gameObject.SetActive(false);
			_hibernate = true;
		}
	}

	public void SetTarget(Transform to)
	{
		_scalingReference = to;
	}

	private void ApplyScaling()
	{
		float diffVecMag = (_scalingReference.position - _viewCamera.transform.position).magnitude;
		_scale = !BindScaling
			? Mathf.LerpUnclamped(MinScale, MaxScale, Mathf.InverseLerp(MinScaleDist, MaxScaleDist, diffVecMag))  :
			Mathf.Lerp(MinScale, MaxScale, Mathf.InverseLerp(MinScaleDist, MaxScaleDist, diffVecMag));
		_hudElement.Scale = _scale * _originalScale;
	}
}
