using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Management;

public class InteractionPointRenderer : MonoBehaviour
{
	public SpriteRenderer interactionType;
	public SpriteRenderer mandatory;
	public GameObject ping;

	private bool viewed;
	private Vector3 startScale;

	private bool isEditor;

	void Start()
	{
		isEditor = SceneManager.GetActiveScene().name.Equals("Editor");
	}

	public void Init(InteractionPointEditor point)
	{
		point.panel.SetActive(false);
		SetInteractionPointTag(point.point, point.tagId);
		interactionType.sprite = InteractionTypeSprites.GetSprite(point.type);

		mandatory.enabled = point.mandatory;
	}

	public void Init(InteractionPointPlayer point)
	{
		point.point.transform.LookAt(Vector3.zero, Vector3.up);
		point.point.transform.RotateAround(point.point.transform.position, point.point.transform.up, 180);

		//NOTE(Simon): Add a sprite to interaction points, indicating InteractionType
		interactionType.sprite = InteractionTypeSprites.GetSprite(point.type);
		ping.SetActive(false);
		point.panel.SetActive(false);

		mandatory.enabled = point.mandatory;

		SetInteractionPointTag(point.point, point.tagId);
	}

	public void Update()
	{
		if (!viewed && !isEditor)
		{
			startScale = XRSettings.isDeviceActive ? new Vector3(5f, 5f, 5f) : new Vector3(10f, 10f, 10f);
			transform.localScale = startScale * (1 + Mathf.SmoothStep(0, 0.3f, Mathf.PingPong(Time.time, 1)));
		}
	}

	private void SetInteractionPointTag(GameObject point, int tagId)
	{
		var shape = point.GetComponent<SpriteRenderer>();
		var tag = TagManager.Instance.GetTagById(tagId);

		shape.sprite = TagManager.Instance.ShapeForIndex(tag.shapeIndex);
		shape.color = tag.color;
		interactionType.color = tag.color.IdealTextColor();
	}

	public void SetPingActive(bool active)
	{
		//ping.SetActive(active);
		if (!active)
		{
			viewed = true;
			transform.localScale = startScale;
		}
	}
}
