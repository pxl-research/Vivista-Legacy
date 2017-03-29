using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
	public GameObject timelineRow;
	public InteractionType type;
	public string title;
	public string body;
	public double startTime;
	public double endTime;
}

public class Editor : MonoBehaviour 
{
	public GameObject interactionPointPrefab;
	private GameObject interactionPointTemp;
	private List<InteractionPoint> interactionPoints;

	public GameObject interactionTypePrefab;

	public GameObject textPanelPrefab;
	public GameObject textPanelEditorPrefab;
	public GameObject imagePanelPrefab;
	public GameObject imagePanelEditorPrefab;

	private GameObject interactionTypePicker;
	private GameObject interactionEditor;

	public GameObject timelineContainer;
	public GameObject timeline;
	public GameObject timelineRow;
	private VideoController videoController;

	private EditorState editorState;

	private InteractionType lastInteractionPointType;

	void Start () 
	{
		videoController = FileLoader.videoController.GetComponent<VideoController>();

		interactionPointTemp = Instantiate(interactionPointPrefab);
		interactionPoints = new List<InteractionPoint>();

		SetActive(false);
	}
	
	void Update () 
	{
		UpdateTimeline();

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
				SetActive(true);
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

			if (Physics.Raycast(ray, out hit, 100, 1 << LayerMask.NameToLayer("interactionPoints")))
			{
				hit.collider.GetComponentInParent<MeshRenderer>().material.color = Color.red;
			}
			
			if (Input.GetKeyDown(KeyCode.F1))
			{
				SetActive(false);
			}
		}

		if (editorState == EditorState.PlacingInteractionPoint && !EventSystem.current.IsPointerOverGameObject())
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
					type = InteractionType.None,
					startTime = videoController.currentTime,
					endTime = videoController.currentTime + 10,
				};

				AddItemToTimeline(point);

				interactionTypePicker = Instantiate(interactionTypePrefab);
				interactionTypePicker.GetComponent<InteractionTypePicker>().Init(newPoint.transform.position);

				editorState = EditorState.PickingInteractionType;
			}

			if (Input.GetKeyUp(KeyCode.Escape))
			{
				SetActive(true);
			}
			if (Input.GetKeyDown(KeyCode.F1))
			{
				SetActive(false);
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
				RemoveItemFromTimeline(lastPlacedPoint);
				Destroy(interactionTypePicker);
			}
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				SetActive(true);
			}
			if (Input.GetKeyDown(KeyCode.F1))
			{
				SetActive(false);
			}
		}

		if (editorState == EditorState.FillingPanelDetails)
		{
			var lastInteractionPoint = interactionPoints[interactionPoints.Count - 1];
			var lastInteractionPointPos = lastInteractionPoint.point.transform.position;
			switch (lastInteractionPointType)
			{
				case InteractionType.Image:
				{
					var editor = interactionEditor.GetComponent<ImagePanelEditor>(); 
					if (editor.answered)
					{
						var panel = Instantiate(imagePanelPrefab);
						panel.GetComponent<ImagePanel>().Init(lastInteractionPointPos, editor.answerTitle, editor.answerURL);
						lastInteractionPoint.title = editor.answerTitle;
						lastInteractionPoint.body = editor.answerURL;

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
						lastInteractionPoint.title = editor.answerTitle;
						lastInteractionPoint.body = editor.answerBody;

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
				RemoveItemFromTimeline(lastPlacedPoint);
				Destroy(interactionEditor);
			}
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				SetActive(true);
			}
			if (Input.GetKeyDown(KeyCode.F1))
			{
				SetActive(false);
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
	
	void SetActive(bool active)
	{
		ResetInteractionPointTemp();
		
		if (active)
		{
			editorState = EditorState.Active;
			timelineContainer.SetActive(true);
		}
		else
		{
			editorState = EditorState.Inactive;
			timelineContainer.SetActive(false);
		}
	}

	void AddItemToTimeline(InteractionPoint point)
	{
		var newRow = Instantiate(timelineRow);
		point.timelineRow = newRow;
		newRow.transform.SetParent(timeline.transform);
		interactionPoints.Add(point);
	}

	void UpdateTimeline()
	{
		foreach(var point in interactionPoints)
		{
			var row = point.timelineRow;
			row.GetComponentInChildren<Text>().text = point.title;
			var offset = row.GetComponentInChildren<Text>().rectTransform.rect.width;
			var max = timelineContainer.GetComponent<RectTransform>().rect.width - offset;

			var begin = offset + (point.startTime / videoController.videoLength) * max;
			var end = offset + (point.endTime / videoController.videoLength) * max;

			var imageRect = row.transform.GetComponentInChildren<Image>().rectTransform;
			imageRect.position = new Vector2((float)begin, imageRect.position.y);
			imageRect.sizeDelta = new Vector2((float)end, imageRect.sizeDelta.y);
		}
	}

	void RemoveItemFromTimeline(InteractionPoint point)
	{
		Destroy(point.timelineRow);
		interactionPoints.Remove(point);
		Destroy(point.point);
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
