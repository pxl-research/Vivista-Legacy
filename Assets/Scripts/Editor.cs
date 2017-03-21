using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

public enum EditorState {
	Inactive,
	Active,
	PlacingInteractionPoint,
	PickingInteractionType
}

public enum InteractionType {
	Text,
	Image,
}

public class Editor : MonoBehaviour 
{
	public GameObject interactionPointPrefab;
	public GameObject interactionPointTemp;
	public List<GameObject> interactionPoints;

	public GameObject interactionTypePrefab;
	public GameObject textPanelPrefab;
	public GameObject imagePanelPrefab;

	public GameObject interactionTypePicker;

	public EditorState editorState;

	void Start () 
	{
		interactionPointTemp = Instantiate(interactionPointPrefab);
		ResetInteractionPointTemp();
		editorState = EditorState.Active;
	}
	
	void Update () 
	{
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		ray.origin = ray.GetPoint(10);
		ray.direction = -ray.direction;

		foreach (var point in interactionPoints)
		{
			point.GetComponent<MeshRenderer>().material.color = Color.white;
		}
		
		if (editorState == EditorState.Inactive)
		{
			if (Input.GetKeyDown(KeyCode.F1))
			{
				editorState = EditorState.Active;
				//Note(Simon): Early return so we don't intergere with the rest of the state machine
				return;
			}
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

			if (Input.GetMouseButtonUp(0))
			{
				var newPoint = Instantiate(interactionPointPrefab, interactionPointTemp.transform.position, interactionPointTemp.transform.rotation);
				interactionPoints.Add(newPoint);

				interactionTypePicker = Instantiate(interactionTypePrefab);
				interactionTypePicker.GetComponent<InteractionTypePicker>().Init(newPoint);

				editorState = EditorState.PickingInteractionType;
			}

			if (Input.GetKeyUp(KeyCode.Escape))
			{
				ResetInteractionPointTemp();
				editorState = EditorState.Active;
			}
			if (Input.GetKeyDown(KeyCode.F1))
			{
				ResetInteractionPointTemp();
				editorState = EditorState.Inactive;
			}
		}

		if (editorState == EditorState.Active)
		{
			if (Input.GetMouseButtonDown(0))
			{
				editorState = EditorState.PlacingInteractionPoint;
			}

			if (Physics.Raycast(ray, out hit, 10, 1 << LayerMask.NameToLayer("interactionPoints")))
			{
				hit.collider.GetComponentInParent<MeshRenderer>().material.color = Color.red;
			}
			
			if (Input.GetKeyDown(KeyCode.F1))
			{
				ResetInteractionPointTemp();
				editorState = EditorState.Inactive;
			}
		}

		if (editorState == EditorState.PickingInteractionType)
		{
			if (interactionTypePicker != null)
			{
				var picker = interactionTypePicker.GetComponent<InteractionTypePicker>();
				if (picker.answered)
				{
					var type = picker.answer;
					var lastInteractionPoint = interactionPoints[interactionPoints.Count - 1];

					switch (type)
					{
						case InteractionType.Image:
						{
							var panel = Instantiate(imagePanelPrefab);
							panel.GetComponent<ImagePanel>().Init(lastInteractionPoint, "TestNewSystem", @"C:\Users\20003613\Documents\Git\360video\Assets\cats2.jpg");
							break;
						}
						case InteractionType.Text:
						{
							var panel = Instantiate(textPanelPrefab);
							panel.GetComponent<TextPanel>().Init(lastInteractionPoint, "TestNewText", "Lorem Ipsum Dolor Sit Amet");
							break;
						}
						default:
						{
							throw new Exception("FFS, you shoulda added it here");
						}
					}

					Destroy(interactionTypePicker);
					editorState = EditorState.Active;
					ResetInteractionPointTemp();
				}
			}
			else
			{
				Debug.Log("interactionTypePicker is null");
			}

			if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F1))
			{
				var lastPlacedPoint = interactionPoints[interactionPoints.Count - 1];
				interactionPoints.RemoveAt(interactionPoints.Count - 1);
				Destroy(lastPlacedPoint);
				Destroy(interactionTypePicker);
				ResetInteractionPointTemp();
			}
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				editorState = EditorState.Active;
			}
			if (Input.GetKeyDown(KeyCode.F1))
			{
				editorState = EditorState.Inactive;
			}
		}
	}

	void ResetInteractionPointTemp()
	{
		Vector3 resetPos = new Vector3(1000, 1000, 1000);
		interactionPointTemp.transform.position = resetPos;
	}
}
