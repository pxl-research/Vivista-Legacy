using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using TusDotNetClient;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

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
	public Text resultMessage;
	public Button viewOnWebsite;
	public Button tryAgain;
	public Button close;

	private float time;
	//NOTE(Simon): This is used to signal other scripts that we're done. As apposed to status.done which is to be used internally
	public bool done;

	private const float timeBetweenUpdates = 1/5f;

	private UploadStatus status;

	public async void StartUpload(Queue<FileUpload> filesToUpload)
	{
		Assert.IsNull(status, "status should be null");

		status = new UploadStatus();
		status.filesUploaded = new List<FileUpload>();
		status.filesToUpload = filesToUpload;
		status.projectId = filesToUpload.Peek().projectGuid;
		UpdateStatusBySavedProgress(status);

		status.totalSizeBytes = ProjectSizeFromStatus(status);
		status.uploadedBytes = BytesUploadedFromStatus(status);

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
			headers.Add("Cookie", Web.formattedCookieHeader);
			headers.Add("filename", status.fileInProgress.filename);

			var filesize = FileHelpers.FileSize(status.fileInProgress.path);

			//NOTE(Simon): If we're not resuming, get a new fileId from the server
			if (!shouldResume)
			{
				(int code, string message) createResult;
				try
				{
					createResult = await client.CreateAsync(Web.fileUrl, filesize);
				}
				catch (Exception e)
				{
					FailUpload("Something went wrong while trying to upload this project. Please try again later", e);
					return;
				}

				if (createResult.code != 200)
				{
					status.filesToUpload.Enqueue(status.fileInProgress);
					status.fileInProgress = null;

					FailUpload($"HTTP error {createResult.code}: {createResult.message}");
					return;
				}

				status.fileInProgressId = createResult.message;
			}

			try
			{
				var uploadOp = client.UploadAsync(status.fileInProgressId, File.OpenRead(status.fileInProgress.path), 20);
				uploadOp.Progressed += OnUploadProgress;
				var _ = await uploadOp;
			}
			catch (Exception e)
			{
				FailUpload("Something went wrong while trying to upload this project. Please try again later", e);
				return;
			}

			status.filesUploaded.Add(status.fileInProgress);
			status.fileInProgress = null;

			status.uploadedBytes += (ulong)filesize;
			status.currentFileProgressBytes = 0;

			//NOTE(Simon): After finishing a file, write progress to disk
			WriteUploadProgress(status);
		}

		ClearUploadProgress();
		if (FinishUpload())
		{
			SucceedUpload();
		}
		else
		{
			FailUpload("Something went wrong while finishing the upload. Please try again later");
		}
	}

	public bool FinishUpload()
	{
		var form = new WWWForm();
		
		form.AddField("id", status.projectId.ToString());

		using var www = UnityWebRequest.Post(Web.finishUploadUrl, form);

		www.SetRequestHeader("Cookie", Web.formattedCookieHeader);
		var request = www.SendWebRequest();
		while (!request.isDone)
		{
		}

		return www.responseCode == 200;
	}

	private void FailUpload(string error, Exception e = null)
	{
		if (e != null)
		{
			Debug.LogError(e);
		}

		status.failed = true;

		progressTime.text = "Error";
		progressSpeed.text = "";
		resultMessage.text = error;

		viewOnWebsite.gameObject.SetActive(false);
		tryAgain.gameObject.SetActive(true);
		close.gameObject.SetActive(true);
	}

	private void SucceedUpload()
	{
		status.done = true;

		progressTime.text = "Finished uploading";
		progressSpeed.text = "";

		resultMessage.text = "Project upload succesful";

		viewOnWebsite.gameObject.SetActive(true);
		tryAgain.gameObject.SetActive(false);
		close.gameObject.SetActive(true);
	}

	private void WriteUploadProgress(UploadStatus status)
	{
		//NOTE(Simon): If no files were uploaded and none are in progress (e.g. in case of failure to create file on server), don't write any progress to disk.
		if (status.filesUploaded.Count == 0 && status.fileInProgress == null)
		{
			return;
		}

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
				//NOTE(Simon): If the line contains a domain different to the currently connected one, the uploadProgress is invalid.
				if (!lines[lines.Length - 1].StartsWith(Web.apiRootUrl))
				{
					ClearUploadProgress();
					return;
				}

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

	private ulong BytesUploadedFromStatus(UploadStatus status)
	{
		ulong size = 0;

		foreach (var file in status.filesUploaded)
		{
			size += (ulong)FileHelpers.FileSize(file.path);
		}

		return size;
	}

	private void OnUploadProgress(long bytestransferred, long bytestotal)
	{
		status.currentFileProgressBytes = (ulong)bytestransferred;
	}

	public void Close()
	{
		done = true;
	}

	public void ViewOnWebsite()
	{
		string id = status.projectId.Encode();
		string url = $"{Web.editVideoUrl}?id={id}";

		Process.Start(new ProcessStartInfo
		{
			FileName = url,
			UseShellExecute = true
		});
	}

	public void Retry()
	{
		var filesToUpload = new Queue<FileUpload>();
		if (status.fileInProgress != null)
		{
			filesToUpload.Enqueue(status.fileInProgress);
		}

		for (int i = status.filesUploaded.Count - 1; i >= 0; i--)
		{
			filesToUpload.Enqueue(status.filesUploaded[i]);
		}

		status = null;

		StartUpload(filesToUpload);
	}

	public void Update()
	{
		if (!status.done && !status.failed)
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
			float speed = status.timings.Count >= 2 ? currentSpeed : float.NaN;
			float timeRemaining = (status.totalSizeBytes - totalUploaded) / speed;
			progressBar.SetProgress(totalUploaded / (float) status.totalSizeBytes);

			//TODO(Simon): Show kB and GB when appropriate
			progressMB.text = $"{MathHelper.FormatBytes(totalUploaded)}/{MathHelper.FormatBytes(status.totalSizeBytes)}";

			time += Time.deltaTime;

			if (time > timeBetweenUpdates)
			{
				if (!float.IsInfinity(timeRemaining) && !float.IsNaN(timeRemaining))
				{
					time %= timeBetweenUpdates;
					progressTime.text = $"{timeRemaining:F0} seconds remaining";
					progressSpeed.text = $"{MathHelper.FormatBytes((long) speed)}/s";
				}
				else
				{
					time %= timeBetweenUpdates;
					progressTime.text = "Connecting...";
					progressSpeed.text = "Connecting...";
				}
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

		Destroy(gameObject);
	}
}