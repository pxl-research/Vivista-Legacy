using UnityEngine;
using UnityEngine.UI;

public class ChapterItem : MonoBehaviour
{
	public Button deleteButton;
	public Text nameLabel;
	public Text descriptionLabel;
	public Text timeLabel;

	public void Init(string name, string description, float time)
	{
		nameLabel.text = name;
		descriptionLabel.text = description;
		timeLabel.text = MathHelper.FormatSeconds(time);
	}
}
