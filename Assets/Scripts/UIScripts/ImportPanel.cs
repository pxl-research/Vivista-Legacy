using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class ImportPanel : MonoBehaviour 
{
	public bool answered;

	public Button importButton;
	public ProgressBar progressbar;

	private ExplorerPanel explorerPanel;

	private float progress;
	private bool unpacking;

	//NOTE(Simon): Cache persistentDataPath, because we need it in a non-main thread
	private string persistentDataPath;

	private void Start()
	{
		importButton.onClick.AddListener(OnStartImport);
		persistentDataPath = Application.persistentDataPath;
	}

	public void Update () 
	{
		if (explorerPanel != null)
		{
			if (explorerPanel.answered)
			{
				if (File.Exists(explorerPanel.answerPath))
				{
					new Thread(() => UnpackFile(explorerPanel.answerPath)).Start();
				}

				Destroy(explorerPanel.gameObject);
			}
		}

		if (unpacking)
		{
			progressbar.SetProgress(progress);
		}
	}

	public void OnStartImport()
	{
		explorerPanel = Instantiate(UIPanels.Instance.explorerPanel, Canvass.main.transform, false);
		explorerPanel.Init("%HOMEPATH%\\Downloads", "*.zip", "Select the zip-file you downloaded earlier");
	}

	//NOTE(Simon): First unpack to random destination. Then read savefile, and copy to correct folder (or overwrite if existing)
	public void UnpackFile(string zipPath)
	{
		unpacking = true;
		long bytesUnpacked = 0;
		long bytesToUnpack = 0;
		long bytesCopied = 0;
		long bytesToCopy = 0;

		string tempDestFolder = Path.Combine(persistentDataPath, Guid.NewGuid().ToString());

		using (var source = new ZipArchive(File.OpenRead(zipPath),ZipArchiveMode.Read))
		{
			foreach (var entry in source.Entries)
			{
				bytesToUnpack += entry.Length;
			}
		}

		using (var source = new ZipArchive(File.OpenRead(zipPath),ZipArchiveMode.Read))
		{
			foreach (var entry in source.Entries)
			{
				string destDir = Path.Combine(tempDestFolder, Path.GetDirectoryName(entry.FullName));
				string destFile = Path.Combine(tempDestFolder, entry.FullName);

				if (destDir.Length > 0)
				{
					Directory.CreateDirectory(destDir);
				}

				using (var writer = File.Create(destFile))
				using (var entryStream = entry.Open())
				{
					var buffer = new byte[80 * 1024];
					int read;
					do
					{
						read = entryStream.Read(buffer, 0, buffer.Length);
						writer.Write(buffer, 0, read);
						bytesUnpacked += read;
						progress = (float) (bytesUnpacked) / bytesToUnpack / 2;
					} while (read > 0);
				}
			}
		}


		var savefileData = SaveFile.OpenFile(tempDestFolder);
		var realGuid = savefileData.meta.guid;
		var realDestFolder = Path.Combine(persistentDataPath, realGuid.ToString());

		bytesToCopy += SaveFile.DirectorySize(new DirectoryInfo(tempDestFolder));

		if (!Directory.Exists(realDestFolder))
		{
			Directory.Move(tempDestFolder, realDestFolder);
		}
		else
		{
			var files = Directory.GetFiles(tempDestFolder);
			foreach (var file in files)
			{
				var filename = Path.GetFileName(file);
				var fileInRealDest = Path.Combine(realDestFolder, filename);
				var fileInTempDest = Path.Combine(tempDestFolder, filename);

				//NOTE(Simon): If file already exists in real destination, first delete. So Move() is safe.
				File.Delete(fileInRealDest);
				File.Move(fileInTempDest, fileInRealDest);

				bytesCopied += new FileInfo(fileInRealDest).Length;
				progress = (float) (bytesUnpacked + bytesCopied) / (bytesToUnpack + bytesToCopy);
			}

			var dirs = Directory.GetDirectories(tempDestFolder);
			foreach (var dir in dirs)
			{
				var dirname = new DirectoryInfo(dir).Name;
				//NOTE(Simon): Make sure directory exists
				Directory.CreateDirectory(Path.Combine(realDestFolder, dirname));

				var dirFiles = Directory.GetFiles(Path.Combine(tempDestFolder, dir));
				foreach (var file in dirFiles)
				{
					var filename = Path.GetFileName(file);
					var fileInRealDest = Path.Combine(realDestFolder, dirname, filename);
					var fileInTempDest = Path.Combine(tempDestFolder, dirname, filename);

					File.Delete(fileInRealDest);
					File.Move(fileInTempDest, fileInRealDest);
					
					bytesCopied += new FileInfo(fileInRealDest).Length;
					progress = (float) (bytesUnpacked + bytesCopied) / (bytesToUnpack + bytesToCopy);
				}
			}
		}

		Directory.Delete(tempDestFolder, true);

		progress = 1f;
		answered = true;
		unpacking = false;
	}
}
