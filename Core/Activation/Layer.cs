using System;
using System.Text;
using Anjin.Nanokin;
using Overworld.Controllers;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;
#if UNITY_EDITOR
using UnityEditor;
using Sirenix.Utilities.Editor;

#endif

namespace Anjin.Core.Flags.Components
{
	[Flags]
	public enum ActivationOptions
	{
		None                  = 0,
		WaitUntilCutsceneDone = 1,
		ActiveForParentCutscene = 2,
	}

	/// <summary>
	/// Renamed from Activator
	/// Specifies which layer a GameObject is part of. (is not a layer in itself)
	/// </summary>
	public class Layer : SerializedMonoBehaviour
	{
		// public bool DefaultState = true;

		[FormerlySerializedAs("options")]
		public ActivationOptions Options = ActivationOptions.None;

		public bool InheritPath = false;

		[DisableInPlayMode]
		[Optional]
		[FormerlySerializedAs("GroupIDOverride")]
		[SerializeField]
		private string ID;

		[NonSerialized, ShowInPlay]
		public bool waitingCutscenes;

		/// <summary>
		/// State of this object in the layer.
		/// </summary>
		[ShowInPlay]
		[NonSerialized]
		public bool state;

		private bool WaitUntilCutsceneDone => (Options & ActivationOptions.WaitUntilCutsceneDone) == ActivationOptions.WaitUntilCutsceneDone;

		private Layer _parent;
		private bool  _hasParent;

		[ShowInPlay] private Cutscene	_parentCutscene;
		[ShowInPlay] private bool		_hasParentCutscene;

		private void Awake()
		{
			UpdateParent();

			Enabler.Register(gameObject);
			LayerController.Register(this);

			Transform parent = transform.parent;
			while (parent != null) {
				if (parent.TryGetComponent(out Cutscene cut)) {
					_parentCutscene    = cut;
					_hasParentCutscene = true;
					break;
				}
				parent = parent.parent;
			}
		}

		private void OnDestroy()
		{
			LayerController.Deregister(this);
			Enabler.Deregister(gameObject);
		}

		/// <summary>
		/// Apply the changed state.
		/// If it hasn't changed, nothing happens.
		/// </summary>
		/// <param name="state"></param>
		/// <param name="forceImmediate"></param>
		public void Apply(bool state, bool forceImmediate = false, bool forceApply = false)
		{
			if (!forceApply && state == this.state) return; // No change, nothing to apply.

			if (!state && (Options & ActivationOptions.ActiveForParentCutscene) == ActivationOptions.ActiveForParentCutscene && _hasParentCutscene && _parentCutscene.playing) {
				state = true;
			}

			// We might wanna make this feature a part of the Enabler?
			if (WaitUntilCutsceneDone && GameController.Live.StateGame == GameController.GameState.Cutscene && !forceImmediate)
			{
				if (!waitingCutscenes)
				{
					waitingCutscenes = true;
					LayerController.waitingObjects.Add(this);
				}

				return;
			}

			this.state = state;
			Enabler.Set(gameObject, state, SystemID.Layer);
		}

		public void ForceApply(bool state)
		{
			this.state = state;
			Enabler.Set(gameObject, state, SystemID.Layer);
		}

		/// <summary>
		/// Get the final layer ID to use.
		/// </summary>
		/// <returns></returns>
		public string GetID()
		{
			_sb.Clear();
			if (InheritPath && _hasParent) {
				_sb.Append(_parent.GetID());
				_sb.Append("/");
			}

			if (!string.IsNullOrEmpty(ID)) {
				_sb.Append(ID);
			} else {
				_sb.Append(gameObject.name);
			}

			return _sb.ToString();
		}

		private void UpdateParent()
		{
			_parent    = null;
			if(transform.parent != null)
				_parent    = transform.parent.GetComponentInParent<Layer>();
			_hasParent = _parent != null;
		}

		private StringBuilder _sb = new StringBuilder();

#if UNITY_EDITOR
		[OnInspectorGUI]
		public void OnInspectorGUI()
		{
			UpdateParent();
			string id = GetID();
			SirenixEditorGUI.BeginBox();
			GUILayout.Label($"Group ID: {id}", EditorStyles.boldLabel);
			if (InheritPath) {
				GUILayout.Label($"Parent: {(_hasParent ? _parent.name : "(none)")}", EditorStyles.boldLabel);
			}
			SirenixEditorGUI.EndBox();

			if (!EditorApplication.isPlaying && !gameObject.activeInHierarchy)
				SirenixEditorGUI.WarningMessageBox("This gameobject is not active in the hierarchy and will not respond to any activation or deactivation events.");
		}
#endif
	}
}