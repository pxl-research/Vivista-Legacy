using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultipleChoiceImagePanelEditor : MonoBehaviour
{
	public InputField question;
	public RectTransform imageAlbumList;
	public List<MultipleChoiceImageEntry> entries;

	public bool answered;
	public string answerQuestion;
	public List<string> answerURLs;
	public int answerCorrect;
	public int answerTagId;

	private const int MAXANSWERS = 6;
	private ToggleGroup2 toggleGroup;

	public bool allowCancel => explorerPanel == null;

	public TagPicker tagPicker;

	public MultipleChoiceImageEntry multipleChoiceImageEntryPrefab;

	private ExplorerPanel explorerPanel;
	private ImageEditorState imageEditorState;

	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	public void OnEnable()
	{
		StartCoroutine(UIAnimation.FadeIn(GetComponent<RectTransform>(), GetComponent<CanvasGroup>()));
	}

	public void Init(string initialTitle, List<string> initialAnswers, int initialCorrect = -1, int tagId = -1)
	{
		entries = new List<MultipleChoiceImageEntry>();
		question.text = initialTitle;
		toggleGroup = gameObject.AddComponent<ToggleGroup2>();
		answerCorrect = initialCorrect;

		if (initialAnswers != null)
		{
			for (int i = 0; i < initialAnswers.Count; i++)
			{
				CreateNewEntry(initialAnswers[i], i == initialCorrect);
			}
		}

		question.onValueChanged.AddListener(_ => OnInputChangeColor(question));

		toggleGroup.onToggleGroupChanged.AddListener(OnSelectCorrectImage);

		tagPicker.Init(tagId);
	}

	void Update()
	{
		if (imageEditorState == ImageEditorState.Opening)
		{
			if (explorerPanel != null)
			{
				if (Input.GetKeyDown(KeyCode.Escape))
				{
					Destroy(explorerPanel.gameObject);
					imageEditorState = ImageEditorState.Showing;
				}
			}

			if (explorerPanel != null && explorerPanel.answered)
			{
				foreach (string path in explorerPanel.answerPaths)
				{
					if (entries.Count < MAXANSWERS)
					{
						CreateNewEntry(path, false);
					}
				}

				imageEditorState = ImageEditorState.Showing;

				Destroy(explorerPanel.gameObject);

				//NOTE(Simon): Reset the background color, in case it was red/invalid previously
				var background = imageAlbumList.parent.parent.GetComponent<Image>();
				background.color = Color.white;
			}
		}

		if (imageEditorState == ImageEditorState.Showing)
		{
			var titleRect = question.GetComponent<RectTransform>();
			float newHeight = UIHelper.CalculateInputFieldHeight(question, 3);
			titleRect.sizeDelta = new Vector2(titleRect.sizeDelta.x, newHeight);
		}
	}

	public void Browse()
	{
		var searchPattern = "*.jpg;*.jpeg;*.bmp;*.png";

		explorerPanel = Instantiate(UIPanels.Instance.explorerPanel);
		explorerPanel.transform.SetParent(Canvass.main.transform, false);
		explorerPanel.GetComponent<ExplorerPanel>().Init("", searchPattern, "Select image", ExplorerPanel.SelectionMode.File, true);

		imageEditorState = ImageEditorState.Opening;
	}

	private void CreateNewEntry(string path, bool correct)
	{
		var albumEntry = Instantiate(multipleChoiceImageEntryPrefab, imageAlbumList).GetComponent<MultipleChoiceImageEntry>();
		albumEntry.toggle.SetIsOnWithoutNotify(correct);
		StartCoroutine(albumEntry.SetUrl(path));

		toggleGroup.RegisterToggle(albumEntry.toggle);
		albumEntry.toggle.group = toggleGroup;
		albumEntry.deleteButton.onClick.AddListener(() => DeleteAlbumEntry(albumEntry.gameObject));


		entries.Add(albumEntry);
	}

	public void DeleteAlbumEntry(GameObject go)
	{
		var entry = go.GetComponent<MultipleChoiceImageEntry>();
		entries.Remove(entry);
		Destroy(go);
	}

	public void OnSelectCorrectImage(Toggle toggle)
	{
		var toggles = toggleGroup.GetAllToggles();
		for (int i = 0; i < toggles.Count; i++)
		{
			if (toggles[i].isOn)
			{
				answerCorrect = i;
			}

			toggles[i].image.color = Color.white;
		}
	}

	public void Answer()
	{
		bool errors = false;

		if (String.IsNullOrEmpty(question.text))
		{
			question.image.color = errorColor;
			errors = true;
		}

		if (entries.Count < 2)
		{
			var background = imageAlbumList.parent.parent.GetComponent<Image>();
			background.color = errorColor;
			errors = true;
		}

		if (!toggleGroup.AnyTogglesOn())
		{
			var toggles = toggleGroup.GetAllToggles();
			for (int i = 0; i < toggles.Count; i++)
			{
				toggles[i].image.color = errorColor;
			}
			errors = true;
		}

		if (!errors)
		{
			answered = true;
			answerQuestion = question.text;
			answerURLs = new List<string>();
			foreach (var entry in entries)
			{
				answerURLs.Add(entry.imageUrl);
			}
			answerTagId = tagPicker.currentTagId;
		}
	}

	public void OnInputChangeColor(InputField input)
	{
		input.image.color = Color.white;
	}
}
