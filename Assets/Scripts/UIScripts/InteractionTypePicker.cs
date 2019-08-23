using UnityEngine;

public class InteractionTypePicker : MonoBehaviour
{
	public bool answered = false;
	public InteractionType answer;

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

	public void AnswerObject()
	{
		answered = true;
		answer = InteractionType.Object;
	}
}
