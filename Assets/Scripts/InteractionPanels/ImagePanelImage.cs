using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR;

public class ImagePanelImage : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public string url;
	public RawImage image;

	private UnityWebRequest request;
	private bool loaded;
	private static Vector2 defaultImageSize = new Vector2(500, 500);
	private Vector2 originalSize;

	public void SetURL(string URL)
	{
		url = URL;
	}

	public IEnumerator LoadImage()
	{
		if (!String.IsNullOrEmpty(url) && !loaded)
		{
			if (!url.StartsWith("file://"))
			{
				url = "file://" + url;
			}

			using (request = UnityWebRequestTexture.GetTexture(url))
			{
				yield return request.SendWebRequest();

				if (request.result == UnityWebRequest.Result.Success)
				{
					var texture = DownloadHandlerTexture.GetContent(request);
					var newSize = MathHelper.ScaleRatio(new Vector2(texture.width, texture.height), defaultImageSize);

					originalSize = newSize;
					image.rectTransform.sizeDelta = newSize;
					image.texture = texture;
					loaded = true;
				}
				else
				{
					Debug.Log(request.result);
					loaded = false;
				}
			}
		}
	}

	public void SetMaxSize(Vector2 size)
	{
		defaultImageSize = size;
		GetComponent<RectTransform>().sizeDelta = defaultImageSize;
		GetComponentInChildren<RawImage>().rectTransform.sizeDelta = defaultImageSize;
		GetComponentInChildren<RawImage>().rectTransform.anchoredPosition = Vector2.zero;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		var input = FindObjectOfType<SphereUIInputModule>();
		if (input != null)
		{
			if (loaded)
			{
				StartCoroutine("TrackMouse");
				StartCoroutine(AnimateZoom(2, 0.25f));
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		var input = FindObjectOfType<SphereUIInputModule>();
		if (input != null)
		{
			StopCoroutine("TrackMouse");
			StartCoroutine(AnimateZoom(1, 0.25f));
			image.rectTransform.anchoredPosition = Vector2.zero;
		}
	}

	private IEnumerator TrackMouse()
	{
		while (true)
		{
			var ray = GetComponentInParent<GraphicRaycaster>();
			var input = FindObjectOfType<SphereUIInputModule>();

			if (ray != null && input != null)
			{
				while (Application.isPlaying)
				{
					var localPos = new List<Vector2>();

					if (XRSettings.isDeviceActive)
					{
						//NOTE(Simon): Prefers right controller over left
						if (VRDevices.hasRightController)
						{
							localPos.Add(input.positions[SphereUIInputModule.rightControllerId]);
						}
						if (VRDevices.hasLeftController)
						{
							localPos.Add(input.positions[SphereUIInputModule.leftControllerId]);
						}
					}
					else
					{
						localPos.Add(input.ScreenPointToSpherePoint(Input.mousePosition));
					}

					var relativePos = new List<Vector2>();

					foreach (var pos in localPos)
					{
						RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, pos, ray.eventCamera, out var outPos);
						relativePos.Add(outPos);
					}

					foreach(var pos in relativePos)
					{
						if (((RectTransform) transform).rect.Contains(pos))
						{
							image.rectTransform.localPosition = pos;
							break;
						}
					}

					yield return 0;
				}
			}
			else
			{
				Debug.LogWarning("Could not find GraphicRaycaster and/or StandaloneInputModule");
				yield return 0;
			}
		}
	}

	private IEnumerator AnimateZoom(float desiredFactor, float animationLength)
	{
		float currentTime = 0;
		float startFactor = image.rectTransform.sizeDelta.x / originalSize.x;

		while (currentTime < animationLength)
		{
			currentTime += Time.deltaTime;
			currentTime = Math.Min(currentTime, animationLength);

			float currentFactor = Mathf.SmoothStep(startFactor, desiredFactor, currentTime / animationLength);

			image.rectTransform.sizeDelta = originalSize * currentFactor;

			yield return new WaitForEndOfFrame();
		}
	}
}
