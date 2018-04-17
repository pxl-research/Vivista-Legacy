using UnityEngine;

public interface IHittable
{
	GameObject ReturnObject();
	void OnHit();
	void Hovering(bool hovering);
}
