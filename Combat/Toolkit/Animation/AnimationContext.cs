using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Combat.Toolkit
{
	// public class AnimationContext
	// {
	// 	private List<object> _nodes;
	//
	// 	public AnimationContext([NotNull] params object[] nodes)
	// 	{
	// 		_nodes = new List<object>(nodes);
	// 	}
	//
	// 	public bool Has<TNode>()
	// 	{
	// 		return _nodes.Any(n => n is TNode);
	// 	}
	//
	// 	[CanBeNull]
	// 	public TNode Get<TNode>(bool optional = false)
	// 	{
	// 		// O(n) complexity but it hopefully won't matter much since n will remain small, usually...
	// 		foreach (object node in _nodes)
	// 		{
	// 			if (node is TNode)
	// 				return (TNode) node;
	// 		}
	//
	// 		if (optional)
	// 			return default;
	//
	// 		throw new Exception($"Couldn't find required AnimationContext node of type {typeof(TNode)}");
	// 	}
	//
	// 	private void Remove<TNode>()
	// 	{
	// 		_nodes.RemoveAll(n => n is TNode);
	// 	}
	//
	// 	public bool TryGet<TNode>([NotNull] out TNode node)
	// 		where TNode : class
	// 	{
	// 		node = Get<TNode>(true);
	// 		return node != null;
	// 	}
	//
	// 	public void Set<TNode>([CanBeNull] TNode node)
	// 		where TNode : class
	// 	{
	// 		if (node == null)
	// 			return;
	//
	// 		if (Has<TNode>())
	// 			Remove<TNode>();
	//
	// 		_nodes.Add(node);
	// 	}
	//
	// 	public void Set([NotNull] AnimationContext other)
	// 	{
	// 		foreach (object node in other._nodes)
	// 		{
	// 			_nodes.RemoveAll(n => n.GetType() == node.GetType());
	// 			_nodes.Add(node);
	// 		}
	// 	}
	//
	// 	public void Import([NotNull] AnimationContext other, bool skipExistingSameType = false)
	// 	{
	// 		if (skipExistingSameType)
	// 		{
	// 			foreach (object otherNode in other._nodes)
	// 			{
	// 				if (_nodes.All(n => n.GetType() != otherNode.GetType()))
	// 				{
	// 					_nodes.Add(otherNode);
	// 				}
	// 			}
	// 		}
	// 		else
	// 		{
	// 			_nodes.AddRange(other._nodes);
	// 		}
	// 	}
	//
	// 	public bool Require<T>([NotNull] out T node, string user) where T : class
	// 	{
	// 		bool has = TryGet(out node);
	// 		if (!has)
	// 		{
	// 			Debug.LogWarning($"Could not find {typeof(T).Name} node in animation context. (user: {user})");
	// 			return false;
	// 		}
	//
	// 		return true;
	// 	}
	// }
}