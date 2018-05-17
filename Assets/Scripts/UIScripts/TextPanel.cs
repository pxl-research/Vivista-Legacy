using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public class TextPanel : MonoBehaviour
{
	public Text title;
	public Text body;
	public RectTransform panel;
	public Canvas canvas;

	void Start()
	{
		//NOTE(Kristof): Initial rotation towards the camera
		canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y);
		if (!XRSettings.enabled)
		{
			canvas.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
		}
	}

	void Update()
	{
		// NOTE(Kristof): Turning every frame only needs to happen in Editor
		if (SceneManager.GetActiveScene().Equals(SceneManager.GetSceneByName("Editor")))
		{
			canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, Camera.main.transform.eulerAngles.z);
		}
	}

	public void Init(string newTitle, string newBody)
	{
		title.text = newTitle;
		body.text = newBody;

		canvas = GetComponent<Canvas>();

		var titleComponent = title.GetComponent<Text>();
		var bodyComponent = body.GetComponent<Text>();

		var titleHeight = UIHelper.CalculateTextFieldHeight(titleComponent.text, titleComponent.font, titleComponent.fontSize, 400, 0);
		var bodyHeight = UIHelper.CalculateTextFieldHeight(bodyComponent.text, bodyComponent.font, bodyComponent.fontSize, 400, 0);

		canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(500, titleHeight + bodyHeight);
	}

	public void Move(Vector3 position)
	{
		var newPos = position;
		newPos.y += 0.015f;
		canvas.GetComponent<RectTransform>().position = position;
	} 
}