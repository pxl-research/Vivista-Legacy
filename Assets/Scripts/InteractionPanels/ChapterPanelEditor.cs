using System;
using UnityEngine;
using UnityEngine.UI;

public class ChapterPanelEditor : MonoBehaviour
{
	public RectTransform chapterItemPrefab;

	public InputField title;
	public RectTransform chapterWrapper;
	public Text chapterName;
	public Text chapterDescription;
	public Text chapterTime;
	public Image chapterBackground;
	public RectTransform chapterSuggestionWrapper;
	public RectTransform chapterSuggestionRoot;

	public bool answered;
	public string answerTitle;
	public int answerChapterId;

	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	public void Init(string newTitle, int newChapterId = -1)
	{
		if (newChapterId != -1)
		{
			var chapter = ChapterManager.Instance.GetChapterById(newChapterId);
			FillChapterDetails(chapter);
		}

		answerChapterId = newChapterId;
		title.text = newTitle;

		title.onValueChanged.AddListener(_ => OnInputChange(title));
	}

	public void OnChapterPickerBegin()
	{
		OnChapterFilterUpdate("");
		chapterSuggestionRoot.gameObject.SetActive(true);
		chapterWrapper.gameObject.SetActive(false);
	}

	private void OnChapterFilterUpdate(string text)
	{
		foreach (Transform suggestion in chapterSuggestionWrapper)
		{
			Destroy(suggestion.gameObject);
		}

		var chapters = ChapterManager.Instance.Filter(text);

		for (int i = 0; i < chapters.Count; i++)
		{
			var item = Instantiate(chapterItemPrefab, chapterSuggestionWrapper).GetComponent<ChapterItem>();
			item.Init(chapters[i].name, chapters[i].description, chapters[i].time);
			int id = chapters[i].id;
			item.GetComponent<Button>().onClick.AddListener(() => OnChapterPickerEnd(id));
		}
	}

	public void OnChapterPickerEnd(int chapterId)
	{
		chapterSuggestionRoot.gameObject.SetActive(false);
		chapterWrapper.gameObject.SetActive(true);

		var chapter = ChapterManager.Instance.GetChapterById(chapterId);
		FillChapterDetails(chapter);
		chapterBackground.color = Color.white;
	}

	private void FillChapterDetails(Chapter chapter)
	{
		chapterName.text = chapter.name;
		chapterDescription.text = chapter.description;
		chapterTime.text = MathHelper.FormatSeconds(chapter.time);
		answerChapterId = chapter.id;
	}

	public void Answer()
	{
		bool errors = false;

		if (String.IsNullOrEmpty(title.text))
		{
			errors = true;
			title.image.color = errorColor;
		}

		if (answerChapterId == -1)
		{
			errors = true;
			chapterBackground.color = errorColor;
		}

		if (!errors)
		{
			answered = true;
			answerTitle = title.text;
		}
	}

	public void OnInputChange(InputField input)
	{
		input.image.color = Color.white;
	}
}
