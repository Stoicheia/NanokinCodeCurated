using System;
using System.Linq;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;


public class GrowingCurvedValueAttribute : Attribute
{
	public int MaxValue { get; }

	public GrowingCurvedValueAttribute(int maxValue)
	{
		MaxValue = maxValue;
	}
}


[Serializable]
public class GrowingCurvedValue
{
	public event Action OnValueChange;

	public float MinValue;
	public float MaxedValue;
	public float MaxCurveValue => Curve.keys.Select(x => x.value).Aggregate(Mathf.Max);
	public float MinCurveValue => Curve.keys.Select(x => x.value).Aggregate(Mathf.Min);

	[FormerlySerializedAs("finalMaxedValue"), HideLabel, HorizontalGroup("EntryRow")]
	public float FinalMaxedValue;

	[FormerlySerializedAs("curve"), HideLabel, OnValueChanged("OnChanged"), HorizontalGroup("EntryRow")]
	public AnimationCurve Curve = AnimationCurve.Linear(0, 0.05f, 1, 1);

#if UNITY_EDITOR
	[NonSerialized] public bool isEditorTableEnabled;
#endif


	public int Calculate(int level, int max, int maxLevel)
	{
		float t      = level / (float) maxLevel;

		float value = 0;
		if(Mathf.Abs(MaxCurveValue) > Mathf.Epsilon)
			value = Mathf.LerpUnclamped(MinValue, FinalMaxedValue * max, Curve.Evaluate((t - 1f/maxLevel) * (maxLevel)/(maxLevel-1)) / Curve.Evaluate(MaxCurveValue));
		int   ivalue = Mathf.CeilToInt(value - 0.01f);

		// int ivalue = level == maxLevel
		// ? value.Ceil()
		// : value.Floor();

		return ivalue;
	}

	public void RecordChanges()
	{
		OnValueChange?.Invoke();
	}

	public event Action<float> OnRandomise;
	public void Randomise()
	{
		FinalMaxedValue = UnityEngine.Random.Range(Curve.Evaluate(0), Curve.Evaluate(1));
		OnRandomise?.Invoke(FinalMaxedValue);
	}

#if UNITY_EDITOR
	[ShowInInspector,
	 HorizontalGroup("EntryRow"),
	 LabelText("Tbl")]
	private void ToggleTable()
	{
		isEditorTableEnabled = !isEditorTableEnabled;
	}

	public void TableButtonAction()
	{
		ToggleTable();
	}

	public void OnChanged()
	{
		// _table?.Update(this);
	}
#endif
}