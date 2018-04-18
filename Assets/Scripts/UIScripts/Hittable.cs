using UnityEngine;
using UnityEngine.Events;

public class Hittable : MonoBehaviour
{
	public UnityEvent onHit;
	public UnityEvent onHover;

	public bool hitting;
	public bool hovering;

	// Use this for initialization
	void Start()
	{
		Player.hittables.Add(this);
	}

	void Update()
	{
		onHit.Invoke();
		onHover.Invoke();
	}
}
