using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Tag
{
	public int id;
	public Color color;
	public int shapeIndex;
	public string name;
}

public class TagManager : MonoBehaviour
{
	public Sprite[] shapes;
	public static TagManager Instance { get; private set; }

	public List<Tag> tags;
	//NOTE(Simon): indexCounter always starts at 1;
	private int indexCounter = 1;

	public static List<Tag> defaultTags = new List<Tag>
	{
		new Tag {id = 1, name = "Instruction", color = new Color(0.8666667f, 0.8f, 0.4666667f), shapeIndex = 2},
		new Tag {id = 2, name = "Information", color = new Color(0.06666667f, 0.4666667f, 0.2f), shapeIndex = 1},
		new Tag {id = 3, name = "Test", color = new Color(0.5333334f, 0.8f, 0.9333334f), shapeIndex = 0},
	};

	void Start()
	{
		tags = new List<Tag>();
		Instance = this;
	}

	public bool AddTag(string name, Color color, int shapeIndex)
	{
		bool error = false;
		for (int i = 0; i < tags.Count; i++)
		{
			if (tags[i].name == name)
			{
				error = true;
				break;
			}
		}

		if (String.IsNullOrEmpty(name))
		{
			error = true;
		}

		if (error)
		{
			return false;
		}

		indexCounter++;
		tags.Add(new Tag
		{
			id = indexCounter,
			name = name,
			color = color,
			shapeIndex = shapeIndex
		});

		return true;
	}

	public void RemoveTag(string name)
	{
		for (int i = tags.Count - 1; i >= 0; i--)
		{
			if (tags[i].name == name)
			{
				tags.RemoveAt(i);
				break;
			}
		}
	}

	public Tag GetTagById(int tagId)
	{
		if (tagId <= 0 || tagId > indexCounter)
		{
			return null;
		}

		for (int i = 0; i < tags.Count; i++)
		{
			if (tags[i].id == tagId)
			{
				return tags[i];
			}
		}

		return null;
	}

	public Sprite ShapeForIndex(int index)
	{
		return shapes[index];
	}

	public Sprite[] GetAllShapes()
	{
		return shapes;
	}

	public void SetTags(List<Tag> newTags)
	{
		tags = newTags;

		for (int i = 0; i < tags.Count; i++)
		{
			if (tags[i].id > indexCounter)
			{
				indexCounter = tags[i].id;
			}
		}
	}

	public List<Tag> Filter(string text)
	{
		var matches = new List<Tag>();

		for (int i = 0; i < tags.Count; i++)
		{
			if (tags[i].name.ToLowerInvariant().Contains(text.ToLowerInvariant()))
			{
				matches.Add(tags[i]);
			}
		}

		return matches;
	}
}
