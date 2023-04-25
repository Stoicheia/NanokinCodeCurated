using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Nanopedia
{

	/// <summary>
	/// Handles the loading and saving of the nanopedia database.
	/// </summary>
	public class NanopediaDatabaseHandler
	{

		static NanopediaDatabase _database;

		public const string resourcesPath = "Resources\\Nanopedia\\";
		public const string fileName = "Nanopedia.dat";

		public static NanopediaDatabase Database
		{
			get
			{
				if(_database == null)
				{
					LoadDatabase();
				}
				return _database;
			}
		}

		public static void CreateDatabase()
		{
			_database = new NanopediaDatabase();

			NanopediaFolder f1 = new NanopediaFolder();
			f1.name = "Sub Folder 1";
			f1.Items.Add(new NanopediaPage("TestPage5", "", null));
			f1.Items.Add(new NanopediaPage("TestPage6", "", null));

			NanopediaFolder f2 = new NanopediaFolder();
			f2.name = "Sub Folder 2";
			f2.Items.Add(new NanopediaPage("TestPage7", "", null));
			f2.Items.Add(new NanopediaPage("TestPage8", "", null));
			f2.Items.Add(new NanopediaPage("TestPage9", "", null));

			Database.rootFolder.Items.Add(new NanopediaPage("TestPage","",null));
			Database.rootFolder.Items.Add(f1);
			Database.rootFolder.Items.Add(new NanopediaPage("TestPage2", "", null));
			Database.rootFolder.Items.Add(new NanopediaPage("TestPage3", "", null));
			Database.rootFolder.Items.Add(new NanopediaPage("TestPage4", "", null));
			Database.rootFolder.Items.Add(f2);

			Database.rootFolder.Items.Add(new NanopediaFolder() { name = "Sub Folder 3"});
			Database.rootFolder.Items.Add(new NanopediaFolder() { name = "Sub Folder 4" });
		}

		public static void LoadDatabase()
		{
			string path = GetDatabasePath();
			if(File.Exists(path))
			{
				string json = File.ReadAllText(GetDatabasePath());

				JsonSerializerSettings jss = new JsonSerializerSettings();
				jss.TypeNameHandling = TypeNameHandling.All;

				_database = JsonConvert.DeserializeObject<NanopediaDatabase>(json,jss);
				_database.OnLoad();

				Debug.Log("Successfully loaded Nanopedia database."); 
			}
			else
			{
				_database = null;
			}
		}

		public static void SaveDatabase() 
		{
			bool refresh = false;

			_database.lastModified = DateTime.UtcNow;

			JsonSerializerSettings jss = new JsonSerializerSettings();
			jss.TypeNameHandling = TypeNameHandling.All;

			string json = JsonConvert.SerializeObject(_database,Formatting.Indented,jss);

			/*if (!Directory.Exists(GetDatabaseDirectory()))
			{
				Directory.CreateDirectory(GetDatabaseDirectory());
				
			}*/

			if(!File.Exists(GetDatabasePath()))
			{
				refresh = true;
				Debug.Log("Refresh");
			}

			//StreamWriter sr = File.CreateText(GetDatabaseDirectory());
			StreamWriter sr = new StreamWriter(GetDatabasePath(),false);

			sr.Write(json);
			sr.Close();

			Debug.Log("Successfully saved Nanopedia to '"+GetDatabasePath()+"' At "+DateTime.UtcNow.ToShortDateString());

			//AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			AssetDatabase.ImportAsset("Assets\\"+resourcesPath+fileName, ImportAssetOptions.ForceUpdate);


			//File.WriteAllText(GetDatabaseDirectory(), json);
		}

		public static string GetDatabaseDirectory()
		{
			return Path.GetFullPath(Application.dataPath).Replace(@"/", @"\\")+@"\\"+resourcesPath;
		}

		public static string GetDatabasePath()
		{
			return GetDatabaseDirectory() + fileName;
		}

		public static void Test()
		{
			Debug.Log(GetDatabaseDirectory());
		}
	}

	public enum DataNodeType
	{
		Folder = 0,
		Page = 1,
		Database = 2
	}

	public interface INanopediaDataNode
	{
		DataNodeType Type { get; }
		string Name { get; }
		string JSONOnLastLoad { get; }
		bool Expandable { get; }
		bool Expand { get; set; }
		List<INanopediaDataNode> Items { get; }
		INanopediaDataNode Parent { get; set; }

		void OnLoad();
	}

	public class NanopediaDatabase : INanopediaDataNode
	{
		[NonSerialized]
		string _JSONOnLastLoad;

		public DateTime lastModified;

		public NanopediaFolder rootFolder;

		public bool expand;


		public NanopediaDatabase()
		{
			rootFolder = new NanopediaFolder();
			rootFolder.name = "Nanopedia";
			//expand = true;
		}

		public void OnLoad()
		{
			_JSONOnLastLoad = JsonConvert.SerializeObject(this, Formatting.Indented);
			rootFolder.Parent = this;
			rootFolder.OnLoad();
		}

		[JsonIgnore]
		public DataNodeType Type { get { return DataNodeType.Database; } }

		[JsonIgnore]
		public string Name { get { return NanopediaDatabaseHandler.fileName; } }

		[JsonIgnore]
		public string JSONOnLastLoad { get { return _JSONOnLastLoad; } }

		[JsonIgnore]
		public bool Expandable { get { return true; } }

		[JsonIgnore]
		public bool Expand { get { return expand; } set { expand = value; } }

		[JsonIgnore]
		public List<INanopediaDataNode> Items { get { return new List<INanopediaDataNode>() { rootFolder }; } }

		[JsonIgnore]
		public INanopediaDataNode Parent { get { return null; } set { } }
	}
	
	public class NanopediaFolder : INanopediaDataNode
	{
		[JsonIgnore]
		string _JSONOnLastLoad;

		public string name;
		public List<INanopediaDataNode> items;

		public bool expand;

		public NanopediaFolder()
		{
			//expand = true;
			items = new List<INanopediaDataNode>();
		}
		[JsonIgnore]
		public DataNodeType Type { get { return DataNodeType.Folder; } }
		[JsonIgnore]
		public string Name { get { return name; } }

		[JsonIgnore]
		public string JSONOnLastLoad { get { return _JSONOnLastLoad; } }

		[JsonIgnore]
		public bool Expandable { get { return true; } }

		[JsonIgnore]
		public bool Expand { get { return expand; } set { expand = value; } }

		[JsonIgnore]
		public List<INanopediaDataNode> Items { get { return items; } }

		[JsonIgnore]
		INanopediaDataNode parent;

		[JsonIgnore]
		public INanopediaDataNode Parent { get { return parent; } set { parent = value; } }

		public void OnLoad()
		{
			_JSONOnLastLoad = JsonConvert.SerializeObject(this,Formatting.Indented);
			foreach(var i in Items)
			{
				i.Parent = this;
				i.OnLoad();
			}
		}
	}

	public class NanopediaPage : INanopediaDataNode
	{
		[NonSerialized]
		string _JSONOnLastLoad;

		public string fileName;
		public string displayName;

		public string Text;
		public Sprite Icon;

		public NanopediaPage(string name,string text, Sprite icon)
		{
			fileName = name;
			displayName = name;
			Text = text;
			Icon = icon;

			OnLoad();
		}

		[JsonIgnore]
		public DataNodeType Type { get { return DataNodeType.Page; } }
		[JsonIgnore]
		public string Name { get { return fileName; } }

		public void OnLoad()
		{
			_JSONOnLastLoad = JsonConvert.SerializeObject(this, Formatting.Indented);
		}

		[JsonIgnore]
		public string JSONOnLastLoad { get { return _JSONOnLastLoad; } }

		[JsonIgnore]
		public bool Expandable { get { return false; } }

		[JsonIgnore]
		public bool Expand { get { return true; } set { } }

		[JsonIgnore]
		public List<INanopediaDataNode> Items { get { return null; } }

		[JsonIgnore]
		INanopediaDataNode parent;

		[JsonIgnore]
		public INanopediaDataNode Parent { get { return parent; } set { parent = value; } }
	}

}