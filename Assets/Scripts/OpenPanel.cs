using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class OpenPanel : MonoBehaviour 
{
	public struct FileItem
	{
		public string guid;
		public string name;
		public GameObject listItem;
	}

	public RectTransform fileList;
	public Text chosenFile;

	public GameObject filenameItemPrefab;

	public bool answered;
	public string answerGuid;

	private List<FileItem> files = new List<FileItem>();
	private int selectedIndex = -1;

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
				listItem.GetComponentInChildren<Text>().color = Color.red;
				listItem.GetComponent<Image>().color = new Color(210 / 255f, 210 / 255f, 210 / 255f);
			}
			else if (i == selectedIndex)
			{
				listItem.GetComponentInChildren<Text>().color = Color.red;
			}
			else
			{
				listItem.GetComponentInChildren<Text>().color = Color.black;
				listItem.GetComponent<Image>().color = new Color(239 / 255f, 239 / 255f, 239 / 255f);
			}

			if (RectTransformUtility.RectangleContainsScreenPoint(listItem.GetComponent<RectTransform>(), Input.mousePosition)
				&& Input.GetMouseButtonDown(0))
			{
				chosenFile.text = "Chosen file: " + file.name;
				answerGuid = file.guid;
				selectedIndex = i;
			}
		}
	}

	public void Answer()
	{
		if (answerGuid != "")
		{
			answered = true;
		}
	}
}
