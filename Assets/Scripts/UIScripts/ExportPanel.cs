using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.IO.Compression;
using System.Threading;

[Flags]
public enum ExportMode
{
	None,
	Full,
	AllowEdit
}

public class ExportPanel : MonoBehaviour
{
	public GameObject explorerPanelPrefab;

	public Toggle fullToggle;
	public Toggle minimalToggle;
	public Toggle allowEdit;
	public Text fullText;
	public Text minimalText;
	public Text editableText;
	public Text notEditableText;
	public Button exportButton;
	public ProgressBar progressBar;
	public Text unsavedChangesWarning;

	private ExplorerPanel explorerPanel;

	private ExportMode exportMode;

	private Guid projectGuid;

	private bool exporting;
	private bool done;
	private long totalFileSize;
	private long totalWritten;

	void Start()
	{
		allowEdit.onValueChanged.AddListener(OnSetEditable);
		fullToggle.onValueChanged.AddListener(OnSetFullMinimal);
		minimalToggle.onValueChanged.AddListener(OnSetFullMinimal);
		exportButton.onClick.AddListener(OnExportInit);

		OnSetFullMinimal();
		OnSetEditable(true);

		unsavedChangesWarning.enabled = UnsavedChangesTracker.Instance.unsavedChanges;
	}

	public void Init(Guid projectGuid)
	{
		this.projectGuid = projectGuid;
	}

	private void Update()
	{
		if (explorerPanel != null)
		{
			if (explorerPanel.answered)
			{
				string answer = explorerPanel.answerPath;
				var projectFolder = Path.Combine(Application.persistentDataPath, projectGuid.ToString());
				new Thread(() => OnExportStart(answer, projectFolder, exportMode)).Start();
				Destroy(explorerPanel.gameObject);
			}
		}

		if (exporting)
		{
			float progress = (float)totalWritten / totalFileSize;
			
			progressBar.SetProgress(progress);
		}

		if (done)
		{
			Destroy(gameObject);
			Canvass.modalBackground.SetActive(false);
		}
	}

	void OnExportInit()
	{
		if (UnsavedChangesTracker.Instance.unsavedChanges)
		{
			if (!Editor.Instance.SaveToFile())
			{
				Toasts.AddToast(5, "File save failed, try again.");
			}
		}

		explorerPanel = Instantiate(explorerPanelPrefab, Canvass.main.transform, false).GetComponent<ExplorerPanel>();
		explorerPanel.InitSaveAs("", ".zip", "*.zip", "Enter a file name");

		exportButton.interactable = false;
	}

	void OnExportStart(string destFile, string projectPath, ExportMode mode)
	{
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		exporting = true;
		var files = new List<string>();

		files.AddRange(Directory.GetFiles(Path.Combine(projectPath, "extra")));
		files.AddRange(Directory.GetFiles(Path.Combine(projectPath, "areaMiniatures")));
		files.Add(Path.Combine(projectPath, "meta.json"));
		files.Add(Path.Combine(projectPath, "tags.json"));

		//NOTE(Simon): If mode is "full", also include the base video and thumb image
		if (mode.HasFlag(ExportMode.Full))
		{
			files.Add(Path.Combine(projectPath, "thumb.jpg"));
			files.Add(Path.Combine(projectPath, "main.mp4"));
		}

		//NOTE(Simon): If mode is "editable", also include the editable file
		if (mode.HasFlag(ExportMode.AllowEdit))
		{
			files.Add(Path.Combine(projectPath, ".editable"));
		}

		foreach (var file in files)
		{
			totalFileSize += new FileInfo(file).Length;
		}

		using (var dest = new ZipArchive(File.OpenWrite(destFile), ZipArchiveMode.Create))
		{
			foreach (string file in files)
			{
				string filenameInZip = file.Substring(projectPath.Length + 1);

				var entry = dest.CreateEntry(filenameInZip, System.IO.Compression.CompressionLevel.NoCompression);

				using (var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read))
				using (var entryStream = entry.Open())
				{
					//NOTE(Simon): 80kB is the buffer size used in .NET's CopyTo()
					var buffer = new byte[80 * 1024];
					int read;
					do
					{
						read = stream.Read(buffer, 0, buffer.Length);
						entryStream.Write(buffer, 0, read);
						totalWritten += read;
					} while (read > 0);
				}
			}
		}

		stopwatch.Stop();
		Debug.Log(stopwatch.Elapsed.TotalSeconds + "s");
		Debug.Log(totalWritten / stopwatch.Elapsed.TotalSeconds / 1024 / 1024 + "MB/s");

		OnExportEnd(destFile);
	}

	void OnExportEnd(string destFile)
	{
		exporting = false;

		ExplorerHelper.ShowPathInExplorer(destFile);

		done = true;
	}

	void OnSetEditable(bool editable)
	{
		if (editable)
		{
			exportMode |= ExportMode.AllowEdit;
			editableText.enabled = true;
			notEditableText.enabled = false;
		}
		else
		{
			exportMode &= ~ExportMode.AllowEdit;
			editableText.enabled = false;
			notEditableText.enabled = true;
		}
	}

	void OnSetFullMinimal(bool _ = false)
	{

		if (fullToggle.isOn)
		{
			fullText.enabled = true;
			minimalText.enabled = false;
			allowEdit.interactable = true;
			exportMode |= ExportMode.Full;
		}
		else if (minimalToggle.isOn)
		{
			fullText.enabled = false;
			minimalText.enabled = true;
			allowEdit.isOn = true;
			allowEdit.interactable = false;
			exportMode &= ~ExportMode.Full;
		}
		else
		{
			fullText.enabled = false;
			minimalText.enabled = false;
			allowEdit.interactable = true;
			exportMode |= ExportMode.Full;
		}
	}
}
