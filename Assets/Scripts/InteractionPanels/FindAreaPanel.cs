using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class FindAreaPanel : MonoBehaviour
{
	public GameObject areaEntryPrefab;
	public Text title;
	public RectTransform areaList;

	private List<AreaEntry> entries = new List<AreaEntry>();
	private List<Area> areas;
	private Guid guid;

	private void OnEnable()
	{
		if (areas == null)
		{
			return;
		}

		for (int i = 0; i < areas.Count; i++)
		{
			var filename = areas[i].miniatureName;
			var path = Path.Combine(Application.persistentDataPath, guid.ToString(), SaveFile.miniaturesPath);
			var fullPath = Path.Combine(path, filename);

			StartCoroutine(entries[i].SetArea(areas[i], fullPath, true));
		}
	}

	public void Init(string newTitle, Guid newGuid, List<Area> newAreas)
	{
		title.text = newTitle;
		areas = newAreas;
		guid = newGuid;

		foreach (var area in newAreas)
		{
			var filename = area.miniatureName;
			var path = Path.Combine(Application.persistentDataPath, newGuid.ToString(), SaveFile.miniaturesPath);
			var fullPath = Path.Combine(path, filename);

			var go = Instantiate(areaEntryPrefab, areaList);
			var entry = go.GetComponent<AreaEntry>();
			entries.Add(entry);
			StartCoroutine(entry.SetArea(area, fullPath, true));
		}
	}
}
