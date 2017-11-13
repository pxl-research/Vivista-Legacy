using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class OpenPanel : MonoBehaviour 
{
	public class FileItem
	{
		public string guid;
		public string name;
		public GameObject listItem;
	}

	public RectTransform fileList;
	public Text chosenFile;

	public Button newButton;
	public Button renameButton;
	public Button deleteButton;
	public Button openButton;

	public GameObject filenameItemPrefab;

	public bool answered;
	public string answerGuid;

	private List<FileItem> files = new List<FileItem>();
	private int selectedIndex = -1;
	private bool isNew;

	public void Init()
	{
		var directories = new DirectoryInfo(Application.persistentDataPath).GetDirectories();
		foreach (var directory in directories)
		{
			var editable = File.Exists(Path.Combine(directory.FullName, ".editable"));
			if (editable)
			{
				var title = SaveFile.OpenFile(Path.Combine(directory.FullName, SaveFile.metaFilename)).meta.title;
				var newFileItem = new FileItem {name = title, guid = directory.Name};
				
				var filenameListItem = Instantiate(filenameItemPrefab);
				filenameListItem.transform.SetParent(fileList, false);
				filenameListItem.GetComponentInChildren<Text>().text = newFileItem.name;
				newFileItem.listItem = filenameListItem;
				
				files.Add(newFileItem);
			}
		}
	}

	void Update ()
	{
		for (var i = 0; i < files.Count; i++)
		{
			var file = files[i];
			var listItem = file.listItem;

			if (RectTransformUtility.RectangleContainsScreenPoint(listItem.GetComponent<RectTransform>(), Input.mousePosition))
			{
				listItem.GetComponent<Image>().color = new Color(210 / 255f, 210 / 255f, 210 / 255f);
			}
			else
			{
				listItem.GetComponentInChildren<Text>().color = Color.black;
				listItem.GetComponent<Image>().color = new Color(239 / 255f, 239 / 255f, 239 / 255f);
			}

			if (i == selectedIndex)
			{
				listItem.GetComponent<Image>().color = new Color(210 / 255f, 210 / 255f, 210 / 255f);
				listItem.GetComponentInChildren<Text>().color = Color.red;
			}

			if (RectTransformUtility.RectangleContainsScreenPoint(listItem.GetComponent<RectTransform>(), Input.mousePosition)
				&& Input.GetMouseButtonDown(0))
			{
				SetIndex(i);

				deleteButton.interactable = true;
				renameButton.interactable = true;
				openButton.interactable = true;
			}
		}
	}

	public void NewStart()
	{
		var newFileItem = new FileItem {name = "New File", guid = Guid.NewGuid().ToString()};
		var filenameListItem = Instantiate(filenameItemPrefab);
		filenameListItem.transform.SetParent(fileList, false);
		filenameListItem.GetComponentInChildren<Text>().text = newFileItem.name;
		newFileItem.listItem = filenameListItem;
		files.Add(newFileItem);
		SetIndex(files.Count - 1);

		isNew = true;
		RenameStart();
	}

	public void NewStop(string title)
	{
		SetIndex(files.Count - 1);
	}

	public void RenameStart()
	{
		if (selectedIndex != -1)
		{
			var label = files[selectedIndex].listItem.GetComponentInChildren<Text>();
			var input = files[selectedIndex].listItem.GetComponentInChildren<InputField>(true);

			label.gameObject.SetActive(false);
			input.gameObject.SetActive(true);
			input.text = label.text;
			input.Select();
			input.onEndEdit.AddListener(RenameStop);
		}
	}

	public void RenameStop(string newTitle)
	{
		if (isNew)
		{
			NewStop(newTitle);

			isNew = false;
		}
		else if (selectedIndex != -1)
		{
			var path = Path.Combine(Application.persistentDataPath, files[selectedIndex].guid);
			var file = SaveFile.OpenFile(Path.Combine(path, SaveFile.metaFilename));
			file.meta.title = newTitle;
		}

		var label = files[selectedIndex].listItem.GetComponentInChildren<Text>(true);
		var input = files[selectedIndex].listItem.GetComponentInChildren<InputField>();

		label.gameObject.SetActive(true);
		input.gameObject.SetActive(false);
		label.text = newTitle;
		files[selectedIndex].name = newTitle;
	}

	public void Delete()
	{
		if (selectedIndex != -1)
		{
			var path = Path.Combine(Application.persistentDataPath, files[selectedIndex].guid);
			Directory.Delete(path);
		}
	}

	public void Answer()
	{
		if (answerGuid != "")
		{
			answered = true;
		}
	}

	public void SetIndex(int i)
	{
		var file = files[i];
		chosenFile.text = "Chosen file: " + file.name;
		answerGuid = file.guid;
		selectedIndex = i;
	}
}
