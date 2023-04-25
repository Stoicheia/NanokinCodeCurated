using System;

public class HeaderBarAttribute : Attribute
{
	public string label;
	public bool   topSpacing;

	public HeaderBarAttribute(string label = null, bool topSpacing = false)
	{
		this.label      = label;
		this.topSpacing = topSpacing;
	}
}