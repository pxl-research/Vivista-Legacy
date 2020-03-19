using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImagePanelEditor : MonoBehaviour
{
	private enum ImageEditorState
	{
		Opening,
		Showing
	}

	public InputField title;
	public ExplorerPanel explorerPanelPrefab;
	public GameObject imageAlbumEntryPrefab;
	public RectTransform imageAlbumList;

	public List<ImageAlbumEntry> entries;

	public bool answered;
	public string answerTitle;
	public List<string> answerURLs;
	public int answerTagId;

	public TagPicker tagPicker;

	private ExplorerPanel explorerPanel;
	private ImageEditorState imageEditorState;

	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	void Update()
	{
		if (imageEditorState == ImageEditorState.Opening)
		{
			if (explorerPanel != null && explorerPanel.answered)
			{
				foreach (string path in explorerPanel.answerPaths)
				{
					CreateNewEntry(path);
				}

				UpdateAlbumSortButtons();
				imageEditorState = ImageEditorState.Showing;

				Destroy(explorerPanel.gameObject);

				//NOTE(Simon): Reset the background color, in case it was red/invalid previously
				var background = imageAlbumList.parent.parent.GetComponent<Image>();
				background.color = Color.white;
			}
		}

		if (imageEditorState == ImageEditorState.Showing)
		{
			var titleRect = title.GetComponent<RectTransform>();
			float newHeight = UIHelper.CalculateInputFieldHeight(title, 3);
			titleRect.sizeDelta = new Vector2(titleRect.sizeDelta.x, newHeight);
		}
	}

	public void Init(string initialTitle, List<string> initialURLs, int tagId = -1)
	{
		title.text = initialTitle;
		title.onValueChanged.AddListener(_ => OnInputChange(title));

		if (initialURLs != null)
		{
			foreach (string initialURL in initialURLs)
			{
				CreateNewEntry(initialURL);
			}
		}

		UpdateAlbumSortButtons();
		imageEditorState = ImageEditorState.Showing;

		tagPicker.Init(tagId);
	}

	public void Browse()
	{
		var searchPattern = "*.jpg;*.jpeg;*.bmp;*.png";

		explorerPanel = Instantiate(explorerPanelPrefab);
		explorerPanel.transform.SetParent(Canvass.main.transform, false);
		explorerPanel.GetComponent<ExplorerPanel>().Init("", searchPattern, "Select image", ExplorerPanel.SelectionMode.File, true);

		imageEditorState = ImageEditorState.Opening;
	}

	public void Answer()
	{
		bool errors = false;
		if (String.IsNullOrEmpty(title.text))
		{
			title.image.color = errorColor;
			errors = true;
		}

		if (entries.Count < 1)
		{
			var background = imageAlbumList.parent.parent.GetComponent<Image>();
			background.color = errorColor;
			errors = true;
		}

		if (!errors)
		{
			answered = true;
			answerTitle = title.text;
			answerURLs = new List<string>();
			foreach (var entry in entries)
			{
				answerURLs.Add(entry.url);
			}
			answerTagId = tagPicker.currentTagId;
		}
	}

	public void DeleteAlbumEntry(GameObject go)
	{
		var entry = go.GetComponent<ImageAlbumEntry>();
		entries.Remove(entry);
		Destroy(go);
		UpdateAlbumSortButtons();
	}

	public void MoveLeftAlbumEntry(GameObject go)
	{
		var trans = go.transform;
		var index = trans.GetSiblingIndex();
		trans.SetSiblingIndex(index - 1);
		SwapElementsInList(index - 1, index);
	}

	public void MoveRightAlbumEntry(GameObject go)
	{
		var trans = go.transform;
		var index = trans.GetSiblingIndex();
		trans.SetSiblingIndex(index + 1);
		SwapElementsInList(index + 1, index);
	}

	private void SwapElementsInList(int index1, int index2)
	{
		var temp = entries[index1];
		entries[index1] = entries[index2];
		entries[index2] = temp;

		UpdateAlbumSortButtons();
	}

	private void UpdateAlbumSortButtons()
	{
		if (entries.Count > 0)
		{
			for (int i = 0; i < entries.Count; i++)
			{
				entries[i].moveLeftButton.interactable = i != 0;
				entries[i].moveRightButton.interactable = i != entries.Count - 1;
			}
		}
	}

	private void CreateNewEntry(string path)
	{
		var albumEntry = Instantiate(imageAlbumEntryPrefab, imageAlbumList).GetComponent<ImageAlbumEntry>();
		StartCoroutine(albumEntry.SetURL(path));

		albumEntry.moveLeftButton.onClick.AddListener(() => MoveLeftAlbumEntry(albumEntry.gameObject));
		albumEntry.moveRightButton.onClick.AddListener(() => MoveRightAlbumEntry(albumEntry.gameObject));
		albumEntry.deleteButton.onClick.AddListener(() => DeleteAlbumEntry(albumEntry.gameObject));

		entries.Add(albumEntry);
	}

	public void OnInputChange(InputField input)
	{
		input.image.color = Color.white;
	}
}
