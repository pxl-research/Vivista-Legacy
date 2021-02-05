using UnityEngine;
using UnityEngine.UI;

public class ChapterItem : MonoBehaviour
{
	public Button deleteButton;
	public Text nameLabel;
	public Text descriptionLabel;
	public Text timeLabel;

	public Color normalColor;
	public Color highlightColor;

	public void Init(Chapter chapter)
	{
		nameLabel.text = chapter.name;
		descriptionLabel.text = chapter.description;
		timeLabel.text = MathHelper.FormatSeconds(chapter.time);
	}

	public void Highlight()
	{
		GetComponent<Image>().color = highlightColor;
	}

	public void UndoHighlight()
	{
		GetComponent<Image>().color = normalColor;
	}
}
