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

	public bool allowCancel => explorerPanel == null;
	private bool exporting;
	private bool done;
	private float progress;

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

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				Destroy(explorerPanel.gameObject);
				exportButton.interactable = true;
			}
		}

		if (exporting)
		{
			progressBar.SetProgress(progress);
		}

		if (done)
		{
			Destroy(gameObject);
			Editor.Instance.editorState = EditorState.Active;
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

		explorerPanel = Instantiate(UIPanels.Instance.explorerPanel, Canvass.main.transform, false).GetComponent<ExplorerPanel>();
		explorerPanel.InitSaveAs("", ".zip", "*.zip", "Enter a file name");

		exportButton.interactable = false;
	}

	void OnExportStart(string destFile, string projectPath, ExportMode mode)
	{
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		exporting = true;
		var files = new List<string>();
		long totalFileSize = 0;;
		long totalWritten = 0;

		files.AddRange(Directory.GetFiles(Path.Combine(projectPath, SaveFile.extraPath)));
		files.AddRange(Directory.GetFiles(Path.Combine(projectPath, SaveFile.miniaturesPath)));
		files.Add(Path.Combine(projectPath, SaveFile.metaFilename));
		files.Add(Path.Combine(projectPath, SaveFile.tagsFilename));

		//NOTE(Simon): If mode is "full", also include the base video and thumb image
		if (mode.HasFlag(ExportMode.Full))
		{
			files.Add(Path.Combine(projectPath, SaveFile.thumbFilename));
			files.Add(Path.Combine(projectPath, SaveFile.videoFilename));
		}

		//NOTE(Simon): If mode is "editable", also include the editable file
		if (mode.HasFlag(ExportMode.AllowEdit))
		{
			files.Add(Path.Combine(projectPath, SaveFile.editableFilename));
		}

		//NOTE(Simon): If zip exists, delete the original, so we don't keep old archive entries around
		File.Delete(destFile);

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
						progress = (float)totalWritten / totalFileSize;
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
