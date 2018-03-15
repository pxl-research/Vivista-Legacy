using UnityEngine;
using UnityEngine.UI;

public class TextPanelEditor : MonoBehaviour 
{
	public Canvas canvas;
	public RectTransform resizePanel;
	public InputField title;
	public InputField body;
	public Button done;

	public bool answered;
	public string answerTitle;
	public string answerBody;

	public void Init(Vector3 position, string initialTitle, string initialBody, bool exactPos = false)
	{
		title.text = initialTitle;
		body.text = initialBody;
		Move(position, exactPos);
	}
	
	public void Move(Vector3 position, bool exactPos = false)
	{
		Vector3 newPos;

		if (exactPos)
		{
			newPos = position;
		}
		else
		{
			if (!Camera.main.orthographic)
			{
				newPos = Vector3.Lerp(position, Camera.main.transform.position, 0.3f);
				newPos.y += 2f;
			}
			else
			{
				newPos = Vector3.Lerp(position, Camera.main.transform.position, 0.001f);
				newPos.y += 0.015f;
			}
		}
		canvas.GetComponent<RectTransform>().position = newPos;
	}


	void Update () 
	{
		var titleRect = title.GetComponent<RectTransform>();
		var newHeight = UIHelper.CalculateTextFieldHeight(title, 30);
		titleRect.sizeDelta = new Vector2(titleRect.sizeDelta.x, newHeight);
	
		var bodyRect = body.GetComponent<RectTransform>();
		newHeight = UIHelper.CalculateTextFieldHeight(body, 100);
		bodyRect.sizeDelta = new Vector2(bodyRect.sizeDelta.x, newHeight);

		resizePanel.sizeDelta = new Vector2(resizePanel.sizeDelta.x,
			title.GetComponent<RectTransform>().sizeDelta.y
			+ body.GetComponent<RectTransform>().sizeDelta.y
			//Padding, spacing, button, fudge factor
			+ 20 + 20 + 30 + 20);

		canvas.transform.rotation = Camera.main.transform.rotation;
	}

	public void Answer()
	{
		answered = true;
		answerTitle = title.text;
		answerBody = body.text;
	}
}
