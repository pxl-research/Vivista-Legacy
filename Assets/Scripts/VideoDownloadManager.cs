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
	public WebClient filesDownloadClient;
	public float progress;
	public long totalBytes;
	public long bytesDownloaded;
	public bool failed;
	public DownloadPanel panel;
	public Queue<DownloadItem> filesToDownload;
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
	//Can't call Application.persistentDataPath from another thread, so cache it
	private string dataPath;

	void Start()
	{
		Main.queued = new Dictionary<string, Download>();
		dataPath = Application.persistentDataPath;
	}

	void Update()
	{
		foreach (var kvp in Main.queued)
		{
			var download = kvp.Value;
			download.panel.UpdatePanel(download.progress);

			if (download.failed)
			{
				download.panel.Fail();
			}

			if (download.panel.ShouldRetry)
			{
				download.client.DownloadFileAsync(new Uri(kvp.Key), Path.Combine(Application.persistentDataPath, download.video.uuid), kvp.Key);
				download.failed = false;
				download.panel.Reset();
			}

			if (download.panel.ShouldCancel)
			{
				download.client.CancelAsync();
			}
		}
	}

	public void AddDownload(VideoSerialize video)
	{
		if (!queued.ContainsKey(video.uuid))
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
			queued.Add(video.uuid, download);

			client.DownloadStringCompleted += OnExtraListDownloaded;
			client.DownloadStringAsync(new Uri(Web.extraURL + "?videoid=" + video.uuid), video.uuid);
		}
	}

	private void OnExtraListDownloaded(object sender, DownloadStringCompletedEventArgs e)
	{
		var uuid = (string)e.UserState;
		var download = queued[uuid];
		download.client.DownloadStringCompleted -= OnExtraListDownloaded;

		int[] files = JsonHelper.ToArray<int>(e.Result);

		string directory = Path.Combine(dataPath, uuid);
		if (!Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		download.filesToDownload.Enqueue(new DownloadItem {url = Web.metaUrl + "/" + uuid, path = Path.Combine(directory, SaveFile.metaFilename)});
		download.filesToDownload.Enqueue(new DownloadItem {url = Web.videoUrl + "/" + uuid, path = Path.Combine(directory, SaveFile.videoFilename)});
		download.filesToDownload.Enqueue(new DownloadItem {url = Web.thumbnailUrl + "/" + uuid, path = Path.Combine(directory, SaveFile.thumbFilename)});

		foreach (int file in files)
		{
			download.filesToDownload.Enqueue(new DownloadItem 
			{
				url = String.Format("{0}/{1}?index={2}", Web.extraURL, uuid, file), 
				path = Path.Combine(directory, "extra" + file)
			});
		}

		download.client.DownloadFileCompleted += OnFileDownloaded;
		var item = download.filesToDownload.Dequeue();
		download.client.DownloadFileAsync(new Uri(item.url), item.path, uuid);
	}

	private void OnFileDownloaded(object sender, AsyncCompletedEventArgs e)
	{
		var uuid = (string)e.UserState;
		if (e.Error != null)
		{
			Debug.Log(e.Error);
			//TODO(Simon): Error handling
			//TODO(Simon): Do not remove if error, but keep in queue to retry
			queued[uuid].failed = true;
		}
		else if (queued[uuid].filesToDownload.Count > 0)
		{
			var item = queued[uuid].filesToDownload.Dequeue();
			queued[uuid].client.DownloadFileAsync(new Uri(item.url), item.path, uuid);
		}
		else
		{
			queued[uuid].progress = 1f;
		}
	}

	private void OnProgress(object sender, DownloadProgressChangedEventArgs e)
	{
		var uuid = (string)e.UserState;

		queued[uuid].totalBytes = e.TotalBytesToReceive;
		queued[uuid].bytesDownloaded = e.BytesReceived;
		queued[uuid].progress = e.ProgressPercentage / 100f;
	}
}
