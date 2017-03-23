using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

public enum EditorState {
	Inactive,
	Active,
	PlacingInteractionPoint,
	PickingInteractionType,
	FillingPanelDetails
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

	public InteractionType lastInteractionPointType;

	void Start () 
	{
		interactionPointTemp = Instantiate(interactionPointPrefab);
		ResetInteractionPointTemp();
		editorState = EditorState.Inactive;
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
				//Note(Simon): Early return so we don't interfere with the rest of the state machine
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
					lastInteractionPointType = picker.answer;
					var lastInteractionPoint = interactionPoints[interactionPoints.Count - 1];

					switch (lastInteractionPointType)
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
							Assert.IsTrue(true, "FFS, you shoulda added it here");
							break;
						}
					}

					Destroy(interactionTypePicker);
					editorState = EditorState.FillingPanelDetails;

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

		if (editorState == EditorState.FillingPanelDetails)
		{
			switch (lastInteractionPointType)
			{
				case InteractionType.Image:
				{
					break;
				}
				case InteractionType.Text:
				{
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException();
				}
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
		interactionPointTemp.transform.position = new Vector3(1000, 1000, 1000);
	}
}
