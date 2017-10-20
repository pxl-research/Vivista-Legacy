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

	public static string FormatTimestampToTimeAgo(DateTime timestamp)
	{
		var elapsed = DateTime.Now - timestamp;
		if (elapsed.Days > 365)
		{
			return String.Format("{0} years ago", elapsed.Days / 365);
		}
		if (elapsed.Days > 31)
		{
			return String.Format("{0} months ago", elapsed.Days / 31);
		}
		if (elapsed.Days > 7)
		{
			return String.Format("{0} weeks ago", elapsed.Days / 7);
		}
		if (elapsed.Days > 1)
		{
			return String.Format("{0} days ago", elapsed.Days);
		}
		if (elapsed.Hours > 1)
		{
			return String.Format("{0} hours ago", elapsed.Hours);
		}
		if (elapsed.Minutes > 1)
		{
			return String.Format("{0} minutes ago", elapsed.Minutes);
		}
	
		return "Just now";
	}

	public static string FormatBytes(int bytes)
	{
 		var names = new[] {"B", "kB", "MB", "GB"};
		var magnitude = (int)Mathf.Max(0, Mathf.Floor(Mathf.Log(bytes, 1024)));
		var calculated = bytes / Mathf.Pow(1024f, magnitude);
		var result = String.Format("{0:0.##} {1}", calculated, names[magnitude]);
		return result;
	}
}
