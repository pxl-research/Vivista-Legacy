using System;
using System.Collections.Generic;
using System.Text;
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
	None,
	Text,
	Image,
}

public class InteractionPoint
{
	public GameObject point;
	public InteractionType type;
	public string title;
	public string body;
	public long startTime;
	public DateTime endTime;
}

public class Editor : MonoBehaviour 
{
	public GameObject interactionPointPrefab;
	public GameObject interactionPointTemp;
	public List<InteractionPoint> interactionPoints;

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
		interactionPoints = new List<InteractionPoint>();
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
			point.point.GetComponent<MeshRenderer>().material.color = Color.white;
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
				var point = new InteractionPoint
				{
					point = newPoint,
					type = InteractionType.None
				};

				interactionPoints.Add(point);

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

		if (editorState == EditorState.PickingInteractionType)
		{
			if (interactionTypePicker != null)
			{
				var picker = interactionTypePicker.GetComponent<InteractionTypePicker>();
				if (picker.answered)
				{
					lastInteractionPointType = picker.answer;
					var lastInteractionPointPos = interactionPoints[interactionPoints.Count - 1].point.transform.position;

					switch (lastInteractionPointType)
					{
						case InteractionType.Image:
						{
							interactionEditor = Instantiate(imagePanelEditorPrefab);
							interactionEditor.GetComponent<ImagePanelEditor>().Init(lastInteractionPointPos, "", "");
							break;
						}
						case InteractionType.Text:
						{
							interactionEditor = Instantiate(textPanelEditorPrefab);
							interactionEditor.GetComponent<TextPanelEditor>().Init(lastInteractionPointPos, "", "");
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
				Destroy(lastPlacedPoint.point);
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
			var lastInteractionPointPos = interactionPoints[interactionPoints.Count - 1].point.transform.position;
			switch (lastInteractionPointType)
			{
				case InteractionType.Image:
				{
					var editor = interactionEditor.GetComponent<ImagePanelEditor>(); 
					if (editor.answered)
					{
						var panel = Instantiate(imagePanelPrefab);
						panel.GetComponent<ImagePanel>().Init(lastInteractionPointPos, editor.answerTitle, editor.answerURL);

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
						panel.GetComponent<TextPanel>().Init(lastInteractionPointPos, editor.answerTitle, editor.answerBody);
						
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
				Destroy(lastPlacedPoint.point);
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

#if UNITY_EDITOR
		if(Input.GetKey(KeyCode.Z) && Input.GetKeyDown(KeyCode.S))
		{
#else
		if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S))
		{
#endif
			if(!SaveToFile())
			{
				Debug.Log("Save error");
			}
		}
	}

	bool SaveToFile()
	{
		var sb = new StringBuilder();
		sb.Append("{");
		if (interactionPoints.Count > 0)
		{
			foreach (var point in interactionPoints)
			{
				//sb.Append(NetJSON.NetJSON.Serialize(point));
			}
		}
		else
		{
				Debug.Log("{}");
		}

		sb.Append("}");

		Debug.Log(sb.ToString());

		return false;
	}

	void ResetInteractionPointTemp()
	{
		interactionPointTemp.transform.position = new Vector3(1000, 1000, 1000);
	}
}
