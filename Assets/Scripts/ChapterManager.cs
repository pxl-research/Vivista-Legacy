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

	public bool AddChapter(string name, string description, out Chapter chapter)
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
			chapter = null;
			return false;
		}

		indexCounter++;
		chapter = new Chapter
		{
			name = name,
			description = description,
			time = 1f,
			id = indexCounter
		};
		chapters.Add(chapter);

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
		Debug.Log("Setting chapters");
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
		Debug.Log("Hit");
		if (controller == null)
		{
			controller = GameObject.Find("FileLoader").GetComponent<FileLoader>().controller;
		}

		controller.Seek(chapter.time);
	}

	public void Refresh()
	{
		SortChaptersChronologically();
	}

	private void SortChaptersChronologically()
	{
		chapters.Sort((x, y) => x.time.CompareTo(y.time));
	}

	public Chapter NextChapter(double time)
	{
		//NOTE(Simon): Find smallest chapter time larger than input. (i.e. beginning of next chapter)
		for (int i = 0; i < chapters.Count; i++)
		{
			if (chapters[i].time > time)
			{
				return chapters[i];
			}
		}
		return null;
	}

	public float NextChapterTime(double time)
	{
		var nextChapter = NextChapter(time);

		return nextChapter == null ? Single.PositiveInfinity : nextChapter.time;
	}

	public float CurrentChapterTime(double time)
	{
		var chapter = ChapterForTime(time);

		//NOTE(Simon): 0 represents a virtual chapter at time 0;
		return chapter != null ? chapter.time : 0;
	}

	public Chapter ChapterForTime(double time)
	{
		Chapter chapter = null;

		//NOTE(Simon): Find largest chapter time smaller than or equal to input. (i.e. beginning of chapter)
		for (int i = 0; i < chapters.Count; i++)
		{
			if (chapters[i].time <= time)
			{
				chapter = chapters[i];
			}
			else
			{
				break;
			}
		}

		return chapter;
	}
}
