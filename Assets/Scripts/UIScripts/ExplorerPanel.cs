using System.IO;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExplorerPanel : MonoBehaviour
{
	private class ExplorerEntry
	{
		public string fullPath;
		public string name;
		public bool selected;
		public DateTime date;
		public Sprite sprite;
		public GameObject explorerPanelItem;
		public EntryType entryType;
	}

	private enum EntryType
	{
		File,
		Directory,
		Drive
	}

	public enum SelectionMode
	{
		File,
		Directory
	}

	public bool answered;
	public string answerPath;
	public List<string> answerPaths;
	private string searchPattern;
	private bool multiSelect;
	private SelectionMode selectionMode;

	public InputField currentPath;
	public Button upButton;
	public ScrollRect directoryContent;
	public GameObject filenameIconItemPrefab;
	public Button sortDateButton;
	public Button sortNameButton;
	public InputField filenameField;
	public Text title;

	public Sprite iconDirectory, iconFile, iconDrive;

	private List<ExplorerEntry> selectedFiles = new List<ExplorerEntry>();

	private static string osType;
	private string currentDirectory;
	private List<ExplorerEntry> entries;
	private bool sortByDate;
	private bool sortByName = true;
	private bool sortAscending = true;

	private float lastClickTime;
	private int lastClickIndex;
	private GameObject lastHoverObject;

	private Queue<GameObject> inactiveExplorerPanelItems;

	private static Color normalColor = Color.white;
	private static Color selectedColor = new Color(210 / 255f, 210 / 255f, 210 / 255f);
	private static Color highlightColor = new Color(225 / 255f, 225 / 255f, 225 / 255f);

	private string cookiePath;

	//TODO(Simon): Show disks on side (on windows)

	//NOTE(Simon): search pattern should be in default windows wildcard style: e.g. "*.zip" for zipfiles, "a.*" for all filetypes with name "a"
	//NOTE(Simon): If you provide "" as startDirectory, startDirectory will default to the last location from where a file was selected
	public void Init(string startDirectory = "C:\\", string searchPattern = "*", string title = "Select file", SelectionMode mode = SelectionMode.File, bool multiSelect = false)
	{
		cookiePath = Path.Combine(Application.persistentDataPath, ".explorer");

		startDirectory = Environment.ExpandEnvironmentVariables(startDirectory);
		if (startDirectory == "")
		{
			if (File.Exists(cookiePath))
			{
				startDirectory = File.ReadAllText(cookiePath);
			}
			
			//TODO(SImon): Is this bugged? Debugger does weird stuff after stepping over this line.
			if (!Directory.Exists(startDirectory))
			{
				startDirectory = "C:\\";
			}
		}

		currentDirectory = new DirectoryInfo(startDirectory).FullName;

		entries = new List<ExplorerEntry>();
		inactiveExplorerPanelItems = new Queue<GameObject>();

		selectionMode = mode;
		this.searchPattern = searchPattern;
		this.title.text = title;
		this.multiSelect = multiSelect;

		answered = false;
		osType = Environment.OSVersion.Platform.ToString();
		sortNameButton.GetComponentInChildren<Text>().text = "Name ↓";
		
		UpdateDir();
	}

	public void DirUp()
	{
		var parent = Directory.GetParent(currentDirectory);
		//TODO(Simon): Test with other OSes
		if (parent == null)
		{
			if (osType == "Win32NT")
			{
				SelectDisk();
			}
			return;
		}

		currentDirectory = parent.ToString();
		UpdateDir();
	}

	public void OnSortNameClick()
	{
		if (sortByName && sortAscending)
		{
			sortNameButton.GetComponentInChildren<Text>().text = "Name ↑";
			sortAscending = false;
		}
		else
		{
			sortDateButton.GetComponentInChildren<Text>().text = "Date";
			sortNameButton.GetComponentInChildren<Text>().text = "Name ↓";
			sortAscending = true;
			sortByName = true;
			sortByDate = false;
		}

		UpdateDir();
	}

	public void OnSortDateClick()
	{
		if (sortByDate && sortAscending)
		{
			sortDateButton.GetComponentInChildren<Text>().text = "Date ↑";
			sortAscending = false;
		}
		else
		{
			sortNameButton.GetComponentInChildren<Text>().text = "Name";
			sortDateButton.GetComponentInChildren<Text>().text = "Date ↓";
			sortAscending = true;
			sortByDate = true;
			sortByName = false;
		}
		UpdateDir();
	}
	
	private void UpdateDir()
	{
		var dirinfo = new DirectoryInfo(currentDirectory);
		var filteredFiles = new List<FileInfo>();
		var patterns = searchPattern.Split(';');
		filenameField.text = "";

		foreach (string p in patterns)
		{
			filteredFiles.AddRange(dirinfo.GetFiles(p));
		}

		var directories = dirinfo.GetDirectories();
		currentPath.text = currentDirectory;

		int direction = sortAscending ? 1 : -1;

		if (sortByName)
		{
			Array.Sort(directories, (x, y) => direction * String.Compare(x.Name, y.Name, StringComparison.Ordinal));
			filteredFiles.Sort((x, y) => direction * String.Compare(x.Name, y.Name, StringComparison.Ordinal));
		}
		if (sortByDate)
		{
			Array.Sort(directories, (x, y) => direction * x.LastWriteTime.CompareTo(y.LastWriteTime));
			filteredFiles.Sort((x, y) => direction * x.LastWriteTime.CompareTo(y.LastWriteTime));
		}

		ClearItems();

		foreach (var directory in directories)
		{
			var entry = new ExplorerEntry
			{
				name = directory.Name,
				sprite = iconDirectory,
				fullPath = directory.FullName,
				date = directory.LastWriteTime,
				entryType = EntryType.Directory
			};

			entries.Add(entry);
		}

		foreach (var file in filteredFiles)
		{
			var entry = new ExplorerEntry
			{
				name = file.Name,
				sprite = iconFile,
				fullPath = file.FullName,
				date = file.LastWriteTime,
				entryType = EntryType.File
			};

			entries.Add(entry);
		}

		FillItems();
	}

	private void ClearItems()
	{
		foreach (var item in entries)
		{
			inactiveExplorerPanelItems.Enqueue(item.explorerPanelItem);
			item.explorerPanelItem.SetActive(false);
		}

		entries.Clear();
	}

	private void FillItems()
	{
		for (int i = 0; i < entries.Count; i++)
		{
			GameObject explorerPanelItem = null;
			if (inactiveExplorerPanelItems.Count > 0)
			{
				explorerPanelItem = inactiveExplorerPanelItems.Dequeue();
				explorerPanelItem.SetActive(true);
			}

			if (explorerPanelItem == null)
			{
				explorerPanelItem = Instantiate(filenameIconItemPrefab);

				var trigger = explorerPanelItem.GetComponent<EventTrigger>();

				var enterTrigger = new EventTrigger.Entry {eventID = EventTriggerType.PointerEnter};
				enterTrigger.callback.AddListener(OnItemEnter);

				var exitTrigger = new EventTrigger.Entry {eventID = EventTriggerType.PointerExit};
				exitTrigger.callback.AddListener(OnItemExit);

				var clickTrigger = new EventTrigger.Entry {eventID = EventTriggerType.PointerClick};
				clickTrigger.callback.AddListener(OnItemClick);

				trigger.triggers.Add(enterTrigger);
				trigger.triggers.Add(exitTrigger);
				trigger.triggers.Add(clickTrigger);
			}

			explorerPanelItem.transform.SetParent(directoryContent.content, false);
			explorerPanelItem.transform.SetAsLastSibling();
			explorerPanelItem.GetComponentsInChildren<Text>()[0].text = entries[i].name;
			explorerPanelItem.GetComponentsInChildren<Text>()[1].text = entries[i].entryType == EntryType.Drive ? "" : entries[i].date.ToString("dd/MM/yyyy");
			explorerPanelItem.GetComponentsInChildren<Image>()[1].sprite = entries[i].sprite;

			entries[i].explorerPanelItem = explorerPanelItem;
		}

		// scroll to top
		Canvas.ForceUpdateCanvases();
		directoryContent.verticalNormalizedPosition = 1;
	}

	private void OnDirectoryClick(string path)
	{
		currentDirectory = path;
		UpdateDir();
	}

	private void SelectDisk()
	{
		upButton.enabled = false;

		var drives = Directory.GetLogicalDrives();

		currentPath.text = "Select Drive";

		ClearItems();
		foreach (string drive in drives)
		{
			entries.Add(new ExplorerEntry
			{
				fullPath = drive,
				name = drive,
				sprite = iconDrive,
				entryType = EntryType.Drive
			});
		}
		FillItems();
	}

	private void DriveClick(string path)
	{
		currentDirectory = path;
		UpdateDir();
		upButton.enabled = true;
	}

	private void Answer()
	{
		var pathList = new List<string>();
		foreach (var file in selectedFiles)
		{
			pathList.Add(file.fullPath);
		}

		answered = true;
		answerPath = pathList[0];
		answerPaths = pathList;
		File.WriteAllText(cookiePath, currentDirectory);
	}

	public void OpenButtonClicked()
	{
		if (selectedFiles.Count > 0)
		{
			bool error = false;
			foreach (var file in selectedFiles)
			{
				if (selectionMode == SelectionMode.File)
				{
					if (!File.Exists(file.fullPath))
					{
						error = true;
					}
				}
				else if (selectionMode == SelectionMode.Directory)
				{
					if (!Directory.Exists(file.fullPath))
					{
						error = true;
					}
				}
			}

			if (!error)
			{
				Answer();
			}
			else
			{
				Debug.LogError("File or Directory does not exist");
			}
		}
	}

	//NOTE(Simon): If this ever stops working, be sure to disable raycasting on child objects
	public void OnItemEnter(BaseEventData data)
	{
		var go = ((PointerEventData)data).pointerEnter;
		int index = EntryIndexForGO(go);
		var entry = entries[index];

		if (!entry.selected)
		{
			entry.explorerPanelItem.GetComponent<Image>().color = highlightColor;
		}
		lastHoverObject = go;
	}

	//NOTE(Simon): If this ever stops working, be sure to disable raycasting on child objects
	public void OnItemExit(BaseEventData data)
	{
		int index = EntryIndexForGO(lastHoverObject);
		var entry = entries[index];

		if (!entry.selected)
		{
			entry.explorerPanelItem.GetComponent<Image>().color = Color.white;
		}
	}

	//NOTE(Simon): If this ever stops working, be sure to disable raycasting on child objects
	public void OnItemClick(BaseEventData data)
	{
		var go = ((PointerEventData)data).pointerEnter;
		int index = EntryIndexForGO(go);
		var entry = entries[index];

		bool controlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
		bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftShift);
		bool doubleclick = lastClickIndex == index && (Time.unscaledTime - lastClickTime) < 0.5f;

		if (doubleclick)
		{
			if (entry.entryType == EntryType.File && selectionMode == SelectionMode.File)
			{
				Answer();
			}
			else if (entry.entryType == EntryType.Directory)
			{
				OnDirectoryClick(entry.fullPath);
			}
			else if (entry.entryType == EntryType.Drive)
			{
				DriveClick(entry.fullPath);
			}
		}
		else if (multiSelect && controlHeld)
		{
			if (selectedFiles.Contains(entry))
			{
				entry.selected = false;
				entry.explorerPanelItem.GetComponent<Image>().color = normalColor;
				selectedFiles.Remove(entry);
			}
			else
			{
				entry.selected = true;
				entry.explorerPanelItem.GetComponent<Image>().color = selectedColor;
				selectedFiles.Add(entry);
			}
		}
		else if (multiSelect && shiftHeld)
		{
			int minIndex = Mathf.Min(lastClickIndex, index);
			int maxIndex = Mathf.Max(lastClickIndex, index);
			int count = maxIndex - minIndex + 1;
			foreach (var file in selectedFiles)
			{
				file.selected = false;
				file.explorerPanelItem.GetComponent<Image>().color = normalColor;
			}
			selectedFiles.Clear();
			selectedFiles.AddRange(entries.GetRange(minIndex, count));
			foreach (var file in selectedFiles)
			{
				file.selected = true;
				file.explorerPanelItem.GetComponent<Image>().color = selectedColor;
			}
		}
		//NOTE(Simon): Plain single click
		else
		{
			foreach (var file in selectedFiles)
			{
				file.selected = false;
				file.explorerPanelItem.GetComponent<Image>().color = normalColor;
			}

			selectedFiles.Clear();

			if (entry.entryType == EntryType.File && selectionMode == SelectionMode.File
				|| entry.entryType == EntryType.Directory && selectionMode == SelectionMode.Directory)
			{
				entry.selected = true;
				selectedFiles.Add(entry);
				entry.explorerPanelItem.GetComponent<Image>().color = selectedColor;
			}
		}

		var fileNames = new StringBuilder();
		int i = 0;
		while (i < selectedFiles.Count && fileNames.Length + selectedFiles[i].name.Length < 16382)
		{
			var file = selectedFiles[i];
			fileNames.Append("\"");
			fileNames.Append(file.name);
			fileNames.Append("\" ");
			i++;
		}

		filenameField.text = fileNames.ToString();

		lastClickTime = Time.unscaledTime;
		lastClickIndex = index;
	}

	int EntryIndexForGO(GameObject go)
	{
		for (int i = 0; i < entries.Count; i++)
		{
			if (entries[i].explorerPanelItem == go)
			{
				return i;
			}
		}
		Assert.IsTrue(true, "Should not be able to get here");
		return -1;
	}
}
