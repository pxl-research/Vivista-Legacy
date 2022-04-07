using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Management;
using Button = UnityEngine.UI.Button;
using Cursor = UnityEngine.Cursor;
using Debug = UnityEngine.Debug;
using Slider = UnityEngine.UI.Slider;

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

public enum Perspective
{
	Perspective360,
	Perspective180,
	PerspectiveFlat
}

public enum TimelineDragMode
{
	None,
	Time,
	Chapter,
	TimelineItem,
	TimelineItemResize,
	TimelineHorizontal,
	TimelineVertical,
	TimelineRowReorder
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
	public GameObject textTooltipPrefab;
	public GameObject interactionPointPrefab;
	private GameObject interactionPointTemp;
	private List<InteractionPointEditor> interactionPoints;
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
	private ChapterManagerPanel chapterPanel;
	private ExportPanel exportPanel;
	private SettingsPanel settingsPanel;
	public GameObject updateAvailableNotification;

	public RectTransform timelineContainer;
	public RectTransform timelineScrollView;
	public RectTransform timeline;
	public RectTransform timeLabelHolder;
	public RectTransform chapterLabelHolder;
	public GameObject timelineRowPrefab;
	public Text timeLabelPrefab;
	public RectTransform chapterLabelPrefab;
	public RectTransform currentTimeLabel;
	public AudioMixer mixer;

	private List<Text> timeLabels = new List<Text>();
	private List<RectTransform> chapterLabels = new List<RectTransform>();
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
	private float timelineLeftMargin;
	private float timelineWidthPixels;
	private bool timelineLabelsDirty;

	private TimelineDragMode dragMode;
	private Vector2 prevMousePosition;
	private Vector2 mouseDelta;
	private InteractionPointEditor timelineItemBeingDragged;
	private InteractionPointEditor timelineItemBeingResized;
	private bool isResizingStart;
	public RectTransform timelineFirstColumnWidth;
	private Chapter chapterBeingDragged;
	private Transform timelineRowBeingDragged;
	private GameObject timelineRowPlaceholder;

	private TimeTooltip timeTooltip;
	private TimelineTooltip textTooltip;

	private Metadata meta;
	private Dictionary<string, InteractionPointEditor> allExtras = new Dictionary<string, InteractionPointEditor>();


	private void Awake()
	{
		Physics.autoSimulation = false;
		Instance = this;
		//NOTE(Kristof): This needs to be called in awake so we're guaranteed it isn't in VR mode
		XRGeneralSettings.Instance.Manager.DeinitializeLoader();
		Screen.SetResolution(Screen.width - 50, Screen.height - 50, FullScreenMode.Windowed);

		SaveFile.MoveProjectsToCorrectFolder();
		StartCoroutine(AutoUpdatePanel.IsUpdateAvailable(ShowUpdateNoticeIfNecessary));
	}

	private void Start()
	{
		interactionPointTemp = Instantiate(interactionPointPrefab);
		interactionPointTemp.name = "Temp InteractionPoint";

		interactionPoints = new List<InteractionPointEditor>();

		timeTooltip = Instantiate(timeTooltipPrefab, new Vector3(-1000, -1000), Quaternion.identity, Canvass.main.transform).GetComponent<TimeTooltip>();
		timeTooltip.ResetPosition();

		textTooltip = Instantiate(textTooltipPrefab, new Vector3(-1000, -1000), Quaternion.identity, Canvass.main.transform).GetComponent<TimelineTooltip>();
		textTooltip.ResetPosition();

		timelineLabelsDirty = true;

		prevMousePosition = Input.mousePosition;

		SetEditorActive(false);
		meta = new Metadata();

		InitOpenProjectPanel();

		fileLoader = GameObject.Find("FileLoader").GetComponent<FileLoader>();
		videoController = fileLoader.controller;
		videoController.mixer = mixer;
		
		//NOTE(Simon): Login if details were remembered
		{
			var details = LoginPanel.GetSavedLogin();
			if (details != null)
			{
				var (success, _) = LoginPanel.SendLoginRequest(details.email, details.password);
				if (success)
				{
					Toasts.AddToast(5, "Logged in");
				}
			}
		}
	}

	private void Update()
	{
		mouseDelta = new Vector2(Input.mousePosition.x - prevMousePosition.x, Input.mousePosition.y - prevMousePosition.y);
		prevMousePosition = Input.mousePosition;

		//NOTE(Simon): Reset InteractionPoint color after a hover
		foreach (var point in interactionPoints)
		{
			point.point.GetComponent<SpriteRenderer>().color = TagManager.Instance.GetTagColorById(point.tagId);
		}

		if (Config.ShowOnlyCurrentInteractions)
		{
			foreach (var point in interactionPoints)
			{
				bool shouldBeActive = point.startTime <= videoController.currentTime && point.endTime >= videoController.currentTime;
				point.point.SetActive(shouldBeActive);
			}

			pointToMove?.point.SetActive(true);
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
				for (int i = 0; i < interactionPoints.Count; i++)
				{
					if (interactionPoints[i].point == hit.transform.gameObject)
					{
						HighlightTimelineRow(interactionPoints[i]);
					}
				}
			}

			if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				&& Input.GetKeyDown(KeyCode.Space) && tagPanel == null && chapterPanel == null)
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
						{
							interactionEditor = Instantiate(UIPanels.Instance.tabularDataPanelEditor, Canvass.main.transform).gameObject;
							interactionEditor.GetComponent<TabularDataPanelEditor>().Init("", 1, 1, new List<string>());
							break;
						}
						case InteractionType.Chapter:
						{
							interactionEditor = Instantiate(UIPanels.Instance.chapterPanelEditor, Canvass.main.transform).gameObject;
							interactionEditor.GetComponent<ChapterPanelEditor>().Init("", -1);
							break;
						}
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

						panel.Init(editor.answerQuestion, editor.answerCorrect, editor.answerAnswers);

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
				case InteractionType.Chapter:
				{
					var editor = interactionEditor.GetComponent<ChapterPanelEditor>();

					if (editor.answered)
					{
						var panel = Instantiate(UIPanels.Instance.chapterPanel, Canvass.main.transform);
						panel.Init(editor.answerTitle, editor.answerChapterId);
						lastPlacedPoint.title = editor.answerTitle;
						lastPlacedPoint.body = editor.answerChapterId.ToString();
						lastPlacedPoint.panel = panel.gameObject;

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
				lastPlacedPoint.filled = true;
				lastPlacedPoint.point.GetComponent<InteractionPointRenderer>().Init(lastPlacedPoint);

				Destroy(interactionEditor);
				editorState = EditorState.Active;
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
						panel.Init(editor.answerQuestion, editor.answerCorrect, editor.answerAnswers);
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
				case InteractionType.Chapter:
				{
					var editor = interactionEditor.GetComponent<ChapterPanelEditor>();

					if (editor.answered)
					{
						var panel = pointToEdit.panel.GetComponent<ChapterPanel>();
						panel.Init(editor.answerTitle, editor.answerChapterId);
						pointToEdit.title = editor.answerTitle;
						pointToEdit.body = editor.answerChapterId.ToString();
						pointToEdit.panel = panel.gameObject;

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

				pointToEdit.point.GetComponent<InteractionPointRenderer>().Init(pointToEdit);

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

				if (!SaveProject())
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

				if (OpenProject(projectFolder))
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

				if (OpenProject(projectFolder))
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
			if (loginPanel.answered)
			{
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
				Destroy(loginPanel.gameObject);
				if (UnsavedChangesTracker.Instance.unsavedChanges)
				{
					InitSaveProjectPanel();
				}
				else
				{
					InitUploadPanel();
				}
			}
			if (savePanel != null && savePanel.answered)
			{
				savePanel.Init(true, meta.title);
				meta.title = savePanel.answerTitle;

				if (!SaveProject())
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
				if (uploadPanel.done)
				{
					editorState = EditorState.Active;
					uploadPanel.Dispose();
					Canvass.modalBackground.SetActive(false);
				}
			}

			if (Input.GetKeyUp(KeyCode.Escape))
			{
				//NOTE(Simon): Only allow cancel if we're not already uploading
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
			InitLoginPanel();
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

		//Note(Simon): By default, make interactionPoint panels invisible on load
		interactionPoints.Add(point);
		if (point.panel != null && hidden)
		{
			point.panel.SetActive(false);
		}

		point.timelineRow.mandatory.isOn = point.mandatory;
		point.timelineRow.mandatory.onValueChanged.AddListener(x => OnMandatoryChanged(point, x));
		
		SetTimelineItemTitleWidth(point);
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
		Texture2D desiredCursor = null;

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
		if (dragMode == TimelineDragMode.None)
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

			timelineLeftMargin = timelineFirstColumnWidth.rect.width;
			timelineWidthPixels = timeline.rect.width - timelineLeftMargin;
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
				while (timeLabels.Count < realNumLabels)
				{
					var label = Instantiate(timeLabelPrefab, timeLabelHolder.transform);
					timeLabels.Add(label);
				}

				while (timeLabels.Count > realNumLabels)
				{
					Destroy(timeLabels[timeLabels.Count - 1].gameObject);
					timeLabels.RemoveAt(timeLabels.Count - 1);
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
						timeLabels[i].enabled = true;
						timeLabels[i].text = MathHelper.FormatSeconds(tickTime);
						timeLabels[i].rectTransform.position = new Vector2(TimeToPx(tickTime), timeLabels[i].rectTransform.position.y);
					}
					DrawLineAtTime(tickTime, 1, new Color(0, 0, 0, 47f / 255), 10);
				}
				else
				{
					timeLabels[i].enabled = false;
				}
			}

			timelineLabelsDirty = false;
		}

		//Note(Simon): Render timeline item length indicators
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

		//NOTE(Simon): Timeline tag sprites
		foreach (var point in interactionPoints)
		{
			var sprite = point.timelineRow.tagShape;
			sprite.sprite = TagManager.Instance.GetTagShapeById(point.tagId);
			sprite.color = TagManager.Instance.GetTagColorById(point.tagId);
		}

		//Note(Simon): timeline buttons. Looping backwards because we're deleting items from the list.
		for (int i = interactionPoints.Count - 1; i >= 0; i--)
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

			deleteRect.position = new Vector3(timelineLeftMargin - 20, deleteRect.position.y);
			editRect.position = new Vector3(timelineLeftMargin - 40, editRect.position.y);
			moveRect.position = new Vector3(timelineLeftMargin - 60, moveRect.position.y);
			mandatoryRect.position = new Vector3(timelineLeftMargin - 80, mandatoryRect.position.y);

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
					case InteractionType.Chapter:
					{
						interactionEditor = Instantiate(UIPanels.Instance.chapterPanelEditor, Canvass.main.transform).gameObject;
						int chapterId = Int32.Parse(point.body);
						interactionEditor.GetComponent<ChapterPanelEditor>().Init(point.title, chapterId);
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
			var time = videoController.rawCurrentTime >= 0 ? videoController.rawCurrentTime : 0;

			if (dragMode == TimelineDragMode.Time)
			{
				time = Mathf.Clamp(PxToAbsTime(Input.mousePosition.x), 0, (float)videoController.videoLength);
				DrawLineAtTime(time, 4, new Color(0, 0, 0, 150f / 255));
				desiredCursor = Cursors.Instance.CursorDrag;

				if (Input.GetMouseButtonUp(0))
				{
					videoController.Seek(time);
					dragMode = TimelineDragMode.None;
				}
			}
			else
			{
				if (RectTransformUtility.RectangleContainsScreenPoint(currentTimeLabel, Input.mousePosition))
				{
					DrawLineAtTime(time, 4, new Color(0, 0, 0, 150f / 255));
					desiredCursor = Cursors.Instance.CursorDrag;

					if (Input.GetMouseButtonDown(0))
					{
						dragMode = TimelineDragMode.Time;
					}
				}
			}

			//NOTE(Simon): current time indicator
			DrawLineAtTime(time, 2, new Color(0, 0, 0, 150f / 255));
			currentTimeLabel.GetComponentInChildren<Text>().text = $"{MathHelper.FormatSeconds(time)}";
			currentTimeLabel.anchoredPosition = new Vector2(TimeToPx(time), 0);


			//NOTE(Simon): Top line. Only draw when inside timeline.
			var offset = new Vector3(0, 0);
			if (timeline.localPosition.y < timeLabelHolder.rect.height - offset.y)
			{
				var headerCoords = new Vector3[4];
				timeLabelHolder.GetWorldCorners(headerCoords);
				UILineRenderer.DrawLine(headerCoords[0] + offset, headerCoords[3] + offset, 1, new Color(0, 0, 0, 47f / 255));
			}
			//NOTE(Simon): Start and end
			if (timelineZoom < 1)
			{
				DrawLineAtTime(0, 2, new Color(0, 0, 0, 47f / 255));
				DrawLineAtTime(timelineEndTime, 2, new Color(0, 0, 0, 47f / 255));
			}
		}

		//NOTE(Simon): Render chapters
		{
			var chapters = ChapterManager.Instance.chapters;
			while (chapters.Count > chapterLabels.Count)
			{
				var newLabel = Instantiate(chapterLabelPrefab, chapterLabelHolder.transform);
				newLabel.SetAsFirstSibling();
				chapterLabels.Add(newLabel);
			}

			while (chapters.Count < chapterLabels.Count)
			{
				Destroy(chapterLabels[chapterLabels.Count - 1].gameObject);
				chapterLabels.RemoveAt(chapterLabels.Count - 1);
			}

			if (dragMode == TimelineDragMode.Chapter)
			{
				chapterBeingDragged.time = Mathf.Clamp(PxToAbsTime(Input.mousePosition.x), 0, (float)videoController.videoLength);
				var tooltipPos = chapterLabels[chapters.IndexOf(chapterBeingDragged)].position + new Vector3(0, 15);
				timeTooltip.SetTime(chapterBeingDragged.time, tooltipPos);
				desiredCursor = Cursors.Instance.CursorDrag;

				if (Input.GetMouseButtonUp(0))
				{
					dragMode = TimelineDragMode.None;
					chapterBeingDragged = null;
					timeTooltip.ResetPosition();
					ChapterManager.Instance.Refresh();
				}
			}
			else
			{
				//NOTE(Simon): Only draw an interactable line if we're not already interacting with a timelineItem
				if (dragMode == TimelineDragMode.None)
				{
					bool hovering = false;
					for (int i = 0; i < chapters.Count; i++)
					{
						if (RectTransformUtility.RectangleContainsScreenPoint(chapterLabels[i], Input.mousePosition))
						{
							hovering = true;

							DrawLineAtTime(chapters[i].time, 4f, new Color(.7f, .3f, .3f));
							desiredCursor = Cursors.Instance.CursorDrag;
							
							var tooltipPos = chapterLabels[i].position + new Vector3(0, 15);
							textTooltip.SetText(chapters[i].name, tooltipPos);

							if (Input.GetMouseButtonDown(0))
							{
								dragMode = TimelineDragMode.Chapter;
								textTooltip.ResetPosition();
								chapterBeingDragged = chapters[i];
							}
							break;
						}
					}

					if (!hovering)
					{
						textTooltip.ResetPosition();
					}
				}
			}

			for (int i = 0; i < chapters.Count; i++)
			{
				DrawLineAtTime(chapters[i].time, chapters[i] == chapterBeingDragged ? 4f : 2f, new Color(.7f, .3f, .3f));
				chapterLabels[i].GetComponentInChildren<Text>().text = $"Ch.{i + 1}";
				chapterLabels[i].anchoredPosition = new Vector2(TimeToPx(chapters[i].time), 0);
			}
		}

		//Note(Simon): resizing and moving of timeline items
		{
			if (dragMode == TimelineDragMode.None)
			{
				foreach (var point in interactionPoints)
				{
					var indicatorRect = point.timelineRow.indicator.rectTransform;

					RectTransformUtility.ScreenPointToLocalPointInRectangle(indicatorRect, Input.mousePosition, null, out var rectPixel);
					var leftAreaX = 5;
					var rightAreaX = indicatorRect.rect.width - 5;

					//NOTE(Simon) Check if conditions are met to start a resize or drag operation on a timeline item
					if (RectTransformUtility.RectangleContainsScreenPoint(indicatorRect, Input.mousePosition)
						&& RectTransformUtility.RectangleContainsScreenPoint(timeline, Input.mousePosition))
					{
						if (rectPixel.x < leftAreaX)
						{
							desiredCursor = Cursors.Instance.CursorResizeHorizontal;
							if (Input.GetMouseButtonDown(0))
							{
								isResizingStart = true;
								dragMode = TimelineDragMode.TimelineItemResize;
								timelineItemBeingResized = point;
							}
						}
						else if (rectPixel.x > rightAreaX)
						{
							desiredCursor = Cursors.Instance.CursorResizeHorizontal;
							if (Input.GetMouseButtonDown(0))
							{
								isResizingStart = false;
								dragMode = TimelineDragMode.TimelineItemResize;
								timelineItemBeingResized = point;
							}
						}
						else
						{
							desiredCursor = Cursors.Instance.CursorDrag;
							if (Input.GetMouseButtonDown(0))
							{
								isResizingStart = false;
								dragMode = TimelineDragMode.TimelineItem;
								timelineItemBeingDragged = point;
							}
						}

						DisableTimelineScroll();
						break;
					}
				}
			}
			else if (dragMode == TimelineDragMode.TimelineItem)
			{
				//NOTE(Simon): End drag operation
				if (!Input.GetMouseButton(0))
				{
					dragMode = TimelineDragMode.None;
					timelineItemBeingDragged = null;
					timeTooltip.ResetPosition();
					UnsavedChangesTracker.Instance.unsavedChanges = true;
					EnableTimelineScroll();
				}
				else
				{
					float newStart = (float)timelineItemBeingDragged.startTime + PxToRelativeTime(mouseDelta.x);
					float newEnd = (float)timelineItemBeingDragged.endTime + PxToRelativeTime(mouseDelta.x);
					if (newStart < 0.0f)
					{
						float length = newEnd - newStart;
						newStart = 0;
						newEnd = length;
					}

					if (newEnd > timelineEndTime)
					{
						float length = newEnd - newStart;
						newEnd = timelineEndTime;
						newStart = newEnd - length;
					}

					timelineItemBeingDragged.startTime = newStart;
					timelineItemBeingDragged.endTime = newEnd;

					var imageRect = timelineItemBeingDragged.timelineRow.indicator.rectTransform;
					var tooltipPos = new Vector2(imageRect.position.x + imageRect.rect.width / 2,
												 imageRect.position.y + imageRect.rect.height / 2);

					timeTooltip.SetTime(newStart, newEnd, tooltipPos);

					HighlightPoint(timelineItemBeingDragged);
					desiredCursor = Cursors.Instance.CursorDrag;
				}
			}
			else if (dragMode == TimelineDragMode.TimelineItemResize)
			{
				//NOTE(Simon): End resize operation
				if (!Input.GetMouseButton(0))
				{
					dragMode = TimelineDragMode.None;
					timelineItemBeingResized = null;
					timeTooltip.ResetPosition();
					UnsavedChangesTracker.Instance.unsavedChanges = true;
					EnableTimelineScroll();
				}
				else
				{
					if (isResizingStart)
					{
						var newStart = Mathf.Max(0.0f,
							(float) timelineItemBeingResized.startTime + PxToRelativeTime(mouseDelta.x));
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
						var newEnd = Mathf.Min(timelineEndTime,
							(float) timelineItemBeingResized.endTime + PxToRelativeTime(mouseDelta.x));
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
					desiredCursor = Cursors.Instance.CursorResizeHorizontal;
				}
			}
		}

		//NOTE(Simon): Reordering of timeline rows
		{
			if (dragMode == TimelineDragMode.None)
			{
				foreach (var point in interactionPoints)
				{
					var reorderRect = point.timelineRow.reorder.rectTransform;

					if (RectTransformUtility.RectangleContainsScreenPoint(reorderRect, Input.mousePosition))
					{
						desiredCursor = Cursors.Instance.CursorDrag;
						if (Input.GetMouseButtonDown(0))
						{
							dragMode = TimelineDragMode.TimelineRowReorder;
							timelineRowBeingDragged = point.timelineRow.transform;
							DisableTimelineScroll();

							timelineRowPlaceholder = new GameObject();
							timelineRowPlaceholder.transform.SetParent(timelineRowBeingDragged.parent);
							timelineRowPlaceholder.transform.SetSiblingIndex(timelineRowBeingDragged.GetSiblingIndex());
							var rect = timelineRowPlaceholder.AddComponent<RectTransform>();
							rect.pivot = Vector2.one;
							rect.sizeDelta = timelineRowBeingDragged.GetComponent<RectTransform>().sizeDelta;

							timelineRowBeingDragged.SetParent(Canvass.main.transform);
						}
					}
				}
			}
			else if (dragMode == TimelineDragMode.TimelineRowReorder)
			{
				if (!Input.GetMouseButton(0))
				{
					var newIndex = timelineRowPlaceholder.transform.GetSiblingIndex();
					timelineRowBeingDragged.SetParent(timelineRowPlaceholder.transform.parent);
					timelineRowBeingDragged.SetSiblingIndex(newIndex);

					var point = interactionPoints.Find(x => x.timelineRow.transform == timelineRowBeingDragged);
					interactionPoints.Remove(point);
					interactionPoints.Insert(newIndex, point);

					Destroy(timelineRowPlaceholder);

					timelineRowBeingDragged = null;
					dragMode = TimelineDragMode.None;
					EnableTimelineScroll();
					UnsavedChangesTracker.Instance.unsavedChanges = true;
				}
				else
				{
					var pos = timelineRowBeingDragged.position;
					pos.y = Input.mousePosition.y;
					timelineRowBeingDragged.position = pos;

					var placeHolderParent = timelineRowPlaceholder.transform.parent;
					int newSiblingIndex = 0;

					for (int i = placeHolderParent.childCount - 1; i >= 0; i--)
					{
						if (timelineRowBeingDragged.position.y < placeHolderParent.GetChild(i).position.y)
						{
							newSiblingIndex = i;
							if (timelineRowPlaceholder.transform.GetSiblingIndex() < newSiblingIndex)
							{
								newSiblingIndex++;
							}
							break;
						}
					}

					timelineRowPlaceholder.transform.SetSiblingIndex(newSiblingIndex);

					desiredCursor = Cursors.Instance.CursorDrag;
				}
			}
		}
		//Note(Simon): Resizing of timeline
		{
			if (dragMode == TimelineDragMode.None)
			{
				var verticalRect = new Rect(new Vector2(0, timelineContainer.rect.height - 4), new Vector2(timelineContainer.rect.width, 4));
				var horizontalRect = new Rect(new Vector2(timelineLeftMargin + 1, 0), new Vector2(4, timelineContainer.rect.height - timeLabelHolder.sizeDelta.y));

				if (verticalRect.Contains(Input.mousePosition))
				{
					if (!Cursors.isOverridingCursor)
					{
						desiredCursor = Cursors.Instance.CursorResizeVertical;
					}

					if (Input.GetMouseButtonDown(0))
					{
						dragMode = TimelineDragMode.TimelineVertical;
					}
				}
				else if (horizontalRect.Contains(Input.mousePosition))
				{
					DrawLineAtTime(0, 2, Color.black);
					if (!Cursors.isOverridingCursor)
					{
						desiredCursor = Cursors.Instance.CursorResizeHorizontal;
					}

					if (Input.GetMouseButtonDown(0))
					{
						dragMode = TimelineDragMode.TimelineHorizontal;
					}
				}
			}
			else if (dragMode == TimelineDragMode.TimelineVertical)
			{
				if (Input.GetMouseButtonUp(0))
				{
					dragMode = TimelineDragMode.None;
					desiredCursor = null;
				}

				var resizeDelta = new Vector2(0, mouseDelta.y);
				timelineContainer.sizeDelta += resizeDelta;
			}
			else if (dragMode == TimelineDragMode.TimelineHorizontal)
			{
				if (Input.GetMouseButtonUp(0))
				{
					dragMode = TimelineDragMode.None;
					desiredCursor = null;
				}

				//NOTE(Simon): Clamp timeline size to 100px of either side of screen, so it can't go offscreen
				timelineFirstColumnWidth.sizeDelta = new Vector2(Mathf.Clamp(timelineFirstColumnWidth.sizeDelta.x + mouseDelta.x, 150, Screen.width - 100), timelineFirstColumnWidth.sizeDelta.y);
				for (int i = 0; i < interactionPoints.Count; i++)
				{
					SetTimelineItemTitleWidth(interactionPoints[i]);
				}

				DrawLineAtTime(0, 2, Color.black);
				timelineLabelsDirty = true;
			}
		}

		//NOTE(Simon): Previews, pinning, Highlight interactionPoint on hover
		if (RectTransformUtility.RectangleContainsScreenPoint(timelineScrollView, Input.mousePosition) && dragMode == TimelineDragMode.None)
		{
			foreach (var point in interactionPoints)
			{
				var rectBackground = point.timelineRow.GetComponent<RectTransform>();
				var rect = point.timelineRow.title.GetComponent<RectTransform>();

				if (RectTransformUtility.RectangleContainsScreenPoint(rectBackground, Input.mousePosition))
				{
					HighlightPoint(point);
				}

				if (RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition)
					&& editorState == EditorState.Active
					&& point.panel != null)
				{
					HighlightTimelineRow(point);

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
		//NOTE(Simon): Hide any active panels if not hovering. Except for pinned panel
		else
		{
			foreach (var point in interactionPoints)
			{
				if (point != pinnedHoverPoint)
				{
					point.panel?.SetActive(false);
					//pinnedHoverPoint?.panel.SetActive(true);
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

	public void HighlightTimelineRow(InteractionPointEditor point)
	{
		var rowCorners = new Vector3[4];
		var rowRect = point.timelineRow.GetComponent<RectTransform>();
		rowRect.GetWorldCorners(rowCorners);

		var timelineCorners = new Vector3[4];
		timelineScrollView.GetComponent<RectTransform>().GetWorldCorners(timelineCorners);

		MathHelper.ClipWorldCorners(timelineCorners, rowCorners);

		//NOTE(Simon): If row rect contained within timeline rect
		{
			var start = new Vector2(rowCorners[0].x, (rowCorners[0].y + rowCorners[1].y) / 2);
			var end = new Vector2(rowCorners[2].x - 3, (rowCorners[2].y + rowCorners[3].y) / 2);
			var thickness = rowCorners[1].y - rowCorners[0].y;
			//NOTE(Simon): Show a darker background on hover
			UILineRenderer.DrawLine(start, end, thickness, new Color(0, 0, 0, 60 / 255f));
		}
	}

	//NOTE(Simon): Positive topOffset is upwards
	public void DrawLineAtTime(double time, float thickness, Color color, float topOffset = 0f)
	{
		var timePx = TimeToPx(time);

		float containerHeight = timelineScrollView.rect.height;

		UILineRenderer.DrawLine(
			new Vector2(timePx, 0),
			new Vector2(timePx, containerHeight + topOffset),
			thickness,
			color);
	}

	//NOTE(Simon): thickness is default thickness, overlapThickness is thickness if mouse overlaps, checkedThickness is the area that is checked for an overlap (but line is not drawn at that thickness)
	public bool DrawLineAtTimeCheckOverlap(double time, float thickness, float overlapThickness, float checkedThickness, Color color, Vector2 mousePos, float topOffset = 0f)
	{
		var timePx = TimeToPx(time);

		float containerHeight = timelineContainer.sizeDelta.y;
		float headerHeight = Mathf.Max(0, timeLabelHolder.sizeDelta.y - timeline.localPosition.y);
		float lineHeight = containerHeight - headerHeight + 18 + topOffset;

		var overlapRect = new Rect(timePx - (checkedThickness / 2f), 0, checkedThickness, lineHeight);

		if (overlapRect.Contains(Input.mousePosition))
		{
			UILineRenderer.DrawLine(
			new Vector2(timePx, 0),
			new Vector2(timePx, lineHeight),
			overlapThickness,
			color);

			return true;
		}
		else
		{
			UILineRenderer.DrawLine(
			new Vector2(timePx, 0),
			new Vector2(timePx, lineHeight),
			thickness,
			color);

			return false;
		}
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
			
			timelineOffsetTime = Mathf.Clamp(timelineOffsetTime, (float)-videoController.videoLength, (float)videoController.videoLength);
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
		Debug.Log($"frame {Time.frameCount}: Initting upload");
		if (String.IsNullOrEmpty(Web.sessionCookie))
		{
			InitLoginPanel();
			editorState = EditorState.SavingThenUploading;
		}
		else
		{
			if (UnsavedChangesTracker.Instance.unsavedChanges)
			{
				InitSaveProjectPanel();
			}
			else
			{
				InitUploadPanel();
			}

			editorState = EditorState.SavingThenUploading;
		}
	}

	private void InitUploadPanel()
	{
		Debug.Log($"frame {Time.frameCount}: Initting uploadpanel");
		uploadPanel = Instantiate(UIPanels.Instance.uploadPanel);
		uploadPanel.transform.SetParent(Canvass.main.transform, false);
		videoController.Pause();
		Canvass.modalBackground.SetActive(true);

		UploadProject();
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

		editorState = EditorState.LoggingIn;
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

	public void ShowChapterPanel()
	{
		if (chapterPanel != null)
		{
			return;
		}

		chapterPanel = Instantiate(UIPanels.Instance.chapterManagerPanel);
		chapterPanel.transform.SetParent(Canvass.main.transform, false);
	}

	public void ShowExportPanel()
	{
		exportPanel = Instantiate(UIPanels.Instance.exportPanel, Canvass.main.transform, false);
		exportPanel.Init(meta.guid);
		Canvass.modalBackground.SetActive(true);
		editorState = EditorState.Exporting;
	}

	public void ShowSettingsPanel()
	{
		if (settingsPanel != null)
		{
			return;
		}

		settingsPanel = Instantiate(UIPanels.Instance.settingsPanel);
		settingsPanel.transform.SetParent(Canvass.main.transform, false);
	}

	public bool SaveProject(bool makeThumbnail = true)
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
		SaveFile.WriteChapters(path, ChapterManager.Instance.chapters);

		CleanExtras();
		UnsavedChangesTracker.Instance.unsavedChanges = false;

		return success;
	}

	private bool OpenProject(string projectFolder)
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
		
		var chaptersPath = Path.Combine(Application.persistentDataPath, meta.guid.ToString());
		var chapters = SaveFile.ReadChapters(chaptersPath);
		ChapterManager.Instance.SetChapters(chapters);

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
					var split = newInteractionPoint.body.Split(new[] { '\f' }, 2);
					var correct = Int32.Parse(split[0]);
					var panel = Instantiate(UIPanels.Instance.multipleChoicePanel, Canvass.main.transform);
					panel.Init(newInteractionPoint.title, correct, split[1].Split('\f'));
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
				case InteractionType.Chapter:
				{
					var panel = Instantiate(UIPanels.Instance.chapterPanel, Canvass.main.transform);
					var chapterId = Int32.Parse(newInteractionPoint.body);
					panel.Init(newInteractionPoint.title, chapterId);
					newInteractionPoint.panel = panel.gameObject;
					break;
				}
				default:
				{
					Debug.LogError($"Forgot to implement OpenFile() branch for  {newInteractionPoint.type}");
					isValidPoint = false;
					break;
				}
			}

			if (isValidPoint)
			{
				newInteractionPoint.point.GetComponent<InteractionPointRenderer>().Init(newInteractionPoint);
				AddItemToTimeline(newInteractionPoint, true);
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
		var interactionTypeRenderer = point.point.GetComponentInChildren<SpriteRenderer>(excludeSelf: true);
		var tag = TagManager.Instance.GetTagById(point.tagId);

		shape.sprite = TagManager.Instance.ShapeForIndex(tag.shapeIndex);
		shape.color = tag.color;
		interactionTypeRenderer.color = tag.color.IdealTextColor();
	}

	private void UploadProject()
	{
		Debug.Log($"frame {Time.frameCount}: building upload queue");

		var filesToUpload = new Queue<FileUpload>();

		var projectPath = Path.Combine(Application.persistentDataPath, meta.guid.ToString());
		var extraPath = Path.Combine(projectPath, SaveFile.extraPath);
		var miniaturesPath = Path.Combine(projectPath, SaveFile.miniaturesPath);

		filesToUpload.Enqueue(new FileUpload(meta.guid, UploadFileType.Video, SaveFile.videoFilename, Path.Combine(projectPath, SaveFile.videoFilename)));
		filesToUpload.Enqueue(new FileUpload(meta.guid, UploadFileType.Meta, SaveFile.metaFilename, Path.Combine(projectPath, SaveFile.metaFilename)));
		filesToUpload.Enqueue(new FileUpload(meta.guid, UploadFileType.Chapters, SaveFile.chaptersFilename, Path.Combine(projectPath, SaveFile.chaptersFilename)));
		filesToUpload.Enqueue(new FileUpload(meta.guid, UploadFileType.Tags, SaveFile.tagsFilename, Path.Combine(projectPath, SaveFile.tagsFilename)));

		var extras = allExtras.Keys;
		foreach (var extra in extras)
		{
			var filename = Path.GetFileName(extra);
			filesToUpload.Enqueue(new FileUpload(meta.guid, UploadFileType.Extra, filename, Path.Combine(extraPath, filename)));
		}

		var miniatures = Directory.GetFiles(miniaturesPath);
		foreach (var miniature in miniatures)
		{
			var filename = Path.GetFileName(miniature);
			filesToUpload.Enqueue(new FileUpload(meta.guid, UploadFileType.Miniature, filename, Path.Combine(miniaturesPath, filename)));
		}

		uploadPanel.StartUpload(filesToUpload);
	}

	private void InitExtrasList()
	{
		var projectFolder = Path.Combine(Application.persistentDataPath, meta.guid.ToString());
		allExtras = new Dictionary<string, InteractionPointEditor>();

		foreach (var point in interactionPoints)
		{
			//NOTE(Simon): Ignore points without files and points with miniatures
			if (!String.IsNullOrEmpty(point.filename) 
				&& (point.type != InteractionType.FindArea && point.type != InteractionType.MultipleChoiceArea))
			{
				var filesInPoint = point.filename.Split('\f');
				foreach (var file in filesInPoint)
				{
					allExtras.Add(file, point);
				}
			}
		}

		var extraFolder = Path.Combine(projectFolder, SaveFile.extraPath);
		Directory.CreateDirectory(extraFolder);
		var filenames = Directory.GetFiles(extraFolder);
		foreach (var file in filenames)
		{
			//NOTE(Simon): Remove everything before "extra" in the paths
			var relativePath = file.Substring(projectFolder.Length + 1);
			if (!allExtras.ContainsKey(relativePath))
			{
				//NOTE(Simon): If we add an extra at this point, it means it was not cleaned up properly in a previous session
				allExtras.Add(relativePath, null);
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

	//NOTE(Simon): Accepts single filenames, or multiple separated by '\f'. Should be the relative ("extra") path, i.e. /extra/<filename>.<ext>
	private void SetExtrasToDeleted(string relativePaths)
	{
		var list = relativePaths.Split('\f');

		foreach (var path in list)
		{
			allExtras[path] = null;
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

	private void ResetInteractionPointTemp()
	{
		interactionPointTemp.transform.position = new Vector3(1000, 1000, 1000);
	}

	public void ShowProjectInExplorer()
	{
		string path = Path.Combine(Application.persistentDataPath, meta.guid.ToString());

		ExplorerHelper.ShowPathInExplorer(path);
	}

	private void SetTimelineItemTitleWidth(InteractionPointEditor point)
	{
		float iconWidth = 102;
		float tagShapeWidth = point.timelineRow.tagShape.rectTransform.sizeDelta.x;
		float columnWidth = timelineFirstColumnWidth.sizeDelta.x;
		float newWidth = columnWidth - tagShapeWidth - iconWidth;
		point.timelineRow.title.rectTransform.sizeDelta = new Vector2(newWidth, point.timelineRow.title.rectTransform.sizeDelta.y);
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
		return (float)(timelineLeftMargin + (fraction * timelineWidthPixels));
	}

	public float PxToAbsTime(double px)
	{
		var realPx = px - timelineLeftMargin;
		var fraction = realPx / timelineWidthPixels;
		var time = fraction * (timelineWindowEndTime - timelineWindowStartTime) + timelineWindowStartTime;
		return (float)time;
	}

	public float PxToRelativeTime(float px)
	{
		return px / timelineWidthPixels * (timelineWindowEndTime - timelineWindowStartTime);
	}

	//NOTE(Simon): This is used as a callback, which is why it receives the seemingly unnecessary 'shouldShow' parameter
	public void ShowUpdateNoticeIfNecessary(bool shouldShow)
	{
		updateAvailableNotification.SetActive(shouldShow);
		if (shouldShow)
		{
			updateAvailableNotification.GetComponent<Button>().onClick.AddListener(InitUpdatePanel);
		}
	}

	public void InitUpdatePanel()
	{
		EventSystem.current.SetSelectedGameObject(null);
		Canvass.modalBackground.SetActive(true);
		var panel = Instantiate(UIPanels.Instance.autoUpdatePanel, Canvass.main.transform, false).GetComponent<AutoUpdatePanel>();
		panel.Init(AutoUpdatePanel.VivistaApplication.Editor, OnUpdateCancel);
	}

	public void OnUpdateCancel()
	{
		Canvass.modalBackground.SetActive(false);
	}
}
