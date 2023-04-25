namespace Combat
{
	public struct ArenaReference
	{
		public string address;
		public Arena  direct;

		public bool IsNull => direct == null && string.IsNullOrEmpty(address);

		public bool IsScene => direct == null && !string.IsNullOrEmpty(address);

		public ArenaReference(string address) : this()
		{
			this.address = address;
		}

		public ArenaReference(Arena direct) : this()
		{
			this.direct = direct;
		}

		public ArenaReference(Arena arena, SceneReference sceneref)
		{
			address = sceneref;
			direct  = arena;
		}

		public static implicit operator ArenaReference(Arena arena) => new ArenaReference(arena);

		public static implicit operator ArenaReference(string str) => new ArenaReference(str);

		public static implicit operator ArenaReference(SceneReference sceneref) => new ArenaReference(sceneref);
	}
}