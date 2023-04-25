using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Data.Combat
{
	[Serializable]
	public class GrowingElements
	{
		[SerializeField, LabelText("Slash"), Inline(true), /*GrowingCurvedValue(StatConstants.MAX_EFFICIENCY),*/ LabelWidth(72f)]
		//public GrowingCurvedValue Slash = new GrowingCurvedValue();
		public int Slash = 0;
		[SerializeField, LabelText("Pierce"), Inline(true), /*GrowingCurvedValue(StatConstants.MAX_EFFICIENCY),*/ LabelWidth(72f)]
		//public GrowingCurvedValue Pierce = new GrowingCurvedValue();
		public int Pierce = 0;
		[SerializeField, LabelText("Blunt"), Inline(true), /*GrowingCurvedValue(StatConstants.MAX_EFFICIENCY),*/ LabelWidth(72f)]
		//public GrowingCurvedValue Blunt = new GrowingCurvedValue();
		public int Blunt = 0;

		[SerializeField, LabelText("Gaia"), Inline(true), /*GrowingCurvedValue(StatConstants.MAX_EFFICIENCY),*/ LabelWidth(72f), Space]
		//public GrowingCurvedValue Gaia = new GrowingCurvedValue();
		public int Gaia = 0;
		[SerializeField, LabelText("Oida"), Inline(true), /*GrowingCurvedValue(StatConstants.MAX_EFFICIENCY),*/ LabelWidth(72f)]
		//public GrowingCurvedValue Oida = new GrowingCurvedValue();
		public int Oida = 0;
		[SerializeField, LabelText("Astra"), Inline(true), /*GrowingCurvedValue(StatConstants.MAX_EFFICIENCY),*/ LabelWidth(72f)]
		//public GrowingCurvedValue Astra = new GrowingCurvedValue();
		public int Astra = 0;

		public void Randomise()
		{
			//Blunt.Randomise();
			//Slash.Randomise();
			//Pierce.Randomise();
			//Gaia.Randomise();
			//Oida.Randomise();
			//Astra.Randomise();
		}
	}
}