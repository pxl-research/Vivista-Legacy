using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public enum EditorState
{
	Inactive,
	Active,
	PlacingInteractionPoint,
	PickingInteractionType,
	FillingPanelDetails,
	MovingInteractionPoint,
	Saving,
	EditingInteractionPoint,
	Opening,
	PickingVideo,
	PickingPerspective,
	SavingThenUploading,
	LoggingIn,
	Exporting
}

public enum InteractionType
{
	None,
	Text,
	Image,
	Video,
	MultipleChoice,
	Audio,
	FindArea,
	MultipleChoiceArea,
	MultipleChoiceImage,
	TabularData
}

public enum Perspective
{
	Perspective360,
	Perspective180,
	PerspectiveFlat
}

[Serializable]
public class InteractionPointEditor
{
	public GameObject point;
	public TimelineRow timelineRow;
	public GameObject panel;
	public InteractionType type;
	public string title;
	public string body;
	public string filename;
	public double startTime;
	public double endTime;
	public int tagId;
	public bool mandatory;
	public bool filled;

	public Vector3 returnRayOrigin;
	public Vector3 returnRayDirection;
}

//NOTE(Simon): If you change something here, update SaveFile and InteractionPointSerializeCompat as well
public class InteractionPointSerialize
{
	public InteractionType type;
	public string title;
	public string body;
	public string filename;
	public double startTime;
	public double endTime;
	public int tagId;
	public bool mandatory;

	public Vector3 returnRayOrigin;
	public Vector3 returnRayDirection;

	public static InteractionPointSerialize FromCompat(InteractionPointSerializeCompat compat)
	{
		var serialize = new InteractionPointSerialize
		{
			type = compat.type,
			title = compat.title,
			body = compat.body,
			filename = compat.filename,
			startTime = compat.startTime,
			endTime = compat.endTime,
			returnRayOrigin = compat.returnRayOrigin,
			returnRayDirection = compat.returnRayDirection,
			//NOTE(Simon): In old savefiles tagId is missing. Unity will decode missing items as default(T), 0 in this case. -1 means no tag
			tagId = compat.tagId <= 0 ? -1 : compat.tagId,
			mandatory = compat.mandatory,
		};

		return serialize;
	}
}

public class UploadStatus
{
	public Coroutine coroutine;
	public UnityWebRequest request;
	public long totalSize;
	public bool done;
	public bool failed;
	public string error;
	public Queue<Timing> timings = new Queue<Timing>();
	public long uploaded;
}

public struct Timing
{
	public float time;
	public float totalUploaded;
}

//NOTE(Simon): If you change something here, update SaveFile and MetaDataCompat as well
public struct Metadata
{
	public int version;
	public string title;
	public string description;
	public Guid guid;
	public float length;

	public static Metadata FromCompat(MetaDataCompat compat)
	{
		return new Metadata
		{
			version = SaveFile.VERSION,
			title = compat.title,
			description = compat.description,
			guid = compat.guid,
			length = compat.length
		};
	}
}

public class Editor : MonoBehaviour
{
	public static Editor Instance;

	public EditorState editorState;

	public Guid currentProjectGuid => meta.guid;

	public GameObject timeTooltipPrefab;
	public GameObject interactionPointPrefab;
	private GameObject interactionPointTemp;
	private List<InteractionPointEditor> interactionPoints;
	private List<InteractionPointEditor> sortedInteractionPoints;
	private InteractionPointEditor pointToMove;
	private InteractionPointEditor pointToEdit;
	private InteractionPointEditor lastPlacedPoint;

	private InteractionTypePicker interactionTypePicker;
	private GameObject interactionEditor;
	private ProjectPanel savePanel;
	private ProjectPanel projectPanel;
	private UploadPanel uploadPanel;
	private LoginPanel loginPanel;
	private ExplorerPanel explorerPanel;
	private TagPanel tagPanel;
	private ExportPanel exportPanel;

	public RectTransform timelineContainer;
	public RectTransform timeline;
	public RectTransform timelineHeader;
	public GameObject timelineRowPrefab;
	public Text labelPrefab;
	public AudioMixer mixer;

	private List<Text> headerLabels = new List<Text>();
	private VideoController videoController;
	private Slider audioSlider;
	private FileLoader fileLoader;
	private InteractionPointEditor pinnedHoverPoint;
	private float timelineStartTime;
	private float timelineWindowStartTime;
	private float timelineWindowEndTime;
	private float timelineEndTime;
	private float timelineZoomTarget = 1;
	private float timelineZoom = 1;
	private float timelineOffsetTime;
	private float timelineOffsetPixels;
	private float timelineWidthPixels;
	private bool timelineLabelsDirty;

	private Vector2 prevMousePosition;
	private Vector2 mouseDelta;
	private InteractionPointEditor timelineItemBeingDragged;
	private bool isDraggingTimelineItem;
	private InteractionPointEditor timelineItemBeingResized;
	private bool isResizingTimelineItem;
	private bool isResizingStart;
	private bool isResizingTimelineVertical;
	private bool isResizingTimelineHorizontal;
	private TimeTooltip timeTooltip;
	public RectTransform timelineFirstColumnWidth;

	private Metadata meta;
	private string userToken = "";
	private UploadStatus uploadStatus;
	private Dictionary<string, InteractionPointEditor> allExtras = new Dictionary<string, InteractionPointEditor>();

	private int interactionPointCount;


	private void Awake()
	{
		Physics.autoSimulation = false;
		Instance = this;
		//NOTE(Kristof): This needs to be called in awake so we're guaranteed it isn't in VR mode
		UnityEngine.XR.XRSettings.enabled = false;
		Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
	}

	private void Start()
	{
		interactionPointTemp = Instantiate(interactionPointPrefab);
		interactionPointTemp.name = "Temp InteractionPoint";

		interactionPoints = new List<InteractionPointEditor>();
		sortedInteractionPoints = new List<InteractionPointEditor>();

		timeTooltip = Instantiate(timeTooltipPrefab, new Vector3(-1000, -1000), Quaternion.identity, Canvass.main.transform).GetComponent<TimeTooltip>();
		timeTooltip.ResetPosition();

		timelineLabelsDirty = true;

		prevMousePosition = Input.mousePosition;

		SetEditorActive(false);
		meta = new Metadata();

		InitOpenProjectPanel();

		fileLoader = GameObject.Find("FileLoader").GetComponent<FileLoader>();
		videoController = fileLoader.controller;
		videoController.mixer = mixer;
		VideoControls.videoController = videoController;
		
		//NOTE(Simon): Login if details were remembered
		{
			var details = LoginPanel.GetSavedLogin();
			if (details != null)
			{
				var response = LoginPanel.SendLoginRequest(details.username, details.password);
				if (response.Item1 == 200)
				{
					userToken = response.Item2;
					Toasts.AddToast(5, "Logged in");
				}
			}
		}
	}

	private void Update()
	{
		mouseDelta = new Vector2(Input.mousePosition.x - prevMousePosition.x, Input.mousePosition.y - prevMousePosition.y);
		prevMousePosition = Input.mousePosition;

		//TODO(Simon): this is a hack to fix a bug. Sort sorts in place. So the sorted list got passed to all kinds of places where we need an unsorted list.
		sortedInteractionPoints.Clear();
		sortedInteractionPoints.AddRange(interactionPoints);
		sortedInteractionPoints.Sort((x, y) => x.startTime != y.startTime
			? x.startTime.CompareTo(y.startTime)
			: x.endTime.CompareTo(y.endTime));
		interactionPointCount = 0;

		//NOTE(Simon): Reset InteractionPoint color. Yep this really is the best place to do this.
		foreach (var point in sortedInteractionPoints)
		{
			point.point.GetComponent<SpriteRenderer>().color = TagManager.Instance.GetTagColorById(point.tagId);
			point.point.GetComponentInChildren<TextMesh>().text = (++interactionPointCount).ToString();
		}

		if (videoController.videoLoaded)
		{
			UpdateTimeline();
		}

		//Note(Simon): Create a reversed raycast to find positions on the sphere with
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		ray = ray.ReverseRay();

		if (editorState == EditorState.Inactive)
		{
			if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				&& Input.GetKeyDown(KeyCode.Space))
			{
				videoController.TogglePlay();
			}
		}

		if (editorState == EditorState.Active)
		{
			if (Input.GetMouseButtonDown(0)
				&& !EventSystem.current.IsPointerOverGameObject()
				&& !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				&& pinnedHoverPoint == null)
			{
				editorState = EditorState.PlacingInteractionPoint;
			}

			if (Physics.Raycast(ray, out hit, 100, 1 << LayerMask.NameToLayer("interactionPoints")))
			{
				hit.collider.GetComponentInParent<SpriteRenderer>().color = Color.red;
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
				SetInteractionpointPosition(interactionPointTemp, hit.point);
			}

			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				SetEditorActive(true);
			}

			if (Input.GetMouseButtonUp(0))
			{
				var newPoint = Instantiate(interactionPointPrefab, interactionPointTemp.transform.position, interactionPointTemp.transform.rotation);
				var startTime = Double.IsNaN(videoController.rawCurrentTime) ? 0 : videoController.rawCurrentTime;
				var length = Double.IsNaN(videoController.videoLength) ? 10 : videoController.videoLength;
				var point = new InteractionPointEditor
				{
					returnRayOrigin = ray.origin,
					returnRayDirection = ray.direction,
					point = newPoint,
					type = InteractionType.None,
					startTime = startTime,
					endTime = startTime + (length / 10),
					tagId = -1,
				};

				lastPlacedPoint = point;
				AddItemToTimeline(point, false);
				SetInteractionPointTag(point);

				interactionTypePicker = Instantiate(UIPanels.Instance.interactionTypePicker, Canvass.main.transform, false);

				editorState = EditorState.PickingInteractionType;
				ResetInteractionPointTemp();
			}

			if (Input.GetKeyUp(KeyCode.Escape))
			{
				SetEditorActive(true);
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
				var picker = interactionTypePicker;
				if (picker.answered)
				{
					lastPlacedPoint.type = picker.answer;

					switch (lastPlacedPoint.type)
					{
						case InteractionType.Image:
						{
							interactionEditor = Instantiate(UIPanels.Instance.imagePanelEditor, Canvass.main.transform).gameObject;
							interactionEditor.GetComponent<ImagePanelEditor>().Init("", null);
							break;
						}
						case InteractionType.Text:
						{
							interactionEditor = Instantiate(UIPanels.Instance.textPanelEditor, Canvass.main.transform).gameObject;
							interactionEditor.GetComponent<TextPanelEditor>().Init("", "");
							break;
						}
						case InteractionType.Video:
						{
							interactionEditor = Instantiate(UIPanels.Instance.videoPanelEditor, Canvass.main.transform).gameObject;
							interactionEditor.GetComponent<VideoPanelEditor>().Init("", "");
							break;
						}
						case InteractionType.MultipleChoice:
						{
							interactionEditor = Instantiate(UIPanels.Instance.multipleChoicePanelEditor, Canvass.main.transform).gameObject;
							interactionEditor.GetComponent<MultipleChoicePanelEditor>().Init("");
							break;
						}
						case InteractionType.Audio:
						{
							interactionEditor = Instantiate(UIPanels.Instance.audioPanelEditor, Canvass.main.transform).gameObject;
							interactionEditor.GetComponent<AudioPanelEditor>().Init("", "");
							break;
						}
						case InteractionType.FindArea:
						{
							interactionEditor = Instantiate(UIPanels.Instance.findAreaPanelEditor, Canvass.main.transform).gameObject;
							interactionEditor.GetComponent<FindAreaPanelEditor>().Init("", meta.guid, null);
							break;
						}
						case InteractionType.MultipleChoiceArea:
						{
							interactionEditor = Instantiate(UIPanels.Instance.multipleChoiceAreaPanelEditor, Canvass.main.transform).gameObject;
							interactionEditor.GetComponent<MultipleChoiceAreaPanelEditor>().Init("", meta.guid, null, -1);
							break;
						}
						case InteractionType.MultipleChoiceImage:
						{
							interactionEditor = Instantiate(UIPanels.Instance.multipleChoiceImagePanelEditor, Canvass.main.transform).gameObject;
							interactionEditor.GetComponent<MultipleChoiceImagePanelEditor>().Init("", null);
							break;
						}
						case InteractionType.TabularData:
							interactionEditor = Instantiate(UIPanels.Instance.tabularDataPanelEditor, Canvass.main.transform).gameObject;
							interactionEditor.GetComponent<TabularDataPanelEditor>().Init("", 1, 1, new List<string>());
							break;
						default:
						{
							Debug.LogError("FFS, you shoulda added it here");
							break;
						}
					}

					interactionEditor.GetComponentInChildren<TagPicker>().Init(-1);
					interactionEditor.GetComponentInChildren<MandatoryPanel>().Init(false);

					Destroy(interactionTypePicker.gameObject);
					editorState = EditorState.FillingPanelDetails;
				}
			}

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				RemoveItemFromTimeline(lastPlacedPoint);
				lastPlacedPoint = null;
				Destroy(interactionTypePicker.gameObject);
				SetEditorActive(true);
			}

			if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				&& Input.GetKeyDown(KeyCode.Space))
			{
				videoController.TogglePlay();
			}
		}

		if (editorState == EditorState.FillingPanelDetails)
		{
			var lastPlacedPointPos = lastPlacedPoint.point.transform.position;
			bool finished = false;
			//NOTE(Simon): Some panels use Esc internally to cancel a child action. We don't want to also cancel the panel in that case.
			bool allowCancel = true;

			switch (lastPlacedPoint.type)
			{
				case InteractionType.Image:
				{
					var editor = interactionEditor.GetComponent<ImagePanelEditor>();
					allowCancel = editor.allowCancel;

					if (editor.answered)
					{
						var originalPaths = editor.answerURLs;
						var newFilenames = new List<string>(originalPaths.Count);
						var newFullPaths = new List<string>(originalPaths.Count);

						foreach (string originalPath in originalPaths)
						{
							var newFilename = CopyNewExtra(lastPlacedPoint, originalPath);
							var newFullPath = Path.Combine(Application.persistentDataPath, meta.guid.ToString(), newFilename);

							newFilenames.Add(newFilename);
							newFullPaths.Add(newFullPath);
						}

						var panel = Instantiate(UIPanels.Instance.imagePanel, Canvass.main.transform);
						panel.Init(editor.answerTitle, newFullPaths);
						lastPlacedPoint.title = editor.answerTitle;
						lastPlacedPoint.body = "";
						lastPlacedPoint.filename = String.Join("\f", newFilenames);
						lastPlacedPoint.panel = panel.gameObject;

						finished = true;
					}
					break;
				}
				case InteractionType.Text:
				{
					var editor = interactionEditor.GetComponent<TextPanelEditor>();
					if (editor.answered)
					{
						var panel = Instantiate(UIPanels.Instance.textPanel, Canvass.main.transform);
						panel.Init(editor.answerTitle, editor.answerBody);
						lastPlacedPoint.title = editor.answerTitle;
						lastPlacedPoint.body = editor.answerBody;
						lastPlacedPoint.panel = panel.gameObject;

						finished = true;
					}
					break;
				}
				case InteractionType.Video:
				{
					var editor = interactionEditor.GetComponent<VideoPanelEditor>();
					allowCancel = editor.allowCancel;

					if (editor.answered)
					{
						var newPath = CopyNewExtra(lastPlacedPoint, editor.answerURL);
						var newFullPath = Path.Combine(Application.persistentDataPath, meta.guid.ToString(), newPath);

						var panel = Instantiate(UIPanels.Instance.videoPanel, Canvass.main.transform);
						panel.Init(editor.answerTitle, newFullPath);
						lastPlacedPoint.title = editor.answerTitle;
						lastPlacedPoint.filename = newPath;
						lastPlacedPoint.panel = panel.gameObject;

						finished = true;
					}
					break;
				}
				case InteractionType.Audio:
				{
					var editor = interactionEditor.GetComponent<AudioPanelEditor>();
					allowCancel = editor.allowCancel;

					if (editor.answered)
					{
						var newPath = CopyNewExtra(lastPlacedPoint, editor.answerURL);
						var newFullPath = Path.Combine(Application.persistentDataPath, meta.guid.ToString(), newPath);

						var panel = Instantiate(UIPanels.Instance.audioPanel, Canvass.main.transform);
						panel.Init(editor.answerTitle, newFullPath);

						lastPlacedPoint.title = editor.answerTitle;
						lastPlacedPoint.filename = newPath;
						lastPlacedPoint.panel = panel.gameObject;

						finished = true;
					}
					break;
				}
				case InteractionType.MultipleChoice:
				{
					var editor = interactionEditor.GetComponent<MultipleChoicePanelEditor>();
					if (editor.answered)
					{
						var panel = Instantiate(UIPanels.Instance.multipleChoicePanel, Canvass.main.transform);
						lastPlacedPoint.title = editor.answerQuestion;
						//NOTE(Kristof): \f is used as a split character to divide the string into an array
						lastPlacedPoint.body = editor.answerCorrect + "\f";
						lastPlacedPoint.body += String.Join("\f", editor.answerAnswers);
						lastPlacedPoint.panel = panel.gameObject;

						//NOTE(Kristof): Init after building the correct body string because the function expect the correct answer index to be passed with the string
						panel.Init(editor.answerQuestion, lastPlacedPoint.body.Split('\f'));

						finished = true;
					}
					break;
				}
				case InteractionType.FindArea:
				{
					var editor = interactionEditor.GetComponent<FindAreaPanelEditor>();
					allowCancel = editor.allowCancel;

					if (editor.answered)
					{
						var panel = Instantiate(UIPanels.Instance.findAreaPanel, Canvass.main.transform);
						var areas = editor.answerAreas;
						var jsonAreas = new StringBuilder();
						var jsonMiniatures = new StringBuilder();
						foreach (var area in areas)
						{
							jsonAreas.Append(JsonHelper.ToJson(area.vertices.ToArray()));
							jsonAreas.Append('\f');
							jsonMiniatures.Append(area.miniatureName);
							jsonMiniatures.Append('\f');
						}

						jsonAreas.Remove(jsonAreas.Length - 1, 1);
						jsonMiniatures.Remove(jsonMiniatures.Length - 1, 1);

						lastPlacedPoint.title = editor.answerTitle;
						lastPlacedPoint.filename = jsonMiniatures.ToString();
						lastPlacedPoint.body = jsonAreas.ToString();
						lastPlacedPoint.panel = panel.gameObject;

						panel.Init(editor.answerTitle, meta.guid, editor.answerAreas);

						finished = true;
					}
					break;
				}
				case InteractionType.MultipleChoiceArea:
				{
					var editor = interactionEditor.GetComponent<MultipleChoiceAreaPanelEditor>();
					allowCancel = editor.allowCancel;
					if (editor.answered)
					{
						var panel = Instantiate(UIPanels.Instance.multipleChoiceAreaPanel, Canvass.main.transform);

						var areas = editor.answerAreas;
						var jsonAreas = new StringBuilder();
						var jsonMiniatures = new StringBuilder();

						jsonAreas.Append(editor.answerCorrect);
						jsonAreas.Append('\f');
						foreach (var area in areas)
						{
							jsonAreas.Append(JsonHelper.ToJson(area.vertices.ToArray()));
							jsonAreas.Append('\f');
							jsonMiniatures.Append(area.miniatureName);
							jsonMiniatures.Append('\f');
						}

						jsonAreas.Remove(jsonAreas.Length - 1, 1);
						jsonMiniatures.Remove(jsonMiniatures.Length - 1, 1);

						lastPlacedPoint.title = editor.answerTitle;
						lastPlacedPoint.body = jsonAreas.ToString();
						lastPlacedPoint.filename = jsonMiniatures.ToString();
						lastPlacedPoint.panel = panel.gameObject;

						panel.Init(editor.answerTitle, meta.guid, editor.answerAreas, editor.answerCorrect);

						finished = true;
					}
					break;
				}
				case InteractionType.MultipleChoiceImage:
				{
					var editor = interactionEditor.GetComponent<MultipleChoiceImagePanelEditor>();
					allowCancel = editor.allowCancel;

					if (editor.answered)
					{
						var originalPaths = editor.answerURLs;
						var newFilenames = new List<string>(originalPaths.Count);
						var newFullPaths = new List<string>(originalPaths.Count);

						foreach (string originalPath in originalPaths)
						{
							var newFilename = CopyNewExtra(lastPlacedPoint, originalPath);
							var newFullPath = Path.Combine(Application.persistentDataPath, meta.guid.ToString(), newFilename);

							newFilenames.Add(newFilename);
							newFullPaths.Add(newFullPath);
						}

						var panel = Instantiate(UIPanels.Instance.multipleChoiceImagePanel, Canvass.main.transform);
						panel.Init(editor.answerQuestion, newFullPaths, editor.answerCorrect);
						lastPlacedPoint.title = editor.answerQuestion;
						lastPlacedPoint.body = editor.answerCorrect.ToString();
						lastPlacedPoint.filename = String.Join("\f", newFilenames);
						lastPlacedPoint.panel = panel.gameObject;

						finished = true;
					}
					break;
				}
				case InteractionType.TabularData:
				{
					var editor = interactionEditor.GetComponent<TabularDataPanelEditor>();

					if (editor.answered)
					{
						var panel = Instantiate(UIPanels.Instance.tabularDataPanel, Canvass.main.transform);
						lastPlacedPoint.title = editor.answerTitle;
						//NOTE(Jitse): \f is used as a split character to divide the string into an array
						lastPlacedPoint.body = $"{editor.answerRows}\f{editor.answerColumns}\f";
						lastPlacedPoint.body += string.Join("\f", editor.answerTabularData);
						lastPlacedPoint.panel = panel.gameObject;

						//NOTE(Jitse): Init after building the correct body string because the function expect the correct answer index to be passed with the string
						panel.Init(editor.answerTitle, editor.answerRows, editor.answerColumns, editor.answerTabularData);

						finished = true;
					}
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException();
				}
			}

			if (finished)
			{
				lastPlacedPoint.tagId = interactionEditor.GetComponentInChildren<TagPicker>().currentTagId;
				lastPlacedPoint.mandatory = interactionEditor.GetComponentInChildren<MandatoryPanel>().isMandatory;
				lastPlacedPoint.timelineRow.mandatory.isOn = lastPlacedPoint.mandatory;
				Destroy(interactionEditor);
				editorState = EditorState.Active;
				lastPlacedPoint.filled = true;
				SetInteractionPointTag(lastPlacedPoint);
				UnsavedChangesTracker.Instance.unsavedChanges = true;
			}

			if (allowCancel && Input.GetKeyDown(KeyCode.Escape))
			{
				RemoveItemFromTimeline(lastPlacedPoint);
				lastPlacedPoint = null;
				Destroy(interactionEditor);
				SetEditorActive(true);
			}
		}

		if (editorState == EditorState.MovingInteractionPoint)
		{
			if (Physics.Raycast(ray, out hit, 100))
			{
				SetInteractionpointPosition(pointToMove.point, hit.point);
				var trans = pointToMove.point.transform;
				transform.position = hit.point;
			}

			if (Input.GetMouseButtonDown(0))
			{
				pointToMove.filled = true;
				pointToMove.returnRayOrigin = ray.origin;
				pointToMove.returnRayDirection = ray.direction;

				SetEditorActive(true);
				pointToMove.timelineRow.transform.Find("Content/Move").GetComponent<Toggle2>().isOn = false;
				UnsavedChangesTracker.Instance.unsavedChanges = true;
			}

			if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				&& Input.GetKeyDown(KeyCode.Space))
			{
				videoController.TogglePlay();
			}
		}

		if (editorState == EditorState.EditingInteractionPoint)
		{
			var finished = false;
			bool allowCancel = true;

			switch (pointToEdit.type)
			{
				case InteractionType.Image:
				{
					var editor = interactionEditor.GetComponent<ImagePanelEditor>();
					allowCancel = editor.allowCancel;

					if (editor.answered)
					{
						SetExtrasToDeleted(pointToEdit.filename);

						var originalPaths = editor.answerURLs;
						var newFilenames = new List<string>(originalPaths.Count);
						var newFullPaths = new List<string>(originalPaths.Count);

						foreach (string originalPath in originalPaths)
						{
							var newFilename = CopyNewExtra(pointToEdit, originalPath);
							var newFullPath = Path.Combine(Application.persistentDataPath, meta.guid.ToString(), newFilename);

							newFilenames.Add(newFilename);
							newFullPaths.Add(newFullPath);
						}

						var panel = pointToEdit.panel.GetComponent<ImagePanel>();
						panel.Init(editor.answerTitle, newFullPaths);

						pointToEdit.title = editor.answerTitle;
						pointToEdit.filename = String.Join("\f", newFilenames);
						pointToEdit.panel = panel.gameObject;
						finished = true;
					}
					break;
				}
				case InteractionType.Text:
				{
					var editor = interactionEditor.GetComponent<TextPanelEditor>();
					if (editor.answered)
					{
						var panel = pointToEdit.panel.GetComponent<TextPanel>();
						panel.Init(editor.answerTitle, editor.answerBody);

						pointToEdit.title = editor.answerTitle;
						pointToEdit.body = editor.answerBody;
						pointToEdit.panel = panel.gameObject;
						finished = true;
					}
					break;
				}
				case InteractionType.Video:
				{
					var editor = interactionEditor.GetComponent<VideoPanelEditor>();
					allowCancel = editor.allowCancel;

					if (editor && editor.answered)
					{
						SetExtrasToDeleted(pointToEdit.filename);

						var newPath = CopyNewExtra(pointToEdit, editor.answerURL);
						var newFullPath = Path.Combine(Application.persistentDataPath, meta.guid.ToString(), newPath);

						var panel = pointToEdit.panel.GetComponent<VideoPanel>();
						panel.Init(editor.answerTitle, newFullPath);

						pointToEdit.title = editor.answerTitle;
						pointToEdit.filename = newPath;
						pointToEdit.panel = panel.gameObject;
						finished = true;
					}
					break;
				}
				case InteractionType.Audio:
				{
					var editor = interactionEditor.GetComponent<AudioPanelEditor>();
					allowCancel = editor.allowCancel;

					if (editor && editor.answered)
					{
						SetExtrasToDeleted(pointToEdit.filename);

						var newPath = CopyNewExtra(pointToEdit, editor.answerURL);
						var newFullPath = Path.Combine(Application.persistentDataPath, meta.guid.ToString(), newPath);

						var panel = pointToEdit.panel.GetComponent<AudioPanel>();
						panel.Init(editor.answerTitle, newFullPath);

						pointToEdit.title = editor.answerTitle;
						pointToEdit.filename = newPath;
						pointToEdit.panel = panel.gameObject;
						finished = true;
					}
					break;
				}
				case InteractionType.MultipleChoice:
				{
					var editor = interactionEditor.GetComponent<MultipleChoicePanelEditor>();
					if (editor.answered)
					{
						var panel = pointToEdit.panel.GetComponent<MultipleChoicePanel>();
						pointToEdit.title = editor.answerQuestion;
						//NOTE(Kristof): \f is used as a split character to divide the string into an array
						pointToEdit.body = editor.answerCorrect + "\f";
						pointToEdit.body += String.Join("\f", editor.answerAnswers);
						pointToEdit.panel = panel.gameObject;

						//NOTE(Kristof): Init after building the correct body string because the function expect the correct answer index to be passed with the string
						panel.Init(editor.answerQuestion, pointToEdit.body.Split('\f'));
						finished = true;
					}
					break;
				}
				case InteractionType.FindArea:
				{
					var editor = interactionEditor.GetComponent<FindAreaPanelEditor>();
					allowCancel = editor.allowCancel;

					if (editor.answered)
					{
						var panel = pointToEdit.panel.GetComponent<FindAreaPanel>();
						panel.Init(editor.answerTitle, meta.guid, editor.answerAreas);

						var areas = editor.answerAreas;
						var jsonAreas = new StringBuilder();
						var jsonMiniatures = new StringBuilder();
						foreach (var area in areas)
						{
							jsonAreas.Append(JsonHelper.ToJson(area.vertices.ToArray()));
							jsonAreas.Append('\f');
							jsonMiniatures.Append(area.miniatureName);
							jsonMiniatures.Append('\f');
						}

						jsonAreas.Remove(jsonAreas.Length - 1, 1);
						jsonMiniatures.Remove(jsonMiniatures.Length - 1, 1);

						pointToEdit.title = editor.answerTitle;
						pointToEdit.body = jsonAreas.ToString();
						pointToEdit.filename = jsonMiniatures.ToString();
						pointToEdit.panel = panel.gameObject;
						finished = true;
					}
					break;
				}
				case InteractionType.MultipleChoiceArea:
				{
					var editor = interactionEditor.GetComponent<MultipleChoiceAreaPanelEditor>();
					allowCancel = editor.allowCancel;

					if (editor.answered)
					{
						var panel = pointToEdit.panel.GetComponent<MultipleChoiceAreaPanel>(); ;
						panel.Init(editor.answerTitle, meta.guid, editor.answerAreas, editor.answerCorrect);

						var areas = editor.answerAreas;
						var jsonAreas = new StringBuilder();
						var jsonMiniatures = new StringBuilder();

						jsonAreas.Append(editor.answerCorrect);
						jsonAreas.Append('\f');
						foreach (var area in areas)
						{
							jsonAreas.Append(JsonHelper.ToJson(area.vertices.ToArray()));
							jsonAreas.Append('\f');
							jsonMiniatures.Append(area.miniatureName);
							jsonMiniatures.Append('\f');
						}

						jsonAreas.Remove(jsonAreas.Length - 1, 1);
						jsonMiniatures.Remove(jsonMiniatures.Length - 1, 1);

						pointToEdit.title = editor.answerTitle;
						pointToEdit.body = jsonAreas.ToString();
						pointToEdit.filename = jsonMiniatures.ToString();
						pointToEdit.panel = panel.gameObject;
						finished = true;
					}
					break;
				}
				case InteractionType.MultipleChoiceImage:
				{
					var editor = interactionEditor.GetComponent<MultipleChoiceImagePanelEditor>();
					allowCancel = editor.allowCancel;

					if (editor.answered)
					{
						SetExtrasToDeleted(pointToEdit.filename);

						var originalPaths = editor.answerURLs;
						var newFilenames = new List<string>(originalPaths.Count);
						var newFullPaths = new List<string>(originalPaths.Count);

						foreach (string originalPath in originalPaths)
						{
							var newFilename = CopyNewExtra(pointToEdit, originalPath);
							var newFullPath = Path.Combine(Application.persistentDataPath, meta.guid.ToString(), newFilename);

							newFilenames.Add(newFilename);
							newFullPaths.Add(newFullPath);
						}

						var panel = pointToEdit.panel.GetComponent<MultipleChoiceImagePanel>();
						panel.Init(editor.answerQuestion, newFullPaths, editor.answerCorrect);

						pointToEdit.title = editor.answerQuestion;
						pointToEdit.body = editor.answerCorrect.ToString();
						pointToEdit.filename = String.Join("\f", newFilenames);
						pointToEdit.panel = panel.gameObject;
						finished = true;
					}
					break;
				}
				case InteractionType.TabularData:
				{
					var editor = interactionEditor.GetComponent<TabularDataPanelEditor>();
					if (editor.answered)
					{
						var panel = pointToEdit.panel.GetComponent<TabularDataPanel>();
						pointToEdit.title = editor.answerTitle;
						//NOTE(Jitse): \f is used as a split character to divide the string into an array
						pointToEdit.body = $"{editor.answerRows}\f{editor.answerColumns}\f";
						pointToEdit.body += string.Join("\f", editor.answerTabularData);
						pointToEdit.panel = panel.gameObject;

						panel.Init(editor.answerTitle, editor.answerRows, editor.answerColumns, editor.answerTabularData);
						finished = true;
					}
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (allowCancel && Input.GetKeyDown(KeyCode.Escape))
			{
				pointToEdit.filled = true;
				pointToEdit = null;
				Destroy(interactionEditor);
				SetEditorActive(true);
			}

			if (finished)
			{
				pointToEdit.tagId = interactionEditor.GetComponentInChildren<TagPicker>().currentTagId;
				pointToEdit.mandatory = interactionEditor.GetComponentInChildren<MandatoryPanel>().isMandatory;
				pointToEdit.timelineRow.mandatory.isOn = pointToEdit.mandatory;
				Destroy(interactionEditor);
				editorState = EditorState.Active;
				pointToEdit.filled = true;

				pointToEdit.panel.SetActive(false);

				SetInteractionPointTag(pointToEdit);
				UnsavedChangesTracker.Instance.unsavedChanges = true;
			}
		}

		if (editorState == EditorState.Saving)
		{
			if (savePanel.answered)
			{
				//NOTE(Simon): If file already exists, we need to get the associated Guid in order to save to the correct file.
				//NOTE(cont.): Could be possible that user overwrites an existing file *different* from the existing file already open
				var newGuid = new Guid(savePanel.answerGuid);

				// NOTE(Lander): When the guid changes, overwrite extra and main.mp4
				if (newGuid != meta.guid && meta.guid != Guid.Empty)
				{
					var oldDir = Path.Combine(Application.persistentDataPath, meta.guid.ToString());
					var newDir = Path.Combine(Application.persistentDataPath, newGuid.ToString());

					File.Copy(Path.Combine(oldDir, SaveFile.videoFilename), Path.Combine(newDir, SaveFile.videoFilename), true);
					Directory.CreateDirectory(Path.Combine(newDir, SaveFile.extraPath));

					foreach (var file in Directory.GetFiles(Path.Combine(newDir, SaveFile.extraPath)))
					{
						File.Delete(file);
					}

					foreach (var file in Directory.GetFiles(Path.Combine(oldDir, SaveFile.extraPath)))
					{
						var newFilename = Path.Combine(Path.Combine(newDir, SaveFile.extraPath), Path.GetFileName(file));
						File.Copy(file, newFilename, true);
					}
				}

				meta.guid = newGuid;
				meta.title = savePanel.answerTitle;

				if (!SaveToFile())
				{
					Debug.LogError("Something went wrong while saving the file");
					return;
				}

				Toasts.AddToast(5, "File saved!");

				SetEditorActive(true);
				Destroy(savePanel.gameObject);
				Canvass.modalBackground.SetActive(false);
			}

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				SetEditorActive(true);
				Destroy(savePanel.gameObject);
				Canvass.modalBackground.SetActive(false);
			}
		}

		if (editorState == EditorState.Opening)
		{
			if (projectPanel.answered)
			{
				var guid = projectPanel.answerGuid;
				var projectFolder = Path.Combine(Application.persistentDataPath, guid);

				if (OpenFile(projectFolder))
				{
					SetEditorActive(true);
					Destroy(projectPanel.gameObject);
					Canvass.modalBackground.SetActive(false);

					pinnedHoverPoint = null;

					InitExtrasList();
					//NOTE(Simon): When opening a project, any previous to-be-deleted-or-copied files are not relevant anymore. So clear them
					CleanExtras();
					UnsavedChangesTracker.Instance.unsavedChanges = false;
				}
				else
				{
					//TODO(Simon): Figure out a way to differentiate between a real error, and when the video is not copied yet.
					Debug.LogError("Something went wrong while loading the file");
				}
			}

			//NOTE(Simon): Don't allow dismissal if no project is opened yet
			if (Input.GetKeyDown(KeyCode.Escape) && meta.title != null)
			{
				Destroy(projectPanel.gameObject);
				Canvass.modalBackground.SetActive(false);
			}
		}

		if (editorState == EditorState.PickingVideo)
		{
			if (explorerPanel.answered)
			{
				var videoPath = Path.Combine(Application.persistentDataPath, Path.Combine(meta.guid.ToString(), SaveFile.videoFilename));
				var projectFolder = Path.Combine(Application.persistentDataPath, meta.guid.ToString());

				File.Copy(explorerPanel.answerPath, videoPath);

				if (OpenFile(projectFolder))
				{
					Destroy(explorerPanel.gameObject);
					SetEditorActive(true);
					Canvass.modalBackground.SetActive(false);
					UnsavedChangesTracker.Instance.unsavedChanges = true;
				}
				else
				{
					Debug.LogError("Something went wrong while loading the file");
				}
			}
		}

		if (editorState == EditorState.LoggingIn)
		{
			if (loginPanel == null)
			{
				InitLoginPanel();
			}

			if (loginPanel.answered)
			{
				userToken = loginPanel.answerToken;
				Destroy(loginPanel.gameObject);
				Canvass.modalBackground.SetActive(false);
				editorState = EditorState.Active;
			}

			if (Input.GetKeyUp(KeyCode.Escape))
			{
				if (loginPanel != null)
				{
					Canvass.modalBackground.SetActive(false);
					Destroy(loginPanel.gameObject);
				}

				editorState = EditorState.Active;
			}
		}

		if (editorState == EditorState.SavingThenUploading)
		{
			if (loginPanel != null && loginPanel.answered)
			{
				userToken = loginPanel.answerToken;
				Destroy(loginPanel.gameObject);
				InitSaveProjectPanel();
			}
			if (savePanel != null && savePanel.answered)
			{
				savePanel.Init(true, meta.title);
				meta.title = savePanel.answerTitle;

				if (!SaveToFile())
				{
					Debug.LogError("Something went wrong while saving the file");
					return;
				}

				Toasts.AddToast(5, "File saved!");

				Destroy(savePanel.gameObject);
				InitUploadPanel();
			}
			if (uploadPanel != null)
			{
				UpdateUploadPanel();
			}

			if (Input.GetKeyUp(KeyCode.Escape))
			{
				//NOTE(Simon): If already uploading, don't allow cancel
				if (uploadPanel == null)
				{
					if (loginPanel != null)
					{
						Destroy(loginPanel.gameObject);
					}

					if (savePanel != null)
					{
						Destroy(savePanel.gameObject);
					}

					Canvass.modalBackground.SetActive(false);
					editorState = EditorState.Active;
				}
			}
		}

		if (editorState == EditorState.Exporting)
		{
			//NOTE(Simon): Some panels use Esc internally to cancel a child action. We don't want to also cancel the panel in that case.
			bool allowCancel = true;

			if (exportPanel != null)
			{
				allowCancel = exportPanel.allowCancel;
			}

			if (allowCancel && Input.GetKeyDown(KeyCode.Escape))
			{
				Destroy(exportPanel.gameObject);
				Canvass.modalBackground.SetActive(false);
				editorState = EditorState.Active;
			}
		}

#if UNITY_EDITOR
		if (Input.GetKey(KeyCode.Z) && Input.GetKeyDown(KeyCode.O) && AreFileOpsAllowed())
#else
		if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.O) && AreFileOpsAllowed())
#endif
		{
			InitOpenProjectPanel();
		}

#if UNITY_EDITOR
		if (Input.GetKey(KeyCode.Z) && Input.GetKeyDown(KeyCode.S) && AreFileOpsAllowed())
#else
		if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.S) && AreFileOpsAllowed())
#endif
		{
			InitSaveProjectPanel();
		}

#if UNITY_EDITOR
		if (Input.GetKey(KeyCode.Z) && Input.GetKeyDown(KeyCode.U) && AreFileOpsAllowed())
#else
		if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.U) && AreFileOpsAllowed())
#endif
		{
			InitUpload();
		}

#if UNITY_EDITOR
		if (Input.GetKey(KeyCode.Z) && Input.GetKeyDown(KeyCode.L) && AreFileOpsAllowed())
#else
		if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.L) && AreFileOpsAllowed())
#endif
		{
			editorState = EditorState.LoggingIn;
		}
	}

	private bool AreFileOpsAllowed()
	{
		return editorState != EditorState.Saving
			&& editorState != EditorState.Opening
			&& editorState != EditorState.PickingPerspective;
	}

	private void SetEditorActive(bool active)
	{
		ResetInteractionPointTemp();

		if (active)
		{
			editorState = EditorState.Active;
			timelineContainer.gameObject.SetActive(true);
		}
		else
		{
			editorState = EditorState.Inactive;
			timelineContainer.gameObject.SetActive(false);
		}
	}

	private void AddItemToTimeline(InteractionPointEditor point, bool hidden)
	{
		var newRow = Instantiate(timelineRowPrefab);
		point.timelineRow = newRow.GetComponent<TimelineRow>();
		point.timelineRow.transform.SetParent(timeline);

		point.point.transform.LookAt(Vector3.zero, Vector3.up);
		point.point.transform.RotateAround(point.point.transform.position, point.point.transform.up, 180);

		//Note(Simon): By default, make interactionPoints invisible on load
		interactionPoints.Add(point);
		if (point.panel != null && hidden)
		{
			point.panel.SetActive(false);
		}

		point.timelineRow.mandatory.isOn = point.mandatory;
		point.timelineRow.mandatory.onValueChanged.AddListener(x => OnMandatoryChanged(point, x));

		float fudgeFactor = 10;
		float offset = point.timelineRow.tagShape.rectTransform.sizeDelta.x + fudgeFactor;
		point.timelineRow.title.rectTransform.sizeDelta = new Vector2(timelineFirstColumnWidth.sizeDelta.x - offset, point.timelineRow.title.rectTransform.sizeDelta.y);
	}

	private void RemoveItemFromTimeline(InteractionPointEditor point)
	{
		Destroy(point.timelineRow.gameObject);
		interactionPoints.Remove(point);
		Destroy(point.point);
		if (point.panel != null)
		{
			Destroy(point.panel);
		}
		UnsavedChangesTracker.Instance.unsavedChanges = true;
	}

	private void UpdateTimeline()
	{
		timelineStartTime = 0;
		timelineEndTime = (float)videoController.videoLength;
		//NOTE(Simon): This happens when a video isn't fully loaded yet. So don't update timeline until it is.
		if (float.IsNaN(timelineEndTime))
		{
			return;
		}

		//Note(Simon): Init if not set yet.
		if (timelineWindowEndTime == 0)
		{
			timelineWindowEndTime = timelineEndTime;
		}

		//Note(Simon): Zoom timeline
		if (!(isDraggingTimelineItem || isResizingTimelineItem))
		{
			if (RectTransformUtility.RectangleContainsScreenPoint(timelineContainer, Input.mousePosition))
			{
				//NOTE(Simon): Zoom only when Ctrl is pressed. Else scroll list.
				if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
				{
					DisableTimelineScroll();
					if (Input.mouseScrollDelta.y > 0)
					{
						timelineZoomTarget = Mathf.Clamp01(timelineZoomTarget * 0.9f);
					}
					else if (Input.mouseScrollDelta.y < 0)
					{
						timelineZoomTarget = Mathf.Clamp01(timelineZoomTarget * 1.1f);
					}
				}
				else
				{
					EnableTimelineScroll();
				}
			}

			if (Mathf.Abs(timelineZoom - timelineZoomTarget) > 0.0025)
			{
				timelineZoom = Mathf.Lerp(timelineZoom, timelineZoomTarget, 0.15f);
				timelineLabelsDirty = true;
			}
			else
			{
				timelineZoom = timelineZoomTarget;
			}
		}

		//Note(Simon): Reset offset when fully zoomed out.
		if (timelineZoom >= 1)
		{
			timelineOffsetTime = 0;
			timelineLabelsDirty = true;
		}

		float zoomedLength;
		//Note(Simon): Correct the timeline offset after zooming
		{
			zoomedLength = (timelineEndTime - timelineStartTime) * timelineZoom;

			var windowMiddle = (timelineEndTime - timelineOffsetTime) / 2;
			timelineWindowStartTime = windowMiddle - zoomedLength / 2;
			timelineWindowEndTime = windowMiddle + zoomedLength / 2;

			timelineOffsetPixels = timelineFirstColumnWidth.rect.width;
			timelineWidthPixels = timelineContainer.rect.width - timelineOffsetPixels;
		}

		//NOTE(Simon): Timeline labels
		{
			if (Single.IsNaN(zoomedLength))
			{
				return;
			}
			var maxNumLabels = Mathf.Floor(timelineWidthPixels / 100);
			var lowerNiceTime = LowerNiceTime(zoomedLength / maxNumLabels);
			var upperNiceTime = UpperNiceTime(zoomedLength / maxNumLabels);

			var lowerNumLabels = Mathf.FloorToInt(zoomedLength / lowerNiceTime);
			var upperNumLabels = Mathf.FloorToInt(zoomedLength / upperNiceTime);
			var closestNiceTime = (maxNumLabels - lowerNumLabels) > (upperNumLabels - maxNumLabels) ? lowerNiceTime : upperNiceTime;
			var realNumLabels = (maxNumLabels - lowerNumLabels) > (upperNumLabels - maxNumLabels) ? lowerNumLabels : upperNumLabels;
			realNumLabels += 2;

			if (timelineLabelsDirty)
			{
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
			}

			var numTicksOffScreen = Mathf.FloorToInt(timelineWindowStartTime / closestNiceTime);

			for (int i = 0; i < realNumLabels; i++)
			{
				var tickTime = (i + numTicksOffScreen) * closestNiceTime;
				if (tickTime >= 0 && tickTime <= timelineEndTime)
				{
					if (timelineLabelsDirty)
					{
						headerLabels[i].enabled = true;
						headerLabels[i].text = MathHelper.FormatSeconds(tickTime);
						headerLabels[i].rectTransform.position = new Vector2(TimeToPx(tickTime), headerLabels[i].rectTransform.position.y);
					}
					DrawLineAtTime(tickTime, 1, new Color(0, 0, 0, 47f / 255));
				}
				else
				{
					headerLabels[i].enabled = false;
				}
			}
			timelineLabelsDirty = false;
		}

		//Note(Simon): Render timeline items
		foreach (var point in interactionPoints)
		{
			var row = point.timelineRow;
			var indicatorRect = row.indicator.rectTransform;
			row.title.text = point.title;

			var zoomedStartTime = point.startTime;
			var zoomedEndTime = point.endTime;

			if (point.endTime < timelineWindowStartTime || point.startTime > timelineWindowEndTime)
			{
				zoomedStartTime = 0;
				zoomedEndTime = 0;
			}
			else
			{
				if (point.startTime < timelineWindowStartTime) { zoomedStartTime = timelineWindowStartTime; }
				if (point.endTime > timelineWindowEndTime) { zoomedEndTime = timelineWindowEndTime; }
			}

			indicatorRect.position = new Vector2(TimeToPx(zoomedStartTime), indicatorRect.position.y);
			indicatorRect.sizeDelta = new Vector2(TimeToPx(zoomedEndTime) - TimeToPx(zoomedStartTime), indicatorRect.sizeDelta.y);

		}

		//Note(Simon): Colors
		foreach (var point in interactionPoints)
		{
			var tagColor = TagManager.Instance.GetTagColorById(point.tagId);
			point.timelineRow.indicator.color = tagColor;
		}

		//NOTE(Simon): Highlight interactionPoint on hover
		//TODO(Simon): Show preview when hovering over timelineRow
		if (RectTransformUtility.RectangleContainsScreenPoint(timelineContainer, Input.mousePosition))
		{
			foreach (var point in interactionPoints)
			{
				var rectBackground = point.timelineRow.GetComponent<RectTransform>();
				var rect = point.timelineRow.title.GetComponent<RectTransform>();
				if (RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition)
					&& !isDraggingTimelineItem && !isResizingTimelineItem
					&& editorState == EditorState.Active
					&& point.panel != null)
				{
					HighlightPoint(point);

					var worldCorners = new Vector3[4];
					rectBackground.GetWorldCorners(worldCorners);
					var start = new Vector2(worldCorners[0].x, (worldCorners[0].y + worldCorners[1].y) / 2);
					var end = new Vector2(worldCorners[2].x - 3, (worldCorners[2].y + worldCorners[3].y) / 2);
					var thickness = worldCorners[1].y - worldCorners[0].y;
					//NOTE(Simon): Show a darker brackground on hover
					UILineRenderer.DrawLine(start, end, thickness, new Color(0, 0, 0, 60 / 255f));

					//NOTE(Simon): If none are pinned, show currently hovered point
					if (pinnedHoverPoint == null)
					{
						point.panel.SetActive(true);
					}

					point.panel.GetComponent<RectTransform>().anchoredPosition = new Vector2(10, timelineContainer.sizeDelta.y + 50);

					if (Input.GetMouseButtonDown(0) && EventSystem.current.currentSelectedGameObject == null)
					{
						if (pinnedHoverPoint == point)
						{
							UnpinPoint();
						}
						else
						{
							UnpinPoint();

							PinPoint(point);
						}
					}
				}
				else
				{
					if (pinnedHoverPoint == null && point.panel != null)
					{
						point.panel.SetActive(false);
					}
				}
			}
		}

		//NOTE(Simon): Timeline tag sprites
		foreach (var point in interactionPoints)
		{
			var sprite = point.timelineRow.tagShape;
			sprite.sprite = TagManager.Instance.GetTagShapeById(point.tagId);
			sprite.color = TagManager.Instance.GetTagColorById(point.tagId);
		}

		//Note(Simon): timeline buttons. Looping backwards because we're deleting items from the list.
		for (var i = interactionPoints.Count - 1; i >= 0; i--)
		{
			var point = interactionPoints[i];
			var edit = point.timelineRow.edit;
			var delete = point.timelineRow.delete;
			var move = point.timelineRow.move;
			var mandatory = point.timelineRow.mandatory;

			var editRect = edit.GetComponent<RectTransform>();
			var deleteRect = delete.GetComponent<RectTransform>();
			var moveRect = move.GetComponent<RectTransform>();
			var mandatoryRect = mandatory.GetComponent<RectTransform>();

			deleteRect.position = new Vector3(timelineOffsetPixels - 20, deleteRect.position.y);
			editRect.position = new Vector3(timelineOffsetPixels - 40, editRect.position.y);
			moveRect.position = new Vector3(timelineOffsetPixels - 60, moveRect.position.y);
			mandatoryRect.position = new Vector3(timelineOffsetPixels - 80, mandatoryRect.position.y);

			if (!point.filled)
			{
				edit.gameObject.SetActive(false);
				move.gameObject.SetActive(false);
				delete.gameObject.SetActive(false);
				mandatory.gameObject.SetActive(false);
			}
			if (point.filled)
			{
				edit.gameObject.SetActive(true);
				move.gameObject.SetActive(true);
				delete.gameObject.SetActive(true);
				mandatory.gameObject.SetActive(true);
			}

			if (delete.state == SelectState.Pressed)
			{
				//NOTE(Simon): Get filenames for extra files to delete, and add to list.
				var file = point.filename;
				if (!String.IsNullOrEmpty(file))
				{
					SetExtrasToDeleted(file);
				}

				//NOTE(Jitse): If the point was pinned, pinnedHoverPoint must also be updated to null.
				if (pinnedHoverPoint == point)
				{
					pinnedHoverPoint = null;
				}

				//NOTE(Simon): Actually remove the point, and all associated data
				RemoveItemFromTimeline(point);
				Destroy(point.point);
				Destroy(point.panel);
				break;
			}

			if (edit.state == SelectState.Pressed && editorState != EditorState.EditingInteractionPoint)
			{
				UnpinPoint();

				editorState = EditorState.EditingInteractionPoint;
				//NOTE(Simon): Set pointToEdit in global state for usage by the main state machine
				pointToEdit = point;
				pointToEdit.filled = false;

				switch (point.type)
				{
					case InteractionType.Text:
					{
						interactionEditor = Instantiate(UIPanels.Instance.textPanelEditor, Canvass.main.transform).gameObject;
						interactionEditor.GetComponent<TextPanelEditor>().Init(point.title, point.body);
						break;
					}
					case InteractionType.Image:
					{
						interactionEditor = Instantiate(UIPanels.Instance.imagePanelEditor, Canvass.main.transform).gameObject;
						var filenames = point.filename.Split('\f');
						var fullPaths = new List<string>(filenames.Length);
						foreach (var file in filenames)
						{
							fullPaths.Add(Path.Combine(Application.persistentDataPath, meta.guid.ToString(), file));
						}
						interactionEditor.GetComponent<ImagePanelEditor>().Init(point.title, fullPaths);
						break;
					}
					case InteractionType.Video:
					{
						interactionEditor = Instantiate(UIPanels.Instance.videoPanelEditor, Canvass.main.transform).gameObject;
						interactionEditor.GetComponent<VideoPanelEditor>().Init(point.title, Path.Combine(Application.persistentDataPath, meta.guid.ToString(), point.filename));
						break;
					}
					case InteractionType.MultipleChoice:
					{
						interactionEditor = Instantiate(UIPanels.Instance.multipleChoicePanelEditor, Canvass.main.transform).gameObject;
						interactionEditor.GetComponent<MultipleChoicePanelEditor>().Init(point.title, point.body.Split('\f'));
						break;
					}
					case InteractionType.Audio:
					{
						interactionEditor = Instantiate(UIPanels.Instance.audioPanelEditor, Canvass.main.transform).gameObject;
						interactionEditor.GetComponent<AudioPanelEditor>().Init(point.title, Path.Combine(Application.persistentDataPath, meta.guid.ToString(), point.filename));
						break;
					}
					case InteractionType.FindArea:
					{
						var areas = Area.ParseFromSave(point.filename, point.body);
						interactionEditor = Instantiate(UIPanels.Instance.findAreaPanelEditor, Canvass.main.transform).gameObject;
						interactionEditor.GetComponent<FindAreaPanelEditor>().Init(point.title, meta.guid, areas);
						break;
					}
					case InteractionType.MultipleChoiceArea:
					{
						//NOTE(Simon): Split with count only accepts an array of separators
						var split = point.body.Split(new[] { '\f' }, 2);
						var correct = Int32.Parse(split[0]);
						var areaJson = split[1];
						var areas = Area.ParseFromSave(point.filename, areaJson);
						interactionEditor = Instantiate(UIPanels.Instance.multipleChoiceAreaPanelEditor, Canvass.main.transform).gameObject;
						interactionEditor.GetComponent<MultipleChoiceAreaPanelEditor>().Init(point.title, meta.guid, areas, correct);
						break;
					}
					case InteractionType.MultipleChoiceImage:
					{
						interactionEditor = Instantiate(UIPanels.Instance.multipleChoiceImagePanelEditor, Canvass.main.transform).gameObject;
						var filenames = point.filename.Split('\f');
						var fullPaths = new List<string>(filenames.Length);
						foreach (var file in filenames)
						{
							fullPaths.Add(Path.Combine(Application.persistentDataPath, meta.guid.ToString(), file));
						}
						var correct = Int32.Parse(point.body);
						interactionEditor.GetComponent<MultipleChoiceImagePanelEditor>().Init(point.title, fullPaths, correct);
						break;
					}
					case InteractionType.TabularData:
					{
						interactionEditor = Instantiate(UIPanels.Instance.tabularDataPanelEditor, Canvass.main.transform).gameObject;
						string[] body = point.body.Split(new[] { '\f' }, 3);
						int rows = Int32.Parse(body[0]);
						int columns = Int32.Parse(body[1]);

						interactionEditor.GetComponent<TabularDataPanelEditor>().Init(point.title, rows, columns, new List<string>(body[2].Split('\f')));
						break;
					}
					default:
						throw new ArgumentOutOfRangeException();
				}

				interactionEditor.GetComponentInChildren<TagPicker>().Init(point.tagId);
				interactionEditor.GetComponentInChildren<MandatoryPanel>().Init(point.mandatory);

				break;
			}

			//NOTE(Simon): Move interactionPoint
			if (move.switchedOn)
			{
				UnpinPoint();

				editorState = EditorState.MovingInteractionPoint;
				pointToMove = point;
				pointToMove.filled = false;
				break;
			}
			if (move.switchedOff)
			{
				editorState = EditorState.Active;
				pointToMove.filled = true;
				pointToMove = null;
				break;
			}
		}

		//Note(Simon): Render various stuff, such as current time, indicator lines for begin and end of video, and separator lines.
		{
			//NOTE(Simon): current time indicator
			DrawLineAtTime(Seekbar.instance.lastSmoothTime * videoController.videoLength, 3, new Color(0, 0, 0, 150f / 255), 5);

			//NOTE(Simon): Top line. Only draw when inside timeline.
			var offset = new Vector3(0, 0);
			if (timeline.localPosition.y < timelineHeader.rect.height - offset.y)
			{
				var headerCoords = new Vector3[4];
				timelineHeader.GetWorldCorners(headerCoords);
				UILineRenderer.DrawLine(headerCoords[0] + offset, headerCoords[3] + offset, 1, new Color(0, 0, 0, 47f / 255));
			}
			//NOTE(Simon): Start and end
			if (timelineZoom < 1)
			{
				DrawLineAtTime(0, 2, new Color(0, 0, 0, 47f / 255));
				DrawLineAtTime(timelineEndTime, 2, new Color(0, 0, 0, 47f / 255));
			}
		}

		Texture2D desiredCursor = null;

		//Note(Simon): resizing and moving of timeline items
		{
			foreach (var point in interactionPoints)
			{
				var indicatorRect = point.timelineRow.indicator.rectTransform;

				RectTransformUtility.ScreenPointToLocalPointInRectangle(indicatorRect, Input.mousePosition, null, out var rectPixel);
				var leftAreaX = 5;
				var rightAreaX = indicatorRect.rect.width - 5;

				//NOTE(Simon): Show correct cursor
				if (isDraggingTimelineItem || isResizingTimelineItem || RectTransformUtility.RectangleContainsScreenPoint(indicatorRect, Input.mousePosition)
					&& RectTransformUtility.RectangleContainsScreenPoint(timelineContainer, Input.mousePosition))
				{
					if (isDraggingTimelineItem)
					{
						desiredCursor = Cursors.Instance.CursorDrag;
					}
					else if (isResizingTimelineItem)
					{
						desiredCursor = Cursors.Instance.CursorResizeHorizontal;
					}
					else if (rectPixel.x < leftAreaX || rectPixel.x > rightAreaX)
					{
						desiredCursor = Cursors.Instance.CursorResizeHorizontal;
					}
					else
					{
						desiredCursor = Cursors.Instance.CursorDrag;
					}
				}

				//NOTE(Simon) Check if conditions are met to start a resize or drag operation on a timeline item
				if (!isDraggingTimelineItem && !isResizingTimelineItem
					&& Input.GetMouseButtonDown(0) && RectTransformUtility.RectangleContainsScreenPoint(indicatorRect, Input.mousePosition)
					&& RectTransformUtility.RectangleContainsScreenPoint(timelineContainer, Input.mousePosition))
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

					DisableTimelineScroll();
					break;
				}
			}

			if (isDraggingTimelineItem)
			{
				//NOTE(Simon): End drag operation
				if (!Input.GetMouseButton(0))
				{
					isDraggingTimelineItem = false;
					timelineItemBeingDragged = null;
					timeTooltip.ResetPosition();
					UnsavedChangesTracker.Instance.unsavedChanges = true;
					EnableTimelineScroll();
				}
				else
				{
					var newStart = Mathf.Max(0.0f, (float)timelineItemBeingDragged.startTime + PxToRelativeTime(mouseDelta.x));
					var newEnd = Mathf.Min(timelineEndTime, (float)timelineItemBeingDragged.endTime + PxToRelativeTime(mouseDelta.x));
					if (newStart > 0.0f || newEnd < timelineEndTime)
					{
						timelineItemBeingDragged.startTime = newStart;
						timelineItemBeingDragged.endTime = newEnd;

						var imageRect = timelineItemBeingDragged.timelineRow.indicator.rectTransform;
						var tooltipPos = new Vector2(imageRect.position.x + imageRect.rect.width / 2,
													imageRect.position.y + imageRect.rect.height / 2);

						timeTooltip.SetTime(newStart, newEnd, tooltipPos);
					}
					HighlightPoint(timelineItemBeingDragged);
				}
			}
			else if (isResizingTimelineItem)
			{
				//NOTE(Simon): End resize operation
				if (!Input.GetMouseButton(0))
				{
					isResizingTimelineItem = false;
					timelineItemBeingResized = null;
					timeTooltip.ResetPosition();
					UnsavedChangesTracker.Instance.unsavedChanges = true;
					EnableTimelineScroll();
				}
				else
				{
					if (isResizingStart)
					{
						var newStart = Mathf.Max(0.0f, (float)timelineItemBeingResized.startTime + PxToRelativeTime(mouseDelta.x));
						if (newStart < timelineItemBeingResized.endTime - 1f)
						{
							timelineItemBeingResized.startTime = newStart;
							var imageRect = timelineItemBeingResized.timelineRow.indicator.rectTransform;
							var tooltipPos = new Vector2(imageRect.position.x,
													imageRect.position.y + imageRect.rect.height / 2);

							timeTooltip.SetTime(newStart, tooltipPos);
						}
					}
					else
					{
						var newEnd = Mathf.Min(timelineEndTime, (float)timelineItemBeingResized.endTime + PxToRelativeTime(mouseDelta.x));
						if (newEnd > timelineItemBeingResized.startTime + 1f)
						{
							timelineItemBeingResized.endTime = newEnd;
							var imageRect = timelineItemBeingResized.timelineRow.indicator.rectTransform;
							var tooltipPos = new Vector2(imageRect.position.x + imageRect.rect.width,
													imageRect.position.y + imageRect.rect.height / 2);

							timeTooltip.SetTime(newEnd, tooltipPos);
						}
					}
					HighlightPoint(timelineItemBeingResized);
				}
			}
		}

		//Note(Simon): Resizing of timeline
		{
			if (!isDraggingTimelineItem && !isResizingTimelineItem)
			{
				if (isResizingTimelineVertical)
				{
					if (Input.GetMouseButtonUp(0))
					{
						isResizingTimelineVertical = false;
						desiredCursor = null;
					}

					var resizeDelta = new Vector2(0, mouseDelta.y);
					timelineContainer.sizeDelta += resizeDelta;
				}
				else if (isResizingTimelineHorizontal)
				{
					if (Input.GetMouseButtonUp(0))
					{
						isResizingTimelineHorizontal = false;
						desiredCursor = null;
					}

					//NOTE(Simon): Clamp timeline size to 100px of either side of screen, so it can't go offscreen
					timelineFirstColumnWidth.sizeDelta = new Vector2(Mathf.Clamp(timelineFirstColumnWidth.sizeDelta.x + mouseDelta.x, 100, Screen.width - 100), timelineFirstColumnWidth.sizeDelta.y);
					for (int i = 0; i < interactionPoints.Count; i++)
					{
						var point = interactionPoints[i];
						float fudgeFactor = 10;
						float offset = point.timelineRow.tagShape.rectTransform.sizeDelta.x + fudgeFactor;
						point.timelineRow.title.rectTransform.sizeDelta = new Vector2(timelineFirstColumnWidth.sizeDelta.x - offset, point.timelineRow.title.rectTransform.sizeDelta.y);
					}

					DrawLineAtTime(0, 2, Color.black, -3);
					timelineLabelsDirty = true;
				}
				else
				{
					var verticalRect = new Rect(new Vector2(0, timelineContainer.rect.height - 4),
						new Vector2(timelineContainer.rect.width, 4));
					var horizontalRect = new Rect(new Vector2(timelineOffsetPixels - 2, 0),
						new Vector2(4, timelineContainer.rect.height - timelineHeader.sizeDelta.y));

					if (verticalRect.Contains(Input.mousePosition))
					{
						if (!Cursors.isOverridingCursor)
						{
							desiredCursor = Cursors.Instance.CursorResizeVertical;
						}

						if (Input.GetMouseButtonDown(0))
						{
							isResizingTimelineVertical = true;
						}
					}
					else if (horizontalRect.Contains(Input.mousePosition))
					{
						DrawLineAtTime(0, 2, Color.black, -3);
						if (!Cursors.isOverridingCursor)
						{
							desiredCursor = Cursors.Instance.CursorResizeHorizontal;
						}

						if (Input.GetMouseButtonDown(0))
						{
							isResizingTimelineHorizontal = true;
						}
					}
				}
			}
		}

		if (!Cursors.isOverridingCursor)
		{
			Cursor.SetCursor(desiredCursor, desiredCursor == null ? Vector2.zero : new Vector2(15, 15), CursorMode.ForceSoftware);
		}
	}

	private void UnpinPoint()
	{
		if (pinnedHoverPoint != null)
		{
			pinnedHoverPoint.panel.SetActive(false);
			pinnedHoverPoint.timelineRow.title.fontStyle = FontStyle.Normal;
			pinnedHoverPoint = null;
		}
	}

	private void PinPoint(InteractionPointEditor point)
	{
		pinnedHoverPoint = point;
		pinnedHoverPoint.panel.SetActive(true);
		pinnedHoverPoint.timelineRow.title.fontStyle = FontStyle.Bold;
	}

	public void HighlightPoint(InteractionPointEditor point)
	{
		point.point.GetComponent<SpriteRenderer>().color = Color.red;
	}

	public void DrawLineAtTime(double time, float thickness, Color color, float topOffset = 0f)
	{
		var timePx = TimeToPx(time);

		float containerHeight = timelineContainer.sizeDelta.y;
		float headerHeight = Mathf.Max(0, timelineHeader.sizeDelta.y - timeline.localPosition.y);

		UILineRenderer.DrawLine(
			new Vector2(timePx, 0),
			new Vector2(timePx, containerHeight - headerHeight + 3 + topOffset),
			thickness,
			color);
	}

	public void OnMandatoryChanged(InteractionPointEditor point, bool mandatory)
	{
		point.mandatory = mandatory;
		UnsavedChangesTracker.Instance.unsavedChanges = true;
	}

	public void OnDrag(BaseEventData e)
	{
		if (Input.GetMouseButton(1))
		{
			var pointerEvent = (PointerEventData)e;
			timelineOffsetTime += PxToRelativeTime(pointerEvent.delta.x) * 2;
			timelineLabelsDirty = true;
		}
	}

	public void DisableTimelineScroll()
	{
		timelineContainer.GetComponentInChildren<ScrollRect>().vertical = false;
	}

	public void EnableTimelineScroll()
	{
		timelineContainer.GetComponentInChildren<ScrollRect>().vertical = true;
	}

	public void InitUpload()
	{
		if (String.IsNullOrEmpty(userToken))
		{
			InitLoginPanel();
			editorState = EditorState.SavingThenUploading;
		}
		else
		{
			InitSaveProjectPanel();
			editorState = EditorState.SavingThenUploading;
		}
	}

	private void InitUploadPanel()
	{
		uploadStatus = new UploadStatus();
		uploadStatus.coroutine = StartCoroutine(UploadFile());
		uploadPanel = Instantiate(UIPanels.Instance.uploadPanel);
		uploadPanel.transform.SetParent(Canvass.main.transform, false);
		videoController.Pause();
		Canvass.modalBackground.SetActive(true);
	}

	public void InitOpenProjectPanel()
	{
		projectPanel = Instantiate(UIPanels.Instance.projectPanel);
		projectPanel.Init(isSaveFileDialog: false);
		projectPanel.transform.SetParent(Canvass.main.transform, false);
		Canvass.modalBackground.SetActive(true);
		editorState = EditorState.Opening;
	}

	public void InitLoginPanel()
	{
		Canvass.modalBackground.SetActive(true);
		loginPanel = Instantiate(UIPanels.Instance.loginPanel);
		loginPanel.transform.SetParent(Canvass.main.transform, false);
	}

	public void InitSaveProjectPanel(bool uploading = false)
	{
		savePanel = Instantiate(UIPanels.Instance.projectPanel);
		savePanel.transform.SetParent(Canvass.main.transform, false);
		savePanel.Init(isSaveFileDialog: true, meta.title);
		Canvass.modalBackground.SetActive(true);
		if (!uploading)
		{
			editorState = EditorState.Saving;
		}
	}

	public void InitExplorerPanel(string searchPattern, string title)
	{
		explorerPanel = Instantiate(UIPanels.Instance.explorerPanel);
		explorerPanel.transform.SetParent(Canvass.main.transform, false);
		explorerPanel.Init("", searchPattern, title);
	}

	public void ShowTagPanel()
	{
		if (tagPanel != null)
		{
			return;
		}

		tagPanel = Instantiate(UIPanels.Instance.tagPanel);
		tagPanel.transform.SetParent(Canvass.main.transform, false);
	}

	public void ShowExportPanel()
	{
		exportPanel = Instantiate(UIPanels.Instance.exportPanel, Canvass.main.transform, false);
		exportPanel.Init(meta.guid);
		Canvass.modalBackground.SetActive(true);
		editorState = EditorState.Exporting;
	}

	public bool SaveToFile(bool makeThumbnail = true)
	{
		string path = SaveFile.GetPathForTitle(meta.title);
		string videoPath = Path.Combine(path, SaveFile.videoFilename);
		if (!File.Exists(videoPath))
		{
			File.Copy(videoController.VideoPath(), videoPath);
		}

		var data = new SaveFileData();
		data.meta = meta;

		if (interactionPoints.Count > 0)
		{
			foreach (var point in interactionPoints)
			{
				if (point.type != InteractionType.None)
				{
					data.points.Add(new InteractionPointSerialize
					{
						type = point.type,
						title = point.title,
						body = point.body,
						filename = point.filename,
						startTime = point.startTime,
						endTime = point.endTime,
						tagId = point.tagId,
						mandatory = point.mandatory,
						returnRayOrigin = point.returnRayOrigin,
						returnRayDirection = point.returnRayDirection,
					});
				}
			}
		}

		bool success = SaveFile.WriteFile(data);

		if (makeThumbnail)
		{
			string thumbname = Path.Combine(path, SaveFile.thumbFilename);
			videoController.Screenshot(thumbname, 10, 1000, 1000);
		}

		SaveFile.WriteTags(path, TagManager.Instance.tags);

		CleanExtras();
		UnsavedChangesTracker.Instance.unsavedChanges = false;

		return success;
	}

	private bool OpenFile(string projectFolder)
	{
		var data = SaveFile.OpenFile(projectFolder);
		meta = data.meta;
		var videoPath = Path.Combine(Application.persistentDataPath, meta.guid.ToString(), SaveFile.videoFilename);

		Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, meta.guid.ToString(), SaveFile.miniaturesPath));

		if (!File.Exists(videoPath))
		{
			InitExplorerPanel("*.mp4", "Choose a video to enrich");
			projectPanel.gameObject.SetActive(false);
			editorState = EditorState.PickingVideo;
			return false;
		}

		fileLoader.LoadFile(videoPath);

		for (var j = interactionPoints.Count - 1; j >= 0; j--)
		{
			RemoveItemFromTimeline(interactionPoints[j]);
		}

		interactionPoints.Clear();
		var tagsPath = Path.Combine(Application.persistentDataPath, meta.guid.ToString());
		var tags = SaveFile.ReadTags(tagsPath);
		TagManager.Instance.SetTags(tags);

		foreach (var point in data.points)
		{
			var newPoint = Instantiate(interactionPointPrefab);

			var newInteractionPoint = new InteractionPointEditor
			{
				startTime = point.startTime,
				endTime = point.endTime,
				title = point.title,
				body = point.body,
				filename = point.filename,
				type = point.type,
				filled = true,
				point = newPoint,
				tagId = point.tagId,
				mandatory = point.mandatory,
				returnRayOrigin = point.returnRayOrigin,
				returnRayDirection = point.returnRayDirection
			};

			bool isValidPoint = true;

			switch (newInteractionPoint.type)
			{
				case InteractionType.None:
				{
					isValidPoint = false;
					Debug.Log("InteractionPoint with Type None encountered");
					break;
				}
				case InteractionType.Text:
				{
					var panel = Instantiate(UIPanels.Instance.textPanel, Canvass.main.transform);
					panel.Init(newInteractionPoint.title, newInteractionPoint.body);
					newInteractionPoint.panel = panel.gameObject;
					break;
				}
				case InteractionType.Image:
				{
					var panel = Instantiate(UIPanels.Instance.imagePanel, Canvass.main.transform);
					var filenames = newInteractionPoint.filename.Split('\f');
					var urls = new List<string>();
					foreach (var file in filenames)
					{
						string url = Path.Combine(Application.persistentDataPath, meta.guid.ToString(), file);
						if (!File.Exists(url))
						{
							Debug.LogWarningFormat($"File missing: {url}");
						}
						urls.Add(url);
					}

					panel.Init(newInteractionPoint.title, urls);
					newInteractionPoint.panel = panel.gameObject;
					break;
				}
				case InteractionType.Video:
				{
					var panel = Instantiate(UIPanels.Instance.videoPanel, Canvass.main.transform);
					string url = Path.Combine(Application.persistentDataPath, meta.guid.ToString(), newInteractionPoint.filename);

					panel.Init(newInteractionPoint.title, url);
					newInteractionPoint.panel = panel.gameObject;
					break;
				}
				case InteractionType.MultipleChoice:
				{
					var panel = Instantiate(UIPanels.Instance.multipleChoicePanel, Canvass.main.transform);
					//TODO(Simon): SPlit here, not in panel
					panel.Init(newInteractionPoint.title, newInteractionPoint.body.Split('\f'));
					newInteractionPoint.panel = panel.gameObject;
					break;
				}
				case InteractionType.Audio:
				{
					var panel = Instantiate(UIPanels.Instance.audioPanel, Canvass.main.transform);
					string url = Path.Combine(Application.persistentDataPath, meta.guid.ToString(), newInteractionPoint.filename);
					panel.Init(newInteractionPoint.title, url);
					newInteractionPoint.panel = panel.gameObject;
					break;
				}
				case InteractionType.FindArea:
				{
					var panel = Instantiate(UIPanels.Instance.findAreaPanel, Canvass.main.transform);
					var areas = Area.ParseFromSave(newInteractionPoint.filename, newInteractionPoint.body);

					panel.Init(newInteractionPoint.title, meta.guid, areas);
					newInteractionPoint.panel = panel.gameObject;
					break;
				}
				case InteractionType.MultipleChoiceArea:
				{
					var split = newInteractionPoint.body.Split(new[] { '\f' }, 2);
					var correct = Int32.Parse(split[0]);
					var areaJson = split[1];
					var panel = Instantiate(UIPanels.Instance.multipleChoiceAreaPanel, Canvass.main.transform);
					var areas = Area.ParseFromSave(newInteractionPoint.filename, areaJson);

					panel.Init(newInteractionPoint.title, meta.guid, areas, correct);
					newInteractionPoint.panel = panel.gameObject;
					break;
				}
				case InteractionType.MultipleChoiceImage:
				{

					var panel = Instantiate(UIPanels.Instance.multipleChoiceImagePanel, Canvass.main.transform);
					var filenames = newInteractionPoint.filename.Split('\f');
					var urls = new List<string>();
					foreach (var file in filenames)
					{
						string url = Path.Combine(Application.persistentDataPath, meta.guid.ToString(), file);
						if (!File.Exists(url))
						{
							Debug.LogWarningFormat($"File missing: {url}");
						}
						urls.Add(url);
					}

					var correct = Int32.Parse(point.body);
					panel.Init(newInteractionPoint.title, urls, correct);
					newInteractionPoint.panel = panel.gameObject;
					break;
				}
				case InteractionType.TabularData:
				{
					var panel = Instantiate(UIPanels.Instance.tabularDataPanel, Canvass.main.transform);
					string[] body = newInteractionPoint.body.Split(new[] { '\f' }, 3);
					int rows = Int32.Parse(body[0]);
					int columns = Int32.Parse(body[1]);

					panel.Init(newInteractionPoint.title, rows, columns, new List<string>(body[2].Split('\f')));
					newInteractionPoint.panel = panel.gameObject;
					break;
				}
				default:
				{
					isValidPoint = false;
					break;
				}
			}

			if (isValidPoint)
			{
				newInteractionPoint.panel.SetActive(false);
				AddItemToTimeline(newInteractionPoint, true);
				SetInteractionPointTag(newInteractionPoint);
			}
			else
			{
				Destroy(newPoint);
			}
		}

		StartCoroutine(UpdatePointPositions());

		return true;
	}

	private IEnumerator UpdatePointPositions()
	{
		//NOTE(Simon): wait one frame
		yield return null;

		foreach (var interactionPoint in interactionPoints)
		{
			var ray = new Ray(interactionPoint.returnRayOrigin, interactionPoint.returnRayDirection);

			if (Physics.Raycast(ray, out var hit, 100))
			{
				SetInteractionpointPosition(interactionPoint.point, hit.point);
				interactionPoint.panel.transform.position = hit.point;
			}
		}
	}

	private void SetInteractionpointPosition(GameObject point, Vector3 pos)
	{
		var trans = point.transform;
		trans.position = pos;

		trans.LookAt(Camera.main.transform);
		var angles = trans.localEulerAngles;
		trans.localEulerAngles = new Vector3(-angles.x, angles.y - 180, 0);
	}

	private void SetInteractionPointTag(InteractionPointEditor point)
	{
		var shape = point.point.GetComponent<SpriteRenderer>();
		var text = point.point.GetComponentInChildren<TextMesh>();
		var tag = TagManager.Instance.GetTagById(point.tagId);

		shape.sprite = TagManager.Instance.ShapeForIndex(tag.shapeIndex);
		shape.color = tag.color;
		text.color = tag.color.IdealTextColor();
	}

	private IEnumerator UploadFile()
	{
		var path = Path.Combine(Application.persistentDataPath, meta.guid.ToString());
		var thumbPath = Path.Combine(path, SaveFile.thumbFilename);
		var metaPath = Path.Combine(path, SaveFile.metaFilename);
		var videoPath = Path.Combine(path, SaveFile.videoFilename);

		var str = SaveFile.GetSaveFileContentsBinary(metaPath);
		uploadStatus.totalSize = SaveFile.DirectorySize(new DirectoryInfo(path));

		var form = new WWWForm();
		form.AddField("token", userToken);
		form.AddField("uuid", meta.guid.ToString());
		form.AddField("downloadSize", uploadStatus.totalSize.ToString());
		form.AddBinaryData("meta", str, SaveFile.metaFilename);

		//NOTE(Simon): Busy wait until file is saved
		while (!File.Exists(thumbPath)) { yield return null; }

		var vidSize = (int)FileSize(videoPath);
		var thumbSize = (int)FileSize(thumbPath);


		//TODO(Simon): Guard against big files
		var videoData = new byte[vidSize];
		var thumbData = new byte[thumbSize];

		//TODO(Simon): This reads the full file into memory. BAD
		using (var thumbStream = File.OpenRead(thumbPath))
		using (var videoStream = File.OpenRead(videoPath))
		{
			try
			{
				videoStream.Read(videoData, 0, vidSize);
				thumbStream.Read(thumbData, 0, thumbSize);
			}
			catch (Exception e)
			{
				uploadStatus.failed = true;
				uploadStatus.error = "Something went wrong while loading the file form disk: " + e.Message;
				yield break;
			}
		}

		form.AddBinaryData("video", videoData, SaveFile.videoFilename, "multipart/form-data");
		form.AddBinaryData("thumb", thumbData, SaveFile.thumbFilename, "multipart/form-data");

		uploadStatus.request = UnityWebRequest.Post(Web.videoUrl, form);

		yield return uploadStatus.request.SendWebRequest();
		var status = uploadStatus.request.responseCode;
		if (status != 200)
		{
			uploadStatus.failed = true;
			uploadStatus.error = status == 401 ? "Not logged in " : "Something went wrong while uploading the file: ";
			yield break;
		}

		uploadStatus.uploaded = vidSize + thumbSize;
		uploadStatus.request.Dispose();

		uploadStatus.coroutine = StartCoroutine(UploadExtras());
	}

	private IEnumerator UploadExtras()
	{
		var path = Path.Combine(Application.persistentDataPath, meta.guid.ToString());

		//TODO(Simon): Get allExtras, and upload them
		var extras = new List<string>();

		foreach (var point in interactionPoints)
		{
			if (point.type == InteractionType.Image)
			{
				extras.Add(point.filename);
			}
			if (point.type == InteractionType.Video)
			{
				throw new NotImplementedException();
			}
			if (point.type == InteractionType.Audio)
			{
				throw new NotImplementedException();
			}
		}

		if (extras.Count == 0)
		{
			uploadStatus.done = true;
			yield break;
		}

		var form = new WWWForm();
		form.AddField("token", userToken);
		form.AddField("videoguid", meta.guid.ToString());
		var guids = String.Join(",", extras.Select(x => x.Substring(x.LastIndexOf('\\') + 1)).ToArray());
		form.AddField("extraguids", guids);

		foreach (var extra in extras)
		{
			var filename = extra.Substring(extra.LastIndexOf('\\') + 1);
			var extraPath = Path.Combine(path, extra);
			var extraSize = (int)FileSize(extraPath);
			var extraData = new byte[extraSize];

			using (var extraStream = File.OpenRead(extraPath))
			{
				try
				{
					extraStream.Read(extraData, 0, extraSize);
				}
				catch (Exception e)
				{
					uploadStatus.failed = true;
					uploadStatus.error = "Something went wrong while loading the file form disk: " + e.Message;
					yield break;
				}
			}

			form.AddBinaryData(filename, extraData, filename, "multipart/form-data");
		}

		uploadStatus.request = UnityWebRequest.Post(Web.extrasURL, form);

		yield return uploadStatus.request.SendWebRequest();

		var status = uploadStatus.request.responseCode;
		if (status != 200)
		{
			uploadStatus.failed = true;
			uploadStatus.error = status == 401 ? "Not logged in " : "Something went wrong while uploading the file: " + uploadStatus.request.error;
			Debug.LogError(uploadStatus.error);
			yield break;
		}

		uploadStatus.done = true;
	}

	private void InitExtrasList()
	{
		var projectFolder = Path.Combine(Application.persistentDataPath, meta.guid.ToString());
		var extraFolder = Path.Combine(projectFolder, SaveFile.extraPath);
		allExtras = new Dictionary<string, InteractionPointEditor>();

		foreach (var point in interactionPoints)
		{
			if (!String.IsNullOrEmpty(point.filename))
			{
				var filesInPoint = point.filename.Split('\f');
				foreach (var file in filesInPoint)
				{
					allExtras.Add(Path.Combine(projectFolder, file), point);
				}
			}
		}

		//TODO(Simon): This gets the full path, but we expect the
		var filenames = Directory.GetFiles(extraFolder);
		foreach (var file in filenames)
		{
			if (!allExtras.ContainsKey(file))
			{
				allExtras.Add(file, null);
			}
		}
	}

	private void CleanExtras()
	{
		var projectFolder = Path.Combine(Application.persistentDataPath, meta.guid.ToString());

		var toRemoveFromDict = new List<string>();
		foreach (var file in allExtras)
		{
			if (file.Value == null)
			{
				File.Delete(Path.Combine(projectFolder, file.Key));
				toRemoveFromDict.Add(file.Key);
			}
		}

		foreach (var key in toRemoveFromDict)
		{
			allExtras.Remove(key);
		}

		//NOTE(Simon): Delete all unused miniatures
		{
			var allMiniatures = Directory.GetFiles(Path.Combine(projectFolder, SaveFile.miniaturesPath));
			var miniaturesInUse = new List<string>();
			foreach (var point in interactionPoints)
			{
				if (point.type == InteractionType.FindArea || point.type == InteractionType.MultipleChoiceArea)
				{
					var files = point.filename.Split('\f');
					foreach (var file in files)
					{
						miniaturesInUse.Add(Path.Combine(projectFolder, SaveFile.miniaturesPath, file));
					}
				}
			}

			foreach (var miniature in allMiniatures)
			{
				if (!miniaturesInUse.Contains(miniature))
				{
					File.Delete(miniature);
				}
			}
		}
	}

	//NOTE(Simon): Accepts single filenames, or mutliple separated by '\f'. Should be the relative ("extra") path, i.e. /extra/<filename>.<ext>
	private void SetExtrasToDeleted(string relativePaths)
	{
		var projectFolder = Path.Combine(Application.persistentDataPath, meta.guid.ToString());
		var list = relativePaths.Split('\f');

		foreach (var path in list)
		{
			allExtras[Path.Combine(projectFolder, path)] = null;
		}
	}

	//NOTE(Simon): Should be the relative ("extra") path, i.e. /extra/<filename>.<ext>
	private string CopyNewExtra(InteractionPointEditor point, string sourcePath)
	{
		var projectFolder = Path.Combine(Application.persistentDataPath, meta.guid.ToString());
		var newFilename = Path.Combine(SaveFile.extraPath, GenerateExtraGuid() + Path.GetExtension(sourcePath));
		var destPath = Path.Combine(projectFolder, newFilename);

		if (File.Exists(sourcePath))
		{
			File.Copy(sourcePath, destPath);
		}

		allExtras.Add(newFilename, point);
		return newFilename;
	}

	private void UpdateUploadPanel()
	{
		if (uploadStatus.done)
		{
			editorState = EditorState.Active;
			Destroy(uploadPanel.gameObject);
			//TODO(Simon): temp fix. Make proper
			try
			{
				uploadStatus.request.Dispose();
			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}
			Canvass.modalBackground.SetActive(false);
			Toasts.AddToast(5, "Upload succesful");
		}
		else if (uploadStatus.failed)
		{
			editorState = EditorState.Active;
			Destroy(uploadPanel.gameObject);
			uploadStatus.request.Dispose();
			Canvass.modalBackground.SetActive(false);
			Toasts.AddToast(5, uploadStatus.error);
		}
		else
		{
			uploadPanel.UpdatePanel(uploadStatus);
		}

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			SetEditorActive(true);
			StopCoroutine(uploadStatus.coroutine);
			Destroy(uploadPanel.gameObject);
			uploadStatus.request.Dispose();
			Canvass.modalBackground.SetActive(false);
			Toasts.AddToast(5, "Upload cancelled");
		}
	}

	private void ResetInteractionPointTemp()
	{
		interactionPointTemp.transform.position = new Vector3(1000, 1000, 1000);
	}

	public void ShowProjectInExplorer()
	{
		string path = Path.Combine(Application.persistentDataPath, meta.guid.ToString());

		ExplorerHelper.ShowPathInExplorer(path);
	}

	public static string GenerateExtraGuid()
	{
		return Guid.NewGuid().ToString().Replace("-", "");
	}

	private static int LowerNiceTime(double time)
	{
		int[] niceTimes = { 1, 2, 5, 10, 15, 30, 60, 2 * 60, 5 * 60, 10 * 60, 15 * 60, 30 * 60, 60 * 60, 2 * 60 * 60 };
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

	private static int UpperNiceTime(double time)
	{
		int[] niceTimes = { 1, 2, 5, 10, 15, 30, 60, 2 * 60, 5 * 60, 10 * 60, 15 * 60, 30 * 60, 60 * 60, 2 * 60 * 60 };

		for (int i = niceTimes.Length - 1; i >= 0; i--)
		{
			if (niceTimes[i] < time)
			{
				return niceTimes[i + 1];
			}
		}

		return niceTimes[0];
	}

	public float TimeToPx(double time)
	{
		//NOTE(Simon): If time is outside currently displayed range, return a pixel _far_ outside the window
		if (time < timelineWindowStartTime || time > timelineWindowEndTime)
		{
			return -1000;
		}
		var fraction = (time - timelineWindowStartTime) / (timelineWindowEndTime - timelineWindowStartTime);
		return (float)(timelineOffsetPixels + (fraction * timelineWidthPixels));
	}

	public float PxToAbsTime(double px)
	{
		var realPx = px - timelineOffsetPixels;
		var fraction = realPx / timelineWidthPixels;
		var time = fraction * (timelineWindowEndTime - timelineWindowStartTime) + timelineWindowStartTime;
		return (float)time;
	}

	public float PxToRelativeTime(float px)
	{
		return px / timelineWidthPixels * (timelineWindowEndTime - timelineWindowStartTime);
	}

	private static long FileSize(string path)
	{
		return new FileInfo(path).Length;
	}
}
