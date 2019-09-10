using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Toast
{
	public float secondsRemaining;
	public string text;
	public GameObject gameObject;
}

public class Toasts : MonoBehaviour 
{
	public RectTransform toastHolder;
	public GameObject toastPrefab;

	private static GameObject prefab;
	private static RectTransform holder;
	private static Queue<Toast> toasts = new Queue<Toast>();
	
	void Start()
	{
		prefab = toastPrefab;
		holder = toastHolder;
	}

	void Update () 
	{
		if (toasts.Count > 0)
		{
			foreach (var toast in toasts)
			{
				toast.secondsRemaining -= Time.deltaTime;
			}

			if (toasts.Peek().secondsRemaining <= 0)
			{
				var toast = toasts.Dequeue();
				Destroy(toast.gameObject);
			}
		}
	}

	public static void AddToast(float seconds, string text)
	{
		if (holder != null)
		{
			var toast = Instantiate(prefab);
			toast.transform.SetParent(holder.transform, false);
			toast.GetComponentInChildren<Text>().text = text;

			toasts.Enqueue(new Toast
			{
				secondsRemaining = seconds,
				text = text,
				gameObject = toast
			});
		}
		else
		{
			Debug.Log("Tried to create a toast, but there is no toast holder in the scene");
		}
	}
}
