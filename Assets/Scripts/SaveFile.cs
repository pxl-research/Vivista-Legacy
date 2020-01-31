using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class SaveFileData
{
	public Metadata meta;
	public List<InteractionPointSerialize> points = new List<InteractionPointSerialize>();

	public SaveFileData()
	{
		meta = new Metadata();
	}
}

public class SaveFileDataCompat
{
	public MetaDataCompat meta;
	public List<InteractionPointSerializeCompat> points = new List<InteractionPointSerializeCompat>();

	public SaveFileDataCompat()
	{
		meta = new MetaDataCompat();
	}
}

[Serializable]
public class MetaDataCompat
{
	public int version;
	public string title;
	public string description;
	public Guid guid;
	public float length;
}

[Serializable]
public class InteractionPointSerializeCompat
{
	public InteractionType type;
	public string title;
	public string body;
	public string filename;
	public double startTime;
	public double endTime;
	public Vector3 returnRayOrigin;
	public Vector3 returnRayDirection;
}

public static class SaveFile
{
	public const int VERSION = 3;

	public static string metaFilename = "meta.json";
	public static string videoFilename = "main.mp4";
	public static string thumbFilename = "thumb.jpg";
	public static string extraPath = "extra";

	public static string GetSaveFileContents(string path)
	{
		string str;
		using (var fileContents = File.OpenText(path))
		{
			str = fileContents.ReadToEnd();
		}

		return str;
	}

	public static byte[] GetSaveFileContentsBinary(string path)
	{
		byte[] data;
		using (var fileContents = File.OpenRead(path))
		{
			data = new byte[(int)fileContents.Length];
			fileContents.Read(data, 0, (int)fileContents.Length);
		}

		return data;
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
				if (File.Exists(metaPath) && new FileInfo(metaPath).Length > 0)
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

	public static SaveFileData OpenFile(string path)
	{
		var raw = GetSaveFileContents(path);

		var result = JsonGetValueFromLine(raw, 0);
		int fileVersion = Convert.ToInt32(result.value, CultureInfo.InvariantCulture);

		if (fileVersion < VERSION)
		{
			var saveFileDataCompat = ParseForVersion(raw, fileVersion);
			UpgradeSaveFileToCurrent(saveFileDataCompat);
			var currentSaveFile = ConvertCompatToCurrent(saveFileDataCompat);
			WriteFile(currentSaveFile);
		}

		var saveFileData = new SaveFileData();

		raw = GetSaveFileContents(path);
		result = JsonGetValueFromLine(raw, 0);
		saveFileData.meta.version = Convert.ToInt32(result.value, CultureInfo.InvariantCulture);

		result = JsonGetValueFromLine(raw, result.endindex);
		saveFileData.meta.guid = new Guid(result.value);

		result = JsonGetValueFromLine(raw, result.endindex);
		saveFileData.meta.title = result.value;

		result = JsonGetValueFromLine(raw, result.endindex);
		saveFileData.meta.description = result.value;

		result = JsonGetValueFromLine(raw, result.endindex);
		saveFileData.meta.length = Convert.ToSingle(result.value, CultureInfo.InvariantCulture);

		saveFileData.points = new List<InteractionPointSerialize>();

		foreach (var obj in ParseInteractionPoints(raw, result.endindex))
		{
			saveFileData.points.Add(JsonUtility.FromJson<InteractionPointSerialize>(obj));
		}

		return saveFileData;
	}

	public static bool WriteFile(SaveFileData data)
	{
		var sb = new StringBuilder();
		var meta = data.meta;

		var path = GetPathForTitle(meta.title);
		
		data.meta = meta;

		sb.Append("version:").Append(VERSION)
			.Append(",\n");

		sb.Append("uuid:")
			.Append(meta.guid)
			.Append(",\n");

		sb.Append("title:")
			.Append(meta.title)
			.Append(",\n");

		sb.Append("description:")
			.Append(meta.description)
			.Append(",\n");

		sb.Append("length:")
			.Append(meta.length)
			.Append(",\n");

		sb.Append("[");
		if (data.points.Count > 0)
		{
			foreach (var point in data.points)
			{
				sb.Append(JsonUtility.ToJson(point, true));
				sb.Append(",");
			}

			sb.Remove(sb.Length - 1, 1);
		}
		else
		{
			sb.Append("[]");
		}

		sb.Append("]");

		try
		{
			string jsonname = Path.Combine(path, SaveFile.metaFilename);
			using (var file = File.CreateText(jsonname))
			{
				file.Write(sb.ToString());
			}
		}
		catch (Exception e)
		{
			Debug.LogError(e.ToString());
			return false;
		}

		return true;
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
				Debug.LogError("Corrupted save file. Aborting");
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

	public static SaveFileDataCompat ParseForVersion(string raw, int version)
	{
		switch (version)
		{
			case 3: return ParseToCompatV3(raw);
			default: throw new IndexOutOfRangeException("This save file is deprecated");
		}
	}

	public static SaveFileDataCompat UpgradeSaveFileToCurrent(SaveFileDataCompat data)
	{
		//NOTE(Simon): Add versions any time savefile format is changed, see example:
		//if (data.meta.version == 0) data = Upgrade0To1(data);

		return data;
	}

	public static SaveFileData ConvertCompatToCurrent(SaveFileDataCompat compatData)
	{
		var currentData = new SaveFileData();
		currentData.meta = Metadata.FromCompat(compatData.meta);
		foreach (var pointCompat in compatData.points)
		{
			currentData.points.Add(InteractionPointSerialize.FromCompat(pointCompat));
		}
		return currentData;
	}

	public static SaveFileDataCompat ParseToCompatV3(string raw)
	{
		var saveFileData = new SaveFileDataCompat();

		var result = new ParsedJsonLine();

		result = JsonGetValueFromLine(raw, result.endindex);
		saveFileData.meta.version = Convert.ToInt32(result.value, CultureInfo.InvariantCulture);

		result = JsonGetValueFromLine(raw, result.endindex);
		saveFileData.meta.guid = new Guid(result.value);

		result = JsonGetValueFromLine(raw, result.endindex);
		saveFileData.meta.title = result.value;

		result = JsonGetValueFromLine(raw, result.endindex);
		saveFileData.meta.description = result.value;

		result = JsonGetValueFromLine(raw, result.endindex);
		saveFileData.meta.length = Convert.ToSingle(result.value, CultureInfo.InvariantCulture);

		saveFileData.points = new List<InteractionPointSerializeCompat>();

		foreach (var obj in ParseInteractionPoints(raw, result.endindex))
		{
			saveFileData.points.Add(JsonUtility.FromJson<InteractionPointSerializeCompat>(obj));
		}
		return saveFileData;
	}

}
