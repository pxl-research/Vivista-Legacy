using System;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using ICSharpCode.SharpZipLib.Zip;

public class ImportPanel : MonoBehaviour 
{
	public bool answered;
	public string sourcePath;
	public string destinationFolder;
	public string destinationPath;
	public string filename;

	public Text text;
	public ProgressBar progressbar;

	private static readonly object importLock = new object();
	private static readonly object unpackLock = new object();
	private float copyProgress;
	private float unpackProgress;
	private bool unpacking;

	public void Init(string sourcePath)
	{
		if (sourcePath == "")
		{
			Debug.Log("No filename received");
			Destroy(this);
			return;
		}

		this.sourcePath = sourcePath;
		filename = Path.GetFileName(sourcePath);

		if (File.Exists(sourcePath))
		{
			destinationFolder = Application.persistentDataPath;
			destinationPath = Path.Combine(destinationFolder, filename);
			new Thread(CopyFile).Start();
		}
	}

	public void Update () 
	{
		float threadLocalCopyProgress;
		float threadLocalUnpackProgress;
		lock (importLock)
		{
			threadLocalCopyProgress = copyProgress;
		}
		lock (unpackLock)
		{
			threadLocalUnpackProgress = unpackProgress;
		}

		if (threadLocalCopyProgress == 1f && !unpacking)
		{
			unpacking = true;
			new Thread(UnpackFile).Start();
		}
		if (threadLocalUnpackProgress == 1f)
		{
			answered = true;
		}

		text.GetComponent<Text>().text = !unpacking ? "Copying..." : "Unpacking...";
		progressbar.SetProgress((threadLocalCopyProgress + threadLocalUnpackProgress) / 2f);
	}

	public void CopyFile()
	{
		var buffer = new byte[4 * 1024 * 1024];

		using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
		{
			long fileLength = source.Length;
			using (var dest = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
			{
				long totalBytes = 0;
				int currentBlockSize;

				while ((currentBlockSize = source.Read(buffer, 0, buffer.Length)) > 0)
				{
					totalBytes += currentBlockSize;
					float percentage = (float)totalBytes / fileLength;

					dest.Write(buffer, 0, currentBlockSize);

					lock (importLock)
					{
						copyProgress = percentage;
					}
				}

				lock (importLock)
				{
					copyProgress = 1f;
				}
			}
		}
	}

	public void UnpackFile()
	{
		long bytesWritten = 0;
		long bytesToWrite = 0;

		using (var source = new ZipInputStream(File.OpenRead(destinationPath)))
		{
			ZipEntry entry;
			while ((entry = source.GetNextEntry()) != null)
			{
				bytesToWrite += entry.Size;
			}
		}

		using (var source = new ZipInputStream(File.OpenRead(destinationPath)))
		{
			ZipEntry entry;
			while ((entry = source.GetNextEntry()) != null)
			{
				string destDir = Path.Combine(destinationFolder, Path.GetDirectoryName(entry.Name));
				string destFile = Path.Combine(destinationFolder, entry.Name);
				string destFilename = Path.GetFileName(entry.Name);

				if (destDir.Length > 0)
				{
					Directory.CreateDirectory(destDir);
				}

				if (destFilename != String.Empty)
				{
					using (var writer = File.Create(destFile))
					{
						int size = 2048;
						var data = new byte[size];
						while (true)
						{
							size = source.Read(data, 0, data.Length);
							if (size > 0)
							{
								writer.Write(data, 0, size);
							}
							else
							{
								break;
							}
							bytesWritten += size;

							lock (unpackLock)
							{
								unpackProgress = (float)bytesWritten / bytesToWrite;
							}
						}
					}
				}
			}
		}

		//NOTE(Simon): Delete  source and dest zip files after unpacking
		//File.Delete(sourcePath);
		File.Delete(destinationPath);

		lock (unpackLock)
		{
			unpackProgress = 1f;
		}
	}
}
