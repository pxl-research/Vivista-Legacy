using UnityEngine;
using UnityEngine.Events;

public class Hittable : MonoBehaviour
{
	public UnityEvent onHit;
	public UnityEvent onHoverStart;
	public UnityEvent onHoverStay;
	public UnityEvent onHoverEnd;

	public bool hitting;
	public bool hovering;
	
	private bool oldHitting;
	public bool oldHovering;

	// Use this for initialization
	void Start()
	{
		Player.hittables.Add(this);
	}

	void Update()
	{
		if (!oldHitting && hitting)
		{
			onHit.Invoke();
		}

		if (!oldHovering && hovering)
		{
			onHoverStart.Invoke();
		}

		if (oldHovering && hovering)
		{
			onHoverStay.Invoke();
		}

		if (oldHovering && !hovering)
		{
			onHoverEnd.Invoke();
		}
		oldHovering = hovering;
		oldHovering = hitting;
	}
}
