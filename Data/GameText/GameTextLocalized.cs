using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Data
{
	[HideReferenceObjectPicker]
	public class
		GameTextLocalized //: ISerializationCallbackReceiver
	{
		/// <summary>
		/// The different languages that can be stored
		/// </summary>
		public enum Langauge
		{
			English = 0,
			Spanish = 1,
		}

		/// <summary>
		/// The default language for the container to use
		/// </summary>
		public const Langauge defaultLanguage = Langauge.English;

		/// <summary>
		/// The string that should be returned whenever an attempt is made to access a line in a language that the container does not hold.
		/// </summary>
		public const string MissingLineVersionError = "$MISSING_LINE_VERSION$";

		/// <summary>
		/// The unique ID for the line.
		/// </summary>
		public string id;

		/// <summary>
		/// The different localizations for the line, held in a simple dictionary.
		/// </summary>
		public Dictionary<Langauge, string> lineVersions;

		#if UNITY_EDITOR
		public bool destroyed;
		#endif

		public void Destroy()
		{
			#if UNITY_EDITOR
			destroyed = true;
			#endif
		}


		#region Serialization Vars

		[SerializeField]
		List<Langauge> versions_languages;

		[SerializeField]
		List<string> versions_lines;

		#endregion

		public GameTextLocalized() : this("") {}
		public GameTextLocalized(string defaultLine)
		{
			id = DataUtil.MakeShortID(6);
			lineVersions = new Dictionary<Langauge, string>();
			lineVersions[defaultLanguage] = defaultLine;

#if UNITY_EDITOR
			destroyed = false;
#endif
			//Debug.Log("constructor");
		}



		/// <summary>
		/// Get the line with the given language.
		/// </summary>
		/// <param name="language"></param>
		/// <returns>If the line in the language exists in the container, it returns it. If not, it returns the missing line error.</returns>
		public string GetLine(Langauge language)
		{
			if (!lineVersions.ContainsKey(language))
			{
				if (!lineVersions.ContainsKey(defaultLanguage))
				{
					return MissingLineVersionError + "(lang: " + language.ToString() + ")";
				}

				return lineVersions[defaultLanguage];
			}

			return lineVersions[language];
		}

		/// <summary>
		/// Set the line for the specified langauge.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="language"></param>
		public void SetLine(string line, Langauge language)
		{
			lineVersions[language] = line;
		}

		#region Unity Serialization

		public void OnBeforeSerialize()
		{
			versions_languages = new List<Langauge>();
			versions_lines = new List<string>();
			if (lineVersions != null)
			{
				var keys = lineVersions.Keys.ToList();
				foreach (Langauge l in keys)
				{
					versions_languages.Add(l);
					versions_lines.Add(lineVersions[l]);
				}
			}
		}

		public void OnAfterDeserialize()
		{
			for (int i = 0; i < versions_languages.Count; i++)
			{
				lineVersions[versions_languages[i]] = versions_lines[i];
			}
		}

		#endregion
	}
}