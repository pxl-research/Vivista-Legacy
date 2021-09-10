using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using UnityEngine;

public class Download
{
	public VideoSerialize video;
	public WebClient client;
	public float progress;
	public long totalBytes;
	public long bytesDownloaded;
	public long previousBytesDownloaded;
	public bool failed;
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
	public Transform DownloadList;
	public GameObject DownloadPanelPrefab;

	public static VideoDownloadManager Main
	{
		get { return _main ?? (_main = GameObject.Find("VideoDownloadManager").GetComponent<VideoDownloadManager>()); }
	}
	private static VideoDownloadManager _main;

	private Dictionary<string, Download> queued;
	//NOTE(Simon): Can't call Application.persistentDataPath from another thread, so cache it
	private string dataPath;
	//NOTE(Simon): Used to temporarily cache elemenets to be removed from queued. Can't remove from Dictionary while looping.
	private List<string> indexesToRemove = new List<string>();

	void Start()
	{
		Main.queued = new Dictionary<string, Download>();
		dataPath = Application.persistentDataPath;
	}

	void Update()
	{
		foreach (var kvp in queued)
		{
			var download = kvp.Value;
			download.panel.UpdatePanel(download.progress);

			if (download.failed)
			{
				download.panel.Fail();
			}
			else if (download.panel.ShouldRetry)
			{
				download.client.DownloadFileAsync(new Uri(download.currentlyDownloading.url), download.currentlyDownloading.path, kvp.Key);
				download.failed = false;
				download.panel.Reset();
			}
			else if (download.panel.ShouldCancel)
			{
				download.client.CancelAsync();
			}
			else if (download.progress >= 1f)
			{
				StartCoroutine(download.panel.Done());
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

	public void AddDownload(VideoSerialize video)
	{
		if (!queued.ContainsKey(video.id))
		{
			var client = new WebClient();

			var panel = Instantiate(DownloadPanelPrefab, DownloadList, false);
			var download = new Download
			{
				client = client,
				video = video,
				panel = panel.GetComponent<DownloadPanel>(),
				filesToDownload = new Queue<DownloadItem>()
			};
			
			download.panel.SetTitle(video.title);
			queued.Add(video.id, download);
			download.totalBytes = download.video.downloadsize;

			client.DownloadStringCompleted += OnExtraListDownloaded;
			client.DownloadStringAsync(new Uri(Web.filesUrl + "?videoid=" + video.id), download);
		}
	}

	private void OnExtraListDownloaded(object sender, DownloadStringCompletedEventArgs e)
	{
		var download = (Download)e.UserState;
		var videoGuid = download.video.id;
		download.client.DownloadStringCompleted -= OnExtraListDownloaded;

		var files = JsonHelper.ToArray<string>(e.Result);

		string directory = Path.Combine(dataPath, videoGuid);

		foreach (string file in files)
		{
			download.filesToDownload.Enqueue(new DownloadItem
			{
				url = $"{Web.fileUrl}?videoid={videoGuid}&filename={file}",
				path = Path.Combine(directory, file)
			});
		}

		download.client.DownloadFileCompleted += OnFileDownloaded;
		download.client.DownloadProgressChanged += OnProgress;
		StartNextDownload(download);
	}

	private void OnFileDownloaded(object sender, AsyncCompletedEventArgs e)
	{
		var download = (Download)e.UserState;

		if (e.Error != null)
		{
			Debug.LogError(e.Error);
			//TODO(Simon): Error handling
			//TODO(Simon): Do not remove if error, but keep in queue to retry
			download.failed = true;
		}
		else if (download.filesToDownload.Count > 0)
		{
			StartNextDownload(download);
		}
		else
		{
			download.progress = 1f;
		}
	}

	private void StartNextDownload(Download download)
	{
		var item = download.filesToDownload.Dequeue();
		Directory.CreateDirectory(Path.GetDirectoryName(item.path));
		download.currentlyDownloading = item;
		download.client.DownloadFileAsync(new Uri(item.url), item.path, download);
	}

	private void OnProgress(object sender, DownloadProgressChangedEventArgs e)
	{
		//NOTE(Simon): EventArgs only holds the total bytes downloaded of the currently downloading file.
		//NOTE(cont.): So we can't easily figure out how many bytes we've downloaded in total. So first we check if bytesReceives < previousBytesReceived.
		//NOTE(cont.): This means a new file has started downloading. In that case, reset previousBytesReceived.
		//NOTE(cont.): Then calculate the delta and add it to the total
		var download = (Download)e.UserState;
		if (e.BytesReceived < download.previousBytesDownloaded)
		{
			download.previousBytesDownloaded = 0;
		}
		var delta = e.BytesReceived - download.previousBytesDownloaded;
		download.bytesDownloaded += delta;
		download.progress = (float)download.bytesDownloaded / download.totalBytes;
		download.previousBytesDownloaded = e.BytesReceived;
	}
}
