using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using TusDotNetClient;

public enum UploadFileType
{
	Video,
	Meta,
	Tags,
	Chapters,
	Extra,
	Miniature
}

public class FileUpload
{
	public string filename;
	public Guid projectGuid;
	public UploadFileType type;
	public string path;

	public FileUpload(Guid projectGuid, UploadFileType uploadFileType, string filename, string path)
	{
		this.filename = filename;
		this.projectGuid = projectGuid;
		type = uploadFileType;
		this.path = path;
	}
}

public class UploadStatus
{
	public Guid projectId;
	public bool done;
	public bool failed;
	public string error;
	public ulong totalSizeBytes;
	public ulong uploadedBytes;
	public ulong currentFileProgressBytes;

	public Queue<FileUpload> filesToUpload;
	public List<FileUpload> filesUploaded;
	public FileUpload fileInProgress;
	public string fileInProgressId;

	public Queue<Timing> timings = new Queue<Timing>();
}

public class UploadPanel : MonoBehaviour 
{
	public Text progressMB;
	public Text progressTime;
	public Text progressSpeed;
	public ProgressBar progressBar;

	public float time;

	const int kilobyte = 1024;
	const int megabyte = 1024 * 1024;
	const int gigabyte = 1024 * 1024 * 1024;
	private const float timeBetweenUpdates = 1/10f;

	public UploadStatus status;

	public async void StartUpload(Queue<FileUpload> filesToUpload)
	{
		Assert.IsNull(status, "status should be null");

		status = new UploadStatus();
		status.filesUploaded = new List<FileUpload>();
		status.filesToUpload = filesToUpload;
		status.projectId = filesToUpload.Peek().projectGuid;
		UpdateStatusBySavedProgress(status);

		status.totalSizeBytes = ProjectSizeFromStatus(status);

		var client = new TusClient();
		while (status.filesToUpload.Count != 0)
		{
			if (status.failed)
			{
				return;
			}

			bool shouldResume = false;
			if (status.fileInProgress == null)
			{
				status.fileInProgress = status.filesToUpload.Dequeue();
			}
			else
			{
				shouldResume = true;
			}

			var headers = client.AdditionalHeaders;
			headers.Clear();
			headers.Add("guid", status.fileInProgress.projectGuid.ToString());
			headers.Add("type", status.fileInProgress.type.ToString());
			headers.Add("Cookie", $"session={Web.sessionCookie}");
			headers.Add("filename", status.fileInProgress.filename);

			var filesize = FileHelpers.FileSize(status.fileInProgress.path);
			//NOTE(Simon): If we;re not resuming, get a new fileId from the server
			if (!shouldResume)
			{
				(int code, string message) createResult;
				try
				{
					createResult = await client.CreateAsync(Web.fileUrl, filesize);
				}
				catch (Exception e)
				{
					WriteUploadProgress(status);
					status.failed = true;
					status.error = "Something went wrong while trying to upload this project. Please try again later";
					Debug.Log(e);
					return;
				}

				if (createResult.code != 200)
				{
					WriteUploadProgress(status);
					status.failed = true;
					status.error = $"HTTP error {createResult.code}: {createResult.message}";
				}

				status.fileInProgressId = createResult.message;
				
			}

			if (status.failed)
			{
				return;
			}

			try
			{
				var uploadOp = client.UploadAsync(status.fileInProgressId, File.OpenRead(status.fileInProgress.path), 20);
				uploadOp.Progressed += OnUploadProgress;
				var _ = await uploadOp;
			}
			catch (Exception e)
			{
				WriteUploadProgress(status);
				status.failed = true;
				status.error = "Something went wrong while trying to upload this project. Please try again alter";
				Debug.Log(e);
				return;
			}

			//NOTE(Simon): Trigger Task completion
			status.filesUploaded.Add(status.fileInProgress);
			status.fileInProgress = null;

			status.uploadedBytes += (ulong)filesize;
			status.currentFileProgressBytes = 0;
		}

		if (!status.failed)
		{
			status.done = true;
			ClearUploadProgress();
			Dispose();
		}

		Dispose();
	}

	public void FinishUpload()
	{
		
	}

	private void WriteUploadProgress(UploadStatus status)
	{
		var projectPath = Path.Combine(Application.persistentDataPath, status.projectId.ToString());
		var filePath = Path.Combine(projectPath, ".uploadProgress");

		var buffer = new StringBuilder();

		foreach (var file in status.filesUploaded)
		{
			buffer.AppendLine(file.filename);
		}

		if (status.fileInProgress != null)
		{
			buffer.AppendLine(status.fileInProgress.filename);
			buffer.AppendLine(status.fileInProgressId);
		}

		using (var stream = File.Create(filePath))
		{
			var bytes = Encoding.ASCII.GetBytes(buffer.ToString());

			stream.Write(bytes, 0, bytes.Length);
		}
	}

	private void ClearUploadProgress()
	{
		var projectPath = Path.Combine(Application.persistentDataPath, status.projectId.ToString());
		var filePath = Path.Combine(projectPath, ".uploadProgress");

		File.Delete(filePath);
	}

	private void UpdateStatusBySavedProgress(UploadStatus status)
	{
		var projectPath = Path.Combine(Application.persistentDataPath, status.projectId.ToString());
		var filePath = Path.Combine(projectPath, ".uploadProgress");

		if (File.Exists(filePath))
		{
			var lines = File.ReadAllLines(filePath);

			string fileInProgress = null;

			//NOTE(Simon): If last entry starts with http, the last two entries are the file in progress and its serverId. So remove them from the array
			if (lines[lines.Length - 1].StartsWith("http"))
			{
				fileInProgress = lines[lines.Length - 2];
				status.fileInProgressId = lines[lines.Length - 1];

				var newArray = new string[lines.Length - 2];
				Array.Copy(lines, newArray, lines.Length - 2);
				lines = newArray;

			}

			//NOTE(Simon): Remove already uploaded items from filesToUpload, and add them to filesUploaded
			var newQueue = new Queue<FileUpload>();
			while (status.filesToUpload.Count > 0)
			{
				var item = status.filesToUpload.Dequeue();

				if (lines.Contains(item.filename))
				{
					status.filesUploaded.Add(item);
				}
				else if (item.filename == fileInProgress)
				{
					status.fileInProgress = item;
				}
				else
				{
					newQueue.Enqueue(item);
				}
			}

			status.filesToUpload = newQueue;
		}
	}

	private ulong ProjectSizeFromStatus(UploadStatus status)
	{
		ulong totalSize = 0;
		foreach (var file in status.filesToUpload)
		{
			totalSize += (ulong)FileHelpers.FileSize(file.path);
		}
		foreach (var file in status.filesUploaded)
		{
			totalSize += (ulong)FileHelpers.FileSize(file.path);
		}

		if (status.fileInProgress != null)
		{
			totalSize += (ulong)FileHelpers.FileSize(status.fileInProgress.path);
		}

		return totalSize;
	}

	private void OnUploadProgress(long bytestransferred, long bytestotal)
	{
		status.currentFileProgressBytes = (ulong)bytestransferred;
	}

	public void UpdatePanel()
	{
		time += Time.deltaTime;

		ulong totalUploaded = status.uploadedBytes + status.currentFileProgressBytes;
		
		var newestTiming = new Timing {time = Time.realtimeSinceStartup, totalUploaded = totalUploaded};
		status.timings.Enqueue(newestTiming);
	
		while (status.timings.Count > 1 && status.timings.Peek().time < Time.realtimeSinceStartup - 1)
		{
			status.timings.Dequeue();
		}

		float currentSpeed = (newestTiming.totalUploaded - status.timings.Peek().totalUploaded) / (newestTiming.time - status.timings.Peek().time);
		float speed = status.timings.Count >= 2 ? currentSpeed: float.NaN;
		float timeRemaining = (status.totalSizeBytes - totalUploaded) / speed;
		progressBar.SetProgress(totalUploaded / (float)status.totalSizeBytes);

		//TODO(Simon): Show kB and GB when appropriate
		progressMB.text = $"{totalUploaded / megabyte:F2}/{status.totalSizeBytes / megabyte:F2}MB";

		time += Time.deltaTime;

		if (time > timeBetweenUpdates)
		{
			if (!float.IsInfinity(timeRemaining) && !float.IsNaN(timeRemaining))
			{
				time %= timeBetweenUpdates;
				progressTime.text = $"{timeRemaining:F0} seconds remaining";
				progressSpeed.text = $"{speed / megabyte:F2}MB/s";
			}
			else
			{
				time %= timeBetweenUpdates;
				progressTime.text = "Connecting...";
				progressSpeed.text = "Connecting...";
			}
		}
	}

	public void Dispose()
	{
		if (status != null)
		{
			if (status.filesToUpload.Count > 0 || status.fileInProgress != null)
			{
				WriteUploadProgress(status);
			}

			status = null;
		}

		Canvass.modalBackground.SetActive(false);
		Destroy(gameObject);
	}
}