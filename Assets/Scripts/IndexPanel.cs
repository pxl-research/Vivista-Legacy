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
	}

	public void Previous()
	{
		if (page > 1)
		{
			page--;
		}
	}

	public void Next()
	{
		if (page < numPages)
		{
			page++;
		}
	}

	public void LoadPage()
	{
		var offset = (page - 1) * itemsPerPage;

		var url = string.Format("{0}?count={1}&offset={2}", Web.indexUrl, itemsPerPage, offset);
		var www = new WWW(url);

		while(!www.isDone) {}

		var response = JsonUtility.FromJson<VideoResponseSerialize>(www.text);

		totalItems = response.totalcount;
		numPages = Mathf.CeilToInt(totalItems / (float)response.count);
		page = response.page;

		if (pageLabels.Count < Mathf.Min(numPages, 11))
		{
			for(int i = pageLabels.Count; i < numPages; i++)
			{
				var newLabel = Instantiate(pageLabelPrefab);
				pageLabels.Add(newLabel);
				newLabel.transform.SetParent(pageLabelContainer.transform, false);
			}
		}
		if (pageLabels.Count > Mathf.Min(numPages, 11))
		{
			for(int i = pageLabels.Count; i > numPages; i--)
			{
				var pageLabel = pageLabels[pageLabels.Count];
				pageLabels.RemoveAt(pageLabels.Count - 1);
				Destroy(pageLabel);
			}
		}

		if (numPages <= 10)
		{
			for (int i = 0; i < pageLabels.Count; i++)
			{
				pageLabels[i].GetComponent<Text>().text = (i + 1).ToString();
			}
		}
		else
		{
			pageLabels[0].GetComponent<Text>().text = "1";
			pageLabels[1].GetComponent<Text>().text = "...";
			var index = 2;
			for (int i = page - 3; i < page + 3; i++)
			{
				pageLabels[index++].GetComponent<Text>().text = (i + 1).ToString();
			}
			pageLabels[pageLabels.Count - 2].GetComponent<Text>().text = "...";
			pageLabels[pageLabels.Count - 1].GetComponent<Text>().text = numPages.ToString();
		}
		
		previousPage.interactable = true;
		nextPage.interactable = true;

		if (page == 1)
		{
			previousPage.interactable = false;
		}
		if (numPages == 1 || page == numPages)
		{
			nextPage.interactable = false;
		}
	}
}
