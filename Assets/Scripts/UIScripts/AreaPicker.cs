using System;
using System.Collections.Generic;
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
	public Material polygonMaterial;

	private GameObject indicator;
	private GameObject goContainer;
	private GameObject polygon;
	private MeshFilter mesh;
	private GameObject polygonOutline;
	private MeshFilter outlineMesh;

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

		polygon = new GameObject("mesh");
		polygon.transform.SetParent(goContainer.transform);
		polygon.layer = AreaLayer;
		
		var r = polygon.AddComponent<MeshRenderer>();
		mesh = polygon.AddComponent<MeshFilter>();
		r.material = polygonMaterial;

		polygonOutline = new GameObject("meshOutline");
		polygonOutline.transform.SetParent(goContainer.transform);
		polygonOutline.layer = AreaLayer;
		r = polygonOutline.AddComponent<MeshRenderer>();
		outlineMesh = polygonOutline.AddComponent<MeshFilter>();
		r.material = pointMaterial;

		MouseLook.Instance.forceActive = true;
	}

	public void Init(Area area)
	{
		answerArea = area;

		for (int i = 0; i < area.rayOrigins.Count; i++)
		{
			areaPoints.Add(NewPoint(area.vertices[i]));

			dirty = true;
		}
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
				answerArea.rayOrigins[dragIndex] = ray.origin;
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

			if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
			{
				//NOTE(Simon): Store if mouse button went down at a valid time (i.e. not while dragging)
				eligibleForPlacement = true;
			}
			if (eligibleForPlacement && Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
			{
				answerArea.rayOrigins.Add(ray.origin);
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

			mesh.mesh.vertices = vertices;
			mesh.mesh.normals = new Vector3[mesh.mesh.vertices.Length];
			var triangulator = new Triangulator(mesh.mesh.vertices);
			mesh.mesh.triangles = triangulator.Triangulate();
			mesh.mesh.RecalculateNormals();
			mesh.mesh.RecalculateBounds();

			//NOTE(Simon): +1 because we need to add the first vertex again, to complete the polygon
			var outlineVertices = new Vector3[vertices.Length + 1];
			Array.Copy(vertices, outlineVertices, vertices.Length);
			outlineVertices[outlineVertices.Length - 1] = outlineVertices[0];
			outlineMesh.mesh.vertices = outlineVertices;

			//NOTE(Simon): Generate indices for a line strip. 
			var indices = new int[outlineVertices.Length];
			for (int i = 0; i < indices.Length; i++) { indices[i] = i; }
			outlineMesh.mesh.SetIndices(indices, MeshTopology.LineStrip, 0);
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

	public Bounds GetBounds()
	{
		return mesh.mesh.bounds;
	}

	public void Dispose()
	{
		Destroy(goContainer);
	}
}
