using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable]
public class VideoResponseSerialize
{
	public int totalcount;
	public int page;
	public int count;
	public List<VideoSerialize> videos;
}

[Serializable]
public class VideoSerialize
{
	public string id;
	public int userid;
	public string username;
	public string timestamp;
	[NonSerialized]
	public DateTime realTimestamp;
	public long downloadsize;

	public string title;
	public int length;
	public string description;

	public bool compatibleVersion = true;
}

public class IndexPanel : MonoBehaviour
{
	public bool answered;
	public string answerVideoId;

	public GameObject pageLabelPrefab;
	public GameObject videoPrefab;
	public GameObject detailPanelPrefab;

	public GameObject detailPanel;
	public ImportPanel importPanel;
	public GameObject pageLabelContainer;
	public GameObject videoContainer;
	public List<GameObject> pageLabels;
	public Button previousPage;
	public Button nextPage;
	public Image spinner;
	public Text noVideos;
	public GameObject modalBackground;
	public GameObject updateAvailableNotification;

	public bool isFinishedLoadingVideos;

	private int page = 1;
	private int numPages = 1;
	private int videosPerPage;

	int lastScreenWidth;
	int lastScreenHeight;
	float lastLayoutRebuildTime;
	bool layoutDirty;

	private VideoResponseSerialize loadedVideos;
	private VideoSerialize detailVideo;

	private GameObjectPool videoPool;

	public void Start()
	{
		videoPool = new GameObjectPool(videoPrefab, videoContainer.transform);

		OpenVideoFromCmdArgument();

		//videoContainer.GetComponent<FlowLayoutGroup>().OnLayoutChange += OnLayoutChange;

		pageLabels = new List<GameObject>();

		LoadPage();

		lastScreenWidth = Screen.width;
		lastScreenHeight = Screen.height;

		StartCoroutine(AutoUpdatePanel.IsUpdateAvailable(ShowUpdateNoticeIfNecessary));
	}

	private void OpenVideoFromCmdArgument()
	{
		var cmd = Environment.GetCommandLineArgs();
		string rawlink = cmd[cmd.Length - 1];
		bool isQuicklink = rawlink.Contains("vivista://");
		string quicklink = isQuicklink ? rawlink.Substring(rawlink.IndexOf("://") + "://".Length) : null;
		var parts = isQuicklink ? quicklink.Split('/') : null;
		string type = isQuicklink ? parts[0] : null;
		string id = isQuicklink ? parts[1] : null;

		if (type == "video" && !String.IsNullOrEmpty(id))
		{
			GuidHelpers.TryDecode(id, out var guid);
			var url = $"{Web.videoApiUrl}?id={guid}";
			using (var request = UnityWebRequest.Get(url))
			{
				request.SendWebRequest();

				while (!request.isDone)
				{
				}

				detailVideo = JsonUtility.FromJson<VideoSerialize>(request.downloadHandler.text);
				if (detailVideo != null && detailVideo.id != null)
				{
					detailVideo.realTimestamp = DateTime.Parse(detailVideo.timestamp);
					ShowVideoDetails(detailVideo, isLocal: false);
					return;
				}
			}
		}
	}

	public void Update()
	{
		if (detailPanel != null)
		{
			var panel = detailPanel.GetComponent<DetailPanel>();
			if (panel.shouldClose)
			{
				Destroy(detailPanel);
				detailPanel = null;
			}
			if (panel.answered)
			{
				if (panel.answerEnableVR)
				{
					PlayVideoVR(detailVideo);
				}
				else
				{
					PlayVideo(detailVideo);
				}
			}
		}

		if (importPanel != null)
		{
			if (importPanel.allowCancel && Input.GetKeyDown(KeyCode.Escape))
			{
				Destroy(importPanel.gameObject);
				transform.localScale = Vector3.one;
			}
		}

		//Note(Simon): Spinner animation
		if (spinner.gameObject.activeSelf)
		{
			spinner.rectTransform.Rotate(0, 0, -1f);
		}

		//Note(Simon): Pages & labels
		{
			const int maxLabels = 11;
			var labelsNeeded = Mathf.Min(numPages, maxLabels);

			while (pageLabels.Count < labelsNeeded)
			{
				var newLabel = Instantiate(pageLabelPrefab);
				pageLabels.Add(newLabel);
				newLabel.transform.SetParent(pageLabelContainer.transform, false);
			}
			while (pageLabels.Count > labelsNeeded)
			{
				var pageLabel = pageLabels[pageLabels.Count - 1];
				pageLabels.RemoveAt(pageLabels.Count - 1);
				Destroy(pageLabel);
			}

			//Note(Simon): This algorithm draws the page labels. We have a max of <numLabels> labels (11 currently). So if numPages > numLabels we want to draw the labels like:
			//Note(Simon): 1 ... 4 5 6 7 8 9 10 ... 20
			{
				if (numPages <= maxLabels)
				{
					for (int i = 0; i < pageLabels.Count; i++)
					{
						pageLabels[i].GetComponent<Text>().text = (i + 1).ToString();
						pageLabels[i].GetComponent<Text>().fontStyle = (i + 1 == page) ? FontStyle.Bold : FontStyle.Normal;
					}
				}
				else
				{
					var index = 0;
					var start = Mathf.Clamp(page - 5, 1, numPages - 10);
					var end = Mathf.Clamp(page + 5, maxLabels, numPages);

					for (int i = start; i <= end; i++)
					{
						pageLabels[index].GetComponent<Text>().fontStyle = (i == page) ? FontStyle.Bold : FontStyle.Normal;
						pageLabels[index++].GetComponent<Text>().text = i.ToString();
					}
					if (page > 6)
					{
						pageLabels[0].GetComponent<Text>().text = "1";
						pageLabels[1].GetComponent<Text>().text = "...";
					}
					if (page < numPages - 5)
					{
						pageLabels[pageLabels.Count - 2].GetComponent<Text>().text = "...";
						pageLabels[pageLabels.Count - 1].GetComponent<Text>().text = numPages.ToString();
					}
				}
			}

			previousPage.gameObject.SetActive(true);
			nextPage.gameObject.SetActive(true);

			if (page == 1)
			{
				previousPage.gameObject.SetActive(false);
			}
			if (numPages == 1 || page == numPages)
			{
				nextPage.gameObject.SetActive(false);
			}
		}

		//NOTE(Simon): Wait until to-be-imported video is copied
		if (importPanel != null)
		{
			if (importPanel.answered)
			{
				//NOTE(Simon): Show window by making scale 1 again
				transform.localScale = new Vector3(1, 1, 1);

				LoadPage();
				Destroy(importPanel.gameObject);
			}
		}

		if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
		{
			lastScreenWidth = Screen.width;
			lastScreenHeight = Screen.height;
			layoutDirty = true;
		}

		if (layoutDirty && lastLayoutRebuildTime < Time.time - .5f)
		{
			LoadPage();
			lastLayoutRebuildTime = Time.time;
			layoutDirty = false;
		}
	}

	public void ShowVideoDetails(VideoSerialize video, bool isLocal = true)
	{
		detailVideo = video;
		detailPanel = Instantiate(detailPanelPrefab);
		detailPanel.transform.SetParent(Canvass.main.transform, false);

		StartCoroutine(detailPanel.GetComponent<DetailPanel>().Init(detailVideo, gameObject, isLocal));
	}

	public void LoadPage()
	{
		videosPerPage = CalculateNumVideosPerPage();

		videoContainer.SetActive(false);
		spinner.gameObject.SetActive(true);
		spinner.rectTransform.rotation = Quaternion.identity;

		var di = new DirectoryInfo(Application.persistentDataPath);

		//Note(Simon): Regex to match Guids
		var localVideos = di.GetDirectories("*-*-*-*-*");

		numPages = Mathf.Max(1, Mathf.CeilToInt(localVideos.Length / (float)videosPerPage));
		if (page > numPages)
		{
			page = numPages;
		}

		if (loadedVideos == null) 
		{ 
			loadedVideos = new VideoResponseSerialize(); 
			loadedVideos.videos = new List<VideoSerialize>();
		}

		loadedVideos.totalcount = localVideos.Length;
		int i;
		for (i = (page - 1) * videosPerPage; i < Mathf.Min(page * videosPerPage, localVideos.Length); i++)
		{
			var projectPath = localVideos[i].FullName;

			int loadedIndex = i % videosPerPage;

			if (loadedIndex > loadedVideos.videos.Count - 1)
			{
				try
				{
					var data = SaveFile.OpenFile(projectPath);
					var folderInfo = new DirectoryInfo(projectPath);

					loadedVideos.videos.Add(new VideoSerialize
					{
						title = data.meta.title,
						downloadsize = FileHelpers.DirectorySize(folderInfo),
						realTimestamp = folderInfo.LastWriteTime,
						id = localVideos[i].Name,
						compatibleVersion = !(data.meta.version > SaveFile.VERSION)
					});
				}
				catch
				{
					loadedVideos.videos.Add(new VideoSerialize
					{
						title = "Corrupted file",
						downloadsize = 0,
						realTimestamp = DateTime.MinValue,
						id = localVideos[i].Name,
						compatibleVersion = true
					});
				}
			}
			else if(localVideos[i].Name != loadedVideos.videos[loadedIndex].id)
			{
				var data = SaveFile.OpenFile(projectPath);
				var folderInfo = new DirectoryInfo(projectPath);
				loadedVideos.videos[loadedIndex] = new VideoSerialize
				{
					title = data.meta.title,
					downloadsize = FileHelpers.DirectorySize(folderInfo),
					realTimestamp = folderInfo.LastWriteTime,
					id = localVideos[i].Name,
					compatibleVersion = !(data.meta.version > SaveFile.VERSION)
				};
				
			}
		}

		var index = (i - 1) % videosPerPage + 1;
		loadedVideos.videos.RemoveRange(index, loadedVideos.videos.Count - index);

		videoContainer.SetActive(true);
		spinner.gameObject.SetActive(false);

		noVideos.enabled = loadedVideos.videos.Count == 0;


		BuildVideoGameObjects(true);
	}

	public void Previous()
	{
		if (page > 1)
		{
			page--;
		}

		LoadPage();
	}

	public void Next()
	{
		if (page < numPages)
		{
			page++;
		}

		LoadPage();
	}

	public void StartImportVideo()
	{
		//NOTE(Simon): Hide window by making scale 0
		transform.localScale = new Vector3(0, 0, 0);

		//NOTE(Simon): Wait for import path from explorer panel
		importPanel = Instantiate(UIPanels.Instance.importPanel, Canvass.main.transform, false);
	}

	public void PlayVideoVR(VideoSerialize video)
	{
		StartCoroutine(Player.Instance.EnableVR());
		PlayVideo(video);
	}

	public void PlayVideo(VideoSerialize video)
	{
		answered = true;
		answerVideoId = video.id;
		if (detailPanel != null)
		{
			Destroy(detailPanel);
			detailPanel = null;
		}
	}

	private void BuildVideoGameObjects(bool isLocal)
	{
		isFinishedLoadingVideos = false;

		videoPool.EnsureActiveCount(loadedVideos.videos.Count);
		
		for (int i = 0; i < loadedVideos.videos.Count; i++)
		{
			var v = loadedVideos.videos[i];
			StartCoroutine(videoPool[i].GetComponent<IndexPanelVideo>().SetData(v, isLocal, this));
		}

		isFinishedLoadingVideos = true;
	}

	//NOTE(Simon): This is used as a callback, which is why it receives the seemingly unnecessary 'shouldShow' parameter
	public void ShowUpdateNoticeIfNecessary(bool shouldShow)
	{
		updateAvailableNotification.SetActive(shouldShow);
		if (shouldShow)
		{
			updateAvailableNotification.GetComponent<Button>().onClick.AddListener(InitUpdatePanel);
		}
	}

	public void InitUpdatePanel()
	{
		EventSystem.current.SetSelectedGameObject(null);
		modalBackground.SetActive(true);
		var panel = Instantiate(UIPanels.Instance.autoUpdatePanel, Canvass.main.transform, false).GetComponent<AutoUpdatePanel>();
		panel.Init(AutoUpdatePanel.VivistaApplication.Player, OnUpdateCancel);
	}

	public void OnUpdateCancel()
	{
		modalBackground.SetActive(false);
	}

	private int CalculateNumVideosPerPage()
	{
		return videoContainer.GetComponent<FlowLayoutGroup>().CalcMaxChildren(videoPrefab.GetComponent<RectTransform>().sizeDelta);
	}
}
