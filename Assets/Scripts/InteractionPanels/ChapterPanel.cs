using UnityEngine;
using UnityEngine.UI;

public class ChapterPanel : MonoBehaviour
{
	public Text title;
	public Text chapterName;
	public Text chapterDescription;
	public Text chapterTime;

	public void Init(string newTitle, int newChapterId)
	{
		var chapter = ChapterManager.Instance.GetChapterById(newChapterId);

		title.text = newTitle;
		chapterName.text = chapter.name;
		chapterDescription.text = chapter.description;
		chapterTime.text = MathHelper.FormatSeconds(chapter.time);
	}
}
