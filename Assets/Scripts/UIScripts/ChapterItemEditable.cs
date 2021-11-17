using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class ChapterItemEditable : MonoBehaviour
{
	public Button deleteButton;
	public InputField nameLabel;
	public InputField descriptionLabel;
	public InputField timeLabel;
	public Text chapterIndex;

	public bool invalid;

	private Chapter chapter;

	private static Color defaultColor;
	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	public void Init(Chapter chapter)
	{
		defaultColor = descriptionLabel.image.color;

		this.chapter = chapter;
		nameLabel.text = chapter.name;
		descriptionLabel.text = chapter.description;
		timeLabel.text = MathHelper.FormatSeconds(chapter.time);
		chapterIndex.text = (transform.GetSiblingIndex() + 1).ToString();
	}

	public void OnTimeChange(string value)
	{
		value = Regex.Replace(value, "[^0-9:]", "");
		timeLabel.text = value;

		invalid = false;
		var groups = value.Split(':');
		var converted = new int[groups.Length];
		float time = 0;

		if (groups.Length < 2 || groups.Length > 3)
		{
			invalid = true;
			goto end;
		}

		for (int i = 0; i < groups.Length; i++)
		{
			if (groups[i].Length > 2 || groups[i].Length < 1)
			{
				invalid = true;
				goto end;
			}

			converted[i] = Int32.Parse(groups[i]);
		}

		//NOTE(Simon): Seconds
		if (converted[converted.Length - 1] > 60)
		{
			invalid = true;
			goto end;
		}

		time += converted[converted.Length - 1];

		//NOTE(Simon): Minutes
		if (converted[converted.Length - 2] > 59)
		{
			invalid = true;
			goto end;
		}

		time += converted[converted.Length - 2] * 60;

		//NOTE(Simon): Hours, if applicable
		if (groups.Length == 3)
		{
			if (converted[0] > 23)
			{
				invalid = true;
				goto end;
			}

			time += converted[0] * 60 * 60;
		}

end:
		timeLabel.image.color = invalid ? errorColor : defaultColor;
		if (!invalid)
		{
			chapter.time = time;
			ChapterManager.Instance.Refresh();
		}
	}

	public void OnNameChange(string value)
	{
		nameLabel.image.color = string.IsNullOrWhiteSpace(value) ? errorColor : defaultColor;
	}

	public void OnNameChangeEnd(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			value = value.Replace('\n', ' ');
			nameLabel.text = value;
			chapter.name = value;
		}
	}

	public void OnDescriptionChangeEnd(string value)
	{
		value = value.Replace('\n', ' ');
		descriptionLabel.text = value;
		chapter.description = value;
	}
}
