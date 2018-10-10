using System;
using UnityEngine;
using UnityEngine.UI;

public class VideoPanelEditor : MonoBehaviour {

	public Button done;
	public RawImage imagePreview;
	public ExplorerPanel explorerPanelPrefab;
	public Canvas canvas;

	public InputField url;
	public InputField title;


	public bool answered;
	public string answerTitle;
	public string answerURL;

	private ExplorerPanel explorerPanel;
	private bool fileOpening;

	//public void Init(Vector3 position, bool exactPost = false)
	// Use this for initialization
	void Start () {
		transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y);
	}

	// Update is called once per frame
	void Update () {
		if (fileOpening)
		{
			if (explorerPanel != null && explorerPanel.answered)
			{
				url.text = explorerPanel.answerPath;
				Destroy(explorerPanel.gameObject);
			}
		}
	}

	public void Init(Vector3 position, string initialTitle, string initialUrl, bool exactPos = false)
	{
		Move(position, exactPos);
		title.text = initialTitle;
		url.text = initialUrl;
	}

	// NOTE(Lander): Copied from imagePanelEditor.cs
	public void Move(Vector3 position, bool exactPos = false)
	{
		Vector3 newPos;

		if (exactPos)
		{
			newPos = position;
		}
		else
		{
			newPos = Vector3.Lerp(position, Camera.main.transform.position, 0.3f);
			newPos.y += 1f;

		}

		canvas.GetComponent<RectTransform>().position = newPos;
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
