using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vexe.Runtime.Extensions;
using static Core.Debug.DebugConsole;

namespace Core.Debug
{
	public class DebugExplorer : StaticBoy<DebugExplorer>
	{
		private int                 _lastSceneID = 0;
		private List<GameObject>    _gameObjects = null;
		private List<MonoBehaviour> _components  = null;

		private void Start()
		{
			AddCommand("scenes", Scenes);
			// AddCommand("list-scene ID", GetSceneObjects);
			AddCommand("list-components", GetSceneComponents);
			AddCommand("find-object NAME", FindObjects);
			AddCommand("find-components NAME", FindComponents);
		}

		private void Scenes(StringDigester digest, CommandIO io)
		{
			int n = SceneManager.sceneCount;
			for (int i = 0; i < n; i++)
			{
				Scene sceneAt = SceneManager.GetSceneAt(i);
				io.output.Add(sceneAt);
			}

			// Log($"{i.ToString().PadLeft(n, ' ')}: {sceneAt.name}");
			// Log(io.output);
		}

		protected override void OnAwake()
		{
			base.OnAwake();

			_gameObjects = new List<GameObject>();
			_components  = new List<MonoBehaviour>();
		}

		private void FindComponents(StringDigester sd, CommandIO io)
		{
			string searchToken = sd.String().ToLower();

			FindObjects(obj =>
			{
				obj.GetComponents(_components);

				return _components.Any(c => c.name.ToLower().Contains(searchToken));
			});

			Log(_gameObjects, res => res.name);
		}

		private void FindObjects(StringDigester sd, CommandIO io)
		{
			string searchToken    = sd.String().ToLower();
			int    selectionIndex = sd.Int();

			FindObjects(obj => obj.name.ToLower().Contains(searchToken));

			if (selectionIndex == -1)
			{
				// LIST THE RESULTS
				Log(_gameObjects, res => res.name);
				return;
			}

			if (!_gameObjects.IsIndexInBounds(selectionIndex))
			{
				Log(_gameObjects, res => res.name);
				throw new ArgumentException($"Selected object {selectionIndex} doesn't exist.");
			}

			GameObject result = _gameObjects[selectionIndex];
			result.GetComponents(_components);
			foreach (MonoBehaviour component in _components)
			{
				// PropertyTree tree = PropertyTree.Create(component);
				LogObject(component);
			}
		}

		private void GetSceneComponents(StringDigester sd, CommandIO io)
		{
			if (_lastSceneID < 0 || _lastSceneID >= SceneManager.sceneCount)
				throw new InvalidOperationException($"Bad scene ID `{_lastSceneID}`. Use list_scene to set the scene id.");

			Scene        scene = SceneManager.GetSceneAt(_lastSceneID);
			GameObject[] roots = scene.GetRootGameObjects();

			int index = sd.Int();
			if (index < -1)
				throw new CommandSyntaxException();
			if (index >= roots.Length)
				throw new ArgumentException("Scene is out of bounds.");

			GameObject root = roots[index];

			MonoBehaviour[] components = root.GetComponents<MonoBehaviour>();
			foreach (MonoBehaviour comp in components)
			{
				Log(comp.name);
			}
		}

		private void GetSceneObjects(StringDigester sd, CommandIO io)
		{
			int index = sd.Int();
			if (index < -1)
				throw new CommandSyntaxException();

			if (index >= SceneManager.sceneCount)
				throw new ArgumentException("Scene is out of bounds.");

			Scene scene = SceneManager.GetSceneAt(index);

			GameObject[] roots = scene.GetRootGameObjects();
			for (int i = 0; i < roots.Length; i++)
			{
				GameObject root = roots[i];
				Log($"{i} - {root.name}");
			}

			_lastSceneID = index;
		}

		private List<GameObject> FindObjects(Func<GameObject, bool> evaluator)
		{
			_gameObjects.Clear();

			void FindInChildren(Transform transform)
			{
				for (int i = 0; i < transform.childCount; i++)
				{
					Transform child = transform.GetChild(i);

					if (evaluator(child.gameObject))
						_gameObjects.Add(child.gameObject);

					FindInChildren(child);
					// if (child.gameObject.name.ToLower().Contains(searchToken))
					// _results.Add(child.gameObject);
				}
			}


			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene        scene = SceneManager.GetSceneAt(i);
				GameObject[] roots = scene.GetRootGameObjects();

				foreach (GameObject root in roots)
				{
					FindInChildren(root.transform);
				}
			}

			return _gameObjects;
		}
	}
}