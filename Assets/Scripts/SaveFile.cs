using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveFile
{
	public static string metaFilename = "meta.json";
	public static string videoFilename = "main.mp4";
	public static string thumbFilename = "thumb.jpg";
	public static string extraPath = "extra";

	public static string GetSaveFileContents(string path)
	{
		string str;
		using (var fileContents = File.OpenText(path))
		{
			try
			{
				str = fileContents.ReadToEnd();
			}
			catch (Exception e)
			{
				Debug.Log("Something went wrong while loading the file.");
				Debug.Log(e.ToString());
				return "";
			}
		}

		return str;
	}

	public static byte[] GetSaveFileContentsBinary(string path)
	{
		byte[] data;
		using (var fileContents = File.OpenRead(path))
		{
			try
			{
				data = new byte[(int)fileContents.Length];
				fileContents.Read(data, 0, (int)fileContents.Length);
			}
			catch (Exception e)
			{
				Debug.Log("Something went wrong while loading the file.");
				Debug.Log(e.ToString());
				return new byte[0];
			}
		}

		return data;
	}

	public class SaveFileData
	{
		public Metadata meta;
		public List<InteractionpointSerialize> points = new List<InteractionpointSerialize>();
	}

	public static Dictionary<string, Guid> GetAllSavefileNames()
	{
		var dirs = new DirectoryInfo(Application.persistentDataPath).GetDirectories();
		var dirNames = new Dictionary<string, Guid>();
		foreach (var dir in dirs)
		{
			if (File.Exists(Path.Combine(dir.FullName, ".editable")))
			{
				var metaPath = Path.Combine(dir.FullName, metaFilename);
				if (new FileInfo(metaPath).Length > 0)
				{
					var meta = OpenFile(metaPath).meta;
					dirNames.Add(meta.title, meta.guid);
				}
			}
		}

		return dirNames;
	}

	public static string GetPathForTitle(string title)
	{
		var dirs = new DirectoryInfo(Application.persistentDataPath).GetDirectories();

		foreach (var dir in dirs)
		{
			if (File.Exists(Path.Combine(dir.FullName, ".editable")))
			{
				var metaPath = Path.Combine(dir.FullName, metaFilename);
				if (new FileInfo(metaPath).Length > 0)
				{
					var meta = OpenFile(metaPath).meta;
					if (meta.title == title)
					{
						return dir.FullName;
					}
				}
			}
		}

		return null;
	}

	/*
	public static List<string> GetExtraFiles(string metaFileName)
	{
		var str = GetSaveFileContents(metaFileName);
	}
	*/
	public static SaveFileData OpenFile(string path)
	{
		var str = GetSaveFileContents(path);

		str = VersionManager.CheckAndUpgradeVersion(str);

		var saveFileData = new SaveFileData();

		var result = new ParsedJsonLine();

		result = JsonGetValueFromLine(str, result.endindex);
		saveFileData.meta.version = Convert.ToInt32(result.value);

		result = JsonGetValueFromLine(str, result.endindex);
		saveFileData.meta.guid = new Guid(result.value);

		result = JsonGetValueFromLine(str, result.endindex);
		saveFileData.meta.title = result.value;

		result = JsonGetValueFromLine(str, result.endindex);
		saveFileData.meta.description = result.value;

		result = JsonGetValueFromLine(str, result.endindex);
		saveFileData.meta.length = Convert.ToSingle(result.value);

		saveFileData.points = new List<InteractionpointSerialize>();

		foreach (var obj in ParseInteractionPoints(str, result.endindex))
		{
			saveFileData.points.Add(JsonUtility.FromJson<InteractionpointSerialize>(obj));
		}

		if (VersionManager.isUpdated)
		{
			try
			{
				using (var file = File.CreateText(path))
				{
					file.Write(str);
				}
			}
			catch (Exception e)
			{
				Debug.Log(e.ToString());
			}
		}

		return saveFileData;
	}

	public static List<string> ParseInteractionPoints(string str, int startIndex)
	{
		var stringObjects = new List<string>();
		var level = 0;
		var start = 0;
		var count = 0;
		var rising = true;

		for (var i = startIndex; i < str.Length; i++)
		{
			if (str[i] == '{')
			{
				if (level == 0)
				{
					start = i;
				}
				rising = true;
				level++;
			}
			if (str[i] == '}')
			{
				level--;
				rising = false;
			}

			count++;

			if (level == 0 && !rising)
			{
				stringObjects.Add(str.Substring(start, count - 1));
				count = 0;
				rising = true;
			}
			if (level < 0)
			{
				Debug.Log("Corrupted save file. Aborting");
				return null;
			}
		}

		return stringObjects;
	}

	public class ParsedJsonLine
	{
		public string value;
		public int endindex;
	}

	public static ParsedJsonLine JsonGetValueFromLine(string json, int startIndex)
	{
		var startValue = json.IndexOf(':', startIndex) + 1;
		var endValue = json.IndexOf('\n', startIndex) + 1;
		return new ParsedJsonLine
		{
			value = json.Substring(startValue, (endValue - startValue) - 2),
			endindex = endValue
		};
	}

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
}
