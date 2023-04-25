using System;
using System.Collections.Generic;
using Assets.Scripts;
using Pathfinding.Util;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		public static string GetNameWithPath(this GameObject go)
		{
			Stack<Transform> stack = new Stack<Transform>();

			Transform current = go.transform;

			while (current != null)
			{
				stack.Push(current);
				current = current.parent;
			}

			string ret = "GameObject(";
			while (stack.Count > 0)
			{
				Transform t = stack.Pop();
				ret += t.name;

				if (stack.Count > 0) ret += " > ";
			}

			ret += ")";

			return ret;
		}

		/// <summary>
		/// Sets localPosition to Vector3.zero, localRotation to Quaternion.identity, and localScale to Vector3.one
		/// </summary>
		public static Transform SetIdentity(this Transform t)
		{
			t.localPosition = Vector3.zero;
			t.localRotation = Quaternion.identity;
			t.localScale    = Vector3.one;
			return t;
		}

		/// <summary>
		/// Adds and returns a child gameObject to this gameObject with the specified name and HideFlags
		/// </summary>
		public static GameObject AddChild(this GameObject parent, string name, HideFlags flags = HideFlags.None)
		{
			GameObject relative = new GameObject(name);
			relative.hideFlags        = flags;
			relative.transform.parent = parent.transform;
			relative.transform.SetIdentity();
			return relative;
		}


		public static bool HasComponent<TComponent>(this GameObject gameObject)
			where TComponent : Component
		{
			return gameObject.GetComponent<TComponent>() != null;
		}

		public static bool HasComponent<TComponent>(this Component component)
			where TComponent : Component
		{
			return component.gameObject.HasComponent<TComponent>();
		}

		public static bool HasComponent<TComponent>(this Component owner, out TComponent component)
			where TComponent : Component
		{
			component = owner.gameObject.GetComponent<TComponent>();
			bool hasComponent = component != null;
			return hasComponent;
		}

		public static bool HasComponentInChildren<TComponent>(this GameObject gameObject)
			where TComponent : Component
		{
			return gameObject.GetComponentInChildren<TComponent>() != null;
		}

		public static bool HasComponentInChildren<TComponent>(this Component component)
			where TComponent : Component
		{
			return component.gameObject.HasComponentInChildren<TComponent>();
		}

		public static bool RemoveComponent<T>(this Component component)
			where T : Component
		{
			if (!component.HasComponent<T>())
				return false;

			Object.Destroy(component.GetComponent<T>());
			return true;
		}

		public static bool RemoveComponent<T>(this GameObject g)
			where T : Component
		{
			return g.transform.RemoveComponent<T>();
		}

		public static void SetLayerRecursively(this GameObject go, int layer)
		{
			if (go == null)
				return;

			foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
			{
				trans.gameObject.layer = layer;
			}
		}

		public static Transform Layer(this Transform go, int layer)
		{
			return go.gameObject.Layer(layer).transform;
		}

		public static GameObject Layer(this GameObject go, int layer)
		{
			go.layer = layer;
			return go;
		}

		public static void InstantiateOptional(this GameObject source, Vector3 position)
		{
			if (source == null)
				return;

			Object.Instantiate(source, position, Quaternion.identity);
		}

		public static T InstantiateNew<T>(this T source, Vector3 pos, Quaternion rot)
			where T : Object
		{
			return Object.Instantiate(source, pos, rot);
		}

		public static T InstantiateNew<T>(this T source, Vector3 pos)
			where T : Object
		{
			return Object.Instantiate(source, pos, Quaternion.identity);
		}

		public static T InstantiateNew<T>(this T source)
			where T : Object
		{
			return Object.Instantiate(source, Vector3.zero, Quaternion.identity);
		}

		public static T InstantiateNew<T>(this T source, Transform parent, bool worldPositionStays = false)
			where T : Object
		{
			return Object.Instantiate(source, parent, worldPositionStays);
		}

		public static GameObject Instantiate(this GameObject source, Transform parent = null)
		{
			GameObject ret = Object.Instantiate(source, Vector3.zero, Quaternion.identity, parent);
			return ret;
		}

		public static T Instantiate<T>(this GameObject source, Transform parent = null)
		{
			GameObject ret = Object.Instantiate(source, Vector3.zero, Quaternion.identity, parent);
			return ret.GetComponent<T>();
		}

		public static Transform ResetLocal(this Transform transform, bool with_rect_transform = false)
		{
			transform.localPosition    = Vector3.zero;
			transform.localEulerAngles = Vector3.zero;
			transform.localScale       = Vector3.one;

			if (with_rect_transform)
			{
				RectTransform rectTransform = transform.GetComponent<RectTransform>();
				rectTransform.sizeDelta        = Vector2.one;
				rectTransform.anchoredPosition = Vector2.zero;
			}

			return transform;
		}

		public static Transform SetActive(this Transform go, bool enabled = true)
		{
			SetActive(go.gameObject, enabled);
			return go;
		}

		public static GameObject SetActive(this GameObject go, bool enabled = true)
		{
			go.SetActive(enabled);
			return go;
		}

		public static void SetChildrenActive(this Transform go, bool enabled = true)
		{
			var children = go.GetComponentsInChildren<Transform>(true);

			for (var i = 0; i < children.Length; i++)
			{
				var child = children[i];
				if (child != go.transform)
				{
					child.gameObject.SetActive(enabled);
				}
			}
		}

		public static T AddComponent<T>(this Component comp)
			where T : Component
		{
			return comp.gameObject.AddComponent<T>();
		}

		public static T GetOrAddComponent<T>(this GameObject go)
			where T : Component
		{
			T result = go.GetComponent<T>();
			if (result != null)
				return result;

			return go.AddComponent<T>();
		}

		public static T GetOrAddComponent<T>(this Component c)
			where T : Component
		{
			return c.gameObject.GetOrAddComponent<T>();
		}

		public static void Destroy(this Object obj)
		{
			if (obj is Transform transform)
				obj = transform.gameObject;

#if UNITY_EDITOR
			if (Application.isPlaying)
			{
				if (EditorApplication.isPlayingOrWillChangePlaymode) // Else we are exiting playmode
					Object.Destroy(obj);
			}
			else
				Object.DestroyImmediate(obj);
#else
			Object.Destroy(obj);
#endif
		}

#if UNITY_EDITOR
		public static string GetAssetGUID(this Object obj)
		{
			return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
		}
#endif

		public static void SortChildrenByName(this Transform transform)
		{
			List<Transform> children = ListPool<Transform>.Claim();

			for (int i = 0; i < transform.childCount; i++)
			{
				Transform child = transform.GetChild(i);
				children.Add(child);
			}

			children.Sort((t1, t2) => string.Compare(t1.name, t2.name, StringComparison.Ordinal));

			for (var i = 0; i < children.Count; i++)
			{
				Transform child = children[i];
				child.SetSiblingIndex(i);
			}

			ListPool<Transform>.Release(children);
		}

		public static void SetActive(this IList<GameObject> objects, bool state)
		{
			for (int i = 0; i < objects.Count; i++)
			{
				GameObject gameObject = objects[i];
				gameObject.SetActive(state);
			}
		}

		public static void SetAutoDestroyPS(this GameObject go, string name = null)
		{
			ParticleAutoDestroyer destroyer = go.GetOrAddComponent<ParticleAutoDestroyer>();
			destroyer.destroyName = name;
		}


		public static C ParentTo<C, T>(this C comp, T target)
			where C: Component
			where T: Component
		{
			comp.transform.SetParent(target.transform);
			return comp;
		}


		public static C ParentTo<C>(this C comp, GameObject target) where C: Component => comp.ParentTo(target.transform);
		public static C ParentTo<C>(this C comp, Transform target) where C: Component {
			comp.gameObject.ParentTo(target.transform);
			return comp;
		}

		public static GameObject ParentTo<T>(this GameObject go, T target) where T : Component => go.gameObject.ParentTo(target.gameObject);

		public static GameObject ParentTo(this GameObject go, GameObject target) => go.ParentTo(target.transform);
		public static GameObject ParentTo(this GameObject go, Transform target) {
			go.transform.SetParent(target);
			return go;
		}
	}
}