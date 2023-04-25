using UnityEngine;
using System.Collections.Generic;

public class ActorDatabase : ScriptableObject,ISerializationCallbackReceiver
{
    public List<ActorReferencePage> pages = new List<ActorReferencePage>();
    public ActorReferencePage currentPage, defaultPage;
	public int actorIdCounter;

	public void OnBeforeSerialize() { }

    public void OnAfterDeserialize()
    {
	    foreach (ActorReferencePage page in pages)
	    {
		    foreach (ActorReference aref in page.references)
		    {
			    aref.holdingPage = page;
			    aref.holdingDatabase = this;
		    }
	    }
//      Debug.Log("Serialize");
    }

    //Only works when created through the ScriptableObjectFactory class
    public void OnInit()
    {
        defaultPage = new ActorReferencePage("Default", ActorType.Actor);
        pages.Add(defaultPage);
        currentPage = defaultPage;
    }
    
    public void SetPageIndex(int i)
    {
        currentPage = pages[i];
    }

    public int GetPageIndex(ActorReferencePage page)
    {
        return pages.FindIndex(x => x == page);
    }

    public List<ActorReference> GetActorsInPage(int pageId)
    {
        return pages[pageId].references;
    }

    public ActorReference FindActor(ActorReferencePage page, string guid)
    {
        return null;
        //return FindPage().references.Find(x => x.guid == guid);
    }

    public ActorReferencePage AddPage(string name,ActorType defaultType,Color pageTextColor)
    {
        ActorReferencePage page = new ActorReferencePage(name, defaultType);
        page.pageTextColor = pageTextColor;
        pages.Add(page);
        return page;
    }

    public ActorReferencePage FindPage(string guid)
    {
        return pages.Find(x => x.guid == guid);
    }

    public bool PageExists(ActorReferencePage page)
    {
        return pages.Exists(x => x.guid == page.guid);
    }
}