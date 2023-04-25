using System;
using System.Collections;
using System.Collections.Generic;
using Anjin.Scripting;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;
using Vexe.Runtime.Extensions;

namespace Overworld.Tags
{
	public static class TagController
	{
		public static List<Tag>                     all;
		public static Dictionary<string, List<Tag>> allByName;

		public static void Register(Tag tag)
		{
			all.AddIfNotExists(tag);

			for (int i = 0; i < tag.Tags.Count; i++)
			{
				string name = tag.Tags[i];

				if (!allByName.TryGetValue(name, out List<Tag> set))
					allByName[name] = set = new List<Tag>();

				set.AddIfNotExists(tag);
			}
		}

		public static void Deregister(Tag tag)
		{
			all.Remove(tag);

			for (int i = 0; i < tag.Tags.Count; i++)
			{
				if (allByName.ContainsKey(tag.Tags[i]))
				{
					List<Tag> set = allByName[tag.Tags[i]];
					set.Remove(tag);
				}
			}
		}

		[LuaGlobalFunc("tags_activate_all")]
		public static void ActivateAll(string tag_name)
		{
			if (!AnyWithTag(tag_name, out List<Tag> objs))
				return;

			for (var i = 0; i < objs.Count; i++)
			{
				Tag tag = objs[i];
				if (tag.state) continue; // Already active

				tag.SetState(true);
			}
		}

		[LuaGlobalFunc("tags_deactivate_all")]
		public static void DeactivateAll(string tag_name)
		{
			if (!AnyWithTag(tag_name, out var objs))
				return;

			for (int i = 0; i < objs.Count; i++)
			{
				Tag tag = objs[i];
				//if (!tag.state) continue; // Already inactive

				tag.SetState(false);
			}
		}

		[LuaGlobalFunc("add_tag")]
		public static void AddTagTo(GameObject obj, string tag)
		{
			Tag tags = EnsureTaggable(obj, out bool hadTags);
			tags.AddTag(tag);
		}

		[LuaGlobalFunc("remove_tag")]
		public static void RemoveTagFrom(GameObject obj, string tag)
		{
			Tag tags = EnsureTaggable(obj, out bool hadTags);
			tags.RemoveTag(tag);
		}

		[LuaGlobalFunc("find_with_tag")]
		public static object FindWithTag(string tag, DynValue _type = null)
		{
			if (allByName.ContainsKey(tag)) {
				var list = allByName[tag];
				if (list.Count <= 0) return null;

				if(_type == null || !_type.AsUserdata(out Type type)) {
					return list[0];
				} else {
					var comp = list[0].GetComponent(type);
					return comp;
				}
			}

			return null;
		}

		[LuaGlobalFunc("find_all_with_tag")]
		public static object FindAllWithTag(string tag, DynValue _type = null)
		{
			if (allByName.ContainsKey(tag)) {
				if(_type == null || !_type.AsUserdata(out Type type))
					return allByName[tag];
				else {
					List<Tag> source = allByName[tag];

					Type  listType = typeof(List<>).MakeGenericType(type);
					IList res      = (IList)Activator.CreateInstance(listType);

					foreach (Tag item in source) {
						var comp = item.GetComponent(type);
						if(comp != null)
							res.Add(comp);
					}

					return res;
				}
			}

			return new List<Tag>();
		}

		public static Tag EnsureTaggable([NotNull] GameObject obj, out bool hadTags)
		{
			if (obj.TryGetComponent(out Tag tag))
			{
				hadTags = true;
				return tag;
			}

			hadTags = false;
			return obj.AddComponent<Tag>();
		}

		public static bool AnyWithTag([CanBeNull] string tag, out List<Tag> objs)
		{
			objs = null;
			if (tag == null)
			{
				Debug.LogError("Tried to find a null tag.");
				return false;
			}

			if (allByName.ContainsKey(tag))
			{
				objs = allByName[tag];
				return true;
			}

			return false;
		}
	}
}