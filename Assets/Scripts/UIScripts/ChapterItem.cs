using UnityEngine;
using UnityEngine.UI;

public class ChapterItem : MonoBehaviour
{
	public Button deleteButton;
	public Text nameLabel;
	public Text descriptionLabel;
	public Text timeLabel;

	public void Init(Chapter chapter)
	{
		nameLabel.text = chapter.name;
		descriptionLabel.text = chapter.description;
		timeLabel.text = MathHelper.FormatSeconds(chapter.time);
	}
}
