using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class VideoResponseSerialize
{
	[Serializable]
	public class VideoSerialize
	{
		public string uuid;
		public int userid;
		public string username;
		public string timestamp;
		[NonSerialized]
		public DateTime realTimestamp;
		public int downloadSize;

		public string title;
		public string thumbnail;
		public int length;
	}

	public int totalcount;
	public int page;
	public int count;
	public List<VideoSerialize> videos;
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

	public GameObject pageLabelContainer;
	public GameObject videoContainer;
	public List<GameObject> pageLabels;
	public List<GameObject> videos;
	public Button previousPage;
	public Button nextPage;
	public Image spinner;

	public Dropdown searchAge;

	private int page = 1;
	private int numPages = 1;
	private int videosPerPage = 9;
	private int totalVideos = 1;

	private int searchParamAgeDays;
	private string searchParamText;
	private string searchParamAuthor;

	private Coroutine LoadFunction;

	public void Start()
	{
		pageLabels = new List<GameObject>();
		
		LoadPageWrapper();

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
		//Note(Simon): Spinner
		{
			if (spinner.enabled)
			{
				spinner.rectTransform.Rotate(0, 0, -0.5f);
			}
		}
		//Note(Simon): Pages & labels
		{
			const int numLabels = 11;
			var labelsNeeded = Mathf.Min(numPages, numLabels);

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
				if (numPages <= numLabels)
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
					var end = Mathf.Clamp(page + 5, numLabels, numPages);

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
		
			previousPage.interactable = true;
			nextPage.interactable = true;

			if (page == 1)
			{
				previousPage.interactable = false;
			}
			if (numPages == 1 || page == (numPages))
			{
				nextPage.interactable = false;
			}
		}
	}

	public void LoadPageWrapper()
	{
		if (LoadFunction != null)
		{
			StopCoroutine(LoadFunction);
		}

		LoadFunction = StartCoroutine(LoadPage());
	}

	public IEnumerator LoadPage()
	{
		videoContainer.SetActive(false);
		spinner.enabled = true;
		spinner.rectTransform.rotation = Quaternion.identity;

		var offset = (page - 1) * videosPerPage;

		var url = string.Format("{0}?count={1}&offset={2}", Web.indexUrl, videosPerPage, offset);
		if (searchParamAgeDays > 0)
		{
			url += String.Format("&agedays={0}", searchParamAgeDays);
		}
		if (!String.IsNullOrEmpty(searchParamAuthor))
		{
			url += String.Format("&search={0}", searchParamText);
		}
		if (!String.IsNullOrEmpty(searchParamAuthor))
		{
			url += String.Format("&author={0}", searchParamAuthor);
		}

		var www = new WWW(url);
		
		yield return www;
		videoContainer.SetActive(true);
		spinner.enabled = false;


		var response = JsonUtility.FromJson<VideoResponseSerialize>(www.text);
		for(int i = offset; i < response.videos.Count; i++)
		{
			response.videos[i].realTimestamp = DateTime.Parse(response.videos[i].timestamp);
		}
		
		totalVideos = response.totalcount;
		numPages = Mathf.CeilToInt(totalVideos / (float)response.count);
		page = response.page;

		//Note(Simon): Videos
		{
			var videosThisPage = response.videos ?? new List<VideoResponseSerialize.VideoSerialize>();
			while (videos.Count < Math.Min(videosPerPage, videosThisPage.Count))
			{
				var video = Instantiate(videoPrefab);
				video.transform.SetParent(videoContainer.transform, false);
				videos.Add(video);
			}
			while (videos.Count > Math.Min(videosPerPage, videosThisPage.Count))
			{
				var video = videos[videos.Count - 1];
				videos.RemoveAt(videos.Count - 1);
				Destroy(video);
			}

			for (int i = 0; i < videosThisPage.Count; i++)
			{
				var v = videosThisPage[i];
				videos[i].GetComponent<IndexPanelVideo>().SetData(v.title, v.username, v.thumbnail, v.downloadSize, v.length, v.realTimestamp);
			}
		}

	}

	public void Previous()
	{
		if (page > 1)
		{
			page--;
		}

		LoadPageWrapper();
	}

	public void Next()
	{
		if (page < numPages)
		{
			page++;
		}

		LoadPageWrapper();
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

		LoadPageWrapper();
		page = 1;
	}

	public void SetSearchText(string text)
	{
		searchParamText = text;
		LoadPageWrapper();
		page = 1;
	}

	public void SetAuthorText(string author)
	{
		searchParamText = author;
		LoadPageWrapper();
		page = 1;
	}
}
