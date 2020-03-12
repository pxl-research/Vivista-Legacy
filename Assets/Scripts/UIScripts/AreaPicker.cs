using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AreaPicker : MonoBehaviour, IDisposable
{
	public Button answerButton;
	public bool answered;
	public Area answerArea = new Area();
	
	private List<GameObject> areaPoints = new List<GameObject>();

	public Material pointMaterial;

	public GameObject areaRendererPrefab;
	private GameObject indicator;
	private GameObject goContainer;
	private AreaRenderer areaRenderer;

	private bool dirty;

	private bool eligibleForPlacement;
	private bool isDragging;
	private int dragIndex;
	private Vector3 dragStartPost;
	private GameObject dragObject;

	private static int AreaLayer;

	void Awake()
	{
		AreaLayer = LayerMask.NameToLayer("Area");

		goContainer = new GameObject("goContainer");

		indicator = NewPoint(Vector3.zero);
		indicator.name = "indicator";
		indicator.layer = 0;
		Destroy(indicator.GetComponent<SphereCollider>());

		areaRenderer = Instantiate(areaRendererPrefab).GetComponent<AreaRenderer>();

		MouseLook.Instance.forceActive = true;
	}

	public void Init(Area area)
	{
		answerArea = area;

		for (int i = 0; i < area.vertices.Count; i++)
		{
			areaPoints.Add(NewPoint(area.vertices[i]));

			dirty = true;
		}
	}

	public string MakeMiniature(Guid projectGuid)
	{
		var camGo = new GameObject();
		var cam = camGo.AddComponent<Camera>();
		cam.cullingMask = LayerMask.GetMask("Area");

		var bounds = areaRenderer.GetBounds();
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
		var path = Path.Combine(Application.persistentDataPath, projectGuid.ToString(), SaveFile.miniaturesPath);
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

	void Update()
	{
		answerButton.interactable = answerArea.vertices.Count >= 3;

		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		ray = ray.ReverseRay();
		ray.direction = -ray.origin;
		RaycastHit hit;

		if (isDragging)
		{
			if (Input.GetMouseButtonUp(0))
			{
				answerArea.vertices[dragIndex] = dragObject.transform.position;
				isDragging = false;
				dragObject = null;
				indicator.SetActive(true);
			}
			else if (Input.GetKeyDown(KeyCode.Escape))
			{
				dragObject.transform.position = dragStartPost;
				isDragging = false;
				dragObject = null;
				dirty = true;
				indicator.SetActive(true);
			}
			else if (Physics.Raycast(ray, out hit, 100f))
			{
				dragObject.transform.position = hit.point;
				dirty = true;
				//NOTE(Simon): indicator is not visible at this point, but still update its position to prevent visual glitch on reappearance
				indicator.transform.position = hit.point;
			}
		}
		else if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("Area")))
		{
			eligibleForPlacement = false;
			Cursors.isOverridingCursor = true;
			Cursor.SetCursor(Cursors.Instance.CursorDrag, new Vector2(15, 15), CursorMode.Auto);
			indicator.SetActive(false);

			if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
			{
				isDragging = true;
				dragStartPost = hit.transform.position;
				dragObject = hit.transform.gameObject;
				dragIndex = answerArea.vertices.IndexOf(dragStartPost);
			}
		}
		else if (Physics.Raycast(ray, out hit, 100f))
		{
			Cursors.isOverridingCursor = false;
			indicator.SetActive(true);
			indicator.transform.position = hit.point;

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				answered = true;
				answerArea = null;
			}

			if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
			{
				//NOTE(Simon): Store if mouse button went down at a valid time (i.e. not while dragging)
				eligibleForPlacement = true;
			}
			if (eligibleForPlacement && Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
			{
				answerArea.vertices.Add(hit.point);

				areaPoints.Add(NewPoint(hit.point));

				dirty = true;
			}
		}

		if (dirty)
		{
			var vertices = new Vector3[areaPoints.Count];
			for (int i = 0; i < areaPoints.Count; i++)
			{
				vertices[i] = areaPoints[i].transform.position;
			}

			areaRenderer.SetVertices(vertices);

			dirty = false;
		}
	}

	private GameObject NewPoint(Vector3 position)
	{
		var newPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		newPoint.transform.position = position;
		newPoint.transform.SetParent(goContainer.transform, true);
		newPoint.name = "areaPoint";
		newPoint.GetComponent<Renderer>().sharedMaterial = pointMaterial;
		newPoint.layer = AreaLayer;

		return newPoint;
	}

	public void Answer()
	{
		answered = true;
		MouseLook.Instance.forceActive = false;
	}

	public void Dispose()
	{
		Destroy(goContainer);
		Destroy(areaRenderer.gameObject);
	}
}
