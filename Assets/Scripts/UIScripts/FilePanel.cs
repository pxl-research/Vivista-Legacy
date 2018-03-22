using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FilePanel : MonoBehaviour 
{
	public class FileItem
	{
		public string guid;
		public string title;
		public GameObject listItem;
	}

	public Text titleSave;
	public Text titleOpen;

	public ScrollRect scrollRect;
	public RectTransform fileList;
	public Text chosenFile;
	public RawImage thumb;

	public Button newButton;
	public Button renameButton;
	public Button deleteButton;
	public Button openButton;
	public Button saveButton;

	public GameObject filenameItemPrefab;

	public bool answered;
	public string answerGuid;
	public string answerTitle;

	private List<FileItem> files = new List<FileItem>();
	private int selectedIndex = -1;
	private bool isNew;
	private bool isSaving;

	private int lastClickIndex;
	private float lastClickDelta;

	public void Init(bool isSaveFileDialog)
	{
		//NOTE(Simon): Window setup. Display controls belonging to either open or save window.
		{
			if (isSaveFileDialog)
			{
				isSaving = true;
				titleOpen.gameObject.SetActive(false);
				openButton.gameObject.SetActive(false);
			}
			else
			{
				titleSave.gameObject.SetActive(false);
				saveButton.gameObject.SetActive(false);
			}
		}

		SetIndex(-1);

		var directories = new DirectoryInfo(Application.persistentDataPath).GetDirectories();
		foreach (var directory in directories)
		{
			var editable = File.Exists(Path.Combine(directory.FullName, ".editable"));
			if (editable)
			{
				FileItem newFileItem;

				try
				{
					string title = SaveFile.OpenFile(Path.Combine(directory.FullName, SaveFile.metaFilename)).meta.title;
					newFileItem = new FileItem { title = title, guid = directory.Name };
				}
				catch (Exception e)
				{
					newFileItem = new FileItem { title = "corrupted file: " + directory.Name, guid = directory.Name };
					Debug.Log(e);
				}

				var filenameListItem = Instantiate(filenameItemPrefab);
				filenameListItem.transform.SetParent(fileList, false);
				filenameListItem.GetComponentInChildren<Text>().text = newFileItem.title;
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
			}

			if (RectTransformUtility.RectangleContainsScreenPoint(listItem.GetComponent<RectTransform>(), Input.mousePosition)
				&& Input.GetMouseButtonDown(0))
			{
				if (lastClickIndex == i && lastClickDelta < .5)
				{
					Answer();
				}

				SetIndex(i);
				lastClickIndex = i;
				lastClickDelta = 0;
			}
		}
		lastClickDelta += Time.deltaTime;
	}

	public void NewStart()
	{
		var newFileItem = new FileItem {title = "New File", guid = Guid.NewGuid().ToString()};
		var filenameListItem = Instantiate(filenameItemPrefab);
		filenameListItem.transform.SetParent(fileList, false);
		filenameListItem.GetComponentInChildren<Text>().text = newFileItem.title;
		newFileItem.listItem = filenameListItem;
		files.Add(newFileItem);
		SetIndex(files.Count - 1);

		Canvas.ForceUpdateCanvases();
		scrollRect.verticalNormalizedPosition = 0.0f;

		isNew = true;
		RenameStart();
	}

	public void NewStop(string title)
	{
		SetIndex(files.Count - 1);

		var path = Path.Combine(Application.persistentDataPath, files[selectedIndex].guid);

		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
			File.Create(Path.Combine(path, ".editable")).Close();
			
			var meta = new Metadata
			{
				title = files[selectedIndex].title,
				description = "",
				guid = new Guid(files[selectedIndex].guid),
			};

			var sb = new StringBuilder();
			sb.Append("uuid:")
				.Append(meta.guid)
				.Append(",\n");

			sb.Append("title:")
				.Append(meta.title)
				.Append(",\n");

			sb.Append("description:")
				.Append(meta.description)
				.Append(",\n");

			sb.Append("perspective:")
				.Append("0")
				.Append(",\n");

			sb.Append("length:")
				.Append("0")
				.Append(",\n");

			sb.Append("[[]]");

			try
			{
				using (var metaFile = File.CreateText(Path.Combine(path, SaveFile.metaFilename)))
				{
					metaFile.WriteLine(sb.ToString());
				}
			}
			catch(Exception e)
			{
				Debug.Log(e.ToString());
			}
		}
		else
		{
			Debug.LogError("The hell you doin' boy");
		}
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
		var label = files[selectedIndex].listItem.GetComponentInChildren<Text>(true);
		var input = files[selectedIndex].listItem.GetComponentInChildren<InputField>();
		input.onEndEdit.RemoveListener(RenameStop);

		label.gameObject.SetActive(true);
		input.gameObject.SetActive(false);
		label.text = newTitle;
		files[selectedIndex].title = newTitle;

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

			var sb = new StringBuilder();

			sb.Append("uuid:")
				.Append(file.meta.guid)
				.Append(",\n");

			sb.Append("title:")
				.Append(file.meta.title)
				.Append(",\n");

			sb.Append("description:")
				.Append(file.meta.description)
				.Append(",\n");

			sb.Append("perspective:")
				.Append(file.meta.perspective)
				.Append(",\n");

			sb.Append("length:")
				.Append(file.meta.length)
				.Append(",\n");

			//NOTE(Kristof): Add the interaction points to the string
			sb.Append("[");
			if (file.points.Count > 0)
			{
				foreach (var point in file.points)
				{
					sb.Append(JsonUtility.ToJson(point, true));
					sb.Append(",");
				}

				sb.Remove(sb.Length - 1, 1);
			}
			else
			{
				sb.Append("[]");
			}

			sb.Append("]");

			try
			{
				string jsonname = Path.Combine(path, SaveFile.metaFilename);
				using (var renamedFile = File.CreateText(jsonname))
				{
					renamedFile.Write(sb.ToString());
				}
			}
			catch (Exception e)
			{
				Debug.Log(e.ToString());
			}
		}
	}

	public void Delete()
	{
		if (selectedIndex != -1)
		{
			var file = files[selectedIndex];
			var path = Path.Combine(Application.persistentDataPath, file.guid);
			Directory.Delete(path, true);

			Destroy(file.listItem);
			files.RemoveAt(selectedIndex);

			SetIndex(selectedIndex);
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
		i = Mathf.Clamp(i, 0, files.Count - 1);

		//NOTE(Simon): If last item was removed, and list is now empty
		if (files.Count == 0)
		{
			deleteButton.interactable = false;
			renameButton.interactable = false;
			openButton.interactable = false;
			saveButton.interactable = false;
			chosenFile.text = isSaving ? "Save as: <none>" : "Chosen file: <none>";

			return;
		}

		var file = files[i];
		if (isSaving)
		{
			chosenFile.text = "Save as: " + file.title;
		}
		else
		{
			chosenFile.text = "Chosen file: " + file.title;
		}

		answerGuid = file.guid;
		answerTitle = file.title;

		selectedIndex = i;

		deleteButton.interactable = true;
		renameButton.interactable = true;
		openButton.interactable = true;
		saveButton.interactable = true;

		var thumbPath = Path.Combine(Application.persistentDataPath, Path.Combine(file.guid, SaveFile.thumbFilename));
		if (File.Exists(thumbPath))
		{
			var data = File.ReadAllBytes(thumbPath);
			var tex = new Texture2D(1, 1);
			tex.LoadImage(data);
			thumb.texture = tex;
		}
		else
		{
			thumb.texture = Texture2D.whiteTexture;
		}
	}
}
