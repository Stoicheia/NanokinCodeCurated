using System;
using System.Collections.Generic;
using System.Linq;

public class StringDigester
{
	private readonly Stack<int> _peeks;
	public           int        index = -1;
	public           int        peek;

	public StringDigester(string content = null)
	{
		Content = content;
		_peeks  = new Stack<int>();
	}

	public bool   Done    => Content == null || index >= Content.Length - 1;
	public string Content { get; private set; }

	public int TotalPeeks
	{
		get { return _peeks.Sum(i => i); }
	}

	public char Char()
	{
		index++;
		return Content[index];
	}

	public char Peek()
	{
		if (!Done)
		{
			peek = 1;
			return Content[index + 1];
		}

		return (char)0;
	}

	public string Peek(int n)
	{
		int lo = index + TotalPeeks + 1;
		int hi = index + TotalPeeks + n;

		if (!Done && hi < Content.Length)
		{
			string ret = Content.Substring(lo, hi - lo + 1);
			peek = n;
			return ret;
		}

		return "";
	}

	public void Retain()
	{
		_peeks.Push(peek);
		peek = 0;
	}

	public void Release()
	{
		if (_peeks.Count > 0)
			peek = _peeks.Pop();
	}

	public void Skip()
	{
		index += TotalPeeks + peek;

		while (_peeks.Count > 0)
		{
			_peeks.Pop();
		}
	}

	public void Reset(string text)
	{
		peek    = 0;
		index   = -1;
		Content = text;
	}

	public string String()
	{
		char endCharacter = (char) 0;

		char first = Peek();
		if (first == '\'' || first == '\"')
		{
			endCharacter = first;
			Skip();
		}

		string ret = "";

		while (!Done && Peek() != endCharacter)
		{
			ret += Char();
		}

		return ret;
	}

	public string Word()
	{
		string ret = "";

		while (!Done && Peek() != ' ')
		{
			ret += Char();
		}

		SkipSeparator();
		return ret;
	}

	public void SkipSeparator()
	{
		if (Peek() == ' ')
			Skip();
	}

	public int Int(int defaultValue = -1)
	{
		string val = Word();
		if (string.IsNullOrEmpty(val)) return defaultValue;

		SkipSeparator();
		return int.Parse(val);
	}

	public List<T> List<T>()
	{
		string   word   = Word();
		string[] tokens = word.Split(',');

		if (typeof(T) == typeof(int))
		{
			return tokens.Select(int.Parse).Cast<T>().ToList();
		} else if (typeof(T) == typeof(string))
		{
			return tokens.Cast<T>().ToList();
		}

		throw new Exception("Unsupported list type!");
	}

	public static (bool success, string left, string right) FindOperands(string option, char s)
	{
		(bool success, string left, string right) ret = (false, null, null);

		if (option.IndexOf(s) != -1)
		{
			string[] strings = option.Split(s);
			ret.left    = strings[0];
			ret.right   = strings[1];
			ret.success = true;
		}


		return ret;
	}
}