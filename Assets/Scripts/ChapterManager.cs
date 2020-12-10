using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Chapter
{
	public int id;
	public string name;
	public string description;
	public float time;
}

public class ChapterManager : MonoBehaviour
{
	public static ChapterManager Instance { get; private set; }
	public List<Chapter> chapters;

	public VideoController controller;

	//NOTE(Simon): Always explicitly start at 1
	private int indexCounter = 1;

	void Start()
	{
		chapters = new List<Chapter>();
		Instance = this;
	}

	public Chapter GetChapterById(int id)
	{
		for (int i = 0; i < chapters.Count; i++)
		{
			if (chapters[i].id == id)
			{
				return chapters[i];
			}
		}

		return null;
	}

	//NOTE(Simon): Filters titles only. Should maybe also filter on description?
	public List<Chapter> Filter(string text)
	{
		var matches = new List<Chapter>();

		for (int i = 0; i < chapters.Count; i++)
		{
			if (chapters[i].name.ToLowerInvariant().Contains(text.ToLowerInvariant()))
			{
				matches.Add(chapters[i]);
			}
		}

		return matches;
	}

	public bool AddChapter(string name, string description)
	{
		bool error = false;
		for (int i = 0; i < chapters.Count; i++)
		{
			if (chapters[i].name == name)
			{
				error = true;
				break;
			}
		}

		if (string.IsNullOrEmpty(name))
		{
			error = true;
		}

		if (error)
		{
			return false;
		}

		indexCounter++;
		chapters.Add(new Chapter
		{
			name = name,
			description = description,
			time = 1f,
			id = indexCounter
		});

		UnsavedChangesTracker.Instance.unsavedChanges = true;
		SortChaptersChronologically();

		return true;
	}

	public void RemoveChapter(string name)
	{
		for (int i = chapters.Count - 1; i >= 0; i--)
		{
			if (chapters[i].name == name)
			{
				chapters.RemoveAt(i);
				break;
			}
		}

		UnsavedChangesTracker.Instance.unsavedChanges = true;
	}

	public void SetChapters(List<Chapter> newChapters)
	{
		chapters = newChapters;

		for (int i = 0; i < chapters.Count; i++)
		{
			if (chapters[i].id > indexCounter)
			{
				indexCounter = chapters[i].id;
			}
		}

		SortChaptersChronologically();
	}

	public void GoToChapter(Chapter chapter)
	{
		if (controller == null)
		{
			controller = GameObject.Find("FileLoader").GetComponent<FileLoader>().controller;
		}

		controller.Seek(chapter.time);
	}

	private void SortChaptersChronologically()
	{
		chapters.Sort((x, y) => x.time.CompareTo(y.time));
	}

	public float NextChapterTime(double time)
	{
		//NOTE(Simon): Find smallest chapter time larger than input. (i.e. beginning of next chapter)
		for (int i = 0; i < chapters.Count; i++)
		{
			if (chapters[i].time > time)
			{
				return chapters[i].time;
			}
		}

		return Single.PositiveInfinity;
	}

	public float CurrentChapterTime(double time)
	{
		float largestTime = 0;
		//NOTE(Simon): Find largest chapter time smaller than or equal to input. (i.e. beginning of current chapter)
		for (int i = 0; i < chapters.Count; i++)
		{
			if (chapters[i].time <= time)
			{
				largestTime = chapters[i].time;
			}
			else
			{
				break;
			}
		}

		//NOTE(Simon): Zero represents a "virtual" chapter at the beginning of the video. Useful if no chapter was defined at the beginning.
		return largestTime;
	}
}
