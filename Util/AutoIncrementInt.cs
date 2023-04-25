using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Util
{

	/// <summary>
	/// A utility class for an auto-incrementing ID.
	/// It increments whenever it is used implicitely in place of an integer (or explicitely with GetNext()).
	/// It allows us to keep some code clean by reducing ceremony code.
	/// It is similar to an autoinc field in database systems such as MySQL.
	/// It is serializable both by Unity and Json.Net.
	/// </summary>
	[Serializable, JsonObject]
	public class AutoIncrementInt
	{
		[SerializeField, JsonProperty] private int _nextId;

		public static AutoIncrementInt Zero => new AutoIncrementInt();

		public int GetNext()
		{
			return _nextId++;
		}

		public static implicit operator int(AutoIncrementInt id)
		{
			return id.GetNext();
		}
	}
}