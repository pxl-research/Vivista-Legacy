using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TabNav))]
public class AudioPanelEditor : MonoBehaviour
{
	public InputField url;
	public InputField title;
	public AudioControl audioControl;

	public bool answered;
	public string answerTitle;
	public string answerURL;

	public bool allowCancel => explorerPanel == null;

	private ExplorerPanel explorerPanel;
	private bool fileOpening;

	private static Color defaultColor = new Color(1, 0.8f, 0.8f, 1f);
	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	public void OnEnable()
	{
		StartCoroutine(UIAnimation.FadeIn(GetComponent<RectTransform>(), GetComponent<CanvasGroup>()));
	}

	void Update()
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
				audioControl.enabled = true;
				audioControl.Init(explorerPanel.answerPath);
			}
		}
	}

	public void Init(string initialTitle, string initialUrl)
	{
		defaultColor = title.image.color;

		title.onValueChanged.AddListener(_ => OnInputChange(title));
		url.onValueChanged.AddListener(_ => OnInputChange(url));

		title.text = initialTitle;
		url.text = initialUrl;
		if (String.IsNullOrEmpty(initialUrl))
		{
			audioControl.enabled = false;
		}
		else
		{
			audioControl.Init(initialUrl);
		}
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
		 var searchPattern = "*.wav;*.aif;*.ogg";

		explorerPanel = Instantiate(UIPanels.Instance.explorerPanel);
		explorerPanel.transform.SetParent(Canvass.main.transform, false);
		explorerPanel.GetComponent<ExplorerPanel>().Init("", searchPattern, "Select audio");

		fileOpening = true;
	}

	public void OnInputChange(InputField input)
	{
		input.image.color = defaultColor;
	}
}
