using UnityEngine;
using UnityEngine.UI;

public enum InteractionType
{
	None,
	Text,
	Image,
	Video,
	MultipleChoice,
	Audio,
	FindArea,
	MultipleChoiceArea,
	MultipleChoiceImage,
	TabularData,
	Chapter
}

public class InteractionTypePicker : MonoBehaviour
{
	public bool answered = false;
	public InteractionType answer;

	public GameObject noChaptersWarning;
	public Text chaptersText;
	public Button chaptersButton;

	public void OnEnable()
	{
		StartCoroutine(UIAnimation.FadeIn(GetComponent<RectTransform>(), GetComponent<CanvasGroup>()));

		bool hasAnyChapters = ChapterManager.Instance.chapters.Count > 0;
		noChaptersWarning.SetActive(!hasAnyChapters);
		chaptersButton.interactable = hasAnyChapters;
		chaptersText.alignment = hasAnyChapters ? TextAnchor.MiddleLeft : TextAnchor.UpperLeft;
	}

	public void AnswerImage()
	{
		answered = true;
		answer = InteractionType.Image;
	}

	public void AnswerText()
	{
		answered = true;
		answer = InteractionType.Text;
	}

	public void AnswerVideo()
	{
		answered = true;
		answer = InteractionType.Video;
	}

	public void AnswerMultipleChoice()
	{
		answered = true;
		answer = InteractionType.MultipleChoice;
	}

	public void AnswerAudio()
	{
		answered = true;
		answer = InteractionType.Audio;
	}

	public void AnswerFindArea()
	{
		answered = true;
		answer = InteractionType.FindArea;
	}

	public void AnswerMultipleChoiceArea()
	{
		answered = true;
		answer = InteractionType.MultipleChoiceArea;
	}

	public void AnswerMultipleChoiceImage()
	{
		answered = true;
		answer = InteractionType.MultipleChoiceImage;
	}

	public void AnswerTabularData()
	{
		answered = true;
		answer = InteractionType.TabularData;
	}
	
	//TODO(Simon): Disable chapter option if no chapters defined. Also show message explaining why
	public void AnswerChapter()
	{
		answered = true;
		answer = InteractionType.Chapter;
	}
}
