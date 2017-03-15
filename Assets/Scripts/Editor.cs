using System.Collections.Generic;
using UnityEngine;

public class Editor : MonoBehaviour 
{
	public bool editorActive = true;
	public GameObject interactionPointPrefab;
	public GameObject interactionPointTemp;
	public List<GameObject> interactionPoints;

	public Canvas canvas;
	public GameObject textPanelPrefab;
	public GameObject imagePanelPrefab;

	public EditorState editorState;

	public enum EditorState {
		PlacingInteractionPoint,
		Reset
	}

	void Start () 
	{
		interactionPointTemp = Instantiate(interactionPointPrefab);
		ResetInteractionPointTemp();
		Physics.queriesHitBackfaces = true;
		editorState = EditorState.Reset;
	}
	
	void Update () 
	{
		if (Input.GetKeyDown(KeyCode.F1))
		{
			if (editorActive)
			{
				ResetInteractionPointTemp();
			}
			editorActive = !editorActive;
			Debug.Log(string.Format("Editor active: {0}", editorActive));
		}

		if (editorActive)
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			ray.origin = ray.GetPoint(10);
			ray.direction = -ray.direction;

			foreach(var point in interactionPoints)
			{
				point.GetComponent<MeshRenderer>().material.color = Color.white;
			}

			if (Input.GetMouseButtonDown(0))
			{
				editorState = EditorState.PlacingInteractionPoint;
			}

			if (editorState == EditorState.PlacingInteractionPoint)
			{
				if (Physics.Raycast(ray, out hit, 10))
				{
					var drawLocation = hit.point + ray.direction.normalized / 50;
					interactionPointTemp.transform.position = drawLocation;
					//Rotate to match sphere's normal
					interactionPointTemp.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
				}
			}

			if (editorState == EditorState.Reset)
			{
				if (Physics.Raycast(ray, out hit, 10, 1 << LayerMask.NameToLayer("interactionPoints")))
				{
					hit.collider.GetComponentInParent<MeshRenderer>().material.color = Color.red;
				}
			}

			if (Input.GetMouseButtonUp(0) && editorState == EditorState.PlacingInteractionPoint)
			{
				var newPoint = Instantiate(interactionPointPrefab, interactionPointTemp.transform.position, interactionPointTemp.transform.rotation);
				interactionPoints.Add(newPoint);
				var panel = Instantiate(imagePanelPrefab);
				panel.transform.SetParent(canvas.transform, false);
				panel.GetComponent<ImagePanel>().Init("TestImage", @"C:\Users\20003613\Desktop\Test\kitteh.jpg");
				ResetInteractionPointTemp();
				editorState = EditorState.Reset;
			}

			if (Input.GetKeyUp(KeyCode.Escape))
			{
				if (editorState == EditorState.PlacingInteractionPoint)
				{
					ResetInteractionPointTemp();
					editorState = EditorState.Reset;
				}
			}
		}
	}

	void ResetInteractionPointTemp()
	{
		Vector3 resetPos = new Vector3(1000, 1000, 1000);
		interactionPointTemp.transform.position = resetPos;
	}
}
