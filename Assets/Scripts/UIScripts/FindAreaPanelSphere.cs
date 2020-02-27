using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

public class FindAreaPanelSphere : MonoBehaviour
{
	public Material polygonMaterial;
	public Material outlineMaterial;

	public Text title;
	public Button StartButton;
	public Text result;
	private List<Area> areas;
	private List<GameObject> areaGos = new List<GameObject>();
	private static int AreaLayer;

	private Player player;
	private Controller[] controllers;
	private List<Ray> rays;

	private bool isFindingArea;
	private bool completed;

	void Awake()
	{
		rays = new List<Ray>();
	}

	void Update()
	{
		if (isFindingArea)
		{
			rays.Clear();

			if (Input.GetMouseButtonDown(0))
			{
				var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				//NOTE(Simon): Add both directions, because winding order determines from wich direction a mesh can collide
				rays.Add(ray);
				rays.Add(ray.ReverseRay());
			}

			for (int i = 0; i < controllers.Length; i++)
			{
				if (controllers[i].triggerPressed)
				{
					var ray = controllers[i].CastRay();
					//NOTE(Simon): Add both directions, because winding order determines from wich direction a mesh can collide
					rays.Add(ray);
					rays.Add(ray.ReverseRay());
				}
			}

			GameObject target = null;
			for (int i = 0; i < rays.Count; i++)
			{
				if (Physics.Raycast(rays[i], out var hit, Mathf.Infinity, LayerMask.GetMask("Area")))
				{
					target = hit.transform.gameObject;
					break;
				}
			}

			if (target != null)
			{
				for (int i = 0; i < areaGos.Count; i++)
				{
					areaGos[i].GetComponent<MeshRenderer>().enabled = true;
					areaGos[i].GetComponentInChildren<MeshRenderer>().enabled = true;
				}

				player.UnsuspendInteractionPoint();

				StartButton.gameObject.SetActive(false);
				result.gameObject.SetActive(true);

				isFindingArea = false;
				completed = true;
			}
		}
	}

	private void OnEnable()
	{
		if (completed)
		{
			for (int i = 0; i < areaGos.Count; i++)
			{
				areaGos[i].GetComponent<MeshRenderer>().enabled = true;
				areaGos[i].GetComponentInChildren<MeshRenderer>().enabled = true;
			}
		}
	}

	private void OnDisable()
	{
		for (int i = 0; i < areaGos.Count; i++)
		{
			areaGos[i].GetComponent<MeshRenderer>().enabled = false;
			areaGos[i].GetComponentInChildren<MeshRenderer>().enabled = false;
		}
	}

	public void Init(string newTitle, List<Area> newAreas)
	{
		AreaLayer = LayerMask.NameToLayer("Area");

		title.text = newTitle;
		areas = newAreas;
		var watch = Stopwatch.StartNew();

		foreach (var area in areas)
		{
			var go = new GameObject();
			go.name = "area";
			go.layer = AreaLayer;
			var coll = go.AddComponent<MeshCollider>();
			var goRenderer = go.AddComponent<MeshRenderer>();
			var goMesh = go.AddComponent<MeshFilter>();
			goRenderer.material = polygonMaterial;
			goRenderer.enabled = false;

			var outlineGo = new GameObject();
			outlineGo.name = "outline";
			outlineGo.layer = AreaLayer;
			outlineGo.transform.SetParent(go.transform);
			var outlineRenderer = outlineGo.AddComponent<MeshRenderer>();
			var outlineMesh = outlineGo.AddComponent<MeshFilter>();
			outlineRenderer.material = outlineMaterial;
			outlineRenderer.enabled = false;

			var mesh = new Mesh();

			mesh.vertices = area.vertices.ToArray();
			var triangulator = new Triangulator(mesh.vertices);
			mesh.triangles = triangulator.Triangulate();

			coll.sharedMesh = mesh;
			goMesh.mesh = mesh;

			//NOTE(Simon): +1 because we need to add the first vertex again, to complete the polygon
			var outlineVertices = new Vector3[mesh.vertices.Length + 1];
			Array.Copy(mesh.vertices, outlineVertices, mesh.vertices.Length);
			outlineVertices[outlineVertices.Length - 1] = outlineVertices[0];
			outlineMesh.mesh.vertices = outlineVertices;

			//NOTE(Simon): Generate indices for a line strip.
			var indices = new int[outlineVertices.Length];
			for (int i = 0; i < indices.Length; i++) { indices[i] = i; }
			outlineMesh.mesh.SetIndices(indices, MeshTopology.LineStrip, 0);

			go.SetActive(false);

			areaGos.Add(go);
		}

		UnityEngine.Debug.Log($"Area collider generation time: {watch.Elapsed.TotalMilliseconds} ms");
	}

	public void OnStartFindArea()
	{
		for (int i = 0; i < areaGos.Count; i++)
		{
			areaGos[i].SetActive(true);
		}

		player = GameObject.Find("Player").GetComponent<Player>();
		controllers = player.GetControllers();
		player.SuspendInteractionPoint();

		isFindingArea = true;
	}
}
