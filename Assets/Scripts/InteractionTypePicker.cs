using UnityEngine;

public class InteractionTypePicker : MonoBehaviour 
{
	public bool answered = false;
	public InteractionType answer;
	public Canvas canvas;

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
		
		canvas = GetComponent<Canvas>();

		canvas.GetComponent<RectTransform>().position = newPos;
	}

	public void Update()
	{
		canvas.transform.rotation = Camera.main.transform.rotation;
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
