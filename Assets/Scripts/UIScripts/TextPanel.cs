using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class TextPanel : MonoBehaviour
{
	public Text title;
	public Text body;
	public RectTransform panel;
	public Canvas canvas;

	public void Init(Vector3 position, string newTitle, string newBody)
	{
		title.text = newTitle;
		body.text = newBody;

		canvas = GetComponent<Canvas>();
		Move(position);

		var titleComponent = title.GetComponent<Text>();
		var bodyComponent = body.GetComponent<Text>();

		var titleHeight = UIHelper.CalculateTextFieldHeight(titleComponent.text, titleComponent.font, titleComponent.fontSize, 400, 0);
		var bodyHeight = UIHelper.CalculateTextFieldHeight(bodyComponent.text, bodyComponent.font, bodyComponent.fontSize, 400, 0);

		canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(500, titleHeight + bodyHeight);
	}

	public void Move(Vector3 position)
	{
		var newPos = Vector3.Lerp(position, Camera.main.transform.position, 0.001f);
		newPos.y += 0.015f;

		canvas.GetComponent<RectTransform>().position = newPos;
	}

	public void Start()
	{
		//NOTE(Kristof): Initial rotation towards the camera
		canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y);
	}

	public void Update()
	{
		//NOTE(Kristof): Rotating the Canvas
		if (!XRSettings.enabled)
		{
			canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y);
		}
	}
}
