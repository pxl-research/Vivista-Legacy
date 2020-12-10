using System;
using UnityEngine;
using UnityEngine.UI;

public class TextPanelEditor : MonoBehaviour
{
	public InputField title;
	public InputField body;
	public Button done;

	public bool answered;
	public string answerTitle;
	public string answerBody;

	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	public void OnEnable()
	{
		StartCoroutine(UIAnimation.FadeIn(GetComponent<RectTransform>(), GetComponent<CanvasGroup>()));
	}

	void Start()
	{
		ResizeToFit();
		title.onValueChanged.RemoveAllListeners();
		title.onValueChanged.AddListener(_ => OnInputChange(title));
		body.onValueChanged.RemoveAllListeners();
		body.onValueChanged.AddListener(_ => OnInputChange(body));
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
		bool errors = false;
		if (String.IsNullOrEmpty(title.text))
		{
			title.image.color = errorColor;
			errors = true;
		}

		if (String.IsNullOrEmpty(body.text))
		{
			body.image.color = errorColor;
			errors = true;
		}

		if (!errors)
		{
			answered = true;
			answerTitle = title.text;
			answerBody = body.text;
		}
	}

	public void OnInputChange(InputField input)
	{
		ResizeToFit();

		input.image.color = Color.white;
	}
}
