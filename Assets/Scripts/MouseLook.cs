using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Management;

public class MouseLook : MonoBehaviour 
{
	public static MouseLook Instance { get; private set; }

	private static MouseLook _instance;
	public Vector3 mousePos;
	public float mouseRotX;
	public float mouseRotY;

	public float maxX = 360;
	public float minX = -360;
	public float maxY = 80;
	public float minY = -80;

	public float sensivity = 0.5f;
	public bool forceActive;

	public Quaternion originalRotation;

	public Editor editor;

	void Awake()
	{
		Instance = this;
	}

	void Start () 
	{
		mousePos = Input.mousePosition;
		originalRotation = transform.localRotation;

		var editorObject = GameObject.Find("Editor");
		if (editorObject != null)
		{
			editor = editorObject.GetComponent<Editor>();
		}
	}
	
	void Update () 
	{
		var mouseDelta = Input.mousePosition - mousePos;
		mousePos = Input.mousePosition;

		//NOTE(Simon): Do not use mouselook in VR
		//NOTE(Simon): Do use mouselook if not in editor
		//NOTE(Simon): Do use mouselook if in editor and correct editorstate
		if (XRGeneralSettings.Instance.Manager.activeLoader == null)
		{
			if (forceActive || editor == null || 
									  (editor.editorState == EditorState.Active
									|| editor.editorState == EditorState.Inactive
									|| editor.editorState == EditorState.MovingInteractionPoint
									|| editor.editorState == EditorState.PlacingInteractionPoint))
			{
				if (Input.GetMouseButton(1) && !EventSystem.current.IsPointerOverGameObject())
				{
					int invertHorizontal = Config.InvertMouseHorizontal ? -1 : 1;
					int invertVertical = Config.InvertMouseVertical ? -1 : 1;

					float zoomFactor = Camera.main.fieldOfView / 120f;
					mouseRotX += invertHorizontal * mouseDelta.x * sensivity * zoomFactor;
					mouseRotY += invertVertical * mouseDelta.y * sensivity * zoomFactor;
					mouseRotX = ClampAngle(mouseRotX, minX, maxX);
					mouseRotY = ClampAngle(mouseRotY, minY, maxY);

					var newRotx = Quaternion.AngleAxis(mouseRotX, Vector3.up);
					var newRoty = Quaternion.AngleAxis(mouseRotY, -Vector3.right);

					transform.localRotation = originalRotation * newRotx * newRoty;
				}

				if (Input.mouseScrollDelta.y != 0 && !EventSystem.current.IsPointerOverGameObject())
				{
					Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView - Input.mouseScrollDelta.y * 5, 40, 120);
				}
			}
		}
	}

	public static float ClampAngle (float angle, float min, float max)
	{
		if (angle < -360F)
		{
			angle += 360F;
		}
		if (angle > 360F)
		{
			angle -= 360F;
		}

		return Mathf.Clamp (angle, min, max);
	}
}
