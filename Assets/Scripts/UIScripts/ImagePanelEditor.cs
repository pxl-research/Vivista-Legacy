﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class ImagePanelEditor : MonoBehaviour
{
	private enum ImageEditorState
	{
		Opening,
		SingleImage,
		Album
	}

	public RectTransform layoutPanel;
	public Canvas canvas;
	public RectTransform resizePanel;
	public InputField title;
	public InputField url;
	public List<InputField> urls;
	public Button done;
	public RawImage imagePreview;
	public ExplorerPanel explorerPanelPrefab;
	public GameObject imageAlbumEntryPrefab;

	public bool answered;
	public string answerTitle;
	public string answerURL;

	private string prevURL = "";
	private bool downloading = false;
	private WWW www;
	private bool fileOpening;
	private ExplorerPanel explorerPanel;
	private ImageEditorState imageEditorState;

	void Update()
	{
		if (imageEditorState == ImageEditorState.Opening)
		{
			if (explorerPanel != null && explorerPanel.answered)
			{
				//NOTE(Kristof): Single image
				if (explorerPanel.answerFilePaths == null)
				{
					url.text = explorerPanel.answerFilePath;
					imageEditorState = ImageEditorState.SingleImage;
				}
				//NOTE(Kristof): Multiple images
				else
				{
					imagePreview.gameObject.SetActive(false);

					foreach (var path in explorerPanel.answerFilePaths)
					{
						var input = Instantiate(imageAlbumEntryPrefab, layoutPanel).GetComponentInChildren<InputField>();
						input.transform.parent.SetSiblingIndex(urls.Count + 1);
						input.text = path;
						urls.Add(input);
					}

					imageEditorState = ImageEditorState.Album;
				}
				Destroy(explorerPanel.gameObject);
			}
		}

		if (imageEditorState == ImageEditorState.SingleImage)
		{
			var titleRect = title.GetComponent<RectTransform>();
			float newHeight = UIHelper.CalculateTextFieldHeight(title, 30);
			titleRect.sizeDelta = new Vector2(titleRect.sizeDelta.x, newHeight);

			resizePanel.sizeDelta = new Vector2(resizePanel.sizeDelta.x,
				title.GetComponent<RectTransform>().sizeDelta.y
				+ url.GetComponent<RectTransform>().sizeDelta.y
				+ imagePreview.rectTransform.sizeDelta.y
				//Padding, spacing, button, fudge factor
				+ 20 + 30 + 30 + 20);

			canvas.transform.rotation = Camera.main.transform.rotation;

			if (url.text != prevURL && !String.IsNullOrEmpty(url.text))
			{
				answerURL = url.text;
				prevURL = url.text;

				if (!Regex.IsMatch(answerURL, "http://|https://"))
				{
					answerURL = "file:///" + url.text;
				}

				www = new WWW(answerURL);
				downloading = true;
			}

			if (downloading && www.isDone)
			{
				var texture = www.texture;
				imagePreview.texture = texture;
				float width = imagePreview.rectTransform.sizeDelta.x;
				float ratio = texture.width / width;
				float height = texture.height / ratio;
				imagePreview.rectTransform.sizeDelta = new Vector2(width, height);

				downloading = false;
			}
		}

		if (imageEditorState == ImageEditorState.Album)
		{
			var titleRect = title.GetComponent<RectTransform>();
			float newHeight = UIHelper.CalculateTextFieldHeight(title, 30);
			titleRect.sizeDelta = new Vector2(titleRect.sizeDelta.x, newHeight);

			var amountEntries = layoutPanel.GetComponentsInChildren<InputField>().Length;

			resizePanel.sizeDelta = new Vector2(resizePanel.sizeDelta.x,
				title.GetComponent<RectTransform>().sizeDelta.y
				//Growing for each entry 
				+ 40 * amountEntries
				+ url.GetComponent<RectTransform>().sizeDelta.y
				//Fudge factor
				+ 10);
		}
	}

	public void Init(Vector3 position, string initialTitle, string initialUrl, bool exactPos = false)
	{
		title.text = initialTitle;
		if (initialUrl.StartsWith(@"file:///"))
		{
			initialUrl = initialUrl.Substring(8);
		}
		url.text = initialUrl;
		Move(position, exactPos);

		imageEditorState = ImageEditorState.SingleImage;
	}

	public void Move(Vector3 position, bool exactPos = false)
	{
		Vector3 newPos;

		if (exactPos)
		{
			newPos = position;
		}
		else
		{
			newPos = Vector3.Lerp(position, Camera.main.transform.position, 0.001f);
			newPos.y -= 3f;
		}

		canvas.GetComponent<RectTransform>().position = newPos;
	}

	public void Browse()
	{
		var searchPattern = "*.jpg;*.jpeg;*.bmp;*.png";

		explorerPanel = Instantiate(explorerPanelPrefab);
		explorerPanel.transform.SetParent(Canvass.main.transform, false);
		explorerPanel.GetComponent<ExplorerPanel>().Init("", searchPattern, "Select image", true);

		imageEditorState = ImageEditorState.Opening;
	}

	public void Answer()
	{
		answered = true;
		answerTitle = title.text;
		//NOTE(Simon): AnswerURL already up to date
	}
}
