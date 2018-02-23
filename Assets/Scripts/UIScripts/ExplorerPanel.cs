using System.Collections;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ExplorerPanel : MonoBehaviour
{

    public bool answered, sortByDate;
    public string filePath, currentDirectory, osType;

    public Text CurrentPath;
    public UnityEngine.UI.Button UpButton;
    public ScrollRect DirectoryContent;
    public GameObject filenameIconItemPrefab;
    public Sprite iconDirectory, iconFile, iconDrive, iconArrowUp;
    public GameObject DirUpItem;


    private FileInfo[] files;
    private DirectoryInfo[] directories;
    private string[] drives;
    private string searchPattern;

    private class ExplorerEntry
    {
        public string fullPath, name, strDate;
        public DateTime date;
        public Sprite sprite;
        public GameObject filenameIconItem;
    }

    private List<ExplorerEntry> explorer;

    // Use this for initialization
    void Start()
    {
        Init();
    }

    void Init(string searchPattern = "*")
    {
        answered = false;
        osType = Environment.OSVersion.Platform.ToString();
        currentDirectory = Directory.GetCurrentDirectory();
        this.searchPattern = searchPattern;

        UpdateDir();
    }

    public void dirUp()
    {
        try
        {
            currentDirectory = Directory.GetParent(currentDirectory).ToString();
            UpdateDir();
        }
        catch (NullReferenceException e)
        {
            if (osType == "Win32NT") // Is that the only string for Windows?
            {
                Debug.Log("Attempting to change disk");
                SelectDisk();
            }
            else
            {
                Debug.LogError("This is the root of the disk");
            }
        }
    }

    void UpdateDir()
    {
        var dirinfo = new DirectoryInfo(currentDirectory);
        files = dirinfo.GetFiles(this.searchPattern);
        directories = dirinfo.GetDirectories();
        CurrentPath.text = currentDirectory;

        if (sortByDate == true)
        {
            Array.Sort(files, (x, y) => { return x.LastWriteTime.CompareTo(y.LastWriteTime); });
            Array.Sort(directories, (x, y) => { return x.LastWriteTime.CompareTo(y.LastWriteTime); });
        }

        ClearItems();

        ExplorerEntry dirUpEntry = new ExplorerEntry();
        dirUpEntry.name = "Dir Up";
        dirUpEntry.sprite = iconArrowUp;
        explorer.Add(dirUpEntry);

        // List Directories
        foreach (DirectoryInfo directory in directories)
        {
            ExplorerEntry entry = new ExplorerEntry();
            entry.name = directory.Name;
            entry.sprite = iconDirectory;
            entry.fullPath = directory.FullName;
            entry.date = directory.LastWriteTime;

            explorer.Add(entry);
        }
        foreach (FileInfo file in files)
        {
            ExplorerEntry entry = new ExplorerEntry();
            entry.name = file.Name;
            entry.sprite = iconFile;
            entry.fullPath = file.FullName;
            entry.date = file.LastWriteTime;

            explorer.Add(entry);
        }

        FillItems();

    }

    void ClearItems()
    {
        if (explorer != null)
        {
            foreach (var item in explorer)
            {
                Destroy(item.filenameIconItem);
            }
            explorer.Clear();
        }
        explorer = new List<ExplorerEntry>();
    }

    void FillItems()
    {
        for (int i = 0; i < explorer.Count; i++)
        {
            GameObject filenameIconItem = Instantiate(filenameIconItemPrefab);
            filenameIconItem.transform.SetParent(DirectoryContent.content, false);
            filenameIconItem.GetComponentsInChildren<Text>()[0].text = explorer[i].name;
            if (explorer[i].date != default(DateTime))
                filenameIconItem.GetComponentsInChildren<Text>()[1].text = explorer[i].date.ToString();
            else
            {
                if (sortByDate)
                    filenameIconItem.GetComponentsInChildren<Text>()[1].text = "Date  ↓";
                else
                    filenameIconItem.GetComponentsInChildren<Text>()[1].text = "Date";
            }
            filenameIconItem.GetComponentsInChildren<Image>()[1].sprite = explorer[i].sprite;
            explorer[i].filenameIconItem = filenameIconItem;
        }

        float scrollHeight = 0;

        // HACK(Lander): this expands the contet below the preview window
        // TODO(Lander): fix the content to be 0 at the start
        if (explorer.Count > 13)
        {
            scrollHeight = 32 * (explorer.Count - 13);
        }

        // set size
        DirectoryContent.content.sizeDelta = new Vector2(DirectoryContent.content.sizeDelta.x, scrollHeight);

        // scroll to top
        DirectoryContent.normalizedPosition = new Vector2(0, 1);
    }


    // Update is called once per frame
    void Update()
    {
        try
        {
            foreach (var entry in explorer)
            {
                if (entry.filenameIconItem != null)
                {
                    entry.filenameIconItem.GetComponent<Image>().color = new Color(255, 255, 255);
                    if (RectTransformUtility.RectangleContainsScreenPoint(entry.filenameIconItem.GetComponent<RectTransform>(), Input.mousePosition))
                    {
                        entry.filenameIconItem.GetComponent<Image>().color = new Color(210 / 255f, 210 / 255f, 210 / 255f);
                        if (Input.GetMouseButtonDown(0))
                        {
                            entry.filenameIconItem.GetComponent<Image>().color = Color.blue;
                            if (entry.sprite == iconDirectory)
                            {
                                DirectoryClick(entry.fullPath);
                            }
                            else if (entry.sprite == iconFile)
                            {
                                FileClick(entry.fullPath);
                            }
                            else if (entry.sprite == iconDrive)
                            {
                                DriveClick(entry.fullPath);
                            }
                            else if (entry.sprite == iconArrowUp)
                            {
                                RectTransform clickArea = entry.filenameIconItem.transform.Find("DateText").gameObject.GetComponent<RectTransform>();

                                if (RectTransformUtility.RectangleContainsScreenPoint(clickArea, Input.mousePosition))
                                {
                                    sortByDate = !sortByDate;
                                    UpdateDir();
                                }
                                else
                                {
                                    dirUp();
                                }
                            }
                        }

                    }
                }
            }
        }
        catch (Exception e)
        {

        }
    }


    void FileClick(string path)
    {
        answered = true;
        filePath = path;
    }
    void DirectoryClick(string path)
    {
        currentDirectory = path;
        UpdateDir();
    }

    void SelectDisk()
    {

        UpButton.enabled = false;
        ClearItems();

        drives = Directory.GetLogicalDrives();

        CurrentPath.text = "Select Drive";

        foreach (string drive in drives)
        {
            ExplorerEntry entry = new ExplorerEntry();
            entry.fullPath = drive;
            entry.name = drive;
            entry.sprite = iconDrive;
            explorer.Add(entry);
        }
        FillItems();

    }

    void DriveClick(string path)
    {
        currentDirectory = path;
        UpdateDir();
        UpButton.enabled = true;
    }
}
