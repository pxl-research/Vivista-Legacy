using UnityEngine;

public class PreviousNext : MonoBehaviour
{
	public void OnHit()
	{
		if (gameObject.name.Equals("Previous"))
		{
			StartCoroutine(transform.root.GetComponent<AnimateProjector>().player.PageSelector(-1));
		}
		else
		{
			StartCoroutine(transform.root.GetComponent<AnimateProjector>().player.PageSelector(+1));
		}
	}
}
