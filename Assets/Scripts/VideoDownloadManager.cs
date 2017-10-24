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
}

public class VideoDownloadManager : MonoBehaviour
{
	private static Dictionary<string, Download> queued;

	void Start()
	{
		queued = new Dictionary<string, Download>();
	}

	void Update()
	{
		foreach (var download in queued)
		{
			if (download.Value.progress == 1f)
			{
				
			}
		}
	}

	public static void AddDownload(VideoSerialize video)
	{
		var url = Web.videoUrl + "/" + Encoding.UTF8.GetString(Convert.FromBase64String(video.uuid)) + ".mp4";
		if (!queued.ContainsKey(url))
		{
			var client = new WebClient();
			client.DownloadFileCompleted += OnComplete;
			client.DownloadProgressChanged += OnProgress;

			client.DownloadFileAsync(new Uri(url), Path.Combine(Application.persistentDataPath, video.uuid + ".mp4"), url);

			queued.Add(url, new Download
			{
				client = client,
				video = video
			});
		}
	}

	private static void OnComplete(object sender, AsyncCompletedEventArgs e)
	{
		var url = (string)e.UserState;
		if (e.Error != null)
		{
			Debug.Log(e.Error);
			//TODO(Simon): Error handling
			//TODO(Simon): Do not remove if error, but keep in queue to retry
			queued.Remove(url);
		}
		else
		{
			queued[url].progress = 1f;
			queued.Remove(url);
		}

	}

	private static void OnProgress(object sender, DownloadProgressChangedEventArgs e)
	{
		var url = (string)e.UserState;

		queued[url].totalBytes = e.TotalBytesToReceive;
		queued[url].bytesDownloaded = e.BytesReceived;
		queued[url].progress = e.ProgressPercentage / 100f;
	}
}
