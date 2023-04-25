using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Anjin.Util
{
	public static partial class Extensions
	{
		private static readonly Dictionary<string, ScriptableObject> FetchCache = new Dictionary<string, ScriptableObject>();

		/// <summary>
		/// Get the string slice between the two indexes.
		/// Inclusive for start index, exclusive for end index.
		/// </summary>
		public static string Slice(this string source, int start, int end)
		{
			if (end < 0) // Keep this for negative end support
				end = source.Length + end;

			int len = end - start;               // Calculate length
			return source.Substring(start, len); // Return Substring of length
		}

		public static string EscapeLuaString(this string s)
		{
			return s.Replace("\"", "\\\"");
		}


#if UNITY_EDITOR
		public static Texture ToEditorIcon(this string path)
		{
			return EditorGUIUtility.FindTexture(path) ?? AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}
#endif
		public static T FetchLocalAsset<T>(this string assetName, string basePath = "Configs/")
			where T : ScriptableObject
		{
			if (FetchCache.ContainsKey(assetName))
				return (T) FetchCache[assetName];

			string localDirPath = Path.Combine(Application.dataPath, $"Resources/{basePath}/_local");
			if (!Directory.Exists(localDirPath))
			{
				Directory.CreateDirectory(localDirPath);
			}

			return assetName.FetchResource<T>($"{basePath}_local/");
		}

		public static T FetchResource<T>(this string assetName, string basePath = "Configs/")
			where T : ScriptableObject
		{
			if (FetchCache.ContainsKey(assetName))
				return (T) FetchCache[assetName];

			ScriptableObject res = Resources.Load<ScriptableObject>($"{basePath}{assetName}");

#if UNITY_EDITOR
			if (res == null)
			{
				// Create the asset if it doesn't exist:

				T      asset = ScriptableObject.CreateInstance<T>();
				string path  = AssetDatabase.GenerateUniqueAssetPath($"Assets/Resources/{basePath}{assetName}.asset");

				AssetDatabase.CreateAsset(asset, path);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				UnityEditor.EditorUtility.FocusProjectWindow();

				Debug.Log($"Created missing ScriptableObject asset '{assetName}' at {path}");
				res = Resources.Load<ScriptableObject>($"{basePath}{assetName}");
			}
#endif

			return (T) (FetchCache[assetName] = res);
		}

		public static GUIContent ToGUIContent(this string s)
		{
			return new GUIContent(s);
		}

		// /// <summary>
		// /// Performs a simple char-by-char comparison to see if input ends with postfix
		// /// </summary>
		// /// <returns></returns>
		// public static bool IsPostfix(this string input, string postfix)
		// {
		// 	if (input == null)
		// 		throw new ArgumentNullException("input");
		//
		// 	if (postfix == null)
		// 		throw new ArgumentNullException("postfix");
		//
		// 	if (input.Length < postfix.Length)
		// 		return false;
		//
		// 	for (int i = input.Length - 1, j = postfix.Length - 1; j >= 0; i--, j--)
		// 		if (input[i] != postfix[j])
		// 			return false;
		// 	return true;
		// }
		//
		// /// <summary>
		// /// Performs a simple char-by-char comparison to see if input starts with prefix
		// /// </summary>
		// /// <returns></returns>
		// public static bool IsPrefix(this string input, string prefix)
		// {
		// 	if (input == null)
		// 		throw new ArgumentNullException("input");
		//
		// 	if (prefix == null)
		// 		throw new ArgumentNullException("prefix");
		//
		// 	if (input.Length < prefix.Length)
		// 		return false;
		//
		// 	for (int i = 0; i < prefix.Length; i++)
		// 		if (input[i] != prefix[i])
		// 			return false;
		// 	return true;
		// }
		//
		//       /// <summary>
		//       ///     Uppers the character specified by the passed index and returns the new string instance
		//       /// </summary>
		//       public static string ToUpperAt(this string input, int index)
		//       {
		//        return input.ReplaceAt(index, char.ToUpper(input[index]));
		//       }
		//
		//       /// <summary>
		// ///     Ex: "thisIsCamelCase" -> "This Is Camel Case"
		// /// </summary>
		// public static string SplitPascalCase(this string input)
		// {
		// 	return string.IsNullOrEmpty(input) ? input : SplitCamelCase(input).ToUpperAt(0);
		// }
		//
		//       public static string GetSuperNiceName(this Type type)
		// {
		// 	string name = type.Name;
		// 	string result;
		// 	if (name.IsPrefix("m_"))
		// 		result = name.Remove(0, 1);
		// 	else
		// 		result = name;
		//
		// 	result = result.ToTitleCase();
		//
		// 	if (name.Length > 1 && char.IsLower(result[0]) && char.IsUpper(result[1]))
		// 	{
		// 		char   lowerResultInitial = char.ToLower(result[0]);
		// 		string nicetype           = type.GetNiceName();
		//
		// 		// check for n as well, eg nSize
		// 		if (nicetype == "int" && lowerResultInitial == 'i' || lowerResultInitial == 'n'
		// 		                                                   || char.ToLower(nicetype[0]) == lowerResultInitial)
		// 			return result.Remove(0, 1).SplitPascalCase();
		// 	}
		//
		// 	return result.SplitPascalCase();
		// }

		/// <summary>
		/// Compresses a string and returns a deflate compressed, Base64 encoded string.
		/// </summary>
		/// <param name="uncompressedString">String to compress</param>
		public static string Compress(this string uncompressedString)
		{
			byte[] compressedBytes;

			using (MemoryStream uncompressedStream = new MemoryStream(Encoding.UTF8.GetBytes(uncompressedString)))
			{
				MemoryStream compressedStream = new MemoryStream();

				// setting the leaveOpen parameter to true to ensure that compressedStream will not be closed when compressorStream is disposed
				// this allows compressorStream to close and flush its buffers to compressedStream and guarantees that compressedStream.ToArray() can be called afterward
				// although MSDN documentation states that ToArray() can be called on a closed MemoryStream, this approach avoids relying on that very odd behavior should it ever change
				using (DeflateStream compressorStream = new DeflateStream(compressedStream, CompressionLevel.Fastest, true))
				{
					uncompressedStream.CopyTo(compressorStream);
				}

				// call compressedStream.ToArray() after the enclosing DeflateStream has closed and flushed its buffer to compressedStream
				compressedBytes = compressedStream.ToArray();
			}

			return Convert.ToBase64String(compressedBytes);
		}

		/// <summary>
		/// Decompresses a deflate compressed, Base64 encoded string and returns an uncompressed string.
		/// </summary>
		/// <param name="compressedString">String to decompress.</param>
		public static string Decompress(this string compressedString)
		{
			byte[] decompressedBytes;

			MemoryStream compressedStream = new MemoryStream(Convert.FromBase64String(compressedString));

			using (DeflateStream decompressorStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
			{
				using (MemoryStream decompressedStream = new MemoryStream())
				{
					decompressorStream.CopyTo(decompressedStream);

					decompressedBytes = decompressedStream.ToArray();
				}
			}

			return Encoding.UTF8.GetString(decompressedBytes);
		}

		/// <summary>
		///     Concatenates the specified elements of a string sequence, using the specified separator between each element.
		/// </summary>
		public static string JoinString<T>(this IEnumerable<T> sequence, string seperator = ",", Func<T, string> selector = null)
		{
			var enumerable = sequence.ToList();
			if (!enumerable.Any()) return "---";

			selector = selector ?? (t => t?.ToString());
			return string.Join(seperator, enumerable.Select(selector).ToArray());
		}

		/// <summary>
		///     Concatenates the specified elements of a string sequence, using the specified separator between each element.
		/// </summary>
		public static string JoinString(this IEnumerable<string> sequence, string seperator)
		{
			return string.Join(seperator, sequence.ToArray());
		}

		/// <summary>
		///     Concatenates the specified elements of a string array, using the specified separator between each element.
		/// </summary>
		public static string JoinString(this string[] sequence, string seperator)
		{
			return string.Join(seperator, sequence);
		}

		public static string Truncate(this string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value)) return value;
			return value.Length <= maxLength ? value : value.Substring(0, maxLength);
		}

		private static readonly Regex SNAKECASE_PATTERN = new Regex(@"[A-Z]{2,}(?=[A-Z][a-z]+[0-9]*|\b)|[A-Z]?[a-z]+[0-9]*|[A-Z]|[0-9]+");
		public static readonly  Regex REGEX_SYMBOLS     = new Regex(@"[!@,<>'\(\)\[\]]", RegexOptions.Compiled);
		public static readonly  Regex REGEX_LOWERDASH   = new Regex("[_ ]", RegexOptions.Compiled);

		public static string ToLowerdash(this string str)
		{
			str = REGEX_SYMBOLS.Replace(str, ""); // Remove symbols

			// Replace spaces and underscores with dashes
			return REGEX_LOWERDASH.Replace(str.ToLower(), "-");
		}

		public static string RemoveSymbols(this string str)
		{
			return REGEX_SYMBOLS.Replace(str, "");
		}

		public static string ToSnakeCase(this string str)
		{
			if (str == null) return "";
			MatchCollection     matches = SNAKECASE_PATTERN.Matches(str);
			IEnumerable<string> parts   = matches.OfType<Match>().Select(m => m.Value);

			return string.Join("_", parts).ToLower();
		}

		/// <summary>
		/// https://docs.unity3d.com/2021.1/Documentation/Manual/BestPracticeUnderstandingPerformanceInUnity5.html
		/// </summary>
		public static bool EndsWithFast(this string a, string b)
		{
			int ap = a.Length - 1;
			int bp = b.Length - 1;

			while (ap >= 0 && bp >= 0 && a[ap] == b[bp])
			{
				ap--;
				bp--;
			}

			return (bp < 0);
		}

		/// <summary>
		/// https://docs.unity3d.com/2021.1/Documentation/Manual/BestPracticeUnderstandingPerformanceInUnity5.html
		/// </summary>
		public static bool StartsWithFast(this string a, string b)
		{
			int aLen = a.Length;
			int bLen = b.Length;

			int ap = 0;
			int bp = 0;

			while (ap < aLen && bp < bLen && a[ap] == b[bp])
			{
				ap++;
				bp++;
			}

			return (bp == bLen);
		}

		public static int LeveinsteinDistance(this string first, string second)
		{
			if (first.Length == 0)
			{
				return second.Length;
			}

			if (second.Length == 0)
			{
				return first.Length;
			}

			var d = new int[first.Length + 1, second.Length + 1];
			for (var i = 0; i <= first.Length; i++)
			{
				d[i, 0] = i;
			}

			for (var j = 0; j <= second.Length; j++)
			{
				d[0, j] = j;
			}

			for (var i = 1; i <= first.Length; i++)
			{
				for (var j = 1; j <= second.Length; j++)
				{
					var cost = (second[j - 1] == first[i - 1]) ? 0 : 1;
					d[i, j] = Minimum(
						d[i - 1, j] + 1,
						d[i, j - 1] + 1,
						d[i - 1, j - 1] + cost
					);
				}
			}

			return d[first.Length, second.Length];
		}

		private static int Minimum(int e1, int e2, int e3) =>
			Math.Min(Math.Min(e1, e2), e3);

		public static int NumberOf(this string str, char c)
		{
			int n = 0;
			for (int i = 0; i < str.Length; i++) {
				if (str[i] == c) n++;
			}

			return n;
		}

		public static bool FirstOccurance(this string str, char c, out int index)
		{
			index = -1;

			for (int i = 0; i < str.Length; i++) {
				if (str[i] == c) {
					index = i;
					return true;
				}
			}

			return false;
		}

#if UNITY_EDITOR
		public static IEnumerable<T> SearchAssetDatabase<T>(this string filter)
			where T : Object
		{
			string[] guids = AssetDatabase.FindAssets(filter);
			return guids.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>);
		}

		public static IEnumerable<T> FindAssets<T>(this Type type)
			where T : Object
		{
			string[] guids = AssetDatabase.FindAssets($"t:{type}");
			return guids.Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<T>);
		}

		[CanBeNull]
		public static Process RunCLI(this string exeName, bool isConsoleEnabled, params string[] args)
		{
			DirectoryInfo projectRootDir = new DirectoryInfo(Application.dataPath).Parent;

			if (projectRootDir == null)
				return null;

			string projroot = projectRootDir.FullName;

			string  stitchTool = Path.Combine(projroot, exeName);
			Process process    = new Process();
			ProcessStartInfo psi = new ProcessStartInfo
			{
				FileName               = stitchTool,
				Arguments              = $"\"{string.Join(" ", args)}\"",
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
				UseShellExecute        = false,
				CreateNoWindow         = !isConsoleEnabled
			};

			process.StartInfo = psi;

			return process;
		}

#endif
	}
}