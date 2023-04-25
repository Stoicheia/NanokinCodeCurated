using System;
using System.Collections.Generic;
using Overworld.Tags;
using Sirenix.Utilities;
using UnityEngine;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;

namespace Overworld.Interactables {
	public class RendererKeywordEnabler : MonoBehaviour, IActivable {

		public string Keyword = "";

		public List<Transform>  RenderersRoots;
		public List<Renderer>	Renderers = new List<Renderer>();

		private MaterialPropertyBlock _mpb;
		[ShowInPlay]
		private List<Renderer>        _renderers;

		private void Awake()
		{
			_mpb       = new MaterialPropertyBlock();
			_renderers = new List<Renderer>();
			_renderers.AddUniqueRange(Renderers);
			foreach (Transform root in RenderersRoots) {
				_renderers.AddUniqueRange(root.GetComponentsInChildren<Renderer>(true));
			}

		}

		private void OnEnable()  => Enable();
		private void OnDisable() => Disable();

		[ShowInPlay]
		public void Enable()
		{
			foreach (Renderer rend in _renderers) {
				rend.material.EnableKeyword(Keyword);
			}
		}

		[ShowInPlay]
		public void Disable()
		{
			foreach (Renderer rend in _renderers) {
				rend.material.DisableKeyword(Keyword);
			}
		}

		public void OnActivate()   => Enable();
		public void OnDeactivate() => Disable();
	}
}