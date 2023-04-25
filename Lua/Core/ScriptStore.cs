using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using Util.Odin.Attributes;
using Object = UnityEngine.Object;

namespace Combat.Scripting
{
	/// <summary>
	/// A serializable utility class (using Odin) to create inputs for a script type without instantiating the script type and serializing it.
	/// </summary>
	[Serializable]
	[DarkBox]
	public class ScriptStore
	{
		public List<Article> articles = new List<Article>();

		[HideReferenceObjectPicker]
		public class Article // TODO refactor to a struct for some nice free perf
		{
			public string identifier;
			public object value;

			public Article() { }

			public Article(string identifier)
			{
				this.identifier = identifier;
			}
		}


		private void EnsureArticles()
		{
			articles = articles ?? new List<Article>();
		}

		public Article Get(string identifier)
		{
			EnsureArticles();
			Article article = articles.FirstOrDefault(a => a != null && a.identifier == identifier);

			if (article == null)
			{
				article = new Article(identifier);
				articles.Add(article);
			}

			return article;
		}

		public object Get(string identifier, Type typeArticle)
		{
			EnsureArticles();
			Article article = articles.FirstOrDefault(a => a != null && a.identifier == identifier);

			if (article == null)
			{
				article = new Article(identifier);
				articles.Add(article);
			}

			if (article.value == null)
			{
				bool isInstantiable = typeArticle.GetConstructor(Type.EmptyTypes) != null || typeArticle.IsValueType;
				bool isUnityObject  = typeof(Object).IsAssignableFrom(typeArticle);

				if (isInstantiable && !isUnityObject)
					article.value = Activator.CreateInstance(typeArticle);
			}

			return article.value;
		}

		public void Set(string identifier, object obj)
		{
			EnsureArticles();
			Article article = articles.FirstOrDefault(a => a != null && a.identifier == identifier);

			if (article == null)
			{
				article = new Article();
				articles.Add(article);

				if (obj is string)
				{
					article.value = "";
				}
			}

			article.value = obj;
		}

		public void Import(ScriptStore other)
		{
			foreach (Article article in other.articles)
			{
				Set(article.identifier, article.value);
			}
		}

		/// <summary>
		/// Makes sure the store only contains properties for the supplied identifiers.
		/// Other properties are removed from the store.
		/// </summary>
		/// <param name="wantedIdentifiers">Identifiers to keep.</param>
		public void KeepOnly(IEnumerable<string> wantedIdentifiers)
		{
			EnsureArticles();
			articles.RemoveAll(article => !wantedIdentifiers.Contains(article.identifier));
		}

		/// <summary>
		/// Sets the values into an object by matching its fields to the identifiers.
		/// </summary>
		/// <param name="scriptInstance"></param>
		public void Apply(object scriptInstance)
		{
			EnsureArticles();
			IEnumerable<FieldInfo> fields = scriptInstance.GetType().GetFields().Where(f => f.GetAttribute<ArticleAttribute>() != null);

			foreach (FieldInfo fi in fields)
			{
				try
				{
					string  identifier = fi.Name;
					Article article    = Get(identifier);

					if (article.value != null && article.value.GetType().IsAssignableFrom(fi.FieldType))
						fi.SetValue(scriptInstance, article.value);
				}
				catch { }
			}
		}

		public void WriteToTable(Table tbl)
		{
			foreach (Article article in articles)
			{
				// CL: Reverted as the below change assumedly causes anything that's not a unity object to write as null.
				// tbl[$"{article.identifier}"] = article.value;

				if (article.value == null || article.value.GetType().IsSubclassOf(typeof(Object)) && article.value as Object == null)
					// Fixes a weird bug where article.value can point to a destroyed object ('null' unity object in UnityEditor)
					tbl.Set(article.identifier, DynValue.Nil);
				else if (article.GetType() != typeof(object))
					// That can happen some times
					tbl[$"{article.identifier}"] = article.value;
			}
		}

		public void UpdateType(string identifier, Type type)
		{
			Article article = Get(identifier);
			if (article.value != null && article.value.GetType() != type)
			{
				article.value = null;
			}
		}
	}
}