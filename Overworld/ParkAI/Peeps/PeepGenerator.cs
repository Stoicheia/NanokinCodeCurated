using System;
using System.Collections.Generic;
using System.Linq;
using API.Spritesheet.Indexing;
using Vexe.Runtime.Extensions;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Anjin.Nanokin.ParkAI
{
	public struct PeepFilter {
		public PeepType?     Type;
		public PeepGender?   Gender;
		public PeepBodyType? BodyType;
		public PeepRace?     Race;

		public PeepAccessory? HeadAccessory;
		public PeepAccessory? BodyAccessory;
	}

	//TODO(C.L): SO MUCH LINQ. SO MANY ALLOCATIONS.
	// Note(C.L. 3/18/2022): This probably isn't as bad as when I wrote the above comment, simply because it only runs once at specific points (map load ect)
	public class PeepGenerator
	{
		//public static List<Peep.PeepType>
		public static PeepSpriteDatabase Database;
		static        Random             _rand;

		static async void Init()
		{
			_init = true;

			//TODO: Fix this

#if UNITY_EDITOR
			if (!EditorApplication.isPlaying)
				Database = AssetDatabase.LoadAssetAtPath<PeepSpriteDatabase>("Assets/Sprites/Overworld/Actors/NPCs/Peeps/Peep Database (Color Replacable).asset");
			else
				Database = GameAssets.Live.PeepSpriteDatabase;
#else
				Database = GameAssets.Live.PeepSpriteDatabase;
#endif

			_rand = new Random();

			Types     = Database.Heads.Concat(Database.Bodies).Where(x => x.sequencer != null).Select(x => x.Type).Distinct().ToArray();
			Genders   = Database.Heads.Concat(Database.Bodies).Where(x => x.sequencer != null).Select(x => x.Gender).Distinct().ToArray();
			BodyTypes = Database.Heads.Concat(Database.Bodies).Where(x => x.sequencer != null).Select(x => x.BodyType).Distinct().ToArray();
			Races     = Database.Heads.Concat(Database.Bodies).Where(x => x.sequencer != null).Select(x => x.Race).Distinct().ToArray();
		}

		static bool _init = false;

		public static PeepType[]     Types;
		public static PeepGender[]   Genders;
		public static PeepBodyType[] BodyTypes;
		public static PeepRace[]     Races;

		public static (IndexedSpritesheetAsset bodySheet, IndexedSpritesheetAsset headSheet, PeepDef peep) MakePeep(PeepFilter filter = default)
		{
			if (!_init || Database == null) Init();

			var fullList = Database.Heads.Concat(Database.Bodies).ToList();

			//Choose a gender
			//We know we have both genders
			PeepGender                    gender     = filter.Gender ?? Genders.RandomElement(_rand);
			List<PeepSpriteDatabaseEntry> genderList = fullList.FindAll(x => x.Gender == gender);

			PeepType                      type     = genderList.Select(x => filter.Type ?? x.Type).Distinct().RandomElement(_rand);
			List<PeepSpriteDatabaseEntry> typeList = genderList.FindAll(x => x.Type == type);

			PeepBodyType                  body       = typeList.Select(x => filter.BodyType ?? x.BodyType).Distinct().RandomElement(_rand);
			List<PeepSpriteDatabaseEntry> bodiesList = typeList.FindAll(x => x.BodyType == body);

			PeepRace                      race     = typeList.Select(x => filter.Race ?? x.Race).Distinct().RandomElement(_rand);
			List<PeepSpriteDatabaseEntry> matching = bodiesList.FindAll(x => x.Race == race);

			PeepDef p = new PeepDef
			{
				Type     = type,
				BodyType = body,
				Gender   = gender,
				Race     = race,
			};

			IndexedSpritesheetAsset bodySheet = null;
			IndexedSpritesheetAsset headSheet = null;

			var r = _rand.NextDouble();

			if (filter.HeadAccessory.HasValue)
				p.HeadAccessory = filter.HeadAccessory.Value;
			else if (p.BodyType != PeepBodyType.Small && r > 0.9f || (p.Type == PeepType.Child && r > 0.6f))
				p.HeadAccessory = PeepAccessory.Hat;

			switch (p.HeadAccessory)
			{
				case PeepAccessory.Hat:
					headSheet = Database.HeadAccessories.Where(x => x.Type == PeepAccessory.Hat && x.Sequencer != null).Select(x => x.Sequencer).RandomElement(_rand);
					break;
			}

			if (matching.Count > 0)
			{
				if (headSheet == null) headSheet = matching.Intersect(Database.Heads).RandomElement(_rand)?.sequencer;
				if (bodySheet == null) bodySheet = matching.Intersect(Database.Bodies).RandomElement(_rand)?.sequencer;
			}

			return (bodySheet, headSheet, p);
		}
	}
}