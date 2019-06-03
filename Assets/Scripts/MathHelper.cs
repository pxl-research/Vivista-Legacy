using System;
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
}
