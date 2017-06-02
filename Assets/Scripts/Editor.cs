using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.VR;

public enum EditorState {
	Inactive,
	Active,
	PlacingInteractionPoint,
	PickingInteractionType,
	FillingPanelDetails,
	MovingInteractionPoint,
	Saving,
	EditingInteractionPoint,
	Opening,
	NewOpen,
	PickingPerspective,
	Uploading,
	SavingThenUploading
}

public enum InteractionType {
	None,
	Text,
	Image,
}

[Serializable]
public class InteractionPointEditor
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
	public Quaternion rotation;
	public InteractionType type;
	public string title;
	public string body;
	public double startTime;
	public double endTime;
}

public class UploadStatus
{
	public Coroutine coroutine;
	public WWW currentRequest;
	public int partSize;
	public int currentPartSize;
	public int parts;
	public int currentPart;
	public int totalSize;
	public bool done;
	public Queue<Timing> timings = new Queue<Timing>();
}

public struct Timing
{
	public float time;
	public float totalUploaded;
}

public class Editor : MonoBehaviour 
{
	private EditorState editorState;

	public GameObject interactionPointPrefab;
	private GameObject interactionPointTemp;
	private List<InteractionPointEditor> interactionPoints;
	private InteractionPointEditor pointToMove;
	private InteractionPointEditor pointToEdit;

	public GameObject interactionTypePrefab;

	public GameObject openPanelPrefab;
	public GameObject newPanelPrefab;
	public GameObject perspectivePanelPrefab;
	public GameObject savePanelPrefab;
	public GameObject textPanelPrefab;
	public GameObject textPanelEditorPrefab;
	public GameObject imagePanelPrefab;
	public GameObject imagePanelEditorPrefab;
	public GameObject uploadPanelPrefab;

	private GameObject interactionTypePicker;
	private GameObject interactionEditor;
	private GameObject savePanel;
	private GameObject newPanel;
	private GameObject perspectivePanel;
	private GameObject openPanel;
	private GameObject uploadPanel;

	public GameObject timelineContainer;
	public GameObject timeline;
	public GameObject timelineHeader;
	public GameObject timelineRow;
	public Text labelPrefab;

	private List<Text> headerLabels = new List<Text>();
	private VideoController videoController;
	private FileLoader fileLoader;
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
	private InteractionPointEditor timelineItemBeingDragged;
	private bool isResizingTimelineItem;
	private bool isResizingStart;
	private InteractionPointEditor timelineItemBeingResized;
	private bool isResizingTimeline;

	private Guid guid;
	private string openFileName = "";
	private string openVideo = "";
	private UploadStatus uploadStatus;

	public Cursors cursors;
	public List<Color> timelineColors;
	private int colorIndex;

	const int kilobyte = 1024;
	const int megabyte = 1024 * 1024;
	const int gigabyte = 1024 * 1024 * 1024;

	void Start () 
	{
		interactionPointTemp = Instantiate(interactionPointPrefab);
		interactionPoints = new List<InteractionPointEditor>();

		prevMousePosition = Input.mousePosition;

		SetEditorActive(false);

		newPanel = Instantiate(newPanelPrefab);
		newPanel.transform.SetParent(Canvass.main.transform, false);
		editorState = EditorState.NewOpen;
		Canvass.modalBackground.SetActive(true);
		fileLoader = GameObject.Find("FileLoader").GetComponent<FileLoader>();
		videoController = fileLoader.videoController.GetComponent<VideoController>();

		VRSettings.enabled = true;
	}
	
	void Update () 
	{
		mouseDelta = new Vector2(Input.mousePosition.x - prevMousePosition.x, Input.mousePosition.y - prevMousePosition.y);
		prevMousePosition = Input.mousePosition;

		if (openVideo != "")
		{
			UpdateTimeline();
		}

		//Note(Simon): Create a reversed raycast to find positions on the sphere with
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		ray.origin = ray.GetPoint(100);
		ray.direction = -ray.direction;

		//NOTE(Simon): Reset InteractionPoint color. Yep this really is the best point to do this.
		foreach (var point in interactionPoints)
		{
			point.point.GetComponent<MeshRenderer>().material.color = Color.white;
		}
		
		if (editorState == EditorState.Inactive)
		{
			if (Input.GetKeyDown(KeyCode.F1))
			{
				SetEditorActive(true);
				//Note(Simon): Early return so we don't interfere with the rest of the state machine
				return;
			}

			if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				&& Input.GetKeyDown(KeyCode.Space))
			{
				videoController.TogglePlay();
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
				SetEditorActive(false);
			}

			if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				&& Input.GetKeyDown(KeyCode.Space))
			{
				videoController.TogglePlay();
			}
		}

		if (editorState == EditorState.PlacingInteractionPoint && !EventSystem.current.IsPointerOverGameObject())
		{
			if (Physics.Raycast(ray, out hit, 100))
			{
				var drawLocation = Vector3.Lerp(hit.point, Camera.main.transform.position, !Camera.main.orthographic ? 0.3f : 0.01f);

				interactionPointTemp.transform.position = drawLocation;
				//NOTE(Simon): Rotate to match sphere's normal
				interactionPointTemp.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
			}

			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				SetEditorActive(true);
			}

			if (Input.GetMouseButtonUp(0))
			{
				var newPoint = Instantiate(interactionPointPrefab, interactionPointTemp.transform.position, interactionPointTemp.transform.rotation);
				var point = new InteractionPointEditor
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
				SetEditorActive(true);
			}
			if (Input.GetKeyDown(KeyCode.F1))
			{
				SetEditorActive(false);
			}

			if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				&& Input.GetKeyDown(KeyCode.Space))
			{
				videoController.TogglePlay();
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
				SetEditorActive(true);
			}
			if (Input.GetKeyDown(KeyCode.F1))
			{
				SetEditorActive(false);
			}

			if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				&& Input.GetKeyDown(KeyCode.Space))
			{
				videoController.TogglePlay();
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
				SetEditorActive(true);
			}
			if (Input.GetKeyDown(KeyCode.F1))
			{
				SetEditorActive(false);
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
				SetEditorActive(true);
				pointToMove.timelineRow.transform.Find("Content/Move").GetComponent<Toggle2>().isOn = false;
			}
			if (Input.GetKeyDown(KeyCode.F1))
			{
				SetEditorActive(false);
				pointToMove.timelineRow.transform.Find("Content/Move").GetComponent<Toggle2>().isOn = false;
			}

			if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				&& Input.GetKeyDown(KeyCode.Space))
			{
				videoController.TogglePlay();
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

		if (editorState == EditorState.NewOpen)
		{
			var panel = newPanel.GetComponent<NewPanel>();
			if (panel.answered)
			{
				if (panel.answerNew)
				{
					var dialog = new System.Windows.Forms.OpenFileDialog
					{
						Title = "Choose a video or photo to enrich",
						Filter = "Video (*.mp4)|*.mp4"
					};

					var result = dialog.ShowDialog();
					if (result == System.Windows.Forms.DialogResult.OK)
					{
						fileLoader.LoadFile(dialog.FileName);
						openVideo = dialog.FileName;

						timelineWindowEndTime = (float)videoController.videoLength;
					}
					else
					{
						panel.answered = false;
						return;
					}
					SetEditorActive(true);
					Destroy(newPanel);
					Canvass.modalBackground.SetActive(false);

					editorState = EditorState.PickingPerspective;
					perspectivePanel = Instantiate(perspectivePanelPrefab);
					perspectivePanel.transform.SetParent(Canvass.main.transform, false);
					Canvass.modalBackground.SetActive(true);
				}

				if (panel.answerOpen)
				{
					OpenFilePanel();
					Destroy(newPanel);
				}
			}
		}

		if (editorState == EditorState.PickingPerspective)
		{
			var panel = perspectivePanel.GetComponent<PerspectivePanel>();
			if (panel.answered)
			{
				fileLoader.SetPerspective(panel.answerPerspective);
				Destroy(perspectivePanel);
				SetEditorActive(true);
				Canvass.modalBackground.SetActive(false);
			}
		}

		if (editorState == EditorState.Saving)
		{
			if (savePanel.GetComponent<SavePanel>().answered)
			{
				var filename = savePanel.GetComponent<SavePanel>().answerFilename;
				if (SaveToFile(filename))
				{
					openFileName = filename;
				}
				else
				{
					openFileName = "";
					Debug.LogError("Something went wrong while saving the file");
				}
				SetEditorActive(true);
				Destroy(savePanel);
				Canvass.modalBackground.SetActive(false);
			}
			
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				SetEditorActive(true);
				Destroy(savePanel);
				Canvass.modalBackground.SetActive(false);
			}
		}

		if (editorState == EditorState.Opening)
		{
			if (openPanel.GetComponent<OpenPanel>().answered)
			{
				var filename = openPanel.GetComponent<OpenPanel>().answerFilename;

				if (OpenFile(filename))
				{
					openFileName = filename;
				}
				else
				{
					openFileName = "";
					Debug.LogError("Something went wrong while loading the file");
				}

				SetEditorActive(true);
				Destroy(openPanel);
				Canvass.modalBackground.SetActive(false);
			}
			
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				SetEditorActive(true);
				Destroy(openPanel);
				Canvass.modalBackground.SetActive(false);
			}
		}

		if (editorState == EditorState.Uploading)
		{
			uploadPanel.GetComponent<UploadPanel>().UpdatePanel(uploadStatus);

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				SetEditorActive(true);
				StopCoroutine(uploadStatus.coroutine);
				Destroy(uploadPanel);
				Canvass.modalBackground.SetActive(false);
			}

			if (uploadStatus.done)
			{
				editorState = EditorState.Active;
				Destroy(uploadPanel);
				Canvass.modalBackground.SetActive(false);
			}
		}

		//NOTE(Simon): This happens if the file was never saved before trying to upload.
		if (editorState == EditorState.SavingThenUploading)
		{
			if (savePanel.GetComponent<SavePanel>().answered)
			{
				var filename = savePanel.GetComponent<SavePanel>().answerFilename;
				if (SaveToFile(filename))
				{
					openFileName = filename;
				}
				else
				{
					openFileName = "";
					Debug.LogError("Something went wrong while saving the file");
				}
				Destroy(savePanel);

				uploadStatus = new UploadStatus();
				editorState = EditorState.Uploading;
				uploadStatus.coroutine = StartCoroutine(UploadFile());
				uploadPanel = Instantiate(uploadPanelPrefab);
				uploadPanel.transform.SetParent(Canvass.main.transform, false);
				videoController.Pause();
				Canvass.modalBackground.SetActive(true);
			}
			else if (uploadStatus.done)
			{
				editorState = EditorState.Active;
				Destroy(uploadPanel);
				Canvass.modalBackground.SetActive(false);
			}
			else
			{
				uploadPanel.GetComponent<UploadPanel>().UpdatePanel(uploadStatus);
			}
			
			if (Input.GetKeyDown(KeyCode.Escape))
			{
				SetEditorActive(true);
				Destroy(savePanel);
				StopCoroutine(uploadStatus.coroutine);
				Destroy(uploadPanel);
				Canvass.modalBackground.SetActive(false);
			}
		}

#if UNITY_EDITOR
		if (Input.GetKey(KeyCode.Z) && Input.GetKeyDown(KeyCode.O) 
			&& AreFileOpsAllowed())
#else
		if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.O)
			&& FileOpsAllowed())
#endif
		{
			OpenFilePanel();
		}

#if UNITY_EDITOR
		if(Input.GetKey(KeyCode.Z) && Input.GetKeyDown(KeyCode.S)
			&& AreFileOpsAllowed())
#else
		if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S) 
			&& FileOpsAllowed())
#endif
		{
			if (openFileName != "")
			{
				if (!SaveToFile(openFileName))
				{
					openFileName = "";
					Debug.LogError("Something went wrong while saving the file");
				}
			}
			else
			{
				savePanel = Instantiate(savePanelPrefab);
				savePanel.transform.SetParent(Canvass.main.transform, false);
				Canvass.modalBackground.SetActive(true);
				editorState = EditorState.Saving;
			}
		}

#if UNITY_EDITOR
		if(Input.GetKey(KeyCode.Z) && Input.GetKeyDown(KeyCode.U)
			&& AreFileOpsAllowed())
#else
		if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S) 
			&& FileOpsAllowed())
#endif
		{
			if (openFileName != "")
			{
				if (!SaveToFile(openFileName))
				{
					openFileName = "";
					Debug.LogError("Something went wrong while saving the file");
				}
			}
			else
			{
				savePanel = Instantiate(savePanelPrefab);
				savePanel.transform.SetParent(Canvass.main.transform, false);
				Canvass.modalBackground.SetActive(true);
				editorState = EditorState.SavingThenUploading;
				return;
			}

			uploadStatus = new UploadStatus();
			editorState = EditorState.Uploading;
			uploadStatus.coroutine = StartCoroutine(UploadFile());
			uploadPanel = Instantiate(uploadPanelPrefab);
			uploadPanel.transform.SetParent(Canvass.main.transform, false);
			videoController.Pause();
			Canvass.modalBackground.SetActive(true);
		}
	}

	bool AreFileOpsAllowed()
	{
		return editorState != EditorState.Saving 
			&& editorState != EditorState.Opening 
			&& editorState != EditorState.NewOpen
			&& editorState != EditorState.Uploading 
			&& editorState != EditorState.PickingPerspective;
	}
	
	void OpenFilePanel()
	{
		openPanel = Instantiate(openPanelPrefab);
		openPanel.GetComponent<OpenPanel>().Init();
		openPanel.transform.SetParent(Canvass.main.transform, false);
		Canvass.modalBackground.SetActive(true);
		editorState = EditorState.Opening;
	}

	void SetEditorActive(bool active)
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

	void AddItemToTimeline(InteractionPointEditor point)
	{
		var newRow = Instantiate(timelineRow);
		point.timelineRow = newRow;
		newRow.transform.SetParent(timeline.transform);
		interactionPoints.Add(point);
	}
	
	void RemoveItemFromTimeline(InteractionPointEditor point)
	{
		Destroy(point.timelineRow);
		interactionPoints.Remove(point);
		Destroy(point.point);
		if (point.panel != null)
		{
			Destroy(point.panel);
		}
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

			var windowMiddle = (timelineEndTime - timelineOffset) / 2; 
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
				headerLabels[i].text = MathHelper.FormatSeconds(time);
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
			var edit = point.timelineRow.transform.Find("Content/Edit").gameObject.GetComponent<Button2>();
			var delete = point.timelineRow.transform.Find("Content/Delete").gameObject.GetComponent<Button2>();
			var move = point.timelineRow.transform.Find("Content/Move").gameObject.GetComponent<Toggle2>();
			var view = point.timelineRow.transform.Find("Content/View").gameObject.GetComponent<Toggle2>();

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

		//Note(Simon): Render various stuff, such as indicator lines for begin and end of video, and lines for the timestamps.
		{
			
		}

		//Note(Simon): Resizing and moving of timeline items. Also Cursors
		{
			Texture2D desiredCursor = null;
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
						desiredCursor = cursors.CursorDrag;
					}
					else if (isResizingTimelineItem)
					{
						desiredCursor = cursors.CursorResizeHorizontal;
					}
					else if (rectPixel.x < leftAreaX || rectPixel.x > rightAreaX)
					{
						desiredCursor = cursors.CursorResizeHorizontal;
					}
					else
					{
						desiredCursor = cursors.CursorDrag;
					}
				}

				if (!isDraggingTimelineItem && !isResizingTimelineItem
					&& Input.GetMouseButtonDown(0) && RectTransformUtility.RectangleContainsScreenPoint(imageRect, Input.mousePosition))
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
	
			Cursor.SetCursor(desiredCursor, desiredCursor == null ? Vector2.zero : new Vector2(15, 15), CursorMode.Auto);

			if (isDraggingTimelineItem)
			{
				if (!Input.GetMouseButton(0))
				{
					isDraggingTimelineItem = false;
					timelineItemBeingDragged = null;
				}
				else
				{
					var newStart = Mathf.Max(0.0f, (float)timelineItemBeingDragged.startTime + PxToRelativeTime(mouseDelta.x));
					var newEnd = Mathf.Min(timelineEndTime, (float)timelineItemBeingDragged.endTime + PxToRelativeTime(mouseDelta.x));
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
						var newStart = Mathf.Max(0.0f, (float)timelineItemBeingResized.startTime + PxToRelativeTime(mouseDelta.x));
						if (newStart < timelineItemBeingResized.endTime - 0.2f)
						{
							timelineItemBeingResized.startTime = newStart;
						}
					}
					else
					{
						var newEnd = Mathf.Min(timelineEndTime, (float)timelineItemBeingResized.endTime + PxToRelativeTime(mouseDelta.x));
						if (newEnd > timelineItemBeingResized.startTime + 0.2f)
						{
							timelineItemBeingResized.endTime = newEnd;
						}
					}
				}
			}
		}

		//Note(Simon): Resizing of timeline
		{
			if (isResizingTimeline)
			{
				if (Input.GetMouseButtonUp(0))
				{
					isResizingTimeline = false;
					Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
				}
				timelineContainer.GetComponent<RectTransform>().sizeDelta += mouseDelta;
			}
			else
			{
				var coords = new Vector3[4];
				timelineContainer.GetComponent<RectTransform>().GetWorldCorners(coords);

				if (Input.mousePosition.y > coords[1].y - 2
					&& Input.mousePosition.y < coords[1].y + 2)
				{
					Cursor.SetCursor(cursors.CursorResizeVertical, new Vector2(15, 15), CursorMode.Auto);
					if (Input.GetMouseButtonDown(0))
					{
						isResizingTimeline = true;
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

	public float PxToAbsTime(double px)
	{
		var realPx = px - timelineXOffset;
		var fraction = realPx / timelineWidth;
		var time = fraction * (timelineWindowEndTime - timelineWindowStartTime) + timelineWindowStartTime;
		return (float) time;
	}

	public float PxToRelativeTime(float px)
	{
		return px * ((timelineWindowEndTime - timelineWindowStartTime) / timelineWidth);
	}
	
	public void OnDrag(BaseEventData e)
	{
		if (Input.GetMouseButton(1))
		{
			var pointerEvent = (PointerEventData)e;
			timelineOffset += PxToRelativeTime(pointerEvent.delta.x * 5);
		}
	}

	private bool SaveToFile(string filename)
	{
		var sb = new StringBuilder();

		if (guid == Guid.Empty)
		{
			guid = Guid.NewGuid();
		}

		sb.Append("uuid:")
			.Append(guid)
			.Append(",\n");

		sb.Append("videoname:")
			.Append(openVideo)
			.Append(",\n");

			
		sb.Append("perspective:")
			.Append(fileLoader.currentPerspective)
			.Append(",\n");

			
		
		sb.Append("[");
		if (interactionPoints.Count > 0)
		{
			foreach (var point in interactionPoints)
			{
				var temp = new InteractionpointSerialize
				{
					position = point.point.transform.position,
					rotation = point.point.transform.rotation,
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
			sb.Append("[]");
		}

		sb.Append("]");

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
		var str = GetSaveFileContents(filename);

		var level = 0;
		var start = 0;
		var count = 0;
		var rising = true;

		var result = new ParsedJsonLine();

		result = JsonGetValueFromLine(str, result.endindex);
		guid = new Guid(result.value);

		result = JsonGetValueFromLine(str, result.endindex);
		openVideo = result.value;

		result = JsonGetValueFromLine(str, result.endindex);
		var perspective = (Perspective)Enum.Parse(typeof(Perspective), result.value);

		var stringObjects = new List<string>();
			
		for(var i = result.endindex; i < str.Length; i++)
		{
			if (str[i] == '{')
			{
				if (level == 0)
				{
					start = i;
				}
				rising = true;
				level++;
			}
			if (str[i] == '}')
			{
				level--;
				rising = false;
			}

			count++;

			if (level == 0 && !rising)
			{
				stringObjects.Add(str.Substring(start, count - 1));
				count = 0;
				rising = true;
			}
			if (level < 0)
			{
				Debug.Log("Corrupted save file. Aborting");
				return false;
			}
		}

		fileLoader.LoadFile(openVideo);
		fileLoader.SetPerspective(perspective);

		var points = new List<InteractionpointSerialize>();

		foreach (var obj in stringObjects)
		{
			points.Add(JsonUtility.FromJson<InteractionpointSerialize>(obj));
		}

		for (var j = interactionPoints.Count - 1; j >= 0; j--)
		{
			RemoveItemFromTimeline(interactionPoints[j]);
		}

		interactionPoints.Clear();

		foreach (var point in points)
		{
			var newPoint = Instantiate(interactionPointPrefab, point.position, point.rotation);

			var newInteractionPoint = new InteractionPointEditor
			{
				startTime = point.startTime,
				endTime = point.endTime,
				title = point.title,
				body = point.body,
				type = point.type,
				filled = true,
				point = newPoint,
			};

			switch (newInteractionPoint.type)
			{
				case InteractionType.Text:
				{
					var panel = Instantiate(textPanelPrefab);
					panel.GetComponent<TextPanel>().Init(point.position, newInteractionPoint.title, newInteractionPoint.body);
					newInteractionPoint.panel = panel;
					break;
				}
				case InteractionType.Image:
				{
					var panel = Instantiate(imagePanelPrefab);
					panel.GetComponent<ImagePanel>().Init(point.position, newInteractionPoint.title, newInteractionPoint.body);
					newInteractionPoint.panel = panel;
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			AddItemToTimeline(newInteractionPoint);
		}

		return true;
	}

	private static string GetSaveFileContents(string filename)
	{
		string str;
		using (var fileContents = File.OpenText(Path.Combine(Application.persistentDataPath, filename)))
		{
			try
			{
				str = fileContents.ReadToEnd();
			}
			catch (Exception e)
			{
				Debug.Log("Something went wrong while loading the file.");
				Debug.Log(e.ToString());
				return "";
			}
		}

		return str;
	}

	private static byte[] GetSaveFileContentsBinary(string filename)
	{
		byte[] data;
		using (var fileContents = File.OpenRead(Path.Combine(Application.persistentDataPath, filename)))
		{
			try
			{
				data = new byte[(int)fileContents.Length];
				fileContents.Read(data, 0, (int)fileContents.Length);
			}
			catch (Exception e)
			{
				Debug.Log("Something went wrong while loading the file.");
				Debug.Log(e.ToString());
				return new byte[0];
			}
		}

		return data;
	}

	private IEnumerator UploadFile()
	{
		const string baseUrl = "localhost";
		const string jsonUrl = baseUrl + "/json";
		const string videoUrl = baseUrl + "/video";

		var str = GetSaveFileContentsBinary(openFileName);

		var formJson = new WWWForm();
		//TODO(Simon): Add a login form
		formJson.AddField("username", "simon");
		formJson.AddField("password", "admin123");
		formJson.AddField("uuid", guid.ToString());
		formJson.AddBinaryData("jsonFile", str, "json" + guid);

		var wwwJson = new WWW(jsonUrl, formJson);

		yield return wwwJson;
		var status = wwwJson.StatusCode();
		if (status != 200)
		{
			if (status == 401)
			{
				Debug.Log("Bad login");
			}
			yield break;
		}

		Debug.Log(wwwJson.text);

		uploadStatus.partSize = 1 * gigabyte;
		uploadStatus.totalSize = (int)new FileInfo(openVideo).Length;
		uploadStatus.parts = uploadStatus.totalSize / uploadStatus.partSize + 1;
		using (var fileContents = File.OpenRead(openVideo))
		{
			for (uploadStatus.currentPart = 0; uploadStatus.currentPart < uploadStatus.parts; uploadStatus.currentPart++)
			{
				var read = 0;
				var data = new byte[uploadStatus.partSize];
				uploadStatus.currentPartSize = uploadStatus.partSize;

				try
				{
					read = fileContents.Read(data, 0, uploadStatus.partSize);
				}
				catch (Exception e)
				{
					Debug.Log("Something went wrong while loading the file.");
					Debug.Log(e.ToString());
				}

				if (read != uploadStatus.partSize)
				{
					var newArray = new byte[read];
					Array.Copy(data, newArray, read);
					data = newArray;
					uploadStatus.currentPartSize = read;
				}

				var formVideo = new WWWForm();
				//TODO(Simon): Add a login form
				formVideo.AddField("username", "simon");
				formVideo.AddField("password", "admin123");
				formVideo.AddField("uuid", guid.ToString());
				formVideo.AddBinaryData("video", data, "video" + guid, "multipart/form-data");

				uploadStatus.currentRequest = new WWW(videoUrl, formVideo);

				yield return uploadStatus.currentRequest;
				status = uploadStatus.currentRequest.StatusCode();
				if (status != 200)
				{
					if (status == 401)
					{
						Debug.Log("Bad login");
					}

					yield break;
				}

				Debug.Log(uploadStatus.currentRequest.text);
			}
		}

		uploadStatus.done = true;
	}

	private void ResetInteractionPointTemp()
	{
		interactionPointTemp.transform.position = new Vector3(1000, 1000, 1000);
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

	private class ParsedJsonLine{
		public string value;
		public int endindex;
	}

	private static ParsedJsonLine JsonGetValueFromLine(string json, int startIndex)
	{
		var startValue = json.IndexOf(':', startIndex) + 1;
		var endValue = json.IndexOf('\n', startIndex) + 1;
		return new ParsedJsonLine
		{
			value = json.Substring(startValue, (endValue- startValue) - 2),
			endindex = endValue
		};
	}
}
