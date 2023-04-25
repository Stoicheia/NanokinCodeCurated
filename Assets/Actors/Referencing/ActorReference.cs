using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using WanzyeeStudio.Json;

[Serializable]
public class ActorReferencePage : IEquatable<ActorReferencePage>
{
	public const string               defaultID     = "_default";
	public       bool                 isDefaultPage = false;
	public       string               name;
	public       List<ActorReference> references;
	public       string               guid;
	public       ActorType            defaultType;

	[JsonConverter(typeof(ColorConverter))]
	public Color pageTextColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

	public ActorReferencePage(string name, ActorType defaultType)
	{
		guid             = Guid.NewGuid().ToString();
		this.defaultType = defaultType;
		this.name        = name;
		references       = new List<ActorReference>();
	}

	public ActorReference AddActor(ActorDatabase db)
	{
		ActorReference r = new ActorReference(db, db.currentPage, ++db.actorIdCounter, defaultType);
		references.Add(r);
		return r;
	}

	public bool Equals(ActorReferencePage page)
	{
		if ((object)page == null)
			return false;

		return guid == page.guid;
	}

	public static bool operator ==(ActorReferencePage n1, ActorReferencePage n2)
	{
		if ((object)n1 == null || (object)n2 == null)
		{
			if ((object) n1 == (object) n2)
				return true;
			else
				return false;
		}
		return n1.guid == n2.guid;
	}

	public static bool operator !=(ActorReferencePage n1, ActorReferencePage n2)
	{
		if ((object)n1 == null || (object)n2 == null)
		{
			if ((object) n1 != (object) n2)
				return true;
			else
				return false;
		}
		return n1.guid != n2.guid;
	}
}


public enum ActorType
{
	Actor,
	Trigger,
	Interactable,
	GameObject
}

[Serializable]
public class ActorReferencePointer
{
	public string database;
	public string page;
	public string reference;

	public bool init;

	[NonSerialized]
	public bool cached;
	[JsonIgnore][NonSerialized]
	public ActorReference cachedReference = null;

#if UNITY_EDITOR
	public ActorReferencePointer Init(ActorDatabase db, ActorReferencePage page, ActorReference reference)
	{
		database       = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(db));
		this.page      = page.guid;
		this.reference = reference.guid;
		cached         = false;
		init           = true;
		return this;
	}

	public void SetToNullRef()
	{
		reference = "";
	}

	public ActorReference GetReference()
	{
		if (init)
		{
			ActorDatabase db = AssetDatabase.LoadAssetAtPath<ActorDatabase>(AssetDatabase.GUIDToAssetPath(database));
			cachedReference = db.pages.FirstOrDefault(x => x.guid == page)?.references.FirstOrDefault<ActorReference>(x => x.guid == reference);
			cached          = true;
			return cachedReference;
		}
		else return null;
	}
#endif

	protected bool Equals(ActorReferencePointer other)
	{
		return string.Equals(database, other.database) && string.Equals(page, other.page) && string.Equals(reference, other.reference);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != GetType()) return false;
		return Equals((ActorReferencePointer) obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			int hashCode = (database != null ? database.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ (page != null ? page.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ (reference != null ? reference.GetHashCode() : 0);
			return hashCode;
		}
	}
}


[Serializable]
public class ActorReference : IEquatable<ActorReference>{

	static ActorDatabase database;

	public bool Equals(ActorReference reference)
	{
		if ((object)reference == null)
			return false;

		return guid == reference.guid;
	}

	public static bool operator ==(ActorReference n1, ActorReference n2)
	{
		if((object) n1 == null || (object) n2 == null)
		{
			if ((object) n1 == (object) n2)
				return true;
			else
				return false;
		}
		return n1.guid == n2.guid;
	}

	public static bool operator !=(ActorReference n1, ActorReference n2)
	{
		if ((object) n1 == null || (object) n2 == null)
		{
			if ((object) n1 != (object) n2)
				return true;
			else
				return false;
		}
		return n1.guid != n2.guid;
	}

	//Updated by the database on deserialization
	[NonSerialized]
	public ActorDatabase holdingDatabase;
	[NonSerialized]
	public ActorReferencePage holdingPage;


	public int    numberId;
	public string guid;
	public string name;

	public ActorType type;

	public ActorReference(ActorDatabase database, ActorReferencePage page, int id, ActorType type)
	{
		this.type       = type;
		holdingDatabase = database;
		holdingPage     = page;
		name            = $"Actor {id}";
		numberId        = id;
		guid            = Guid.NewGuid().ToString();
	}

#if UNITY_EDITOR
	public static bool DrawActorReferenceDisplay(ActorReferencePointer ptr, bool editable, bool selected, ref bool fieldsChanged)
	{
		if (ptr == null)
			return false;

		return DrawActorReferenceDisplay(ptr.GetReference(), (ptr.GetReference()!=null) ? ptr.GetReference().holdingPage:null, editable, selected, ref fieldsChanged);
	}

	public static bool DrawActorReferenceDisplay(ActorReference actor, ActorReferencePage page, bool editable, bool selected, ref bool fieldsChanged)
	{
		return DrawActorReferenceDisplay(null, page, actor, editable, false, selected, ref fieldsChanged);
	}

	public static bool DrawActorReferenceDisplay(ActorDatabase database, ActorReferencePage page, ActorReference actor, bool editable, bool selectable, bool selected, ref bool fieldsChanged)
	{
		bool clicked = false;
		if (actor != null)
		{
			if (selected)
				GUI.backgroundColor = Color.blue;
			GUILayout.BeginVertical(EditorStyles.helpBox, editable ? GUILayout.MinHeight(48) : GUILayout.MinHeight(0));
			GUI.backgroundColor = Color.white;
			// Row 1
			GUILayout.BeginHorizontal();
			GUILayout.Label(actor.numberId.ToString(), GUILayout.ExpandWidth(false), GUILayout.MinWidth(24));

			if (editable)
				GUILayout.Label("Name:", GUILayout.ExpandWidth(false));
			if (editable)
			{
				string s = GUILayout.TextField(actor.name);
				if (s != actor.name)
				{
					actor.name    = s;
					fieldsChanged = true;
				}
			}
			else
				GUILayout.Label(actor.name);

			if (selectable)
			{
				if (GUILayout.Button(selected ? "Unselect" : "Select", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
				{
					clicked = true;
				}
			}

			if (editable && database != null && GUILayout.Button("X", GUILayout.ExpandWidth(false)))
			{
				if (EditorUtility.DisplayDialog("Delete reference?", "Are you sure you'd like to delete this reference?", "Yes",
												"No"))
				{
					Undo.RecordObject(database, "delete '" + actor.name + "' from page '" + page.name + "'");
					page.references.Remove(actor);
					EditorUtility.SetDirty(database);
					AssetDatabase.SaveAssets();
				}
			}

			GUILayout.EndHorizontal();

			// Row 2
			GUILayout.BeginHorizontal();
			if (editable)
			{
				ActorType a = (ActorType) EditorGUILayout.EnumPopup(actor.type);
				if (a != actor.type)
				{
					actor.type    = a;
					fieldsChanged = true;
				}
			}
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();

			GUI.backgroundColor = Color.white;
		}

		return clicked;
	}
#endif

	public static ActorReferencePointer ReferenceToPointer(ActorReference aref)
	{
#if UNITY_EDITOR
		if (aref != null)
			return new ActorReferencePointer().Init(aref.holdingDatabase, aref.holdingPage, aref);
		else
#endif
		{
			return null;
		}
	}

}