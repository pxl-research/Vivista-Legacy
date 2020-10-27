using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Object3DPanelEditor : MonoBehaviour
{
	public Button done;

	public InputField urlObject;
	public InputField urlMaterial;
	public InputField title;

	public bool answered;
	public string answerTitle;
	public List<string> answerURLs;

	public bool allowCancel => explorerPanel == null;

	private ExplorerPanel explorerPanel;
	private GameObject objectRenderer;

	private bool fileOpening;
	private bool fileIsObject;
	private bool fileIsMaterial;

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
				if (fileIsObject)
				{
					urlObject.text = explorerPanel.answerPath;
					fileIsObject = false;
				}
				if (fileIsMaterial)
				{
					urlMaterial.text = explorerPanel.answerPath;
					fileIsMaterial = false;
				}
				Destroy(explorerPanel.gameObject);
			}
		}
	}

	public void Init(string initialTitle, List<string> initialUrls, string object3dName)
	{
		title.text = initialTitle;
		objectRenderer = GameObject.Find("ObjectRenderer");

		var objects3d = objectRenderer.GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < objects3d.Length; i++)
		{
			var tempObject = objects3d[i];
			Debug.Log(tempObject.ToString());
			if (tempObject.name == object3dName)
			{
				Destroy(tempObject.gameObject);
				break;
			}
		}

		if (initialUrls != null && initialUrls.Count > 0)
		{
			urlObject.text = initialUrls[0];
			if (initialUrls.Count > 1)
			{
				urlMaterial.text = initialUrls[1];
			}
		}

		title.onValueChanged.AddListener(_ => OnInputChange(title));
		urlObject.onValueChanged.AddListener(_ => OnInputChange(urlObject));
	}

	public void Answer()
	{
		bool errors = false;
		if (String.IsNullOrEmpty(title.text))
		{
			title.image.color = errorColor;
			errors = true;
		}

		if (String.IsNullOrEmpty(urlObject.text))
		{
			urlObject.image.color = errorColor;
			errors = true;
		}

		if (!errors)
		{
			answered = true;
			answerURLs = new List<string>();
			answerURLs.Add(urlObject.text);
			answerURLs.Add(urlMaterial.text);
			answerTitle = title.text;
		}
	}

	private void Browse(string searchPattern, string title)
	{
		explorerPanel = Instantiate(UIPanels.Instance.explorerPanel);
		explorerPanel.transform.SetParent(Canvass.main.transform, false);
		explorerPanel.GetComponent<ExplorerPanel>().Init("", searchPattern, title);

		fileOpening = true;
	}

	public void BrowseObjects()
	{
		fileIsObject = true;
		Browse("*.obj", "Select 3D object");
	}

	public void BrowseMaterials()
	{
		fileIsMaterial = true;
		Browse("*.mtl", "Select material");
	}

	public void OnInputChange(InputField input)
	{
		input.image.color = Color.white;
	}
}
