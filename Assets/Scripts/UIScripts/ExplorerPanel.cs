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
		public DateTime date;
		public string extension;
		public long size;
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

	public enum ExplorerMode
	{
		Save,
		Open
	}

	public enum SortedBy
	{
		Name,
		Date,
		Type,
		Size,
		Resolution
	}

	public bool answered;
	public string answerPath;
	public List<string> answerPaths;
	private string searchPattern;
	private bool multiSelect;
	private SelectionMode selectionMode;
	private ExplorerMode explorerMode;

	public InputField currentPath;
	public Button upButton;
	public ScrollRect directoryContent;
	public GameObject filenameIconItemPrefab;
	public Button sortDateButton;
	public Button sortNameButton;
	public Button sortSizeButton;
	public InputField filenameField;
	public Text title;
	public Text extension;
	public Button answerButton;

	public RectTransform sidebar;

	public Sprite iconDirectory, iconFile, iconDrive;

	private List<ExplorerEntry> selectedFiles = new List<ExplorerEntry>();
	private List<GameObject> sidebarItems = new List<GameObject>();

	private static string osType;
	private string currentDirectory;
	private List<ExplorerEntry> entries;
	private bool sortAscending = true;
	private SortedBy sortedBy = SortedBy.Name;

	private float lastClickTime;
	private int lastClickIndex;
	private GameObject lastHoverObject;

	private Queue<GameObject> inactiveExplorerPanelItems;

	private static Color normalColor = Color.white;
	private static Color selectedColor = new Color(216 / 255f, 216 / 255f, 216 / 255f);

	private string cookiePath;

	//TODO(Simon): Show disks on side (on windows)

	//NOTE(Simon): search pattern should be in default windows wildcard style: e.g. "*.zip" for zipfiles, "a.*" for all filetypes with name "a"
	//NOTE(Simon): If you provide "" as startDirectory, startDirectory will default to the last location from where a file was selected
	public void Init(string startDirectory = "C:\\", string searchPattern = "*", string title = "Select file", SelectionMode mode = SelectionMode.File, bool multiSelect = false)
	{
		InitCommon(startDirectory);

		explorerMode = ExplorerMode.Open;

		selectionMode = mode;
		this.searchPattern = searchPattern;
		this.title.text = title;
		this.multiSelect = multiSelect;

		answerButton.GetComponentInChildren<Text>().text = "Open";

		UpdateDir();
	}

	public void InitSaveAs(string startDirectory = "C:\\", string defaultExtension = "", string searchPattern = "*", string title = "Select file")
	{
		InitCommon(startDirectory);

		explorerMode = ExplorerMode.Save;

		selectionMode = SelectionMode.File;
		this.searchPattern = searchPattern;
		this.title.text = title;
		this.multiSelect = false;

		answerButton.GetComponentInChildren<Text>().text = "Save As";
		EventSystem.current.SetSelectedGameObject(filenameField.gameObject);

		if (!String.IsNullOrEmpty(defaultExtension))
		{
			var rect = filenameField.GetComponent<RectTransform>();
			rect.anchorMax = new Vector2(extension.GetComponent<RectTransform>().anchorMin.x, rect.anchorMax.y);

			extension.gameObject.SetActive(true);
			extension.text = defaultExtension;
		}

		UpdateDir();
	}

	private void InitCommon(string startDirectory)
	{
		cookiePath = Path.Combine(Application.persistentDataPath, ".explorer");

		startDirectory = Environment.ExpandEnvironmentVariables(startDirectory);
		if (startDirectory == "")
		{
			if (File.Exists(cookiePath))
			{
				startDirectory = File.ReadAllText(cookiePath);
			}

			if (!Directory.Exists(startDirectory))
			{
				startDirectory = "C:\\";
			}
		}

		currentDirectory = new DirectoryInfo(startDirectory).FullName;

		entries = new List<ExplorerEntry>();
		inactiveExplorerPanelItems = new Queue<GameObject>();

		answered = false;
		osType = Environment.OSVersion.Platform.ToString();
		sortNameButton.GetComponentInChildren<Text>().text = "Name ↓";

		FillSidebar();

		filenameField.onValueChanged.AddListener(_ => OnFilenameFieldChanged());
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
		ResetSortButtonLabels();

		if (sortedBy == SortedBy.Name && sortAscending)
		{
			sortNameButton.GetComponentInChildren<Text>().text = "Name ↑";
			sortAscending = false;
		}
		else
		{
			sortNameButton.GetComponentInChildren<Text>().text = "Name ↓";
			sortAscending = true;
			sortedBy = SortedBy.Name;
		}

		UpdateDir();
	}

	public void OnSortDateClick()
	{
		ResetSortButtonLabels();


		if (sortedBy == SortedBy.Date && sortAscending)
		{
			sortDateButton.GetComponentInChildren<Text>().text = "Date ↑";
			sortAscending = false;
		}
		else
		{
			sortDateButton.GetComponentInChildren<Text>().text = "Date ↓";
			sortAscending = true;
			sortedBy = SortedBy.Date;
		}
		UpdateDir();
	}

	public void OnSortFileSizeClick()
	{
		ResetSortButtonLabels();

		if (sortedBy == SortedBy.Size && sortAscending)
		{
			sortSizeButton.GetComponentInChildren<Text>().text = "Size ↑";
			sortAscending = false;
		}
		else
		{
			sortSizeButton.GetComponentInChildren<Text>().text = "Size ↓";
			sortAscending = true;
			sortedBy = SortedBy.Size;
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

		if (sortedBy == SortedBy.Name)
		{
			Array.Sort(directories, (x, y) => direction * String.Compare(x.Name, y.Name, StringComparison.Ordinal));
			filteredFiles.Sort((x, y) => direction * String.Compare(x.Name, y.Name, StringComparison.Ordinal));
		}
		if (sortedBy == SortedBy.Date)
		{
			Array.Sort(directories, (x, y) => direction * x.LastWriteTime.CompareTo(y.LastWriteTime));
			filteredFiles.Sort((x, y) => direction * x.LastWriteTime.CompareTo(y.LastWriteTime));
		}
		if (sortedBy == SortedBy.Size)
		{
			filteredFiles.Sort((x, y) => direction * x.Length.CompareTo(y.Length));
		}

		ClearItems();

		foreach (var directory in directories)
		{
			if (directory.Attributes.HasFlag(FileAttributes.System) ||
				directory.Attributes.HasFlag(FileAttributes.Hidden))
			{
				continue;
			}

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
			if (file.Attributes.HasFlag(FileAttributes.System) ||
				file.Attributes.HasFlag(FileAttributes.Hidden))
			{
				continue;
			}

			var entry = new ExplorerEntry
			{
				name = file.Name,
				sprite = iconFile,
				fullPath = file.FullName,
				date = file.LastWriteTime,
				size = file.Length,
				extension = file.Extension,
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

				var button = explorerPanelItem.GetComponent<Button>();

				button.onClick.AddListener(() => OnItemClick(explorerPanelItem));
			}

			bool isFile = entries[i].entryType == EntryType.File;
			bool isDrive = entries[i].entryType == EntryType.Drive;

			var labels = explorerPanelItem.GetComponentsInChildren<Text>();
			explorerPanelItem.transform.SetParent(directoryContent.content, false);
			explorerPanelItem.transform.SetAsLastSibling();
			explorerPanelItem.GetComponent<Image>().color = normalColor;
			labels[0].text = entries[i].name;
			labels[1].text = isDrive ? "" : entries[i].date.ToString("dd/MM/yyyy");
			labels[2].text = isFile ? PrettyPrintFileType(entries[i].extension) : "";
			labels[3].text = isFile ? PrettyPrintFileSize(entries[i].size) : "";
			//TODO(Simon): Get resolution
			labels[4].text = "";
			explorerPanelItem.GetComponentsInChildren<Image>()[1].sprite = entries[i].sprite;

			entries[i].explorerPanelItem = explorerPanelItem;
		}

		Canvas.ForceUpdateCanvases();
		//NOTE(Simon): scroll to top
		directoryContent.verticalNormalizedPosition = 1;
	}

	private void FillSidebar()
	{
		sidebar.gameObject.SetActive(false);
		if (Application.platform == RuntimePlatform.WindowsEditor ||
			Application.platform == RuntimePlatform.WindowsPlayer)
		{
			sidebar.gameObject.SetActive(true);

			sidebarItems.Add(NewSidebarFolder("Desktop", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)));
			sidebarItems.Add(NewSidebarFolder("Documents", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)));
			sidebarItems.Add(NewSidebarFolder("Downloads", Environment.ExpandEnvironmentVariables("%userprofile%\\Downloads")));
			sidebarItems.Add(NewSidebarFolder("Music", Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)));
			sidebarItems.Add(NewSidebarFolder("Pictures", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)));
			sidebarItems.Add(NewSidebarFolder("Videos", Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)));

			sidebarItems.Add(NewSidebarItem(""));

			var drives = Directory.GetLogicalDrives();
			foreach (var drive in drives)
			{
				sidebarItems.Add(NewSidebarDrive(NativeCalls.GetDriveName(drive), drive));
			}
		}
	}

	private GameObject NewSidebarItem(string name)
	{
		var newItem = new GameObject("SidebarItem");
		newItem.transform.SetParent(sidebar.transform, false);

		var rect = newItem.AddComponent<RectTransform>();
		rect.sizeDelta = new Vector2(200, 25);

		var textGo = new GameObject("Name");
		textGo.transform.SetParent(newItem.transform, false);

		var textRect = textGo.AddComponent<RectTransform>();
		textRect.anchorMin = Vector2.zero;
		textRect.anchorMax = Vector2.one;
		textRect.offsetMin = new Vector2(10, 0);
		textRect.offsetMax = Vector2.zero;

		var text = textGo.AddComponent<Text>();
		text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
		text.color = Color.black;
		text.text = name;
		text.alignment = TextAnchor.MiddleLeft;

		//NOTE(Simon): If no name, assume it's a divider. So don't make it interactable
		if (!String.IsNullOrEmpty(name))
		{
			var button = newItem.gameObject.AddComponent<Button>();
			button.transition = Selectable.Transition.ColorTint;

			//NOTE(Simon): Prevents "selected" state, where you can't highlight a button by hovering.
			var nav = button.navigation;
			nav.mode = Navigation.Mode.None;
			button.navigation = nav;

			button.colors = new ColorBlock
			{
				normalColor = selectedColor,
				pressedColor = normalColor,
				selectedColor = selectedColor,
				highlightedColor = new Color(normalColor.r, normalColor.g, normalColor.b, .8f),
				colorMultiplier = 1f,
				fadeDuration = 0f
			};

			var image = newItem.gameObject.AddComponent<Image>();
			button.targetGraphic = image;
		}

		return newItem;
	}

	private GameObject NewSidebarFolder(string name, string path)
	{
		var item = NewSidebarItem(name);
		if (!String.IsNullOrEmpty(path))
		{
			var button = item.GetComponentInChildren<Button>();
			button.onClick.AddListener(() => OnDirectoryClick(path));
		}
		return item;
	}

	private GameObject NewSidebarDrive(string name, string path)
	{
		var item = NewSidebarItem(name);
		if (!String.IsNullOrEmpty(path))
		{
			var button = item.GetComponentInChildren<Button>();
			button.onClick.AddListener(() => OnDriveClick(path));
		}
		return item;
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

	private void OnDriveClick(string path)
	{
		currentDirectory = path;
		UpdateDir();
		upButton.enabled = true;
	}

	private void Answer()
	{
		answered = true;

		if (explorerMode == ExplorerMode.Open)
		{
			var pathList = new List<string>();
			foreach (var file in selectedFiles)
			{
				pathList.Add(file.fullPath);
			}

			answerPath = pathList[0];
			answerPaths = pathList;
		}
		else if (explorerMode == ExplorerMode.Save)
		{
			if (selectedFiles.Count > 0)
			{
				answerPath = selectedFiles[0].fullPath;
			}
			else
			{
				answerPath = Path.Combine(currentDirectory, filenameField.text + extension.text);
			}

			try
			{
				string _ = Path.GetFullPath(answerPath);
			}
			catch
			{
				//NOTE(Simon): If Path.GetFullPath() fails, it means the path is invalid. e.g. has banned characters, is an empty string, etc.
				answered = false;
			}
		}

		File.WriteAllText(cookiePath, currentDirectory);
	}

	public void OpenButtonClicked()
	{
		if (explorerMode == ExplorerMode.Open)
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
		else if (explorerMode == ExplorerMode.Save)
		{
			if (!String.IsNullOrWhiteSpace(filenameField.text))
			{
				Answer();
			}
		}
	}

	public void OnItemClick(GameObject go)
	{
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
				OnDriveClick(entry.fullPath);
			}
		}
		else if (multiSelect && controlHeld)
		{
			if (selectedFiles.Contains(entry))
			{
				entry.explorerPanelItem.GetComponent<Image>().color = normalColor;
				selectedFiles.Remove(entry);
			}
			else
			{
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
				file.explorerPanelItem.GetComponent<Image>().color = normalColor;
			}
			selectedFiles.Clear();
			selectedFiles.AddRange(entries.GetRange(minIndex, count));
			foreach (var file in selectedFiles)
			{
				file.explorerPanelItem.GetComponent<Image>().color = selectedColor;
			}
		}
		//NOTE(Simon): Plain single click
		else
		{
			foreach (var e in entries)
			{
				e.explorerPanelItem.GetComponent<Image>().color = normalColor;
			}

			selectedFiles.Clear();

			entry.explorerPanelItem.GetComponent<Image>().color = selectedColor;

			if (entry.entryType == EntryType.File && selectionMode == SelectionMode.File
				|| entry.entryType == EntryType.Directory && selectionMode == SelectionMode.Directory)
			{
				selectedFiles.Add(entry);
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

		filenameField.SetTextWithoutNotify(fileNames.ToString());

		lastClickTime = Time.unscaledTime;
		lastClickIndex = index;
	}

	public void OnFilenameFieldChanged()
	{
		foreach (var file in selectedFiles)
		{
			file.explorerPanelItem.GetComponent<Image>().color = normalColor;
		}

		selectedFiles.Clear();
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

	public static string PrettyPrintFileSize(long size)
	{
		long prevSize;
		//NOTE(Jeroen): We go through the loop at least once, so offset by -1
		int loops = -1;

		do
		{
			loops++;
			prevSize = size;
			size /= 1024;
		} while (size > 1);

		var names = new[] { "B", "kB", "MB", "GB" };

		return prevSize + names[loops];
	}

	public static string PrettyPrintFileType(string extension)
	{
		var types = new Dictionary<string, string>()
		{
			{".jpg", "Image" },
			{".jpeg", "Image" },
			{".bmp", "Image" },
			{".png", "Image" },
			{".mp4", "Video" },
			{".webm", "Video" },
			{".m4v", "Video" },
			{".mp3", "Audio" },
			{".wav", "Audio" },
			{".aif", "Audio" },
			{".ogg", "Audio" },
		};

		if (extension.Length <= 1)
		{
			return "";
		}

		var extensionLower = extension.ToLowerInvariant();

		string fileType;

		if (types.ContainsKey(extensionLower))
		{
			fileType = types[extensionLower];
		}
		else
		{
			fileType = extension.Substring(1);
		}

		return fileType;
	}

	public void ResetSortButtonLabels()
	{
		sortNameButton.GetComponentInChildren<Text>().text = "Name";
		sortDateButton.GetComponentInChildren<Text>().text = "Date";
		sortSizeButton.GetComponentInChildren<Text>().text = "Type";
	}

}
