using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

public static class MathHelper
{
	public static string FormatSeconds(double time)
	{
		var hours = (int)(time / (60 * 60));
		time -= hours * (60 * 60);
		var minutes = (int)(time / 60);
		time -= minutes * 60;
		var seconds = (int)time;

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

	public static string FormatBytes(ulong bytes)
	{
		return FormatBytes((long)bytes);
	}

	public static string FormatBytes(long bytes)
	{
		var names = new[] { "B", "kB", "MB", "GB" };
		int magnitude = (int)Mathf.Max(0, Mathf.Floor(Mathf.Log(bytes, 1024)));
		float calculated = bytes / Mathf.Pow(1024f, magnitude);
		string result = $"{calculated:0.##} {names[magnitude]}";
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
		return new Vector2(orignalSize.x / biggestRatio, orignalSize.y / biggestRatio);
	}

	public static float Smoothstep(float edge0, float edge1, float x)
	{
		x = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
		return x * x * (3 - 2 * x);
	}

	public static float Smootherstep(float edge0, float edge1, float x)
	{
		x = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
		return x * x * x * (x * (x * 6 - 15) + 10);
	}

	public static float SmoothPingPong(float x, float length)
	{
		return Smoothstep(0, length, Mathf.PingPong(x, length));
	}

	//NOTE(Simon): Convert a linear(UI) volume value to a logarithmic(mixer) volume value
	public static float LinearToLogVolume(float value)
	{
		return Mathf.Log10(value) * 20;
	}

	public static void ClipWorldCorners(Vector3[] outerRect, Vector3[] innerRect)
	{
		float minX = outerRect[0].x;
		float maxX = outerRect[3].x;
		float minY = outerRect[0].y;
		float maxY = outerRect[1].y;

		innerRect[0] = new Vector3(Mathf.Clamp(innerRect[0].x, minX, maxX), Mathf.Clamp(innerRect[0].y, minY, maxY));
		innerRect[1] = new Vector3(Mathf.Clamp(innerRect[1].x, minX, maxX), Mathf.Clamp(innerRect[1].y, minY, maxY));
		innerRect[2] = new Vector3(Mathf.Clamp(innerRect[2].x, minX, maxX), Mathf.Clamp(innerRect[2].y, minY, maxY));
		innerRect[3] = new Vector3(Mathf.Clamp(innerRect[3].x, minX, maxX), Mathf.Clamp(innerRect[3].y, minY, maxY));
	}
}

public class UIAnimation
{
	public const float animationLength = 0.25f;

	public static IEnumerator FadeIn(RectTransform transform, CanvasGroup canvas, float length = animationLength, float endOpacity = 1.0f)
	{
		float animTime = 0;
		float scaleOffset = .8f;
		while (animTime < length)
		{
			animTime += Mathf.Clamp01(Time.deltaTime);
			float step = MathHelper.Smootherstep(0, length, animTime);
			float scale = scaleOffset + (step * .2f);
			transform.localScale = new Vector3(scale, scale, 1);
			canvas.alpha = step * endOpacity;
			yield return new WaitForEndOfFrame();
		}
	}

	public static IEnumerator FadeOut(RectTransform transform, CanvasGroup canvas, float length = animationLength)
	{
		float startOpacity = canvas.alpha;
		float animTime = 0;
		float scaleOffset = .8f;
		while (animTime < length)
		{
			animTime += Mathf.Clamp01(Time.deltaTime);
			float step = MathHelper.Smootherstep(0, length, length - animTime);
			float scale = scaleOffset + (step * .2f);
			transform.localScale = new Vector3(scale, scale, 1);
			canvas.alpha = step * startOpacity;
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
#elif UNITY_STANDALONE_OSX
		//NOTE(Simon): If path is a file we need to include the "-R" parameter
		if (Directory.Exists(path))
		{
			Process.Start("open", $"\"{path}\"");
		}
		else if (File.Exists(path))
		{
			Process.Start("open", $"-R \"{path}\"");
		}
#elif UNITY_STANDALONE_LINUX
		Process.Start("xdg-open", path);
#else
#error Function not defined for this platform
#endif
	}
}

public static class GameObjectHelper
{
	public static T GetOrAddComponent<T>(this GameObject go) where T : Component
	{
		var component = go.GetComponent<T>();
		if (component == null)
		{
			component = go.AddComponent<T>();
		}

		return component;
	}

	public static T GetComponentInChildren<T>(this GameObject go, bool excludeSelf)
	{
		if (excludeSelf)
		{

			var components = new HashSet<T>(go.GetComponentsInChildren<T>());

			var componentInSelf = go.GetComponent<T>();

			components.Remove(componentInSelf);

			return components.ToArray()[0];
		}
		else
		{
			return go.GetComponentInChildren<T>();
		}
	}
}

public static class GuidHelpers
{
	public static string Encode(this Guid guid)
	{
		string encoded = Convert.ToBase64String(guid.ToByteArray());
		encoded = encoded.Replace("/", "_").Replace("+", "-");
		return encoded.Substring(0, 22);
	}

	public static bool TryDecode(string value, out Guid guid)
	{
		value = value.Replace("_", "/").Replace("-", "+");
		byte[] buffer = Convert.FromBase64String(value + "==");
		try
		{
			guid = new Guid(buffer);
			return true;
		}
		catch
		{
			guid = new Guid();
			return false;
		}
	}
}

public static class FileHelpers
{
	public static long DirectorySize(DirectoryInfo directory)
	{
		long size = 0;
		var files = directory.GetFiles();

		foreach (var file in files)
		{
			size += file.Length;
		}

		var subDirectories = directory.GetDirectories();

		foreach (var sub in subDirectories)
		{
			size += DirectorySize(sub);
		}
		return size;
	}

	public static long FileSize(string path)
	{
		return new FileInfo(path).Length;
	}

	public static string GetDriveName(string path)
	{
		var drive = new DriveInfo(path);

		if (String.IsNullOrEmpty(drive.VolumeLabel) || drive.VolumeLabel == drive.Name)
		{
			return $"Local Disk ({path})";
		}
		else
		{
			return $"{drive.VolumeLabel} ({path})";
		}
	}
}