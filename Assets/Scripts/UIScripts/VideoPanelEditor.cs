using System;
using UnityEngine;
using UnityEngine.UI;

public class VideoPanelEditor : MonoBehaviour {

	public Button done;
	public ExplorerPanel explorerPanelPrefab;

	public InputField url;
	public InputField title;

	public bool answered;
	public string answerTitle;
	public string answerURL;
	public int answerTagId;

	public TagPicker tagPicker;

	private ExplorerPanel explorerPanel;
	private bool fileOpening;

	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	void Update () 
	{
		if (fileOpening)
		{
			if (explorerPanel != null && explorerPanel.answered)
			{
				url.text = explorerPanel.answerPath;
				Destroy(explorerPanel.gameObject);
			}
		}
	}

	public void Init(string initialTitle, string initialUrl, int tagId = -1)
	{
		title.text = initialTitle;
		url.text = initialUrl;

		title.onValueChanged.AddListener(_ => OnInputChange(title));
		url.onValueChanged.AddListener(_ => OnInputChange(url));

		tagPicker.Init(tagId);
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
			answerTagId = tagPicker.currentTagId;
		}
	}

	public void Browse()
	{
		var searchPattern = "*.mp4;*.webm;*.m4v";

		explorerPanel = Instantiate(explorerPanelPrefab);
		explorerPanel.transform.SetParent(Canvass.main.transform, false);
		explorerPanel.GetComponent<ExplorerPanel>().Init("", searchPattern, "Select video");

		fileOpening = true;
	}

	public void OnInputChange(InputField input)
	{
		input.image.color = Color.white;
	}
}
