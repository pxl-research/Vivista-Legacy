using System.Collections.Generic;
using UnityEngine;

//NOTE(Simon): This pool recycles GameObjects. When a GO is removed, it is added to a list of inactive GO to be used next time a GO is asked for.
//NOTE(Simon): EnsureActiveCount is useful if you know how many GO you need. It will Instantiate or Remove them in bulk. But it will remove the last in the list, without regard. So don't use if you need fixed references.
//NOTE(Simon): If you need fixed references, always use Get/Remove
public class GameObjectPool
{
	private List<GameObject> activeObjects = new List<GameObject>();
	private List<GameObject> inactiveObjects = new List<GameObject>();

	public GameObject prototype;
	public Transform parent;

	public int Count
	{
		get { return activeObjects.Count; }
	}

	public GameObjectPool(GameObject prototype, Transform parent)
	{
		this.prototype = prototype;
		this.parent = parent;
	}

	public GameObject Get()
	{
		if (inactiveObjects.Count == 0)
		{
			inactiveObjects.Add(GameObject.Instantiate(prototype, parent));
		}
		var active = inactiveObjects[inactiveObjects.Count - 1];
		inactiveObjects.RemoveAt(inactiveObjects.Count - 1);
		activeObjects.Add(active);
		active.SetActive(true);
		return active;
	}

	public void EnsureActiveCount(int count)
	{
		while (count > activeObjects.Count)
		{
			Get();
		}

		while (count < activeObjects.Count)
		{
			RemoveLastActive();
		}
	}

	public void RemoveLastActive()
	{
		var inactive = activeObjects[activeObjects.Count - 1];
		activeObjects.RemoveAt(activeObjects.Count - 1);
		inactiveObjects.Add(inactive);
		inactive.SetActive(false);
	}

	//NOTE(Simon): Uses list, so removing elements in the middle implies an array copy
	public void Remove(GameObject toRemove)
	{
		int index = activeObjects.IndexOf(toRemove);
		var inactive = activeObjects[index];
		activeObjects.RemoveAt(index);
		inactiveObjects.Add(inactive);
		inactive.SetActive(false);
	}

	public void ClearActive()
	{
		inactiveObjects.AddRange(activeObjects);
		foreach (var obj in activeObjects)
		{
			obj.SetActive(false);
		}
		activeObjects.Clear();
	}

	public GameObject this[int i]
	{
		get
		{
			return activeObjects[i];
		}
	}
}
