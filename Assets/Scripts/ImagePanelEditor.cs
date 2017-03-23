using UnityEngine;
using UnityEngine.UI;

public class ImagePanelEditor : MonoBehaviour
{
	public Canvas canvas;
	public RectTransform resizePanel;
	public InputField title;
	public InputField url;
	public Button done;

	public bool answered;
	public string answerTitle;
	public string answerURL;

	public void Init(GameObject newInteractionPoint, string initialTitle, string initialUrl)
	{
		title.text = initialTitle;
		url.text = initialUrl;

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

		canvas.GetComponent<RectTransform>().position = newPos;
	}

	void Update()
	{
		var titleRect = title.GetComponent<RectTransform>();
		var newHeight = UIHelper.CalculateTextFieldHeight(title, 30);
		titleRect.sizeDelta = new Vector2(titleRect.sizeDelta.x, newHeight);

		resizePanel.sizeDelta = new Vector2(resizePanel.sizeDelta.x,
			title.GetComponent<RectTransform>().sizeDelta.y
			+ url.GetComponent<RectTransform>().sizeDelta.y
			//Padding, spacing, button, fudge factor
			+ 20 + 20 + 30 + 20);

		canvas.transform.rotation = Camera.main.transform.rotation;
	}
}
