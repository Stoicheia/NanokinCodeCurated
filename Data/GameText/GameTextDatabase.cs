using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Data;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;


[CreateAssetMenu(menuName = "Anjin/Game Content/GameTextDatabase")]
public class GameTextDatabase : SerializedScriptableObject
{
	public const string DBResourcesPath = "Data/GameText";

	[ShowInInspector]
	public static GameTextDatabase LoadedDB;

	[ListDrawerSettings(CustomAddFunction = "NewPage")]
	public List<GameTextDatabasePage> Pages;

	[SerializeField]
	int pageNum;

	[FormerlySerializedAs("testRef")] public GameText Test;

	public void NewPage(string name)
	{
		pageNum++;
		Pages.Add(new GameTextDatabasePage(name));
	}

	[Button]
	public static void LoadDB()
	{
		LoadedDB = Resources.Load<GameTextDatabase>(DBResourcesPath);
	}

	public static GameTextDatabasePage GetPage(string page)
	{
		if (LoadedDB == null)
			LoadDB();

		return LoadedDB.Pages.FirstOrDefault(x => x.id == page);
	}
}


public class GameTextDatabasePage
{
	public string id;
	public string name;

	[ListDrawerSettings(CustomRemoveIndexFunction = "_onRemoveLine"), ShowInInspector]
	public List<GameTextLocalized> Lines;

	public GameTextDatabasePage(string _name)
	{
		name  = _name;
		id    = DataUtil.MakeShortID(5);
		Lines = new List<GameTextLocalized>();
	}

	public GameTextLocalized GetLine(string id)
	{
		return Lines.FirstOrDefault(x => x.id == id);
	}

	#if UNITY_EDITOR
	public GameTextLocalized AddLine(string line = "")
	{
		var l = new GameTextLocalized(line);
		Undo.RecordObject(GameTextDatabase.LoadedDB, "Add Line To Page");
		Lines.Add(l);
		return l;
	}

	public void RemoveLine(int index)
	{
		Undo.RecordObject(GameTextDatabase.LoadedDB, "Remove line from page");
		Lines[index].Destroy();
		Lines.RemoveAt(index);
	}

	public void Destroy()
	{
		for (int i = 0; i < Lines.Count; i++)
		{
			RemoveLine(i);
		}
	}
	#endif
}

//TODO: Can this be made into a struct?
[Serializable]
public struct GameText
{
	[TextArea(2, 6)] public string Text;

	public                                  bool   DatabaseLinked;
	[FormerlySerializedAs("pageID")] public string db_pageID;
	[FormerlySerializedAs("lineID")] public string db_lineID;

	public GameText(string text)
	{
		Text = text;

		DatabaseLinked = false;
		db_pageID      = "";
		db_lineID      = "";
	}

	public static GameText Default = new GameText("");

	public void LinkToDB(string pageID, string lineID)
	{
		DatabaseLinked = true;
		db_lineID      = lineID;
		db_pageID      = pageID;
	}

	public string GetString()
	{
		if (Text == null && !DatabaseLinked) return "$NULL$";
		if (!DatabaseLinked) return Text;

		var page = GameTextDatabase.GetPage(db_pageID);
		if (page == null) return $"$PAGE_MISSING({db_pageID})$";

		var line = page.GetLine(db_lineID);
		if (line == null) return $"$LINE_VERSION_MISSING({db_lineID})$";

		return line.lineVersions[GameTextLocalized.defaultLanguage];
	}

	public static implicit operator string(GameText g) => g.Text;
	public static implicit operator GameText(string s) => new GameText(s);
}