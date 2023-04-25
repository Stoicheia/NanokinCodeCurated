public readonly struct SpriteFlips
{
	public static readonly SpriteFlips None = new SpriteFlips(false, false);

	public readonly bool x, y;

	public SpriteFlips(bool x, bool y)
	{
		this.x = x;
		this.y = y;
	}

	public static SpriteFlips operator ^(SpriteFlips p1, SpriteFlips p2)
	{
		return new SpriteFlips(
			p1.x ^ p2.x,
			p1.x ^ p2.x
		);
	}
}