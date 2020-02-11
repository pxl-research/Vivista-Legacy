using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AreaPicker : MonoBehaviour, IDisposable
{
	public bool answered;
	public List<Vector3> rayDirections = new List<Vector3>();
	public List<Vector3> rayOrigins = new List<Vector3>();
	private List<Vector3> rayHitPositions = new List<Vector3>();

	private List<GameObject> areaPoints = new List<GameObject>();

	public Material pointMaterial;
	public Material polygonMaterial;

	private GameObject indicator;
	private GameObject goContainer;
	private GameObject polygon;
	private MeshFilter mesh;
	private GameObject polygonOutline;
	private MeshFilter outlineMesh;

	private bool isDragging;
	private Vector3 DragOrigin;
	private GameObject dragObject;

	private static int AreaLayer;

	void Start()
	{
		AreaLayer = LayerMask.NameToLayer("Area");

		goContainer = new GameObject("goContainer");

		indicator = NewPoint(Vector3.zero);
		indicator.name = "indicator";
		indicator.layer = 0;
		Destroy(indicator.GetComponent<SphereCollider>());

		polygon = new GameObject("mesh");
		polygon.transform.SetParent(goContainer.transform);
		
		var r = polygon.AddComponent<MeshRenderer>();
		mesh = polygon.AddComponent<MeshFilter>();
		r.material = polygonMaterial;

		polygonOutline = new GameObject("meshOutline");
		polygonOutline.transform.SetParent(goContainer.transform);
		r = polygonOutline.AddComponent<MeshRenderer>();
		outlineMesh = polygonOutline.AddComponent<MeshFilter>();
		r.material = pointMaterial;
	}

	void Update()
	{
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		ray = ray.ReverseRay();
		RaycastHit hit;

		bool dirty = false;

		if (isDragging)
		{
			if (Input.GetMouseButtonUp(0))
			{
				isDragging = false;
				dragObject = null;
				indicator.SetActive(true);
			}
			else if(Physics.Raycast(ray, out hit, 100f))
			{
				dragObject.transform.position = hit.point;
				dirty = true;
				//NOTE(Simon): indicator is not visible at this point, but still update its position to prevent visual glitch on reappearance
				indicator.transform.position = hit.point;
			}
		}
		else if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("Area")))
		{
			Cursors.isOverridingCursor = true;
			Cursor.SetCursor(Cursors.Instance.CursorDrag, new Vector2(15, 15), CursorMode.Auto);
			indicator.SetActive(false);

			if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
			{
				isDragging = true;
				DragOrigin = hit.transform.position;
				dragObject = hit.transform.gameObject;
			}
		}
		else if (Physics.Raycast(ray, out hit, 100f))
		{
			Cursors.isOverridingCursor = false;
			indicator.SetActive(true);
			indicator.transform.position = hit.point;

			if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
			{
				rayDirections.Add(ray.origin);
				rayOrigins.Add(ray.direction);
				rayHitPositions.Add(hit.point);

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

			//NOTE(Simon): +1 because we need to add the first vertex again, to complete the polygon
			var outlineVertices = new Vector3[vertices.Length + 1];
			Array.Copy(vertices, outlineVertices, vertices.Length);
			outlineVertices[outlineVertices.Length - 1] = outlineVertices[0];
			outlineMesh.mesh.vertices = outlineVertices;

			//NOTE(Simon): Generate indices for a line strip. 
			var indices = new int[outlineVertices.Length];
			for (int i = 0; i < indices.Length; i++) { indices[i] = i; }
			outlineMesh.mesh.SetIndices(indices, MeshTopology.LineStrip, 0);
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
	}

	public void Dispose()
	{
		Destroy(goContainer);
	}
}
