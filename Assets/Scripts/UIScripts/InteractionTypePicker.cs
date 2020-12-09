using UnityEngine;

public class InteractionTypePicker : MonoBehaviour
{
	public bool answered = false;
	public InteractionType answer;

	public void OnEnable()
	{
		StartCoroutine(UIAnimation.FadeIn(GetComponent<RectTransform>(), GetComponent<CanvasGroup>()));
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
}
