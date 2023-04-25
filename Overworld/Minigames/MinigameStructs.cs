using System.Collections.Generic;
using Anjin.Nanokin;
using Anjin.Scripting;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;

namespace Anjin.Minigames
{
	public interface IMinigameSettings { }

	public interface IMinigameResults { }

	[LuaUserdata]
	public struct MinigameRankBracket
	{
		public int          amount;
		public MinigameRank rank;

		[LuaGlobalFunc]
		public static MinigameRankBracket minigame_rank_bracket(int amount, MinigameRank rank)
			=> new MinigameRankBracket {amount = amount, rank = rank};
	}

	[LuaUserdata(StaticAuto = true)]
	[LuaBox]
	public struct CollectionMinigameSettings : IMinigameSettings
	{
		public MinigameDifficulty difficulty;
		public float              time;
		public SpawnPoint         spawn;
		public GameObject         root;
		public string             tag;
		public PlayableDirector   intro_director;

		[TableList]
		public List<MinigameRankBracket> rank_brackets;

		public static CollectionMinigameSettings Default = new CollectionMinigameSettings
		{
			difficulty    = MinigameDifficulty.None,
			time          = 10,
			rank_brackets = null,
		};

		public static CollectionMinigameSettings New(Table vals)
		{
			var obj = Default;

			vals.TrySetUsing(nameof(difficulty), ref obj.difficulty);
			vals.TrySetUsing(nameof(time), ref obj.time);

			if (vals.TryGet("brackets", out Table brackets) && brackets.Length > 0)
			{
				obj.rank_brackets = new List<MinigameRankBracket>();
				foreach (var dynValue in brackets.Values)
				{
					if (dynValue.AsUserdata(out MinigameRankBracket bracket))
					{
						obj.rank_brackets.Add(bracket);
					}
				}
			}

			return obj;
		}
	}

	[LuaUserdata]
	public struct MinigameResults : IMinigameResults
	{
		public MinigameRank rank;
		public int          score;
		public float        time;
		public int          place;
		public bool         was_quit;

		public static MinigameResults Default = new MinigameResults
		{
			rank  = MinigameRank.None,
			score = 0,
			place = 0,
		};

		// Note: Set as a script to CLR conversion in LuaUtil.cs
		[LuaGlobalFunc("minigame_results")]
		public static object FromTable(DynValue v)
		{
			MinigameResults results = Default;

			v.Table.TryGet("rank",     out results.rank,  results.rank);
			v.Table.TryGet("score",    out results.score, results.score);
			v.Table.TryGet("time",     out results.time,  results.time);
			v.Table.TryGet("place",    out results.place, results.place);
			v.Table.TryGet("was_quit", out results.was_quit, results.was_quit);

			return results;
		}
	}
}