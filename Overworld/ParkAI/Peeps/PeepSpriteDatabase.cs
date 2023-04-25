using System.Collections.Generic;
using System.Linq;
using API.Spritesheet.Indexing;
using Overworld.Rendering;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Nanokin.ParkAI
{

	[CreateAssetMenu(menuName = "Anjin/ParkAI/Peep Database")]
	public class PeepSpriteDatabase : SerializedScriptableObject
	{
		/*
			Skins:
			w - white
			b - black
			a - asian
			h - hispanic

			Bodies:
			a - average
			r - round
			s - small child
			c - child

			m/f

			b (body)
			h (head)
			#

			Examples:
			- arf_b2
			- wsm_h1
			etc.
		 */


		[Button]
		public void AddEntries(List<IndexedSpritesheetAsset> sequences)
		{
			foreach (var sequence in sequences)
			{
				AddEntryFromSequence(sequence);
			}
		}

		[Button]
		public void AddEntryFromSequence(IndexedSpritesheetAsset sequence)
		{
			//Format: [Race]<Type><Gender>_<head:body>_<h:b>(variant)_c(color variant)

			PeepSpriteDatabaseEntry entry = new PeepSpriteDatabaseEntry();

			entry.sequencer = sequence;

			string name = sequence.spritesheet.Spritesheet.Texture.name.ToLower();

			int i = 0;

			if(name[2] != '_')
			{
				switch(name[i++])
				{
					case 'w':	entry.Race = PeepRace.White;		break;
					case 'a':	entry.Race = PeepRace.Asian;		break;
					case 'b':	entry.Race = PeepRace.Black;		break;
					case 'h':	entry.Race = PeepRace.Hispanic;	break;
					default:	entry.Race = PeepRace.Generic;		break;
				}
			}
			else
			{
				entry.Race = PeepRace.Generic;
			}

			switch(name[i++])
			{
				case 'a': entry.Type = PeepType.Adult;
					entry.BodyType   = PeepBodyType.Average; break;

				case 'r': entry.Type = PeepType.Adult;
					entry.BodyType   = PeepBodyType.Round; break;

				case 'c': entry.Type = PeepType.Child;
					entry.BodyType   = PeepBodyType.Average; break;

				case 's': entry.Type = PeepType.Child;
					entry.BodyType   = PeepBodyType.Small; break;
			}

			switch(name[i++])
			{
				case 'm': entry.Gender = PeepGender.Male; 	 break;
				case 'f': entry.Gender = PeepGender.Female; break;
			}

			i++;

			switch(name[i++])
			{
				case 'b': Bodies.Add(entry); break;
				case 'h': Heads.Add(entry);  break;
			}

		}

		[TableList, ListDrawerSettings(CustomAddFunction = "AddHead")]
		public List<PeepSpriteDatabaseEntry> Heads;

		[TableList, ListDrawerSettings(CustomAddFunction = "AddBody")]
		public List<PeepSpriteDatabaseEntry> Bodies;

		public void AddHead() => Heads.Add(new PeepSpriteDatabaseEntry());
		public void AddBody() => Bodies.Add(new PeepSpriteDatabaseEntry());

		public List<ColorReplacementProfile> SkinProfiles;
		public List<ColorReplacementProfile> HairProfiles;

		[TableList, ListDrawerSettings(CustomAddFunction = "AddHeadAccessory")]
		public List<Accessory> HeadAccessories;

		[TableList, ListDrawerSettings(CustomAddFunction = "AddBodyAccessory")]
		public List<Accessory> BodyAccessories;

		public void AddHeadAccessory() => HeadAccessories.Add(new Accessory());
		public void AddBodyAccessory() => BodyAccessories.Add(new Accessory());

		public bool ContainsSequencer(IndexedSpritesheetAsset sequencer) => Heads.Any(x => x.sequencer == sequencer) || Bodies.Any((x => x.sequencer == sequencer));
	}

	public class PeepSpriteDatabaseEntry
	{
		[ShowInInspector]
		public string Name => sequencer.spritesheet.Spritesheet.Name;

		public IndexedSpritesheetAsset sequencer;

		[TableColumnWidth(56, false)]
		public PeepType Type;

		[TableColumnWidth(64, false)]
		public PeepGender Gender;

		[TableColumnWidth(72, false)]
		public PeepBodyType BodyType;

		[TableColumnWidth(72, false)]
		public PeepRace Race;

		[TableColumnWidth(64, false)]
		public int Color;
	}

	public class Accessory {
		public string                  ID;
		public PeepAccessory           Type;
		public IndexedSpritesheetAsset Sequencer;
	}
}