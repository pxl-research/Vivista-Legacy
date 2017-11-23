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
	public WebClient metaDownloadClient;
	public WebClient filesDownloadClient;
	public float progress;
	public long totalBytes;
	public long bytesDownloaded;
	public bool failed;
	public DownloadPanel panel;
	public Queue<string> filesToDownload;
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
				download.metaDownloadClient.DownloadFileAsync(new Uri(kvp.Key), Path.Combine(Application.persistentDataPath, kvp.Value.video.uuid), kvp.Key);
				download.failed = false;
				download.panel.Reset();
			}

			if (download.panel.ShouldCancel)
			{
				download.metaDownloadClient.CancelAsync();
			}
		}
	}

	public void AddDownload(VideoSerialize video)
	{
		if (!queued.ContainsKey(video.uuid))
		{
			var client = new WebClient();
			client.DownloadFileCompleted += OnMetaComplete;
			client.DownloadProgressChanged += OnProgress;
			
			string decodedUuid = Encoding.UTF8.GetString(Convert.FromBase64String(video.uuid));
			string directory = Path.Combine(Application.persistentDataPath, decodedUuid);
			string path = Path.Combine(directory, SaveFile.metaFilename);

			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			var metaUrl = Web.metaUrl + "/" + decodedUuid;
			client.DownloadFileAsync(new Uri(metaUrl), path, video.uuid);
			var panel = Instantiate(DownloadPanelPrefab, DownloadList, false);
			var download = new Download
			{
				metaDownloadClient = client,
				video = video,
				panel = panel.GetComponent<DownloadPanel>()
			};

			download.panel.SetTitle(video.title);

			queued.Add(video.uuid, download);
		}
	}

	private void OnMetaComplete(object sender, AsyncCompletedEventArgs e)
	{
		var uuid = (string)e.UserState;
		if (e.Error != null)
		{
			Debug.Log(e.Error);
			//TODO(Simon): Error handling
			//TODO(Simon): Do not remove if error, but keep in queue to retry
			queued[uuid].failed = true;
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
