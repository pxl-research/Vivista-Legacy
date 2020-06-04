using System;
using UnityEngine;
using UnityEngine.UI;

public class VideoPanelEditor : MonoBehaviour {

	public Button done;

	public InputField url;
	public InputField title;

	public bool answered;
	public string answerTitle;
	public string answerURL;

	public bool allowCancel => explorerPanel == null;

	private ExplorerPanel explorerPanel;
	private bool fileOpening;

	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	public void OnEnable()
	{
		StartCoroutine(UIAnimation.FadeIn(GetComponent<RectTransform>(), GetComponent<CanvasGroup>()));
	}

	void Update () 
	{
		if (fileOpening)
		{
			if (explorerPanel != null)
			{
				if (Input.GetKeyDown(KeyCode.Escape))
				{
					Destroy(explorerPanel.gameObject);
				}
			}

			if (explorerPanel != null && explorerPanel.answered)
			{
				url.text = explorerPanel.answerPath;
				Destroy(explorerPanel.gameObject);
			}
		}
	}

	public void Init(string initialTitle, string initialUrl)
	{
		title.text = initialTitle;
		url.text = initialUrl;

		title.onValueChanged.AddListener(_ => OnInputChange(title));
		url.onValueChanged.AddListener(_ => OnInputChange(url));
	}

	public void Answer()
	{
		bool errors = false;
		if (String.IsNullOrEmpty(title.text))
		{
			title.image.color = errorColor;
			errors = true;
		}

		if (String.IsNullOrEmpty(url.text))
		{
			url.image.color = errorColor;
			errors = true;
		}

		if (!errors)
		{
			answered = true;
			answerURL = url.text;
			answerTitle = title.text;
		}
	}

	public void Browse()
	{
		var searchPattern = "*.mp4;*.webm;*.m4v";

		explorerPanel = Instantiate(UIPanels.Instance.explorerPanel);
		explorerPanel.transform.SetParent(Canvass.main.transform, false);
		explorerPanel.GetComponent<ExplorerPanel>().Init("", searchPattern, "Select video");

		fileOpening = true;
	}

	public void OnInputChange(InputField input)
	{
		input.image.color = Color.white;
	}
}
