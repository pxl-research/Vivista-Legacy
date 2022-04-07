using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using UnityEngine;

public class Download
{
	public VideoSerialize video;
	public float progress;
	public long totalBytes;
	public long bytesDownloaded;
	public bool failed;
	public string directory;
	public DownloadPanel panel;
	public Queue<DownloadItem> filesToDownload;
	public DownloadItem currentlyDownloading;
}

public class DownloadItem
{
	public string url;
	public string path;
}

public class VideoDownloadManager : MonoBehaviour
{
	public Transform downloadList;
	public GameObject downloadPanelPrefab;
	public GameObject downloadInProgressPanelPrefab;

	public static VideoDownloadManager Main
	{
		get { return _main ?? (_main = GameObject.Find("VideoDownloadManager").GetComponent<VideoDownloadManager>()); }
	}
	private static VideoDownloadManager _main;

	private HttpClient client;

	private bool forceQuit;
	private Dictionary<string, Download> queued;
	private Download currentDownload;

	//NOTE(Simon): Can't call Application.persistentDataPath from another thread, so cache it
	private string dataPath;
	//NOTE(Simon): Used to temporarily cache elemenets to be removed from queued. Can't remove from Dictionary while looping.
	private List<string> indexesToRemove = new List<string>();

	void Start()
	{
		queued = new Dictionary<string, Download>();
		dataPath = Application.persistentDataPath;
		Application.wantsToQuit += OnWantsToQuit;
		client = new HttpClient();
	}

	void Update()
	{
		foreach (var kvp in queued)
		{
			currentDownload = kvp.Value;
			currentDownload.panel.UpdatePanel(currentDownload.progress);

			if (currentDownload.failed)
			{
				currentDownload.panel.Fail();
			}
			else if (currentDownload.panel.ShouldRetry)
			{
				RetryDownload(currentDownload);
				currentDownload.failed = false;
				currentDownload.panel.Reset();
			}
			else if (currentDownload.panel.ShouldCancel)
			{
				client.CancelPendingRequests();
			}
			else if (currentDownload.progress >= 1f)
			{
				StartCoroutine(currentDownload.panel.Done());
				indexesToRemove.Add(kvp.Key);
			}
		}

		if (indexesToRemove.Count > 0)
		{
			foreach (string index in indexesToRemove)
			{
				queued.Remove(index);
			}
			indexesToRemove.Clear();
		}
	}

	public Download GetDownload(string guid)
	{
		return queued.ContainsKey(guid) ? queued[guid] : null;
	}

	public async void AddDownload(VideoSerialize video)
	{
		if (!queued.ContainsKey(video.id))
		{
			var panel = Instantiate(downloadPanelPrefab, downloadList, false);
			var download = new Download
			{
				video = video,
				panel = panel.GetComponent<DownloadPanel>(),
				filesToDownload = new Queue<DownloadItem>()
			};
			
			download.panel.SetTitle(video.title);
			queued.Add(video.id, download);
			download.totalBytes = download.video.downloadsize;

			string extraList = await client.GetStringAsync(Web.filesUrl + "?videoid=" + video.id);
			OnExtraListDownloaded(extraList, download);
		}
	}

	private void OnExtraListDownloaded(string extraList, Download download)
	{
		string videoGuid = download.video.id;
		string[] files = JsonHelper.ToArray<string>(extraList);
		download.directory = Path.Combine(dataPath, videoGuid);

		foreach (string file in files)
		{
			download.filesToDownload.Enqueue(new DownloadItem
			{
				url = $"{Web.fileUrl}?videoid={videoGuid}&filename={file}",
				path = Path.Combine(download.directory, file)
			});
		}

		StartNextDownload(download);
	}

	private void OnFileDownloaded(Download download)
	{
		if (download.filesToDownload.Count > 0)
		{
			StartNextDownload(download);
		}
		else
		{
			download.progress = 1f;
		}
	}

	private async void StartNextDownload(Download download)
	{
		var item = download.filesToDownload.Dequeue();
		Directory.CreateDirectory(Path.GetDirectoryName(item.path));

		download.currentlyDownloading = item;

		//NOTE(Simon): 65k buffer
		byte[] buffer = new byte[1 << 16];

		using (var response = await client.GetAsync(item.url, HttpCompletionOption.ResponseHeadersRead))
		using (var stream = await response.Content.ReadAsStreamAsync())
		using (var fileStream = new FileStream(item.path, FileMode.Create))
		{
			int bytesRead;
			while((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
			{
				fileStream.Write(buffer, 0, bytesRead);
				OnProgress(bytesRead, download);
			}
		}

		OnFileDownloaded(download);
	}

	private void RetryDownload(Download download)
	{
		download.filesToDownload.Enqueue(download.currentlyDownloading);
		StartNextDownload(download);
	}

	private void OnProgress(int bytes, Download download)
	{
		download.bytesDownloaded += bytes;
		download.progress = (float)download.bytesDownloaded / download.totalBytes;
	}

	private bool OnWantsToQuit()
	{
#if UNITY_EDITOR
		client.CancelPendingRequests();
		client.Dispose();
#endif
		if (forceQuit)
		{
			return true;
		}


		if (queued.Count > 0)
		{
			var go = Instantiate(downloadInProgressPanelPrefab);
			go.transform.SetParent(Canvass.main.transform, false);
			var panel = go.GetComponent<DownloadInProgressPanel>();
			Canvass.modalBackground.SetActive(true);
			//dsadsa
				//TODO(Simon): Fix Abort Screen
			panel.OnAbortDownload += () =>
			{
				client.CancelPendingRequests();
				client.Dispose();

				Directory.Delete(currentDownload.directory, true);

				ForceQuit();
			};
			panel.OnContinue += () =>
			{
				Destroy(panel.gameObject);
				Canvass.modalBackground.SetActive(false);
			};

			return false;
		}
		else
		{
			return true;
		}
	}

	private void ForceQuit()
	{
		forceQuit = true;
		Application.Quit();
	}
}
