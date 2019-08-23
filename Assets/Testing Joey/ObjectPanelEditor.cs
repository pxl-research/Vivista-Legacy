using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectPanelEditor : MonoBehaviour {

	public Button done;
	public ExplorerPanel explorerPanelPrefab;

	public InputField url;
	public InputField title;

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
		answered = true;
		answerURL = url.text;
		answerTitle = string.IsNullOrEmpty(title.text) ? "<unnamed>" : title.text;
	}

	public void Browse()
	{
		var searchPattern = "*.fbx;*.obj";

		explorerPanel = Instantiate(explorerPanelPrefab);
		explorerPanel.transform.SetParent(Canvass.main.transform, false);
		explorerPanel.GetComponent<ExplorerPanel>().Init("", searchPattern, "Select a 3D object");

		fileOpening = true;
	}
}
