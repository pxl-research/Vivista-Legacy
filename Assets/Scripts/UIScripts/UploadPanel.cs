using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
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
	public Coroutine coroutine;
	public ulong totalSize;
	public ulong currentFileProgressBytes;
	public bool done;
	public bool failed;
	public string error;
	public Queue<Timing> timings = new Queue<Timing>();
	public ulong uploaded;
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

	public IEnumerator StartUpload(List<FileUpload> filesToUpload)
	{
		Debug.Log($"frame {Time.frameCount}: Beginning upload");
		Assert.IsNull(status, "status should be null");

		status = new UploadStatus();
		ulong totalSize = 0;

		foreach (var file in filesToUpload)
		{
			totalSize += (ulong)FileHelpers.FileSize(file.path);
		}

		status.totalSize = totalSize;

		if (true)
		{
			var client = new TusClient();
			foreach (var file in filesToUpload)
			{
				if (status.failed)
				{
					Debug.Log($"frame {Time.frameCount}: Ending uploads because of error");
					yield return null;
				}
				else
				{
					var headers = client.AdditionalHeaders;
					headers.Clear();
					headers.Add("guid", file.projectGuid.ToString());
					headers.Add("type", file.type.ToString());
					headers.Add("Cookie", $"session={Web.sessionCookie}");
					headers.Add("filename", file.filename);

					var filesize = FileHelpers.FileSize(file.path);
					var op = client.CreateAsync(Web.fileUrl, filesize);

					while (!op.IsCompleted)
					{
						yield return new WaitForEndOfFrame();
					}

					(int code, string message) createResult;

					try
					{
						createResult = op.Result;
						if (createResult.code != 200)
						{
							status.failed = true;
							status.error = $"HTTP error {createResult.code}: {createResult.message}";
						}
					}
					catch (Exception e)
					{
						status.failed = true;
						status.error = "Something went wrong while trying to upload this project. Please try again later";
						Debug.Log(e);
						yield break;
					}

					if (status.failed)
					{
						Debug.Log($"frame {Time.frameCount}: Ending uploads because of error");
						yield return null;
					}

					var upload = client.UploadAsync(createResult.message, File.OpenRead(file.path), 20);
					upload.Progressed += OnUploadProgress;

					while (!upload.Operation.IsCompleted)
					{
						yield return new WaitForEndOfFrame();
					}

					try
					{
						//NOTE(Simon): Trigger Task completion
						var _ = upload.Operation.Result;
						status.uploaded += (ulong)filesize;
						status.currentFileProgressBytes = 0;
					}
					catch (Exception e)
					{
						status.failed = true;
						status.error = "Something went wrong while trying to upload this project. Please try again alter";
						Debug.Log(e);
						yield break;
					}
				}
			}

			if (!status.failed)
			{
				status.done = true;
			}
		}
	}

	private void OnUploadProgress(long bytestransferred, long bytestotal)
	{
		status.currentFileProgressBytes = (ulong)bytestransferred;
	}

	public void UpdatePanel()
	{
		time += Time.deltaTime;

		ulong totalUploaded = status.uploaded + status.currentFileProgressBytes;
		
		var newestTiming = new Timing {time = Time.realtimeSinceStartup, totalUploaded = totalUploaded};
		status.timings.Enqueue(newestTiming);
	
		while (status.timings.Count > 1 && status.timings.Peek().time < Time.realtimeSinceStartup - 1)
		{
			status.timings.Dequeue();
		}

		float currentSpeed = (newestTiming.totalUploaded - status.timings.Peek().totalUploaded) / (newestTiming.time - status.timings.Peek().time);
		float speed = status.timings.Count >= 2 ? currentSpeed: float.NaN;
		float timeRemaining = (status.totalSize - totalUploaded) / speed;
		progressBar.SetProgress(totalUploaded / (float)status.totalSize);

		//TODO(Simon): Show kB and GB when appropriate
		progressMB.text = $"{totalUploaded / megabyte:F2}/{status.totalSize / megabyte:F2}MB";

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
		if (status == null)
		{
			return;
		}

		if (status.coroutine != null)
		{
			StopCoroutine(status.coroutine);
		}

		status = null;

		Canvass.modalBackground.SetActive(false);
		Destroy(gameObject);
	}
}