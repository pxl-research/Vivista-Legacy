using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR;

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
	private enum AgeOptions
	{
		Forever,
		Today,
		ThisWeek,
		ThisMonth,
		ThisYear,
	}

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
	public List<GameObject> videos;
	public Button previousPage;
	public Button nextPage;
	public Image spinner;
	public Text noVideos;
	public GameObject serverConnectionError;
	public GameObject filters;

	public Button2 localButton;
	public Button2 internetButton;
	public bool isLocal;
	public bool isFinishedLoadingVideos;

	public Dropdown2 searchAge;

	private float downloadedMessageTime;
	private const float downloadedMessageRefreshTime = 1.0f;
	private float lastFilterInteractionTime;
	private const float filterInteractionRefreshTime = 0.4f;

	private int page = 1;
	private int numPages = 1;
	private int videosPerPage = 9;
	private int totalVideos = 1;

	private int searchParamAgeDays;
	private string searchParamText;
	private string searchParamAuthor;

	private VideoResponseSerialize loadedVideos;
	private VideoSerialize detailVideo;

	private Coroutine loadFunction;

	public void Start()
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
			var url = $"{Web.videoUrl}?id={guid}";
			var www = UnityWebRequest.Get(url);

			www.SendWebRequest();

			while (!www.isDone) { }

			detailVideo = JsonUtility.FromJson<VideoSerialize>(www.downloadHandler.text);
			if (detailVideo != null && detailVideo.id != null)
			{
				ShowVideoDetails(detailVideo);
				return;
			}
		}

		pageLabels = new List<GameObject>();
		isLocal = true;

		LoadPage();

		searchAge.options.Clear();
		foreach (var option in Enum.GetNames(typeof(AgeOptions)))
		{
			var cleanName = new StringBuilder();

			for (int i = 0; i < option.Length; i++)
			{
				if (option[i] > 65 && option[i] < 90)
				{
					cleanName.Append(" ");
				}

				cleanName.Append(option[i]);
			}

			searchAge.options.Add(new Dropdown.OptionData {text = cleanName.ToString(), image = null});
		}

		page = 1;
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
				answered = true;
				answerVideoId = panel.answerVideoId;
				//TODO(Simon): Does this work if destroy is triggered at wrong time? e.g. destroy before this.answered can be read
				Destroy(detailPanel);
				detailPanel = null;
			}
		}

		//Note(Simon): Spinner animation
		if (spinner.gameObject.activeSelf)
		{
			spinner.rectTransform.Rotate(0, 0, -1f);
		}

		//Note(Simon): Video Hovering & clicking
		if (videoContainer.activeSelf)
		{
			for (int i = 0; i < videos.Count; i++)
			{
				var rect = new Vector3[4];
				videos[i].GetComponent<RectTransform>().GetWorldCorners(rect);

				bool hovering;

				//NOTE(Simon): Check if hovering
				hovering = Input.mousePosition.x > rect[0].x && Input.mousePosition.x < rect[2].x && Input.mousePosition.y > rect[0].y && Input.mousePosition.y < rect[2].y && !searchAge.isOpen();

				//TODO: Get this to work with Hittable
				if (!XRSettings.enabled)
				{
					videos[i].GetComponent<Image>().color = hovering ? new Color(0, 0, 0, 0.1f) : new Color(0, 0, 0, 0f);
				}

				if (hovering && Input.GetMouseButtonDown(0))
				{
					detailVideo = loadedVideos.videos[i];
					ShowVideoDetails(detailVideo);
				}
			}
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
				var pageLabel = pageLabels[pageLabels.Count];
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

		//NOTE(Simon): "Downloaded" message display
		foreach (var video in videos)
		{
			downloadedMessageTime += Time.deltaTime;
			if ((downloadedMessageTime > downloadedMessageRefreshTime) && isFinishedLoadingVideos)
			{
				video.GetComponent<IndexPanelVideo>().Refresh();
				downloadedMessageTime = 0;
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

		if (Time.time > lastFilterInteractionTime + filterInteractionRefreshTime)
		{
			LoadPage();
			lastFilterInteractionTime = float.MaxValue;
		}
	}

	public void ShowVideoDetails(VideoSerialize video)
	{
		detailPanel = Instantiate(detailPanelPrefab);
		detailPanel.transform.SetParent(Canvass.main.transform, false);

		StartCoroutine(detailPanel.GetComponent<DetailPanel>().Init(detailVideo, gameObject, isLocal));
	}

	public void LoadPage()
	{
		if (loadFunction != null)
		{
			StopCoroutine(loadFunction);
		}

		if (isLocal)
		{
			LoadLocalPageInternal();
		}
		else
		{
			loadFunction = StartCoroutine(LoadInternetPageInternal());
		}
	}

	public IEnumerator LoadInternetPageInternal()
	{
		serverConnectionError.SetActive(false);
		videoContainer.SetActive(false);
		spinner.gameObject.SetActive(true);
		spinner.rectTransform.rotation = Quaternion.identity;

		var offset = (page - 1) * videosPerPage;

		var url = $"{Web.indexUrl}?count={videosPerPage}&offset={offset}";
		if (searchParamAgeDays > 0)
		{
			url += $"&agedays={searchParamAgeDays}";
		}
		if (!string.IsNullOrEmpty(searchParamText))
		{
			url += $"&search={searchParamText}";
		}
		if (!string.IsNullOrEmpty(searchParamAuthor))
		{
			url += $"&author={searchParamAuthor}";
		}

		var www = UnityWebRequest.Get(url);

		yield return www.SendWebRequest();
		spinner.gameObject.SetActive(false);

		if (www.isNetworkError || www.isHttpError)
		{
			serverConnectionError.SetActive(true);
			yield break;
		}

		loadedVideos = JsonUtility.FromJson<VideoResponseSerialize>(www.downloadHandler.text);

		videoContainer.SetActive(true);
		noVideos.enabled = loadedVideos.videos.Count == 0;

		for (int i = offset; i < loadedVideos.videos.Count; i++)
		{
			var video = loadedVideos.videos[i];
			video.realTimestamp = DateTime.Parse(video.timestamp);
		}

		totalVideos = loadedVideos.totalcount;
		numPages = Mathf.Max(1, Mathf.CeilToInt(totalVideos / (float)videosPerPage));
		page = loadedVideos.page;

		//Note(Simon): Videos
		{
			StartCoroutine(BuildVideoGameObjects(false));
		}
	}

	public void LoadLocalPageInternal()
	{
		serverConnectionError.SetActive(false);
		videoContainer.SetActive(false);
		spinner.gameObject.SetActive(true);
		spinner.rectTransform.rotation = Quaternion.identity;

		var di = new DirectoryInfo(Application.persistentDataPath);

		//Note(Simon): Regex to match Guids
		var localVideos = di.GetDirectories("*-*-*-*-*");

		if (loadedVideos == null) { loadedVideos = new VideoResponseSerialize(); }
		loadedVideos.videos = new List<VideoSerialize>();

		loadedVideos.totalcount = localVideos.Length;

		for (var i = (page - 1) * videosPerPage; i < Mathf.Min(page * videosPerPage, localVideos.Length); i++)
		{
			var projectPath = localVideos[i].FullName;
			var folderInfo = new DirectoryInfo(projectPath);

			try
			{
				var data = SaveFile.OpenFile(projectPath);

				loadedVideos.videos.Add(new VideoSerialize
				{
					title = data.meta.title,
					description = data.meta.description,
					downloadsize = SaveFile.DirectorySize(folderInfo),
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
					description = "",
					downloadsize = 0,
					realTimestamp = DateTime.MinValue,
					id = localVideos[i].Name,
					compatibleVersion = true
				});
			}
		}

		videoContainer.SetActive(true);
		spinner.gameObject.SetActive(false);

		noVideos.enabled = loadedVideos.videos.Count == 0;

		totalVideos = loadedVideos.totalcount;
		numPages = Mathf.Max(1, Mathf.CeilToInt(totalVideos / (float)videosPerPage));

		//Note(Simon): Videos
		{
			StartCoroutine(BuildVideoGameObjects(true));
		}
	}

	public List<GameObject> LoadedVideos()
	{
		return videos;
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

	public void SetSearchAge(int index)
	{
		switch ((AgeOptions)index)
		{
			case AgeOptions.Today:
				searchParamAgeDays = 1;
				break;
			case AgeOptions.ThisWeek:
				searchParamAgeDays = 7;
				break;
			case AgeOptions.ThisMonth:
				searchParamAgeDays = 31;
				break;
			case AgeOptions.ThisYear:
				searchParamAgeDays = 365;
				break;
			case AgeOptions.Forever:
				searchParamAgeDays = -1;
				break;
		}

		lastFilterInteractionTime = Time.time;
		page = 1;
	}

	public void SetSearchText(string text)
	{
		searchParamText = text;
		lastFilterInteractionTime = Time.time;
		page = 1;
	}

	public void SetAuthorText(string author)
	{
		searchParamAuthor = author;
		lastFilterInteractionTime = Time.time;
		page = 1;
	}

	public void SetLocal()
	{
		isLocal = true;
		localButton.GetComponent<Image>().color = new Color(1, 1, 1);
		internetButton.GetComponent<Image>().color = new Color(200f / 255, 200f / 255, 200f / 255);
		filters.SetActive(false);
		LoadPage();
	}

	public void SetInternet()
	{
		isLocal = false;
		localButton.GetComponent<Image>().color = new Color(200f / 255, 200f / 255, 200f / 255);
		internetButton.GetComponent<Image>().color = new Color(1, 1, 1);
		filters.SetActive(true);
		LoadPage();
	}

	public void StartImportVideo()
	{
		//NOTE(Simon): Hide window by making scale 0
		transform.localScale = new Vector3(0, 0, 0);

		//NOTE(Simon): Wait for import path from explorer panel
		importPanel = Instantiate(UIPanels.Instance.importPanel, Canvass.main.transform, false);
	}

	private IEnumerator BuildVideoGameObjects(bool isLocal)
	{
		isFinishedLoadingVideos = false;

		var videosThisPage = loadedVideos.videos ?? new List<VideoSerialize>();
		while (videos.Count < Mathf.Min(videosPerPage, videosThisPage.Count))
		{
			var video = Instantiate(videoPrefab);
			video.transform.SetParent(videoContainer.transform, false);
			videos.Add(video);
		}
		while (videos.Count > Mathf.Min(videosPerPage, videosThisPage.Count))
		{
			var video = videos[videos.Count - 1];
			videos.RemoveAt(videos.Count - 1);
			Destroy(video);
		}

		for (int i = 0; i < videosThisPage.Count; i++)
		{
			var v = videosThisPage[i];
			StartCoroutine(videos[i].GetComponent<IndexPanelVideo>().SetData(v, isLocal));
			//TODO(Simon): Is this needed?
			//NOTE(Kristof): Space out SetData over some time to prevent lag spike in VR
			if (XRSettings.enabled)
			{
				yield return new WaitForSeconds(0.1f);
			}
		}

		isFinishedLoadingVideos = true;
	}
}
