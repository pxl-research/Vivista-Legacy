using UnityEngine;
using UnityEngine.UI;

public class ChapterSelectorPanel : MonoBehaviour
{
	public RectTransform content;
	public GameObject scrollView;
	public GameObject chapterSelectorItemPrefab;

	private bool collapsed;

	private void Start()
	{
		var chapters = ChapterManager.Instance.chapters;

		if (chapters.Count == 0)
		{
			gameObject.SetActive(false);
		}

		for (int i = 0; i < chapters.Count; i++)
		{
			var chapter = chapters[i];
			var chapterItem = Instantiate(chapterSelectorItemPrefab, content, false).GetComponent<ChapterItem>();
			chapterItem.Init(chapter.name, chapter.name, chapter.time);
			chapterItem.GetComponent<Button>().onClick.AddListener(() => ChapterManager.Instance.GoToChapter(chapter));
		}

		ToggleCollapse();
	}

	public void ToggleCollapse()
	{
		collapsed = !collapsed;
		scrollView.SetActive(!collapsed);
	}
}
