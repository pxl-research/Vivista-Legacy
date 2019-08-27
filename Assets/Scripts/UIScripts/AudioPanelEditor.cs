using System;
using UnityEngine;
using UnityEngine.UI;

public class AudioPanelEditor : MonoBehaviour
{
	public Button done;
	public ExplorerPanel explorerPanelPrefab;

	public InputField url;
	public InputField title;
	public AudioControl audioControl;

	public bool answered;
	public string answerTitle;
	public string answerURL;

	private ExplorerPanel explorerPanel;
	private bool fileOpening;

	void Update()
	{
		if (fileOpening)
		{
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
		answered = true;
		answerURL = url.text;
		answerTitle = String.IsNullOrEmpty(title.text) ? "<unnamed>" : title.text;
	}

	public void Browse()
	{
		 var searchPattern = "*.mp3;*.wav;*.aif;*.ogg";

		explorerPanel = Instantiate(explorerPanelPrefab);
		explorerPanel.transform.SetParent(Canvass.main.transform, false);
		explorerPanel.GetComponent<ExplorerPanel>().Init("", searchPattern, "Select audio");

		fileOpening = true;
	}
}
