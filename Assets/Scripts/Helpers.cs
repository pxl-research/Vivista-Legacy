using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public static class MathHelper
{
	public static string FormatSeconds(double time)
	{
		var hours = (int)(time / (60 * 60));
		time -= hours * (60 * 60);
		var minutes = (int)(time / 60);
		time -= minutes * 60;
		var seconds = (int) time;

		var formatted = "";
		if (hours > 0)
		{
			formatted += hours + ":";
		}

		formatted += minutes.ToString("D2");
		formatted += ":";
		formatted += seconds.ToString("D2");

		return formatted;
	}

	public static string FormatMillis(double time)
	{
		var hours = (int)(time / (60 * 60));
		time -= hours * (60 * 60);
		var minutes = (int)(time / 60);
		time -= minutes * 60;
		var seconds = (int)time;
		time -= seconds;
		var millis = (int)(time * 100);

		var formatted = "";
		if (hours > 0)
		{
			formatted += hours + ":";
		}

		formatted += minutes.ToString("D2");
		formatted += ":";
		formatted += seconds.ToString("D2");
		formatted += ":";
		formatted += millis.ToString("D2");

		return formatted;
	}

	public static string FormatTimestampToTimeAgo(DateTime timestamp)
	{
		var elapsed = DateTime.Now - timestamp;
		if (elapsed.Days > 365)
		{
			return $"{elapsed.Days / 365} years ago";
		}
		if (elapsed.Days > 31)
		{
			return $"{elapsed.Days / 31} months ago";
		}
		if (elapsed.Days > 7)
		{
			return $"{elapsed.Days / 7} weeks ago";
		}
		if (elapsed.Days > 1)
		{
			return $"{elapsed.Days} days ago";
		}
		if (elapsed.Hours > 1)
		{
			return $"{elapsed.Hours} hours ago";
		}
		if (elapsed.Minutes > 1)
		{
			return $"{elapsed.Minutes} minutes ago";
		}
	
		return "Just now";
	}

	public static string FormatBytes(long bytes)
	{
 		var names = new[] {"B", "kB", "MB", "GB"};
		var magnitude = (int)Mathf.Max(0, Mathf.Floor(Mathf.Log(bytes, 1024)));
		var calculated = bytes / Mathf.Pow(1024f, magnitude);
		var result = $"{calculated:0.##} {names[magnitude]}";
		return result;
	}

	public static Ray ReverseRay(this Ray ray, float desiredLength = 100f)
	{
		var newRay = ray;
		newRay.origin = ray.GetPoint(desiredLength);
		newRay.direction = -ray.direction;
		return newRay;
	}

	public static Vector2 ScaleRatio(Vector2 orignalSize, Vector2 targetSize)
	{
		float widthRatio = orignalSize.x / targetSize.x;
		float heightRatio = orignalSize.y / targetSize.y;
		float biggestRatio = Mathf.Max(heightRatio, widthRatio);
		return new Vector2(orignalSize.x / biggestRatio, orignalSize.y/ biggestRatio);
	}

	public static float smoothstep(float edge0, float edge1, float x)
	{
		x = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
		return x * x * (3 - 2 * x);
	}

	public static float smootherstep(float edge0, float edge1, float x)
	{
		x = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
		return x * x * x * (x * (x * 6 - 15) + 10);
	}

	//NOTE(Simon): Convert a linear(wrong) volume value to a logarithmic(correct) volume value
	public static float LinearToLogVolume(float value)
	{
		return Mathf.Log10(value) * 20;
	}
}

public class UIAnimation
{
	public static float animationLength = 0.25f;

	public static IEnumerator FadeIn(RectTransform transform, CanvasGroup canvas)
	{
		float animTime = 0;
		float scaleOffset = .8f;
		while (animTime < animationLength)
		{
			animTime += Mathf.Clamp01(Time.deltaTime);
			float step = MathHelper.smootherstep(0, animationLength, animTime);
			float scale = scaleOffset + (step * .2f);
			transform.localScale = new Vector3(scale, scale, 1);
			canvas.alpha = step;
			yield return new WaitForEndOfFrame();
		}
	}
}

public class JsonHelper
{
	public static T[] ToArray<T>(string json)
	{
		var wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
		return wrapper.array;
	}

	public static string ToJson<T>(T[] array)
	{
		var wrapper = new Wrapper<T> { array = array };
		return JsonUtility.ToJson(wrapper);
	}

	[Serializable]
	private class Wrapper<T>
	{
		public T[] array;
	}
}

public static class ColorHelper
{
	public static Color IdealTextColor(this Color backgroundColor)
	{
		double luma = 0.299 * backgroundColor.r + 0.587 * backgroundColor.g + 0.114 * backgroundColor.b;

		return luma > 0.5 ? Color.black : Color.white;
	}
}

public static class ExplorerHelper
{
	public static void ShowPathInExplorer(string path)
	{
#if UNITY_STANDALONE_WIN
		path = path.Replace('/', '\\');
		Process.Start("explorer.exe", $"/select,\"{path}\"");
#endif

#if UNITY_STANDALONE_OSX
		Process.Start("open", "-R " + path);
#endif

#if UNITY_STANDALONE_LINUX
		Process.Start("xdg-open", path);
#endif
	}
}

public static class GameObjectHelper
{
	public static T GetOrAddComponent<T>(this GameObject go) where T:Component
	{
		var component = go.GetComponent<T>();
		if (component == null)
		{
			component = go.AddComponent<T>();
		}

		return component;
	}
}