using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

//NOTE(Simon): If you change something here, update SaveFile and SaveFileDataCompat as well
public class SaveFileData
{
	public Metadata meta;
	public List<InteractionPointSerialize> points = new List<InteractionPointSerialize>();

	public SaveFileData()
	{
		meta = new Metadata();
	}
}

//NOTE(Simon): This is a compatibility structure. The idea is to keep all data in here that has ever been in SaveFileData.
//NOTE(Simon): SaveFileData converts from this to the current version.
public class SaveFileDataCompat
{
	public MetaDataCompat meta;
	public List<InteractionPointSerializeCompat> points = new List<InteractionPointSerializeCompat>();

	public SaveFileDataCompat()
	{
		meta = new MetaDataCompat();
	}
}

//NOTE(Simon): This is a compatibility structure. The idea is to keep all data in here that has ever been in MetaData.
//NOTE(Simon): MetaData converts from this to the current version.
[Serializable]
public class MetaDataCompat
{
	public int version;
	public string title;
	public string description;
	public Guid guid;
	public float length;
}

//NOTE(Simon): This is a compatibility structure. The idea is to keep all data in here that has ever been in InteractionPointSerialize.
//NOTE(Simon): InteractionPointSerialize converts from this to the current version.
[Serializable]
public class InteractionPointSerializeCompat
{
	public InteractionType type;
	public string title;
	public string body;
	public string filename;
	public double startTime;
	public double endTime;
	public int tagId;
	public bool mandatory;

	public Vector3 returnRayOrigin;
	public Vector3 returnRayDirection;
}

public static class SaveFile
{
	public const int VERSION = 4;

	public static string metaFilename = "meta.json";
	public static string videoFilename = "main.mp4";
	public static string thumbFilename = "thumb.jpg";
	public static string tagsFilename = "tags.json";
	public static string chaptersFilename = "chapters.json";
	public static string editableFilename = ".editable";
	public static string extraPath = "extra";
	public static string miniaturesPath = "areaMiniatures";


	public static string GetMetaContents(string projectFolder)
	{
		string str;
		using (var fileContents = File.OpenText(Path.Combine(projectFolder, metaFilename)))
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

	public static string GetPathForTitle(string title)
	{
		var dirs = new DirectoryInfo(Application.persistentDataPath).GetDirectories();

		foreach (var dir in dirs)
		{
			if (File.Exists(Path.Combine(dir.FullName, editableFilename)))
			{
				var metaPath = Path.Combine(dir.FullName, metaFilename);
				if (File.Exists(metaPath) && new FileInfo(metaPath).Length > 0)
				{
					var meta = OpenFile(dir.FullName).meta;
					if (meta.title == title)
					{
						return dir.FullName;
					}
				}
			}
		}

		return null;
	}

	public static SaveFileData OpenFile(string projectFolder)
	{
		var raw = GetMetaContents(projectFolder);

		var result = JsonGetValueFromLine(raw, 0);
		int fileVersion = Convert.ToInt32(result.value, CultureInfo.InvariantCulture);

		if (fileVersion < VERSION)
		{
			var saveFileDataCompat = ParseForVersion(raw, fileVersion);
			var currentSaveFile = ConvertCompatToCurrent(saveFileDataCompat);
			WriteFile(projectFolder, currentSaveFile);
		}

		var saveFileData = new SaveFileData();

		raw = GetMetaContents(projectFolder);
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

	public static List<Tag> ReadTags(string projectFolder)
	{
		string tagsname = Path.Combine(projectFolder, tagsFilename);

		List<Tag> tags;

		if (File.Exists(tagsname))
		{
			try
			{
				using (var fileContents = File.OpenText(tagsname))
				{
					string raw = fileContents.ReadToEnd();
					tags = new List<Tag>(JsonHelper.ToArray<Tag>(raw));
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
				return null;
			}
		}
		else
		{
			tags = TagManager.defaultTags;
		}

		return tags;
	}

	public static List<Chapter> ReadChapters(string projectFolder)
	{
		string chaptersname = Path.Combine(projectFolder, chaptersFilename);

		List<Chapter> chapters;

		if (File.Exists(chaptersname))
		{
			try
			{
				using (var fileContents = File.OpenText(chaptersname))
				{
					string raw = fileContents.ReadToEnd();
					chapters = new List<Chapter>(JsonHelper.ToArray<Chapter>(raw));
				}
			}
			catch (Exception e)
			{
				Debug.LogError(e.ToString());
				return null;
			}
		}
		else
		{
			chapters = new List<Chapter>();
		}

		return chapters;
	}

	public static bool WriteFile(SaveFileData data)
	{
		var meta = data.meta;

		var path = GetPathForTitle(meta.title);

		return WriteFile(path, data);
	}

	public static bool WriteFile(string projectFolder, SaveFileData data)
	{
		var sb = new StringBuilder();
		var meta = data.meta;

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
			string jsonname = Path.Combine(projectFolder, metaFilename);
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

	public static bool WriteTags(string projectFolder, List<Tag> tags)
	{
		try
		{
			string tagsname = Path.Combine(projectFolder, tagsFilename);
			using (var file = File.CreateText(tagsname))
			{
				file.Write(JsonHelper.ToJson(tags.ToArray()));
			}
		}
		catch (Exception e)
		{
			Debug.LogError(e.ToString());
			return false;
		}

		return true;
	}

	public static bool WriteChapters(string projectFolder, List<Chapter> chapters)
	{
		try
		{
			string tagsname = Path.Combine(projectFolder, chaptersFilename);
			using (var file = File.CreateText(tagsname))
			{
				file.Write(JsonHelper.ToJson(chapters.ToArray()));
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

	//NOTE(Simon): Parses the input version to the compat version
	public static SaveFileDataCompat ParseForVersion(string raw, int version)
	{
		switch (version)
		{
			case 3: return ParseToCompatV3toV4(raw);
			case 4: return ParseToCompatV3toV4(raw);
			default: throw new IndexOutOfRangeException("This save file is deprecated");
		}
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


	//NOTE(Simon): These function parse a specific version to the current Compat class.
	//NOTE(Simon): Add more of these when changing the savefile version.
	#region Upgraders
	public static SaveFileDataCompat ParseToCompatV3toV4(string raw)
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
	#endregion
}
