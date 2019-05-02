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

			client.DownloadStringCompleted += OnExtraListDownloaded;
			client.DownloadStringAsync(new Uri(Web.extrasURL + "?videoid=" + video.id), video.id);
		}
	}

	private void OnExtraListDownloaded(object sender, DownloadStringCompletedEventArgs e)
	{
		string uuid = (string)e.UserState;
		var download = queued[uuid];
		download.client.DownloadStringCompleted -= OnExtraListDownloaded;

		var files = JsonHelper.ToArray<string>(e.Result);

		string directory = Path.Combine(dataPath, uuid);
		string extraDirectory = Path.Combine(directory, "extra");
		//NOTE(Simon): Creates all folders in path
		Directory.CreateDirectory(extraDirectory);

		download.filesToDownload.Enqueue(new DownloadItem {url = Web.metaUrl + "/" + uuid, path = Path.Combine(directory, SaveFile.metaFilename)});
		download.filesToDownload.Enqueue(new DownloadItem {url = Web.videoUrl + "?videoid=" + uuid, path = Path.Combine(directory, SaveFile.videoFilename)});
		download.filesToDownload.Enqueue(new DownloadItem {url = Web.thumbnailUrl + "/" + uuid, path = Path.Combine(directory, SaveFile.thumbFilename)});

		foreach (string file in files)
		{
			Guid guid;
			if (Guid.TryParse(file, out guid))
			{
				download.filesToDownload.Enqueue(new DownloadItem
				{
					url = String.Format("{0}/?videoid={1}&extraid={2}", Web.extraURL, uuid, file),
					path = Path.Combine(directory, "extra\\" + file)
				});
			}
		}

		download.client.DownloadFileCompleted += OnFileDownloaded;
		//download.client.DownloadProgressChanged += OnProgress;
		var item = download.filesToDownload.Dequeue();
		download.currentlyDownloading = item;
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
			queued[uuid].currentlyDownloading = item;
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
