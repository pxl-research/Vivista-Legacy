using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChapterSelectorPanel : MonoBehaviour
{
	public RectTransform content;
	public GameObject scrollView;
	public GameObject chapterSelectorItemPrefab;
	private List<ChapterItem> chapterItems = new List<ChapterItem>();

	private int currentChapterIndex;
	private bool collapsed;
	private VideoController videoController;

	public void Init(VideoController videoController)
	{
		this.videoController = videoController;
	}

	private void Start()
	{
		var chapters = ChapterManager.Instance.chapters;
		currentChapterIndex = -1;

		if (chapters.Count == 0)
		{
			gameObject.SetActive(false);
		}

		for (int i = 0; i < chapters.Count; i++)
		{
			var chapter = chapters[i];
			var chapterItem = Instantiate(chapterSelectorItemPrefab, content, false).GetComponent<ChapterItem>();
			chapterItems.Add(chapterItem);
			chapterItem.Init(chapter);
			chapterItem.GetComponent<Button>().onClick.AddListener(() => ChapterManager.Instance.GoToChapter(chapter));
		}

		ToggleCollapse();
	}

	private void Update()
	{
		var chapter = ChapterManager.Instance.ChapterForTime(videoController.currentTime);
		if (chapter != null)
		{
			int index = ChapterManager.Instance.chapters.IndexOf(chapter);

			if (index != currentChapterIndex)
			{
				//NOTE(Simon): CurrentChapterIndex is initialized to -1, to indicate "no chapter". There is no chapter to undo the highlight for in this case
				if (currentChapterIndex >= 0)
				{
					chapterItems[currentChapterIndex].UndoHighlight();
				}

				chapterItems[index].Highlight();
				currentChapterIndex = index;
			}
		}
	}

	public void ToggleCollapse()
	{
		collapsed = !collapsed;
		scrollView.SetActive(!collapsed);
	}
}
