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
	
	void Start()
	{
		ResizeToFit();
		title.onValueChanged.RemoveAllListeners();
		title.onValueChanged.AddListener(delegate { OnInputChanged(); });
		body.onValueChanged.RemoveAllListeners();
		body.onValueChanged.AddListener(delegate { OnInputChanged(); });
	}

	void ResizeToFit()
	{
		var titleRect = title.GetComponent<RectTransform>();
		titleRect.sizeDelta = new Vector2(titleRect.sizeDelta.x, UIHelper.CalculateInputFieldHeight(title, 3));

		var bodyRect = body.GetComponent<RectTransform>();
		bodyRect.sizeDelta = new Vector2(bodyRect.sizeDelta.x, UIHelper.CalculateInputFieldHeight(body, 10));
	}

	public void Init(string initialTitle, string initialBody)
	{
		title.text = initialTitle;
		body.text = initialBody;
	}

	public void Answer()
	{
		answered = true;
		answerTitle = title.text;
		answerBody = body.text;
	}

	public void OnInputChanged()
	{
		ResizeToFit();
	}
}
