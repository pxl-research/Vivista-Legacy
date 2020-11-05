using System.IO;
using System.Collections.Generic;
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
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
		public ExplorerPanelItem explorerPanelItem;
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

	public Button upButton;
	public ScrollRect directoryContent;
	public GameObject filenameIconItemPrefab;
	public Button sortDateButton;
	public Button sortNameButton;
	public Button sortSizeButton;
	public InputField currentPathField;
	public InputField filenameField;
	public Text title;
	public Text extension;
	public Button answerButton;
	public RawImage previewImage;
	public RectTransform previewHolder;

	public RectTransform sidebar;

	public Sprite iconDirectory, iconFile, iconDrive;

	private List<ExplorerEntry> selectedFiles = new List<ExplorerEntry>();
	private List<GameObject> sidebarItems = new List<GameObject>();

	private static string osType;
	private string currentDirectory;
	private FileSystemWatcher fileWatcher;
	private bool shouldUpdate;
	private List<ExplorerEntry> entries;
	private bool sortAscending = true;
	private SortedBy sortedBy = SortedBy.Name;
	private Coroutine imageLoadsInProgress;

	private float lastClickTime;
	private int lastClickIndex;

	private Queue<ExplorerPanelItem> inactiveExplorerPanelItems;
	private Queue<Texture> texturesToDestroy = new Queue<Texture>();

	private static Color normalColor = Color.white;
	private static Color selectedColor = new Color(216 / 255f, 216 / 255f, 216 / 255f);
	private float zoom;

	private string cookiePath;

	public void Update()
	{
		if (shouldUpdate)
		{
			UpdateDir();
			shouldUpdate = false;
		}

		while (texturesToDestroy.Count > 0)
		{
			var texture = texturesToDestroy.Dequeue();
			//NOTE(Simon): These three icons are assets, so do not destroy them
			if (texture != iconDirectory.texture 
				&& texture != iconFile.texture 
				&& texture != iconDirectory.texture)
			{
				Destroy(texture);
			}
		}
	}

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
		HidePreview();
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
		inactiveExplorerPanelItems = new Queue<ExplorerPanelItem>();

		answered = false;
		osType = Environment.OSVersion.Platform.ToString();
		sortNameButton.GetComponentInChildren<Text>().text = "Name ↓";

		FillSidebar();

		filenameField.onValueChanged.AddListener(_ => OnFilenameFieldChanged());
		currentPathField.onEndEdit.AddListener(_ => OnPathFieldChanged());
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
		ClearItems();

		if (fileWatcher == null)
		{
			fileWatcher = new FileSystemWatcher(currentDirectory);
			fileWatcher.Changed += OnFileEvent;
			fileWatcher.Created += OnFileEvent;
			fileWatcher.Deleted += OnFileEvent;
			fileWatcher.Renamed += OnFileEvent;
			fileWatcher.EnableRaisingEvents = true;
		}

		fileWatcher.Path = currentDirectory;

		var dirinfo = new DirectoryInfo(currentDirectory);
		var filteredFiles = new List<FileInfo>();
		var patterns = searchPattern.Split(';');
		filenameField.text = "";

		foreach (string p in patterns)
		{
			filteredFiles.AddRange(dirinfo.GetFiles(p));
		}

		var directories = dirinfo.GetDirectories();
		currentPathField.text = currentDirectory;

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

	private void OnFileEvent(object o, FileSystemEventArgs args)
	{
		shouldUpdate = true;
	}

	private void ClearItems()
	{
		if (imageLoadsInProgress != null)
		{
			StopCoroutine(imageLoadsInProgress);
		}

		foreach (var entry in entries)
		{
			if (entry.entryType == EntryType.File && IsImage(entry.extension))
			{
				texturesToDestroy.Enqueue(entry.explorerPanelItem.icon.texture);
			}
		}

		foreach (var item in entries)
		{
			inactiveExplorerPanelItems.Enqueue(item.explorerPanelItem);
			item.explorerPanelItem.gameObject.SetActive(false);
		}

		entries.Clear();
	}

	private void FillItems()
	{
		for (int i = 0; i < entries.Count; i++)
		{
			ExplorerPanelItem explorerPanelItem = null;
			if (inactiveExplorerPanelItems.Count > 0)
			{
				explorerPanelItem = inactiveExplorerPanelItems.Dequeue();
				explorerPanelItem.gameObject.SetActive(true);
			}

			if (explorerPanelItem == null)
			{
				explorerPanelItem = Instantiate(filenameIconItemPrefab).GetComponent<ExplorerPanelItem>();

				var it = explorerPanelItem;

				it.button.onClick.AddListener(() => OnItemClick(explorerPanelItem));
			}

			bool isFile = entries[i].entryType == EntryType.File;
			bool isDrive = entries[i].entryType == EntryType.Drive;

			explorerPanelItem.transform.SetParent(directoryContent.content, false);
			explorerPanelItem.transform.SetAsLastSibling();
			
			var item = explorerPanelItem.GetComponent<ExplorerPanelItem>();
			item.background.color = normalColor;
			item.filename.text = entries[i].name;
			item.date.text = isDrive ? "" : entries[i].date.ToString("dd/MM/yyyy");
			item.fileType.text = isFile ? PrettyPrintFileType(entries[i].extension) : "";
			item.fileSize.text = isFile ? PrettyPrintFileSize(entries[i].size) : "";
			//TODO(Simon): Get resolution
			item.fileResolution.text = "";
			item.icon.texture = entries[i].sprite.texture;
			item.explorerPanel = this;
			item.SetHeight(zoom);

			var texture = item.icon.texture;
			var newSize = MathHelper.ScaleRatio(new Vector2(texture.width, texture.height), item.iconHolder.rect.size);

			item.icon.rectTransform.sizeDelta = newSize;

			entries[i].explorerPanelItem = explorerPanelItem;
		}

		imageLoadsInProgress = StartCoroutine(LoadFileThumbnail(entries));

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

	private IEnumerator LoadFileThumbnail(List<ExplorerEntry> entries)
	{
		foreach (var entry in entries)
		{
			var icon = entry.explorerPanelItem.icon;

			if (entry.entryType == EntryType.File && IsImage(entry.extension))
			{
				//NOTE(Simon): Cancel any remaining request, if the previous coroutine was cancelled partway through
				using (var request = UnityWebRequestTexture.GetTexture("file://" + entry.fullPath, false))
				{
					yield return request.SendWebRequest();

					var texture = DownloadHandlerTexture.GetContent(request);
					var newSize = MathHelper.ScaleRatio(new Vector2(texture.width, texture.height), entry.explorerPanelItem.iconHolder.rect.size);

					yield return new WaitForEndOfFrame();
					TextureScale.Point(texture, (int) newSize.x, (int) newSize.y);

					icon.texture = texture;
					icon.color = Color.white;
					icon.rectTransform.sizeDelta = newSize;
				}
			}
		}
	}

	public IEnumerator ShowPreview(int index)
	{
		var entry = entries[index - inactiveExplorerPanelItems.Count];
		if (entry.entryType == EntryType.File && IsImage(entry.extension))
		{
			texturesToDestroy.Enqueue(previewImage.texture);

			using (var request = UnityWebRequestTexture.GetTexture("file://" + entry.fullPath, false))
			{
				yield return request.SendWebRequest();

				var texture = DownloadHandlerTexture.GetContent(request);
				var newSize = MathHelper.ScaleRatio(new Vector2(texture.width, texture.height), previewHolder.rect.size);

				TextureScale.Point(texture, (int) newSize.x, (int) newSize.y);

				texturesToDestroy.Enqueue(previewImage.texture);
				previewImage.texture = texture;
				previewImage.color = Color.white;
				previewImage.rectTransform.sizeDelta = newSize;

				previewHolder.gameObject.SetActive(true);
			}
		}
	}

	public void HidePreview()
	{
		previewHolder.gameObject.SetActive(false);
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

	public void OnItemClick(ExplorerPanelItem item)
	{
		int index = EntryIndexForGO(item);
		var entry = entries[index];

		bool controlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
		bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftShift);
		bool doubleclick = lastClickIndex == index && (Time.unscaledTime - lastClickTime) < 0.5f;

		bool resetDoubleClick = false;

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

			resetDoubleClick = true;
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

		lastClickTime = resetDoubleClick ? 0 : Time.unscaledTime;
		lastClickIndex = resetDoubleClick ? -1 : index;
	}

	public void OnFilenameFieldChanged()
	{
		foreach (var file in selectedFiles)
		{
			file.explorerPanelItem.GetComponent<Image>().color = normalColor;
		}

		selectedFiles.Clear();
	}

	public void OnPathFieldChanged()
	{
		var newPath = currentPathField.text;

		newPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(newPath));

		if (Directory.Exists(newPath))
		{
			currentDirectory = newPath;
			UpdateDir();
		}
		else
		{
			currentPathField.text = currentDirectory;
		}
	}

	public void OnZoomStep(float value)
	{
		zoom = value;
		for (int i = 0; i < entries.Count; i++)
		{
			entries[i].explorerPanelItem.SetHeight(value);
		}
	}

	public void OnZoomEnd()
	{
		if (imageLoadsInProgress != null)
		{
			StopCoroutine(imageLoadsInProgress);
		}

		imageLoadsInProgress = StartCoroutine(LoadFileThumbnail(entries));
	}

	private int EntryIndexForGO(ExplorerPanelItem item)
	{
		for (int i = 0; i < entries.Count; i++)
		{
			if (entries[i].explorerPanelItem == item)
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

	public static bool IsImage(string extension)
	{
		var types = new List<string> {".jpg", ".jpeg", ".bmp", ".png"};
		return types.Contains(extension.ToLowerInvariant());
	}

	public void ResetSortButtonLabels()
	{
		sortNameButton.GetComponentInChildren<Text>().text = "Name";
		sortDateButton.GetComponentInChildren<Text>().text = "Date";
		sortSizeButton.GetComponentInChildren<Text>().text = "Type";
	}

}
