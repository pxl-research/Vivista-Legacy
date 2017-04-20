using System;
using System.Collections.Generic;
using System.IO;
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
	FillingPanelDetails,
	MovingInteractionPoint,
	Saving,
	EditingInteractionPoint,
	Opening
}

public enum InteractionType {
	None,
	Text,
	Image,
}

[Serializable]
public class InteractionPoint
{
	public GameObject point;
	public GameObject timelineRow;
	public GameObject panel;
	public InteractionType type;
	public string title;
	public string body;
	public double startTime;
	public double endTime;
	public bool filled;
}

public class InteractionpointSerialize
{
	public Vector3 position;
	public InteractionType type;
	public string title;
	public string body;
	public double startTime;
	public double endTime;
}

public class Editor : MonoBehaviour 
{
	private EditorState editorState;

	public GameObject interactionPointPrefab;
	private GameObject interactionPointTemp;
	private List<InteractionPoint> interactionPoints;
	private InteractionPoint pointToMove;
	private InteractionPoint pointToEdit;

	public GameObject interactionTypePrefab;

	public GameObject openPanelPrefab;
	public GameObject savePanelPrefab;
	public GameObject textPanelPrefab;
	public GameObject textPanelEditorPrefab;
	public GameObject imagePanelPrefab;
	public GameObject imagePanelEditorPrefab;

	private GameObject interactionTypePicker;
	private GameObject interactionEditor;
	private GameObject savePanel;
	private GameObject openPanel;

	public GameObject timelineContainer;
	public GameObject timeline;
	public GameObject timelineHeader;
	public GameObject timelineRow;
	public Text labelPrefab;

	private List<Text> headerLabels = new List<Text>();
	private VideoController videoController;
	private float timelineStartTime;
	private float timelineWindowStartTime;
	private float timelineWindowEndTime;
	private float timelineEndTime;
	private int timelineTickSize;
	private float timelineZoom = 1;
	private float timelineOffset;
	private float timelineWidth;
	private float timelineXOffset;
	private Vector2 prevMousePosition;
	private Vector2 mouseDelta;
	private bool isDraggingTimelineItem;
	private InteractionPoint timelineItemBeingDragged;
	private bool isResizingTimelineItem;
	private bool isResizingStart;
	private InteractionPoint timelineItemBeingResized;


	public Cursors cursors;
	public List<Color> timelineColors;
	private int colorIndex;

	void Start () 
	{
		videoController = FileLoader.videoController.GetComponent<VideoController>();
		timelineWindowEndTime = (float)videoController.videoLength;

		interactionPointTemp = Instantiate(interactionPointPrefab);
		interactionPoints = new List<InteractionPoint>();

		SetActive(false);
		prevMousePosition = Input.mousePosition;
	}
	
	void Update () 
	{
		mouseDelta = new Vector2(Input.mousePosition.x - prevMousePosition.x, Input.mousePosition.y - prevMousePosition.y);
		prevMousePosition = Input.mousePosition;

		UpdateTimeline();

		//Note(Simon): Create a reversed raycast to find positions on the sphere with
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
			if (Input.GetMouseButtonDown(0) && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
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
				var drawLocation = Vector3.Lerp(hit.point, Camera.main.transform.position, !Camera.main.orthographic ? 0.4f : 0.01f);

				interactionPointTemp.transform.position = drawLocation;
				//NOTE(Simon): Rotate to match sphere's normal
				interactionPointTemp.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
			}

			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				SetActive(true);
			}

			if (Input.GetMouseButtonUp(0))
			{
				var newPoint = Instantiate(interactionPointPrefab, interactionPointTemp.transform.position, interactionPointTemp.transform.rotation);
				var point = new InteractionPoint
				{
					point = newPoint,
					type = InteractionType.None,
					startTime = videoController.currentTime,
					endTime = videoController.currentTime + (videoController.videoLength / 10),
				};

				AddItemToTimeline(point);

				interactionTypePicker = Instantiate(interactionTypePrefab);
				interactionTypePicker.GetComponent<InteractionTypePicker>().Init(newPoint.transform.position);

				editorState = EditorState.PickingInteractionType;
				ResetInteractionPointTemp();
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

		if (editorState == EditorState.MovingInteractionPoint)
		{
			if (Physics.Raycast(ray, out hit, 100))
			{
				var drawLocation = Vector3.Lerp(hit.point, Camera.main.transform.position, !Camera.main.orthographic ? 0.4f : 0.01f);

				pointToMove.point.transform.position = drawLocation;
				pointToMove.point.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);

				switch(pointToMove.type)
				{
					case InteractionType.Text:
						pointToMove.panel.GetComponent<TextPanel>().Move(pointToMove.point.transform.position);
						break;
					case InteractionType.Image:
						pointToMove.panel.GetComponent<ImagePanel>().Move(pointToMove.point.transform.position);
						break;
					case InteractionType.None:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			if (Input.GetKeyUp(KeyCode.Escape))
			{
				SetActive(true);
				pointToMove.timelineRow.transform.Find("Content/Move").GetComponent<Toggle2>().isOn = false;
			}
			if (Input.GetKeyDown(KeyCode.F1))
			{
				SetActive(false);
				pointToMove.timelineRow.transform.Find("Content/Move").GetComponent<Toggle2>().isOn = false;
			}
		}

		if (editorState == EditorState.EditingInteractionPoint)
		{
			switch(pointToEdit.type)
			{
				case InteractionType.Image:
				{
					var editor = interactionEditor.GetComponent<ImagePanelEditor>(); 
					if (editor.answered)
					{
						var panel = Instantiate(imagePanelPrefab);
						panel.GetComponent<ImagePanel>().Init(pointToEdit.point.transform.position, editor.answerTitle, editor.answerURL);
						pointToEdit.title = editor.answerTitle;
						pointToEdit.body = editor.answerURL;
						pointToEdit.panel = panel;

						Destroy(interactionEditor);
						editorState = EditorState.Active;
						pointToEdit.filled = true;
					}
					break;
				}
				case InteractionType.Text:
				{
					var editor = interactionEditor.GetComponent<TextPanelEditor>(); 
					if (editor.answered)
					{
						var panel = Instantiate(textPanelPrefab);
						panel.GetComponent<TextPanel>().Init(pointToEdit.point.transform.position, editor.answerTitle, editor.answerBody);
						pointToEdit.title = String.IsNullOrEmpty(editor.answerTitle) ? "<unnamed>" : editor.answerTitle;
						pointToEdit.body = editor.answerBody;
						pointToEdit.panel = panel;

						Destroy(interactionEditor);
						editorState = EditorState.Active;
						pointToEdit.filled = true;
					}
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		if (editorState == EditorState.PickingInteractionType)
		{
			if (interactionTypePicker != null)
			{
				var picker = interactionTypePicker.GetComponent<InteractionTypePicker>();
				if (picker.answered)
				{
					var lastPlacedPoint = interactionPoints[interactionPoints.Count - 1];
					lastPlacedPoint.type = picker.answer;
					var lastPlacedPointPos = interactionPoints[interactionPoints.Count - 1].point.transform.position;

					switch (lastPlacedPoint.type)
					{
						case InteractionType.Image:
						{
							interactionEditor = Instantiate(imagePanelEditorPrefab);
							interactionEditor.GetComponent<ImagePanelEditor>().Init(lastPlacedPointPos, "", "");
							break;
						}
						case InteractionType.Text:
						{
							interactionEditor = Instantiate(textPanelEditorPrefab);
							interactionEditor.GetComponent<TextPanelEditor>().Init(lastPlacedPointPos, "", "");

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
			var lastPlacedPoint = interactionPoints[interactionPoints.Count - 1];
			var lastPlacePointPos = lastPlacedPoint.point.transform.position;
			switch (lastPlacedPoint.type)
			{
				case InteractionType.Image:
				{
					var editor = interactionEditor.GetComponent<ImagePanelEditor>(); 
					if (editor.answered)
					{
						var panel = Instantiate(imagePanelPrefab);
						panel.GetComponent<ImagePanel>().Init(lastPlacePointPos, editor.answerTitle, editor.answerURL);
						lastPlacedPoint.title = editor.answerTitle;
						lastPlacedPoint.body = editor.answerURL;
						lastPlacedPoint.panel = panel;

						Destroy(interactionEditor);
						editorState = EditorState.Active;
						lastPlacedPoint.filled = true;
					}
					break;
				}
				case InteractionType.Text:
				{
					var editor = interactionEditor.GetComponent<TextPanelEditor>(); 
					if (editor.answered)
					{
						var panel = Instantiate(textPanelPrefab);
						panel.GetComponent<TextPanel>().Init(lastPlacePointPos, editor.answerTitle, editor.answerBody);
						lastPlacedPoint.title = String.IsNullOrEmpty(editor.answerTitle) ? "<unnamed>" : editor.answerTitle;
						lastPlacedPoint.body = editor.answerBody;
						lastPlacedPoint.panel = panel;

						Destroy(interactionEditor);
						editorState = EditorState.Active;
						lastPlacedPoint.filled = true;
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

		if (editorState == EditorState.Saving)
		{
			if (savePanel.GetComponent<SavePanel>().answered)
			{
				if (!SaveToFile(savePanel.GetComponent<SavePanel>().answerFilename))
				{
					Debug.LogError("Something went wrong while saving the file");
				}
				SetActive(true);
				Destroy(savePanel);
			}
			
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				SetActive(true);
				Destroy(savePanel);
			}
		}

		if (editorState == EditorState.Opening)
		{
			if (openPanel.GetComponent<OpenPanel>().answered)
			{
				if (!OpenFile(openPanel.GetComponent<OpenPanel>().answerFilename))
				{
					Debug.LogError("Something went wrong while loading the file");
				}
				SetActive(true);
				Destroy(openPanel);
			}
			
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				SetActive(true);
				Destroy(openPanel);
			}
		}

#if UNITY_EDITOR
		if (Input.GetKey(KeyCode.Z) && Input.GetKeyDown(KeyCode.O))
#else
		if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.O))
#endif
		{
			if (Physics.Raycast(ray, out hit, 100))
			{
				openPanel = Instantiate(openPanelPrefab);
				openPanel.GetComponent<OpenPanel>().init();
				openPanel.transform.SetParent(Canvass.main.transform, false);
				editorState = EditorState.Opening;
			}
		}

#if UNITY_EDITOR
		if(Input.GetKey(KeyCode.Z) && Input.GetKeyDown(KeyCode.S) && editorState != EditorState.Saving)
#else
		if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S) && editorState != EditorState.Saving)
#endif
		{
			if (Physics.Raycast(ray, out hit, 100))
			{
				savePanel = Instantiate(savePanelPrefab);
				savePanel.GetComponent<SavePanel>().init(hit.point);
				editorState = EditorState.Saving;
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
	
	void RemoveItemFromTimeline(InteractionPoint point)
	{
		Destroy(point.timelineRow);
		interactionPoints.Remove(point);
		Destroy(point.point);
	}

	void UpdateTimeline()
	{
		timelineStartTime = 0;
		timelineEndTime = (float)videoController.videoLength;
		
		//Note(Simon): Init if not set yet.
		if (timelineWindowEndTime == 0)
		{
			timelineWindowEndTime = timelineEndTime;
		}

		//Note(Simon): Zoom timeline
		{
			var coords = new Vector3[4];
			timelineContainer.GetComponentInChildren<RectTransform>().GetWorldCorners(coords);

			if (Input.mousePosition.y < coords[1].y)
			{
				if (Input.mouseScrollDelta.y > 0)
				{
					timelineZoom = Mathf.Clamp01(timelineZoom * 0.9f);
				}
				else if (Input.mouseScrollDelta.y < 0)
				{
					timelineZoom = Mathf.Clamp01(timelineZoom * 1.1f);
				}
			}
		}

		//Note(Simon): Reset offset when fully zoomed out.
		if (timelineZoom >= 1)
		{
			timelineOffset = 0;
		}

		float zoomedLength;
		//Note(Simon): Correct the timeline offset after zooming
		{
			zoomedLength = (timelineEndTime - timelineStartTime) * timelineZoom;
			var windowMiddle = (timelineEndTime - (timelineStartTime + timelineOffset * timelineZoom)) / 2;
			timelineWindowStartTime = Mathf.Lerp(timelineStartTime, windowMiddle, 1 - timelineZoom);
			timelineWindowEndTime = Mathf.Lerp(timelineEndTime, windowMiddle, 1 - timelineZoom);
		
			timelineXOffset = timelineHeader.GetComponentInChildren<Text>().rectTransform.rect.width;
			timelineWidth = timelineContainer.GetComponent<RectTransform>().rect.width - timelineXOffset;
		}

		//NOTE(Simon): Timeline labels
		{
			var maxNumLabels = Math.Floor(timelineWidth / 100);
			var lowerround = FloorTime(zoomedLength / maxNumLabels);
			var upperround = CeilTime(zoomedLength / maxNumLabels);

			var lowerroundNum = Mathf.FloorToInt(zoomedLength / lowerround);
			var upperroundNum = Mathf.FloorToInt(zoomedLength / upperround);
			var closestRounding = (maxNumLabels - lowerroundNum) > (upperroundNum - maxNumLabels) ? lowerround : upperround;
			var realNumLabels = (maxNumLabels - lowerroundNum) > (upperroundNum - maxNumLabels) ? lowerroundNum : upperroundNum;
			realNumLabels += 1;

			timelineTickSize = closestRounding;
		 
			while (headerLabels.Count < realNumLabels)
			{
				var label = Instantiate(labelPrefab, timelineHeader.transform);
				headerLabels.Add(label);
			}
			while (headerLabels.Count > realNumLabels)
			{
				Destroy(headerLabels[headerLabels.Count - 1].gameObject);
				headerLabels.RemoveAt(headerLabels.Count - 1);
			}

			var numTicksOffScreen = Mathf.FloorToInt(timelineWindowStartTime / timelineTickSize);
			
			for (int i = 0; i < realNumLabels; i++)
			{
				var time = (i + numTicksOffScreen) * timelineTickSize;
				headerLabels[i].text = FormatTime(time);
				headerLabels[i].rectTransform.position = new Vector2(TimeToPx(time), headerLabels[i].rectTransform.position.y);
			}
		}

		//Note(Simon): Render timeline items
		foreach(var point in interactionPoints)
		{
			var row = point.timelineRow;
			var imageRect = row.transform.GetComponentInChildren<Image>().rectTransform;
			row.GetComponentInChildren<Text>().text = point.title;

			var zoomedStartTime = point.startTime;
			var zoomedEndTime = point.endTime;

			if (point.endTime < timelineWindowStartTime || point.startTime > timelineWindowEndTime)
			{
				zoomedStartTime = 0;
				zoomedEndTime = 0;
			}
			else
			{
				if (point.startTime < timelineWindowStartTime)	{ zoomedStartTime = timelineWindowStartTime; }
				if (point.endTime > timelineWindowEndTime)		{ zoomedEndTime = timelineWindowEndTime; }
			}

			imageRect.position = new Vector2(TimeToPx(zoomedStartTime), imageRect.position.y);
			imageRect.sizeDelta = new Vector2(TimeToPx(zoomedEndTime) - TimeToPx(zoomedStartTime), imageRect.sizeDelta.y);
			
		}

		colorIndex = 0;
		//Note(Simon): Colors
		foreach(var point in interactionPoints)
		{
			var image = point.timelineRow.transform.GetComponentInChildren<Image>();
		
			image.color = timelineColors[colorIndex];
			colorIndex = (colorIndex + 1) % timelineColors.Count;
		}

		//Note(Simon): timeline buttons. Looping backwards because we're deleting items from the list.
		for (var i = interactionPoints.Count - 1; i >= 0; i--)
		{
			var point = interactionPoints[i];
			var edit = point.timelineRow.transform.FindChild("Content/Edit").gameObject.GetComponent<Button2>();
			var delete = point.timelineRow.transform.FindChild("Content/Delete").gameObject.GetComponent<Button2>();
			var move = point.timelineRow.transform.FindChild("Content/Move").gameObject.GetComponent<Toggle2>();
			var view = point.timelineRow.transform.FindChild("Content/View").gameObject.GetComponent<Toggle2>();

			if (!point.filled)
			{
				edit.interactable = false;
				move.interactable  = false;
			}
			if (point.filled)
			{
				edit.interactable  = true;
				move.interactable  = true;
			}

			if (delete.state == SelectState.Pressed)
			{
				//Note(Simon): If it is the last placed one, we might still be editing that one. So extra cleanup is necessary
				if (i == interactionPoints.Count - 1)
				{
					if (editorState == EditorState.FillingPanelDetails)
					{
						editorState = EditorState.Active;
						Destroy(interactionEditor);
					}
					if (editorState == EditorState.PickingInteractionType)
					{
						editorState = EditorState.Active;
						Destroy(interactionTypePicker);
					}
				}

				RemoveItemFromTimeline(point);
				Destroy(point.point);
				Destroy(point.panel);
				break;
			}

			if (edit.state == SelectState.Pressed && editorState != EditorState.EditingInteractionPoint)
			{
				editorState = EditorState.EditingInteractionPoint;
				pointToEdit = point;
				var panel = pointToEdit.panel;

				switch (pointToEdit.type)
				{
					case InteractionType.Text:
						interactionEditor = Instantiate(textPanelEditorPrefab);
						interactionEditor.GetComponent<TextPanelEditor>().Init(panel.transform.position, 
																				panel.GetComponent<TextPanel>().title.text, 
																				panel.GetComponent<TextPanel>().body.text, 
																				true);
						break;
					case InteractionType.Image:
						interactionEditor = Instantiate(imagePanelEditorPrefab);
						interactionEditor.GetComponent<ImagePanelEditor>().Init(panel.transform.position, 
																				panel.GetComponent<ImagePanel>().title.text, 
																				panel.GetComponent<ImagePanel>().imageURL, 
																				true);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				Destroy(pointToEdit.panel);
				break;
			}

			if (move.switchedOn)
			{
				editorState = EditorState.MovingInteractionPoint;
				pointToMove = point;
				break;
			}
			if (move.switchedOff)
			{
				editorState = EditorState.Active;
				pointToMove = null;
				break;
			}

			if (view.switchedOn)
			{
				point.panel.SetActive(false);
			}
			else if (view.switchedOff)
			{
				point.panel.SetActive(true);
			}
		}

		//Note(Simon): Render various stuff
		{
			
		}

		//Note(Simon): Resizing and moving of timeline items
		{
			foreach(var point in interactionPoints)
			{
				var row = point.timelineRow;
				var imageRect = row.transform.GetComponentInChildren<Image>().rectTransform;

				Vector2 rectPixel;
				RectTransformUtility.ScreenPointToLocalPointInRectangle(imageRect, Input.mousePosition, null, out rectPixel);
				var leftAreaX = 5;
				var rightAreaX = imageRect.rect.width - 5;

				if (isDraggingTimelineItem || isResizingTimelineItem || RectTransformUtility.RectangleContainsScreenPoint(imageRect, Input.mousePosition))
				{
					if (isDraggingTimelineItem)
					{
						Cursor.SetCursor(cursors.CursorDrag, new Vector2(15, 15), CursorMode.Auto);
					}
					else if (isResizingTimelineItem)
					{
						Cursor.SetCursor(cursors.CursorResize, new Vector2(15, 15), CursorMode.Auto);
					}
					else if (rectPixel.x < leftAreaX || rectPixel.x > rightAreaX)
					{
						Cursor.SetCursor(cursors.CursorResize, new Vector2(15, 15), CursorMode.Auto);
					}
					else
					{
						Cursor.SetCursor(cursors.CursorDrag, new Vector2(15, 15), CursorMode.Auto);
					}
				}
				else 
				{
					Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
				}

				if (!isDraggingTimelineItem && !isResizingTimelineItem
					&& Input.GetMouseButton(0) && RectTransformUtility.RectangleContainsScreenPoint(imageRect, Input.mousePosition))
				{
					if (rectPixel.x < leftAreaX)
					{
						isResizingStart = true;
						isResizingTimelineItem = true;
						timelineItemBeingResized = point;
					}
					else if (rectPixel.x > rightAreaX)
					{
						isResizingStart = false;
						isResizingTimelineItem = true;
						timelineItemBeingResized = point;
					}
					else
					{
						isDraggingTimelineItem = true;
						timelineItemBeingDragged = point;
					}
					break;
				}
			}

			if (isDraggingTimelineItem)
			{
				if (!Input.GetMouseButton(0))
				{
					isDraggingTimelineItem = false;
					timelineItemBeingDragged = null;
				}
				else
				{
					var newStart = Mathf.Max(0.0f, (float)timelineItemBeingDragged.startTime + (mouseDelta.x / 8.0f) * timelineZoom);
					var newEnd = Mathf.Min(timelineEndTime, (float)timelineItemBeingDragged.endTime + (mouseDelta.x / 8.0f) * timelineZoom);
					if (newStart > 0.0f && newEnd < timelineEndTime)
					{
						timelineItemBeingDragged.startTime = newStart;
						timelineItemBeingDragged.endTime = newEnd;
					}
				}
			}
			else if (isResizingTimelineItem)
		{
			if (!Input.GetMouseButton(0))
			{
				isResizingTimelineItem = false;
				timelineItemBeingResized = null;
			}
			else
			{
				if(isResizingStart)
				{
					var newStart = Mathf.Max(0.0f, (float)timelineItemBeingResized.startTime + (mouseDelta.x / 8.0f) * timelineZoom);
					if (newStart < timelineItemBeingResized.endTime - 0.2f)
					{
						timelineItemBeingResized.startTime = newStart;
					}
				}
				else
				{
					var newEnd = Mathf.Min(timelineEndTime, (float)timelineItemBeingResized.endTime + (mouseDelta.x / 8.0f) * timelineZoom);
					if (newEnd > timelineItemBeingResized.startTime + 0.2f)
					{
						timelineItemBeingResized.endTime = newEnd;
					}
				}
			}
		}
		}
	}
	
	public float TimeToPx(double time)
	{
		if (time < timelineWindowStartTime || time > timelineWindowEndTime)
		{
			return -1000;
		}
		var fraction = (time - timelineWindowStartTime) / (timelineWindowEndTime - timelineWindowStartTime);
		return (float)(timelineXOffset + (fraction * timelineWidth));
	}
	
	private static int FloorTime(double time)
	{
		int[] niceTimes = {1, 2, 5, 10, 15, 30, 60, 2 * 60, 5 * 60, 10 * 60, 15 * 60, 30 * 60, 60 * 60, 2 * 60 * 60};
		var result = niceTimes[0];

		for (int i = 0; i < niceTimes.Length; i++)
		{
			if (time > niceTimes[i])
			{
				result = niceTimes[i];
			}
		}

		return result;
	}

	private static int CeilTime(double time)
	{
		int[] niceTimes = {1, 2, 5, 10, 15, 30, 60, 2 * 60, 5 * 60, 10 * 60, 15 * 60, 30 * 60, 60 * 60, 2 * 60 * 60};
		
		for (int i = niceTimes.Length - 1; i >= 0; i--)
		{
			if (niceTimes[i] < time)
			{
				return niceTimes[i + 1];
			}
		}

		return niceTimes[0];
	}

	public void OnDrag(BaseEventData e)
	{
		if (Input.GetMouseButton(1))
		{
			var pointerEvent = (PointerEventData)e;
			timelineOffset = timelineOffset + pointerEvent.delta.x;
		}
	}

	private bool SaveToFile(string filename)
	{
		var sb = new StringBuilder();
		sb.Append("[");
		if (interactionPoints.Count > 0)
		{
			foreach (var point in interactionPoints)
			{
				var temp = new InteractionpointSerialize
				{
					position = point.point.transform.position,
					type = point.type,
					title = point.title,
					body = point.body,
					startTime = point.startTime,
					endTime = point.endTime
				};

				sb.Append(JsonUtility.ToJson(temp, true));
				sb.Append(",");
			}

			sb.Remove(sb.Length - 1, 1);
		}
		else
		{
			Debug.Log("[]");
		}

		sb.Append("]");

		Debug.Log(sb.ToString());

		try
		{
			using (var file = File.CreateText(Path.Combine(Application.persistentDataPath, filename)))
			{
				file.Write(sb.ToString());
				file.Close();
			}
		}
		catch(Exception e)
		{
			Debug.Log(e.ToString());
			return false;
		}

		return true;
	}

	private bool OpenFile(string filename)
	{
		using (var fileContents = File.OpenText(Path.Combine(Application.persistentDataPath, filename)))
		{
			string str = fileContents.ReadToEnd();
			List<string> stringObjects = new List<string>();
			
			var level = 0;
			var start = 0;
			var count = 0;

			for(int i = 0; i < str.Length; i++)
			{
				if (str[i] == '{')
				{
					if (level == 0)
					{
						start = i;
					}
					level++;
				}
				if (str[i] == '}')
				{
					level--;
				}
				if (level == 0 && (str[i] == ',' || str[i] == ']'))
				{
					stringObjects.Add(str.Substring(start, count - 1));
					count = 0;
				}
				if (level < 0)
				{
					Debug.Log("Corrupted save file. Aborting");
					return false;
				}

				count++;
			}

			var points = new List<InteractionpointSerialize>();

			foreach (var obj in stringObjects)
			{
				points.Add(JsonUtility.FromJson<InteractionpointSerialize>(obj));
			}

			foreach (var point in points)
			{
				
			}
		}
		return true;
	}

	private void ResetInteractionPointTemp()
	{
		interactionPointTemp.transform.position = new Vector3(1000, 1000, 1000);
	}

	private static string FormatTime(double time)
	{
		var hours = (int)(time / (60 * 60));
		time -= hours * 60;
		var minutes = (int)(time / 60);
		time -= minutes * 60;
		var seconds = (int) time;

		var formatted = "";
		if (hours > 0)
		{
			formatted += hours + ":";
		}

		formatted += minutes.ToString("D2");
		formatted += ":";
		formatted += seconds.ToString("D2");

		return formatted;
	}

}
