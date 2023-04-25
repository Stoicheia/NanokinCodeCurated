using System;

using System.Text;
using Random = System.Random;

public static class DataUtil
{
	public static string GetShortGUID()
	{
		var base64Guid = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

		// Replace URL unfriendly characters with better ones
		base64Guid = base64Guid.Replace('+', '-').Replace('/', '_');

		// Remove the trailing ==
		return base64Guid.Substring(0, base64Guid.Length - 2);
	}

	static char[] Chars = "abcdefghjklmnopqrstuvwxyzABCDEFGHIJKLMNPQRSTUVWXYZ123456789".ToCharArray();


	private static Random _rand = new Random();
	public static string MakeShortID(int length, Random random = null)
	{
		StringBuilder builder = new StringBuilder();

		if(random == null) random = _rand;

		int index = 0;
		for (int i = 0; i < length; i++)
		{
			index = random.Next(0, Chars.Length);
			builder.Append(Chars[index]);
		}

		return builder.ToString();
	}

}