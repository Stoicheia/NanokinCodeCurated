using System.Globalization;
using Anjin.Util;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

/// <summary>
/// Utility asset to easily implement comparison UI into other
/// UI panels.
/// </summary>
public class UIComparator : ScriptableObject
{
	public Color RegularTextColor;
	public Color EqualGainColor = Color.gray;
	public Color PositiveGainColor;
	public Color NegativeGainColor;

	public void UpdateNumber([NotNull] TMP_Text label, float actual)
	{
		label.text = ((int) actual).ToString(CultureInfo.InvariantCulture);
	}

	public void UpdatePercent([NotNull] TMP_Text label, float actual)
	{
		label.text = ((int) actual).ToString(CultureInfo.InvariantCulture);
	}

	public void UpdateNumber(bool enable, [NotNull] TMP_Text label, float actual, float comparator, bool diffcolored = true)
	{
		float display = enable ? comparator : actual;

		label.text  = ((int) display).ToString(CultureInfo.InvariantCulture);
		label.color = diffcolored ? GetValueColor(enable, actual, comparator) : RegularTextColor;
	}

	public void UpdatePercent(bool enable, [NotNull] TMP_Text label, float actual, float comparator, bool diffcolored = true)
	{
		float display = enable ? comparator : actual;

		//label.text  = $"{(int) (display * 100)}%";
		label.text = $"{(int)(display * 1)}%";
		label.color = diffcolored ? GetValueColor(enable, actual, comparator) : RegularTextColor;
	}

	public void UpdateBracket(bool enable, [NotNull] TMP_Text label, float actual, float comparator, bool diffcolored = true)
	{
		float display = enable ? comparator : actual;

		label.text = $"{System.Enum.GetName(typeof(Data.Combat.ResistanceBracket), Data.Combat.Elementf.GetBracket(display))}";
		label.color = diffcolored ? GetValueColor(enable, actual, comparator) : RegularTextColor;
	}

	public Color GetValueColor(bool enable, float actual, float comparator)
	{
		return enable ? GetDiffColor(actual, comparator) : RegularTextColor;
	}

	public void UpdateComparatorNumber(bool enable, [CanBeNull] TMP_Text label, float actual, float comparator, bool hideUnchanged = false)
	{
		if (label == null) return;

		bool unchanged = Mathf.Approximately(actual, comparator);
		if (!enable || hideUnchanged && unchanged)
		{
			label.text  = "";
			label.color = RegularTextColor;
		}
		else
		{
			label.text  = $"{(int) comparator}";
			label.color = GetDiffColor(actual, comparator);
		}
	}

	public void UpdateComparatorPercent(bool enable, [CanBeNull] TMP_Text label, float actual, float comparator, bool hideUnchanged = false)
	{
		if (label == null) return;

		bool unchanged = Mathf.Approximately(actual, comparator);
		if (!enable || hideUnchanged && unchanged)
		{
			label.text  = "";
			label.color = RegularTextColor;
		}
		else
		{
			//label.text  = $"{(int) (comparator * 100)}%";
			label.text = $"{(int)(comparator * 1)}%";
			label.color = GetDiffColor(actual, comparator);
		}
	}

	public void UpdateComparatorBracket(bool enable, [CanBeNull] TMP_Text label, float actual, float comparator, bool hideUnchanged = false)
	{
		if (label == null) return;

		bool unchanged = Mathf.Approximately(actual, comparator);
		if (!enable || hideUnchanged && unchanged)
		{
			label.text = "";
			label.color = RegularTextColor;
		}
		else
		{
			//label.text  = $"{(int) (comparator * 100)}%";
			label.text = $"{System.Enum.GetName(typeof(Data.Combat.ResistanceBracket), Data.Combat.Elementf.GetBracket(comparator))}";
			label.color = GetDiffColor(actual, comparator);
		}
	}


	/// <summary>
	/// Update a label to show a comparison between two floats as a difference.
	/// For example:
	///
	/// actual: 33
	/// comparator: 50
	/// ---------------
	/// text: +13
	/// color: positive gain
	///
	/// </summary>
	public void UpdateDiffNumber([CanBeNull] TMP_Text label, float actual, float comparator)
	{
		if (label == null) return;
		label.text  = GetDiffString(actual, comparator);
		label.color = GetDiffColor(actual, comparator);
	}

	/// <summary>
	/// Update a label to show a comparison between two floats as a difference.
	/// For example:
	///
	/// actual: 0.2
	/// comparator: 0.42
	/// ---------------
	/// text: +22%
	/// color: positive gain
	///
	/// </summary>
	public void UpdateDiffPercent([CanBeNull] TMP_Text label, float actual, float comparator)
	{
		if (label == null) return;
		label.text = (int) comparator - (int) actual == 0
			? ""
			: $"{GetDiffString(actual, comparator)}%";

		label.color = GetDiffColor(actual, comparator);
	}

	public static string GetDiffString(float actual, float comparator)
	{
		var diff = (int) (comparator - actual);
		return diff == 0
			? ""
			: $"{(diff < 0 ? '-' : '+')}{diff.Abs()}";
	}

	public Color GetDiffColor(float actual, float comparator)
	{
		float diff = (int) comparator - actual;

		if (diff > 0) return PositiveGainColor;
		else if (diff == 0) return EqualGainColor;
		else if (diff < 0) return NegativeGainColor;

		return RegularTextColor;
	}
}