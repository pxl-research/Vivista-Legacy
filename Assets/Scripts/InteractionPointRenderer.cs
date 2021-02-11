using UnityEngine;

public class InteractionPointRenderer : MonoBehaviour
{
	public SpriteRenderer interactionType;
	public SpriteRenderer mandatory;
	public GameObject ping;

	void Start()
	{
		gameObject.transform.localScale = UnityEngine.XR.XRSettings.enabled ? new Vector3(2.5f,2.5f,2.5f) : new Vector3(10f, 10f, 10f);
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
		ping.SetActive(true);
		point.panel.SetActive(false);

		mandatory.enabled = point.mandatory;

		SetInteractionPointTag(point.point, point.tagId);
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
		ping.SetActive(active);
	}
}
