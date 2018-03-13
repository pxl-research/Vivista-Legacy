using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public enum PlayerState
{
	Opening,
	Watching,
}

public class InteractionPointPlayer
{
	public GameObject point;
	public GameObject panel;
	public Vector3 position;
	public Quaternion rotation;
	public InteractionType type;
	public string title;
	public string body;
	public string filename;
	public double startTime;
	public double endTime;
	public float interactionTimer;

	public Vector3 returnRayOrigin;
	public Vector3 returnRayDirection;
}

public class Player : MonoBehaviour
{
	private PlayerState playerState;

	private List<InteractionPointPlayer> interactionPoints;
	private FileLoader fileLoader;
	private VideoController videoController;
	private Image crosshair;
	private Image crosshairTimer;

	public GameObject interactionPointPrefab;
	public GameObject indexPanelPrefab;
	public GameObject imagePanelPrefab;
	public GameObject textPanelPrefab;
	public GameObject localAvatarPrefab;

	public GameObject controllerLeft;
	public GameObject controlleRight;

	private GameObject indexPanel;

	private string openVideo;

	void Start()
	{
		StartCoroutine(EnableVr());

		interactionPoints = new List<InteractionPointPlayer>();

		fileLoader = GameObject.Find("FileLoader").GetComponent<FileLoader>();
		videoController = fileLoader.videoController.GetComponent<VideoController>();
		OpenFilePanel();
		playerState = PlayerState.Opening;
		crosshair = Canvass.main.transform.Find("Crosshair").GetComponent<Image>();
		crosshairTimer = crosshair.transform.Find("CrosshairTimer").GetComponent<Image>();
	}

	void Update()
	{
		VRDevices.DetectDevices();

		Ray ray;
		//Note(Simon): Create a reversed raycast to find positions on the sphere with
		var cameraRay = Camera.main.ViewportPointToRay(new Vector2(0.5f, 0.5f));
		//TODO(Kristof): envetually make it so you check for last used controller rather than only using the right controller
		var controllerRay = new Ray(controlleRight.transform.position, controlleRight.transform.forward);

		if (VRDevices.loadedControllerSet != VRDevices.LoadedControllerSet.NoControllers)
		{
			ray = controllerRay;
		}
		else
		{
			ray = cameraRay;
		}

		ray.origin = ray.GetPoint(100);
		ray.direction = -ray.direction;

		if (playerState == PlayerState.Watching)
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				videoController.TogglePlay();
			}

			RaycastHit hit;
			Physics.Raycast(ray, out hit, 100, 1 << LayerMask.NameToLayer("interactionPoints"));

			if (XRSettings.enabled)
			{
				videoController.transform.position = Camera.main.transform.position;
			}

			if (VRDevices.loadedControllerSet != VRDevices.LoadedControllerSet.NoControllers)
			{
				crosshair.enabled = false;
				Canvass.main.renderMode = RenderMode.ScreenSpaceCamera;
			}
			else
			{
				crosshair.enabled = true;

				if (VRDevices.loadedSdk == VRDevices.LoadedSdk.None)
				{
					Canvass.main.renderMode = RenderMode.ScreenSpaceOverlay;
				}
				else
				{
					Canvass.main.renderMode = RenderMode.ScreenSpaceCamera;
				}
			}

			if (Input.mouseScrollDelta.y != 0)
			{
				Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView - Input.mouseScrollDelta.y * 5, 20, 120);
			}

			//Note(Simon): Interaction with points
			{
				bool interacting = false;
				foreach (var point in interactionPoints)
				{
					const float timeToInteract = 0.75f;

					var pointActive = point.startTime <= videoController.currentTime && point.endTime >= videoController.currentTime;
					point.point.SetActive(pointActive);

					if (hit.transform != null && hit.transform.gameObject == point.point)
					{
						interacting = true;
						point.interactionTimer += Time.deltaTime;
						crosshairTimer.fillAmount = point.interactionTimer / timeToInteract;
						crosshair.fillAmount = 1 - (point.interactionTimer / timeToInteract);

						if (point.interactionTimer > timeToInteract)
						{
							point.panel.SetActive(true);
						}
					}
					else if (point.panel.activeSelf)
					{
						point.panel.SetActive(false);
						point.point.GetComponent<MeshRenderer>().material.color = Color.white;
					}
					else
					{
						point.interactionTimer = 0;
					}
				}

				if (!interacting)
				{
					crosshairTimer.fillAmount = 0;
					crosshair.fillAmount = 1;
				}
			}
		}

		if (playerState == PlayerState.Opening)
		{
			var panel = indexPanel.GetComponent<IndexPanel>();
			if (panel.answered)
			{
				var metaFilename = Path.Combine(Application.persistentDataPath, Path.Combine(panel.answerVideoId, SaveFile.metaFilename));
				if (OpenFile(metaFilename))
				{
					Destroy(indexPanel);
					playerState = PlayerState.Watching;
					Canvass.modalBackground.SetActive(false);
				}
				else
				{
					Debug.Log("Couldn't open savefile");
				}
			}
		}
	}

	private bool OpenFile(string path)
	{
		var data = SaveFile.OpenFile(path);

		openVideo = Path.Combine(Application.persistentDataPath, Path.Combine(data.meta.guid.ToString(), SaveFile.videoFilename));
		fileLoader.LoadFile(openVideo);

		for (var j = interactionPoints.Count - 1; j >= 0; j--)
		{
			RemoveInteractionPoint(interactionPoints[j]);
		}

		interactionPoints.Clear();

		foreach (var point in data.points)
		{
			var newPoint = Instantiate(interactionPointPrefab, point.position, point.rotation);

			var newInteractionPoint = new InteractionPointPlayer
			{
				startTime = point.startTime,
				endTime = point.endTime,
				title = point.title,
				body = point.body,
				filename = "file:///" + Path.Combine(Application.persistentDataPath, Path.Combine(data.meta.guid.ToString(), point.filename)),
				type = point.type,
				point = newPoint,
				returnRayOrigin = point.returnRayOrigin,
				returnRayDirection = point.returnRayDirection
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
					panel.GetComponent<ImagePanel>().Init(point.position, newInteractionPoint.title, newInteractionPoint.filename, false);
					newInteractionPoint.panel = panel;
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}

			AddInteractionPoint(newInteractionPoint);
		}
		StartCoroutine(UpdatePointPositions());
		return true;
	}

	private void OpenFilePanel()
	{
		indexPanel = Instantiate(indexPanelPrefab);
		indexPanel.GetComponent<IndexPanel>();
		indexPanel.transform.SetParent(Canvass.main.transform, false);
		Canvass.modalBackground.SetActive(true);
		playerState = PlayerState.Opening;
	}

	private void AddInteractionPoint(InteractionPointPlayer point)
	{
		interactionPoints.Add(point);
	}

	private void RemoveInteractionPoint(InteractionPointPlayer point)
	{
		interactionPoints.Remove(point);
		Destroy(point.point);
		if (point.panel != null)
		{
			Destroy(point.panel);
		}
	}

	//NOTE(Simon): This needs to be a coroutine so that we can wait a frame before recalculating point positions. If this were run in the first frame, collider positions would not be up to date yet.
	private IEnumerator UpdatePointPositions()
	{
		//NOTE(Simon): wait one frame
		yield return null;

		foreach (var interactionPoint in interactionPoints)
		{
			var ray = new Ray(interactionPoint.returnRayOrigin, interactionPoint.returnRayDirection);

			RaycastHit hit;

			if (Physics.Raycast(ray, out hit, 100))
			{
				var drawLocation = hit.point;
				interactionPoint.point.transform.position = drawLocation;
			}
		}
	}

	private IEnumerator EnableVr()
	{
		//NOTE(Kristof) If More APIs need to be implemented, add them here
		XRSettings.LoadDeviceByName(new[] { "OpenVR", "None"});

		//NOTE(Kristof): wait one frame to allow the device to be loaded
		yield return null;

		if (XRSettings.loadedDeviceName.Equals("Oculus"))
		{
			VRDevices.loadedSdk = VRDevices.LoadedSdk.Oculus;
			Instantiate(localAvatarPrefab);
			localAvatarPrefab.GetComponent<OvrAvatar>().StartWithControllers = true;
			XRSettings.enabled = true;
		}
		else if (XRSettings.loadedDeviceName.Equals("OpenVR"))
		{
			VRDevices.loadedSdk = VRDevices.LoadedSdk.OpenVr;
			XRSettings.enabled = true;
		}
		else if (XRSettings.loadedDeviceName.Equals(""))
		{
			VRDevices.loadedSdk = VRDevices.LoadedSdk.None;
		}
	}
}
