using UnityEngine;
using UnityEngine.UI;

public class ChapterPanelSphere : MonoBehaviour
{
	public Text title;
	public Text chapterName;
	public Text chapterDescription;

	private Chapter chapter;
	private Player player;

	public void Init(string newTitle, int newChapterId, Player player)
	{
		this.player = player;
		chapter = ChapterManager.Instance.GetChapterById(newChapterId);

		title.text = newTitle;
		chapterName.text = chapter.name;
		chapterDescription.text = chapter.description;
	}

	public void OnGoToChapter()
	{
		ChapterManager.Instance.GoToChapter(chapter);
		player.DeactivateActiveInteractionPoint();
	}
}
