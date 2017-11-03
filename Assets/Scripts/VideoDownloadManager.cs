using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;

public class Download
{
	public VideoSerialize video;
	public WebClient client;
	public float progress;
	public long totalBytes;
	public long bytesDownloaded;
	public bool failed;
	public DownloadPanel panel;
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

	void Start()
	{
		Main.queued = new Dictionary<string, Download>();
	}

	void Update()
	{
		foreach (var kvp in Main.queued)
		{
			var download = kvp.Value;
			download.panel.UpdatePanel(kvp.Value.progress);

			if (download.failed)
			{
				download.panel.Fail();
			}

			if (download.panel.ShouldRetry)
			{
				download.client.DownloadFileAsync(new Uri(kvp.Key), Path.Combine(Application.persistentDataPath, kvp.Value.video.uuid + ".mp4"), kvp.Key);
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
		var url = Web.videoUrl + "/" + Encoding.UTF8.GetString(Convert.FromBase64String(video.uuid)) + ".mp4";
		if (!queued.ContainsKey(url))
		{
			var client = new WebClient();
			client.DownloadFileCompleted += OnComplete;
			client.DownloadProgressChanged += OnProgress;

			client.DownloadFileAsync(new Uri(url), Path.Combine(Application.persistentDataPath, video.uuid + ".mp4"), url);
			var panel = Instantiate(DownloadPanelPrefab, DownloadList, false);
			var download = new Download
			{
				client = client,
				video = video,
				panel = panel.GetComponent<DownloadPanel>()
			};

			download.panel.SetTitle(video.title);

			queued.Add(url, download);
		}
	}

	private void OnComplete(object sender, AsyncCompletedEventArgs e)
	{
		var url = (string)e.UserState;
		if (e.Error != null)
		{
			Debug.Log(e.Error);
			//TODO(Simon): Error handling
			//TODO(Simon): Do not remove if error, but keep in queue to retry
			queued[url].failed = true;
		}
		else
		{
			queued[url].progress = 1f;
		}
	}

	private void OnProgress(object sender, DownloadProgressChangedEventArgs e)
	{
		var url = (string)e.UserState;

		queued[url].totalBytes = e.TotalBytesToReceive;
		queued[url].bytesDownloaded = e.BytesReceived;
		queued[url].progress = e.ProgressPercentage / 100f;
	}
}
