using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Toast
{
	public float secondsRemaining;
	public string text;
	public GameObject gameObject;
}

//NOTE(Simon): Handles Toasts. Scene should contain a panel in which toasts can appear. A prefab of such a toast should also be assigned.
public class Toasts : MonoBehaviour 
{
	public RectTransform toastHolder;
	public GameObject toastPrefab;

	private static GameObject prefab;
	private static RectTransform holder;
	private static Queue<Toast> toasts = new Queue<Toast>();
	private static Queue<Toast> newToasts = new Queue<Toast>();
	
	void Start()
	{
		prefab = toastPrefab;
		holder = toastHolder;
	}

	void Update () 
	{
		//NOTE(Simon): Decouple toast GO creation from AddToast(), so that AddToast() is thread safe (Instantiate can only be called from main thread)
		while (newToasts.Count > 0)
		{
			var newToast = newToasts.Dequeue();
			var toast = Instantiate(prefab);
			toast.transform.SetParent(holder.transform, false);
			toast.GetComponentInChildren<Text>().text = newToast.text;
			newToast.gameObject = toast;
			toasts.Enqueue(newToast);
		}

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
			newToasts.Enqueue(new Toast
			{
				secondsRemaining = seconds,
				text = text
			});
		}
		else
		{
			Debug.LogError("Tried to create a toast, but there is no toast holder in the scene");
		}
	}
}
