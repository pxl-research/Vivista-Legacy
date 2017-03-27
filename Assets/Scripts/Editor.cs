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
	public GameObject textPanelEditorPrefab;
	public GameObject imagePanelPrefab;
	public GameObject imagePanelEditorPrefab;

	public GameObject interactionTypePicker;
	public GameObject interactionEditor;

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
		ray.origin = ray.GetPoint(100);
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
			if (Physics.Raycast(ray, out hit, 100))
			{
				var drawLocation = Vector3.Lerp(hit.point, Camera.main.transform.position, 0.4f);
				interactionPointTemp.transform.position = drawLocation;
				//NOTE(Simon): Rotate to match sphere's normal
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
							interactionEditor = Instantiate(imagePanelEditorPrefab);
							interactionEditor.GetComponent<ImagePanelEditor>().Init(lastInteractionPoint, "", "");
							break;
						}
						case InteractionType.Text:
						{
							interactionEditor = Instantiate(textPanelEditorPrefab);
							interactionEditor.GetComponent<TextPanelEditor>().Init(lastInteractionPoint, "", "");
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
			var lastInteractionPoint = interactionPoints[interactionPoints.Count - 1];
			switch (lastInteractionPointType)
			{
				case InteractionType.Image:
				{
					var editor = interactionEditor.GetComponent<ImagePanelEditor>(); 
					if (editor.answered)
					{
						var panel = Instantiate(imagePanelPrefab);
						panel.GetComponent<ImagePanel>().Init(lastInteractionPoint, editor.answerTitle, editor.answerURL);

						Destroy(interactionEditor);
						editorState = EditorState.Active;
					}
					break;
				}
				case InteractionType.Text:
				{
					var editor = interactionEditor.GetComponent<TextPanelEditor>(); 
					if (editor.answered)
					{
						var panel = Instantiate(textPanelPrefab);
						panel.GetComponent<TextPanel>().Init(lastInteractionPoint, editor.answerTitle, editor.answerBody);
						
						Destroy(interactionEditor);
						editorState = EditorState.Active;
					}
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
				Destroy(interactionEditor);
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
