using UnityEngine;

public class InteractionTypePicker : MonoBehaviour 
{
	public bool answered = false;
	public InteractionType answer;

	public void Init(GameObject newInteractionPoint)
	{
		var newPos = newInteractionPoint.transform.position;

		if (!Camera.main.orthographic)
		{
			newPos = Vector3.Lerp(newPos, Camera.main.transform.position, 0.3f);
			newPos.y += 0.01f;
		}
		else
		{
			newPos = Vector3.Lerp(newPos, Camera.main.transform.position, 0.001f);
			newPos.y += 0.015f;
		}

		GetComponent<Canvas>().GetComponent<RectTransform>().position = newPos;
	}

	public void AnswerImage()
	{
		answered = true;
		answer =  InteractionType.Image;
	}

	public void AnswerText()
	{
		answered = true;
		answer =  InteractionType.Text;
	}
}
