using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Editor : MonoBehaviour 
{
	public bool editorActive = true;
	public GameObject interactionPointPrefab;
	public GameObject interactionPointTemp;
	public List<GameObject> interactionPoints;

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

				var panel = Instantiate(textPanelPrefab);
				panel.GetComponent<TextPanel>().Init(newPoint, "TestImage", @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Ut in tortor eu neque fringilla tempor vel nec metus. Nullam ultricies sed turpis finibus consequat. Quisque fermentum massa lectus. Integer iaculis facilisis elit et posuere. Sed dapibus gravida diam et sollicitudin. Vestibulum feugiat dapibus egestas. Suspendisse eget orci mollis, posuere sapien vel, tempus mi. Sed elementum metus massa, sit amet viverra arcu molestie sed. Curabitur lacinia nec odio tristique condimentum. Pellentesque sit amet tempor eros, dictum varius lacus. Praesent hendrerit neque et nisl euismod, sed ultrices ex imperdiet. Fusce sodales, lacus eu sodales volutpat, turpis sem ullamcorper turpis, sed maximus sapien nunc at sem. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec pulvinar malesuada libero eu accumsan. Donec ac ligula pellentesque, dapibus ipsum vitae, condimentum libero. Nulla facilisi. ");

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
				else if (Application.platform == RuntimePlatform.Android)
				{
					editorActive = false;
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
