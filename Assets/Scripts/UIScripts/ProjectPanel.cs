using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

//TODO(Simon): Consider renaming to project panel
public class ProjectPanel : MonoBehaviour
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
	public Texture logo;

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
				var filenameListItem = Instantiate(filenameItemPrefab);

				try
				{
					var meta = SaveFile.OpenFile(directory.FullName).meta;
					string title;
					if (meta.version > SaveFile.VERSION)
					{
						title = $"Project version too high. Please update the Editor: {directory.Name}";
						filenameListItem.GetComponentInChildren<Text>().color = Color.red;
					}
					else
					{
						title = meta.title;
					}

					newFileItem = new FileItem { title = title, guid = directory.Name };

				}
				catch (FileNotFoundException e)
				{
					newFileItem = new FileItem { title = "<b>corrupted file: " + directory.Name + "</b>", guid = directory.Name };
					filenameListItem.GetComponentInChildren<Text>().color = Color.red;
				}
				catch (Exception e)
				{
					newFileItem = new FileItem { title = "<b>corrupted file: " + directory.Name + "</b>", guid = directory.Name };
					filenameListItem.GetComponentInChildren<Text>().color = Color.red;
					Debug.LogError(e);
				}

				filenameListItem.transform.SetParent(fileList, false);
				filenameListItem.GetComponentInChildren<Text>().text = newFileItem.title;
				newFileItem.listItem = filenameListItem;

				files.Add(newFileItem);
			}
		}
	}

	void Update()
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
		var newFileItem = new FileItem { title = "New File", guid = Guid.NewGuid().ToString() };
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

		var projectFolder = Path.Combine(Application.persistentDataPath, files[selectedIndex].guid);

		if (!Directory.Exists(projectFolder))
		{
			try
			{
				Directory.CreateDirectory(projectFolder);
				File.Create(Path.Combine(projectFolder, ".editable")).Close();
				Directory.CreateDirectory(Path.Combine(projectFolder, SaveFile.extraPath));
				Directory.CreateDirectory(Path.Combine(projectFolder, SaveFile.miniaturesPath));
			}
			catch (Exception e)
			{
				Toasts.AddToast(5, "Something went wrong while creating a new project");
				Debug.LogError(e.StackTrace);
			}

			var data = new SaveFileData
			{
				meta = new Metadata
				{
					version = SaveFile.VERSION,
					title = files[selectedIndex].title,
					description = "",
					guid = new Guid(files[selectedIndex].guid),
				}
			};

			SaveFile.WriteFile(projectFolder, data);
		}
		else
		{
			Debug.LogError("Guid collision");
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

			var data = SaveFile.OpenFile(Path.Combine(path, SaveFile.metaFilename));
			data.meta.title = newTitle;
			SaveFile.WriteFile(data);
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
			thumb.texture = logo;
		}
	}
}
