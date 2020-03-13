﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class MultipleChoiceAreaPanelEditor : MonoBehaviour
{
	public GameObject areaPickerPrefab;
	public GameObject multipleChoiceAreaEntryPrefab;

	public InputField title;
	public GameObject resizePanel;
	private AreaPicker areaPicker;
	public RectTransform areaList;
	private ToggleGroup2 group;

	public bool answered;
	public string answerTitle;
	public List<Area> answerAreas;
	public int answerCorrect;

	private bool editing;
	private GameObject editingGo;

	private Guid guid;
	
	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	public void Init(string newTitle, Guid newGuid, List<Area> newAreas, int newCorrect)
	{
		guid = newGuid;
		title.text = newTitle;
		title.onValueChanged.AddListener(delegate { OnInputChange(title); });
		answerCorrect = newCorrect;
		group = gameObject.AddComponent<ToggleGroup2>();

		if (newAreas != null)
		{
			answerAreas = newAreas;
			for (int i = 0; i < answerAreas.Count; i++)
			{
				var filename = answerAreas[i].miniatureName;
				var path = Path.Combine(Application.persistentDataPath, newGuid.ToString(), SaveFile.miniaturesPath);
				var fullPath = Path.Combine(path, filename);

				var go = Instantiate(multipleChoiceAreaEntryPrefab, areaList);
				var entry = go.GetComponent<MultipleChoiceAreaEntry>();
				StartCoroutine(entry.SetArea(answerAreas[i], fullPath));

				entry.deleteButton.onClick.RemoveAllListeners();
				entry.deleteButton.onClick.AddListener(() => OnDeleteArea(go));
				entry.editButton.onClick.RemoveAllListeners();
				entry.editButton.onClick.AddListener(() => OnEditArea(go));
				group.RegisterToggle(entry.toggle);
				entry.toggle.group = group;
			}
		}
		else
		{
			answerAreas = new List<Area>();
		}

		group.onToggleGroupChanged.AddListener(OnSelectCorrectArea);
	}

	void Update()
	{
		if (areaPicker != null && areaPicker.answered)
		{
			var filename = areaPicker.MakeMiniature(guid);
			var path = Path.Combine(Application.persistentDataPath, guid.ToString(), SaveFile.miniaturesPath);
			var fullPath = Path.Combine(path, filename);

			var go = editing ? editingGo : Instantiate(multipleChoiceAreaEntryPrefab, areaList);
			var entry = go.GetComponent<MultipleChoiceAreaEntry>();
			areaPicker.answerArea.miniatureName = filename;
			StartCoroutine(entry.SetArea(areaPicker.answerArea, fullPath));

			entry.deleteButton.onClick.RemoveAllListeners();
			entry.deleteButton.onClick.AddListener(() => OnDeleteArea(go));
			entry.editButton.onClick.RemoveAllListeners();
			entry.editButton.onClick.AddListener(() => OnEditArea(go));
			group.RegisterToggle(entry.toggle);
			entry.toggle.group = group;

			answerAreas.Add(areaPicker.answerArea);

			areaPicker.Dispose();
			Destroy(areaPicker.gameObject);
			resizePanel.SetActive(true);

			//NOTE(Simon): Reset the background color, in case it was red/invalid previously
			var background = areaList.parent.parent.GetComponent<Image>();
			background.color = Color.white;

			if (editing) { editing = false; }
		}
	}

	public void OnAddArea()
	{
		var go = Instantiate(areaPickerPrefab, Canvass.main.transform);
		areaPicker = go.GetComponent<AreaPicker>();
		resizePanel.SetActive(false);
	}

	public void OnDeleteArea(GameObject go)
	{
		var entry = go.GetComponent<MultipleChoiceAreaEntry>();
		group.UnregisterToggle(entry.toggle);
		File.Delete(entry.miniatureUrl);
		answerAreas.Remove(entry.area);
		Destroy(go);
	}

	public void OnEditArea(GameObject go)
	{
		var entry = go.GetComponent<MultipleChoiceAreaEntry>();
		areaPicker = Instantiate(areaPickerPrefab, Canvass.main.transform).GetComponent<AreaPicker>();
		var area = entry.area;

		areaPicker.Init(area);
		File.Delete(entry.miniatureUrl);

		editing = true;
		editingGo = go;
		resizePanel.SetActive(false);
	}

	public void OnSelectCorrectArea(Toggle toggle)
	{
		var toggles = group.GetAllToggles();
		for (int i = 0; i < toggles.Count; i++)
		{
			if (toggles[i].isOn)
			{
				answerCorrect = i;
			}

			toggles[i].image.color = Color.white;
		}
	}

	public void Answer()
	{
		bool errors = false;

		if (String.IsNullOrEmpty(title.text))
		{
			title.image.color = errorColor;
			errors = true;
		}

		if (answerAreas.Count < 2)
		{
			var background = areaList.parent.parent.GetComponent<Image>();
			background.color = errorColor;
			errors = true;
		}

		if (!group.AnyTogglesOn())
		{
			var toggles = group.GetAllToggles();
			for (int i = 0; i < toggles.Count; i++)
			{
				toggles[i].image.color = errorColor;
			}
			errors = true;
		}

		if (!errors)
		{
			answered = true;
			answerTitle = title.text;
		}
	}

	public void OnInputChange(InputField input)
	{
		input.image.color = Color.white;
	}
}