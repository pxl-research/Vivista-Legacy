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

	private ExplorerPanel explorerPanel;
	private bool fileOpening;

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

	public void Init(string initialTitle, string initialUrl)
	{
		title.text = initialTitle;
		url.text = initialUrl;
	}

	public void Answer()
	{
		// NOTE(Lander): ensured that this is the right path?
		answered = true;
		answerURL = url.text;
		answerTitle = string.IsNullOrEmpty(title.text) ? "<unnamed>" : title.text;
	}

	public void Browse()
	{
		var searchPattern = "*.mp4;*.webm;*.m4v";

		explorerPanel = Instantiate(explorerPanelPrefab);
		explorerPanel.transform.SetParent(Canvass.main.transform, false);
		explorerPanel.GetComponent<ExplorerPanel>().Init("", searchPattern, "Select image");

		fileOpening = true;
	}
}
