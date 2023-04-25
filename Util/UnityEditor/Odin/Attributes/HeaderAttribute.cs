using System;

public class HeaderAttribute : Attribute
{
	public string name;
	public bool   topSpacing;

	public HeaderAttribute(string name, bool topSpacing = true)
	{
		this.name       = name;
		this.topSpacing = topSpacing;
	}
}