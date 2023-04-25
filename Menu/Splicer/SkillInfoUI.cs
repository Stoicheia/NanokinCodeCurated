using System;
using Combat;
using Combat.UI;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

public class SkillInfoUI : MonoBehaviour
{
	[SerializeField] private GameObject    Root_CostSP;
	[SerializeField] private TargetUI_UGUI Targeting;

	[SerializeField] private TextMeshProUGUI TMP_Index;
	[SerializeField] private TextMeshProUGUI TMP_Name;
	[SerializeField] private TextMeshProUGUI TMP_Locked;
	[SerializeField] private TextMeshProUGUI TMP_CostSP;
	[SerializeField] private TextMeshProUGUI TMP_Description;

	[NonSerialized] public SkillAsset skill;
	[NonSerialized] public bool       unlocked;

	private bool  _awaken = false;
	private Color _nameColor;
	private Color _descriptionColor;

	private void Awake()
	{
		if (_awaken) return;
		_awaken           = true;

		_nameColor        = TMP_Name.color;
		_descriptionColor = TMP_Description.color;
	}

	public void ChangeSkill([CanBeNull] SkillAsset skill, bool locked, int mastery)
	{
		Awake();

		if (skill == null)
			locked = true; // The skill can't possibly be unlocked if it doesn't exist

		this.skill    = skill;
		this.unlocked = !locked;

		// Refresh UI
		// ----------------------------------------

		TMP_Index.gameObject.SetActive(!locked);
		TMP_Name.gameObject.SetActive(!locked);
		TMP_Locked.gameObject.SetActive(locked);
		Root_CostSP.SetActive(!locked);

		TMP_Name.color        = _nameColor;
		TMP_Description.color = _descriptionColor;

		if (Targeting != null)
		{
			Targeting.asset = !locked ? skill : null;
			Targeting.Refresh();
		}

		if (!locked)
		{
			TMP_Index.text       = $"{mastery.ToString()}.";

			SkillAsset.EvaluatedInfo einfo = skill.EvaluateInfo();

			TMP_Name.text = einfo.displayName;
			TMP_Description.text = einfo.description;

			if (einfo.scriptLoadError == null)
			{
				TMP_CostSP.text = einfo.spcost.ToString();
			}
			else
			{
				TMP_Name.color        = Color.red;
				TMP_Description.color = Color.red;
				TMP_Description.text  = einfo.scriptLoadError;
			}
		}
		else
		{
			TMP_Name.text        = "???";
			TMP_Description.text = "";
		}
	}
}