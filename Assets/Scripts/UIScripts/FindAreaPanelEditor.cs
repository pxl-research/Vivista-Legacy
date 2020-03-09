using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Area
{
	public List<Vector3> vertices = new List<Vector3>();
	public string miniatureName;

	public static List<Area> ParseFromSave(string filename, string body)
	{
		var areas = new List<Area>();
		var filenames = filename.Split('\f');
		var vertices = body.Split('\f');
		for (int i = 0; i < filenames.Length; i++)
		{
			areas.Add(new Area
			{
				miniatureName = filenames[i],
				vertices = new List<Vector3>(JsonHelper.ToArray<Vector3>(vertices[i]))
			});
		}

		return areas;
	}
}

//TODO(Simon): error checking
public class FindAreaPanelEditor : MonoBehaviour
{
	public GameObject areaPickerPrefab;
	public GameObject areaEntryPrefab;

	public InputField title;
	public GameObject resizePanel;
	private AreaPicker areaPicker;
	public RectTransform areaList;

	public bool answered;
	public string answerTitle;
	public List<Area> answerAreas = new List<Area>();

	private bool editing;
	private GameObject editingGo;

	private Guid guid;

	private static Color errorColor = new Color(1, 0.8f, 0.8f, 1f);

	public void Init(string newTitle, Guid newGuid, List<Area> newAreas)
	{
		guid = newGuid;
		title.text = newTitle;
		title.onValueChanged.AddListener(delegate { OnInputChange(title); });

		if (newAreas != null)
		{
			answerAreas = newAreas;
			for (int i = 0; i < answerAreas.Count; i++)
			{
				var filename = answerAreas[i].miniatureName;
				var path = Path.Combine(Application.persistentDataPath, newGuid.ToString(), SaveFile.miniaturesPath);
				var fullPath = Path.Combine(path, filename);

				var go = Instantiate(areaEntryPrefab, areaList);
				var entry = go.GetComponent<AreaEntry>();
				StartCoroutine(entry.SetArea(answerAreas[i], fullPath));

				entry.deleteButton.onClick.RemoveAllListeners();
				entry.deleteButton.onClick.AddListener(() => OnDeleteArea(go));
				entry.editButton.onClick.RemoveAllListeners();
				entry.editButton.onClick.AddListener(() => OnEditArea(go));
			}
		}
		else
		{
			answerAreas = new List<Area>();
		}
	}

	void Update()
	{
		if (areaPicker != null && areaPicker.answered)
		{
			var filename = MakeMiniature();
			var path = Path.Combine(Application.persistentDataPath, guid.ToString(), SaveFile.miniaturesPath);
			var fullPath = Path.Combine(path, filename);

			var go = editing ? editingGo : Instantiate(areaEntryPrefab, areaList);
			var entry = go.GetComponent<AreaEntry>();
			areaPicker.answerArea.miniatureName = filename;
			StartCoroutine(entry.SetArea(areaPicker.answerArea, fullPath));

			entry.deleteButton.onClick.RemoveAllListeners();
			entry.deleteButton.onClick.AddListener(() => OnDeleteArea(go));
			entry.editButton.onClick.RemoveAllListeners();
			entry.editButton.onClick.AddListener(() => OnEditArea(go));

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

	private string MakeMiniature()
	{
		var camGo = new GameObject();
		var cam = camGo.AddComponent<Camera>();
		cam.cullingMask = LayerMask.GetMask("Area");

		var bounds = areaPicker.GetBounds();
		camGo.transform.LookAt(bounds.center, Vector3.up);

		var size = bounds.size.magnitude;
		var distanceFromPoint = (Camera.main.transform.position - bounds.center).magnitude;
		cam.fieldOfView = 2.0f * Mathf.Rad2Deg * Mathf.Atan((0.5f * size) / distanceFromPoint);

		var targetTexture = new RenderTexture(200, 200, 0);
		targetTexture.antiAliasing = 8;
		targetTexture.Create();

		var previousTexture = RenderTexture.active;
		RenderTexture.active = targetTexture;
		cam.targetTexture = targetTexture;
		cam.Render();

		var texture = new Texture2D(200, 200);
		texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0, false);
		texture.Apply();

		var textureData = texture.EncodeToPNG();
		var path = Path.Combine(Application.persistentDataPath, guid.ToString(), SaveFile.miniaturesPath);
		var filename = Guid.NewGuid() + ".png";
		var fullPath = Path.Combine(path, filename);

		Directory.CreateDirectory(path);

		using (var file = File.Open(fullPath, FileMode.OpenOrCreate))
		{
			file.Write(textureData, 0, textureData.Length);
		}

		Destroy(camGo);
		RenderTexture.active = previousTexture;
		targetTexture.Release();

		return filename;
	}

	public void OnAddArea()
	{
		var go = Instantiate(areaPickerPrefab, Canvass.main.transform);
		areaPicker = go.GetComponent<AreaPicker>();
		resizePanel.SetActive(false);
	}

	public void OnDeleteArea(GameObject go)
	{
		var entry = go.GetComponent<AreaEntry>();
		File.Delete(entry.miniatureUrl);
		answerAreas.Remove(entry.area);
		Destroy(go);
	}

	public void OnEditArea(GameObject go)
	{
		var entry = go.GetComponent<AreaEntry>();
		areaPicker = Instantiate(areaPickerPrefab, Canvass.main.transform).GetComponent<AreaPicker>();
		var area = entry.area;

		areaPicker.Init(area);
		File.Delete(entry.miniatureUrl);

		editing = true;
		editingGo = go;
		resizePanel.SetActive(false);
	}

	public void Answer()
	{
		bool errors = false;

		if (String.IsNullOrEmpty(title.text))
		{
			title.image.color = errorColor;
			errors = true;
		}

		if (answerAreas.Count == 0)
		{
			var background = areaList.parent.parent.GetComponent<Image>();
			background.color = errorColor;
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
