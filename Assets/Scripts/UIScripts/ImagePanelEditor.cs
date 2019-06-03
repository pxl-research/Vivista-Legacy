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

	public Canvas canvas;
	public InputField title;
	public ExplorerPanel explorerPanelPrefab;
	public GameObject imageAlbumEntryPrefab;
	public RectTransform imageAlbumList;

	public List<ImageAlbumEntry> entries;

	public bool answered;
	public string answerTitle;
	public List<string> answerURLs;

	private ExplorerPanel explorerPanel;
	private ImageEditorState imageEditorState;

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
			}
		}

		if (imageEditorState == ImageEditorState.Showing)
		{
			var titleRect = title.GetComponent<RectTransform>();
			float newHeight = UIHelper.CalculateInputFieldHeight(title, 3);
			titleRect.sizeDelta = new Vector2(titleRect.sizeDelta.x, newHeight);
		}
	}

	public void Init(string initialTitle, List<string> initialURLs)
	{
		title.text = initialTitle;
		if (initialURLs != null)
		{
			foreach (string initialURL in initialURLs)
			{
				CreateNewEntry(initialURL);
			}
		}

		UpdateAlbumSortButtons();
		imageEditorState = ImageEditorState.Showing;
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
		answered = true;
		answerTitle = title.text;
		answerURLs = new List<string>();
		foreach (var entry in entries)
		{
			answerURLs.Add(entry.url.text);
		}
	}

	public Texture TextureForIndex(int index)
	{
		return entries[index].preview.texture;
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
		albumEntry.SetURL(path);

		albumEntry.moveLeftButton.onClick.AddListener(() => MoveLeftAlbumEntry(albumEntry.gameObject));
		albumEntry.moveRightButton.onClick.AddListener(() => MoveRightAlbumEntry(albumEntry.gameObject));
		albumEntry.deleteButton.onClick.AddListener(() => DeleteAlbumEntry(albumEntry.gameObject));

		entries.Add(albumEntry);
	}
}
