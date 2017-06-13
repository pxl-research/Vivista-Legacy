using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
class VideoSerialize
{
	public string uuid;
	public int userid;
	public DateTime timestamp;
	public int downloadSize;
}

[Serializable]
class VideoResponseSerialize
{
	public int totalcount;
	public int page;
	public int count;
	public VideoSerialize[] videos;
}

public class IndexPanel : MonoBehaviour 
{
	public bool answered;
	public string answerVideoId;

	public GameObject pageLabelPrefab;

	public GameObject pageLabelContainer;
	public List<GameObject> pageLabels;
	public Button previousPage;
	public Button nextPage;

	private int page = 1;
	private int numPages = 1;
	private int itemsPerPage = 9;
	private int totalItems = 1;


	public void Start()
	{
		pageLabels = new List<GameObject>();
		foreach (Transform child in pageLabelContainer.transform)
		{
			pageLabels.Add(child.gameObject);
		}
		LoadPage();
		page = 1;
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

	public void LoadPage()
	{
		var offset = (page - 1) * itemsPerPage;

		var url = string.Format("{0}?count={1}&offset={2}", Web.indexUrl, itemsPerPage, offset);
		var www = new WWW(url);

		while(!www.isDone) {}

		var response = JsonUtility.FromJson<VideoResponseSerialize>(www.text);
		/*
		totalItems = response.totalcount;
		numPages = Mathf.CeilToInt(totalItems / (float)response.count);
		page = response.page;
		*/

		totalItems = 9;
		numPages = 1;
		const int numLabels = 11;

		var labelsNeeded = Mathf.Min(numPages, numLabels);

		if (pageLabels.Count < labelsNeeded)
		{
			for(int i = pageLabels.Count; i < labelsNeeded; i++)
			{
				var newLabel = Instantiate(pageLabelPrefab);
				pageLabels.Add(newLabel);
				newLabel.transform.SetParent(pageLabelContainer.transform, false);
			}
		}
		if (pageLabels.Count > labelsNeeded)
		{
			for(int i = pageLabels.Count; i > labelsNeeded; i--)
			{
				var pageLabel = pageLabels[pageLabels.Count];
				pageLabels.RemoveAt(pageLabels.Count - 1);
				Destroy(pageLabel);
			}
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
				var end = Mathf.Clamp(page + 5, numPages - 10, numPages);

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
