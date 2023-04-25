using System.Collections.Generic;
using Anjin.Util;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// - Supports detecting children graphics along with the root.
/// - Implements callbacks for every event handler.
/// </summary>
public class SelectableRoot : Selectable
{
	public bool DetectChildren = true;

	private CanvasRenderer       _root;
	private List<CanvasRenderer> _children;

	public static Selectable[] AllSelectables => s_Selectables;

	protected override void Awake()
	{
		_children = new List<CanvasRenderer>();

		if (DetectChildren)
		{
			GetComponentsInChildren(true, _children);
		}

		if (!TryGetComponent(out _root))
		{
			_root = gameObject.AddComponent<CanvasRenderer>();
		}
		else
		{
			// GetComponentsInChildren includes the root, which we don't want to be in _children
			// Wouldn't break anything, it's just redundant
			_children.Remove(_root);
		}

		// Selectable is written such that we cannot query any of its properties
		// So we absolutely need a root graphic that will receive Selectable's
		// properties such that we can apply them to our children.
		if (!gameObject.HasComponent<Graphic>())
		{
			image               = gameObject.AddComponent<Image>();
			image.hideFlags     = HideFlags.NotEditable | HideFlags.DontSave;
			image.color         = Color.clear;
			image.raycastTarget = false;
		}

		base.Awake();
	}

	public void AddChild(CanvasRenderer child)
	{
		_children.Add(child);
	}

	protected virtual void Update()
	{
		if (_root == null) return;
		Color color = _root.GetColor();

		for (var i = 0; i < _children.Count; i++)
		{
			if (_children[i] == null)
			{
				_children.RemoveAt(i);
				continue;
			}
			_children[i].SetColor(color);
		}
	}
}