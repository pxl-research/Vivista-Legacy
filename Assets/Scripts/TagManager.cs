using System.Collections.Generic;
using UnityEngine;

public class Tag
{
	public Color color;
	public int imageIndex;
	public string name;
}

public class TagManager : MonoBehaviour
{
	public Sprite[] shapes;
	public static TagManager Instance { get; private set; }

	private List<Tag> tags;

	void Start()
	{
		tags = new List<Tag>();
		Instance = this;
	}

	public bool AddTag(string name, Color color, int imageIndex)
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

		if (string.IsNullOrEmpty(name))
		{
			error = true;
		}

		if (error)
		{
			return false;
		}

		tags.Add(new Tag
		{
			name = name,
			color = color,
			imageIndex = imageIndex
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

	public Sprite ShapeForIndex(int index)
	{
		return shapes[index];
	}

	public Sprite[] GetAllShapes()
	{
		return shapes;
	}
}
