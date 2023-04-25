

/*public class RuntimeDatabase
{
	private static Dictionary<ActorReferencePointer, List<GameObject>> referenceAssociationMap = new Dictionary<ActorReferencePointer, List<GameObject>>();
	private static Dictionary<GameObject, ActorReferencePointer> gameObjectMap = new Dictionary<GameObject, ActorReferencePointer>();

	private static void ValidateList(ActorReferencePointer ptr)
	{
		if (!referenceAssociationMap.ContainsKey(ptr))
			referenceAssociationMap[ptr] = new List<GameObject>();
	}

	public static void Register(GameObject goActor, ActorReferencePointer ptr)
	{
		Assert.IsNotNull(ptr, "Attempted to register a null Actor Reference Pointer into the Actor Runtime Database.");

		ValidateList(ptr);
		referenceAssociationMap[ptr].Add(goActor);
		gameObjectMap[goActor] = ptr;
	}

	public static void Remove(GameObject goActor)
	{
		if (!gameObjectMap.ContainsKey(goActor))
		{
			Debug.LogError($"Attempted to dereference a non-referenced game object from the Actor Runtime Database. GameObject: {goActor}");
			return;
		}

		ActorReferencePointer actorRef = gameObjectMap[goActor];
		gameObjectMap.Remove(goActor);
		referenceAssociationMap[actorRef].Remove(goActor);
	}

	public static List<GameObject> Get(ActorReferencePointer ptr)
	{
		if (!referenceAssociationMap.ContainsKey(ptr))
		{
/*#if UNITY_EDITOR
            Debug.LogError("No actor reference of type `" + ptr.GetReference().name + "` found");
#endif#1#
            return new List<GameObject>();
		}

		return referenceAssociationMap[ptr];
	}

}*/
