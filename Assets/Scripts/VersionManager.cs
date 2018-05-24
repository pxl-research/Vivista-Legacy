//#define DEBUG_VERSION

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;


[Serializable]
public class MetaDataCompat
{
	public int version;
	public string title;
	public string description;
	public Guid guid;
	public Perspective perspective;
	public float length;
}

[Serializable]
public class InteractionpointSerializeCompat
{
	public Vector3 position;
	public Quaternion rotation;
	public InteractionType type;
	public string title;
	public string body;
	public string filename;
	public double startTime;
	public double endTime;
	public Vector3 returnRayOrigin;
	public Vector3 returnRayDirection;
}

[Serializable]
public class InteractionPointLatest
{
	public InteractionType type;
	public string title;
	public string body;
	public string filename;
	public double startTime;
	public double endTime;
	public Vector3 returnRayOrigin;
	public Vector3 returnRayDirection;

	public InteractionPointLatest(InteractionpointSerializeCompat pointCompat)
	{
		type = pointCompat.type;
		title = pointCompat.title;
		body = pointCompat.body;
		filename = pointCompat.filename;
		startTime = pointCompat.startTime;
		endTime = pointCompat.endTime;
		returnRayOrigin = pointCompat.returnRayOrigin;
		returnRayDirection = pointCompat.returnRayDirection;
	}
}

public class VersionManager
{
	public static bool isUpdated;
	public const int VERSION = 3;

	public static string CheckAndUpgradeVersion(string jsonString)
	{
		var metaCompat = new MetaDataCompat();
		var pointList = new List<InteractionpointSerializeCompat>();

		//NOTE(Kristof): First check if the json contains a version
		if (!jsonString.StartsWith("version:"))
		{
			metaCompat.version = 0;
		}
		else
		{
			var result = SaveFile.JsonGetValueFromLine(jsonString, 0);
			metaCompat.version = Convert.ToInt32(result.value);

			if (metaCompat.version == VERSION)
			{
				return jsonString;
			}
		}

		//NOTE(Kristof): Build objects from json
		{
			var result = new SaveFile.ParsedJsonLine();

			if (metaCompat.version > 0)
			{
				result = SaveFile.JsonGetValueFromLine(jsonString, result.endindex);
				metaCompat.version = Convert.ToInt32(result.value);
			}

			result = SaveFile.JsonGetValueFromLine(jsonString, result.endindex);
			metaCompat.guid = new Guid(result.value);

			result = SaveFile.JsonGetValueFromLine(jsonString, result.endindex);
			metaCompat.title = result.value;

			result = SaveFile.JsonGetValueFromLine(jsonString, result.endindex);
			metaCompat.description = result.value;

			if (metaCompat.version < 2)
			{
				result = SaveFile.JsonGetValueFromLine(jsonString, result.endindex);
				metaCompat.perspective = (Perspective)Enum.Parse(typeof(Perspective), result.value);
			}

			result = SaveFile.JsonGetValueFromLine(jsonString, result.endindex);
			metaCompat.length = Convert.ToSingle(result.value);

			foreach (var obj in SaveFile.ParseInteractionPoints(jsonString, result.endindex))
			{
				pointList.Add(JsonUtility.FromJson<InteractionpointSerializeCompat>(obj));
			}
		}

		//NOTE(Kristof): Json gets updated with changes if not latest version
		isUpdated = UpgradeSaveFile(metaCompat, pointList, ref jsonString);

		//NOTE(Kristof): Pass back json, with any potentional changes
		return jsonString;
	}

	private static bool UpgradeSaveFile(MetaDataCompat meta, List<InteractionpointSerializeCompat> points, ref string json)
	{
		//NOTE(Kristof): Pass meta and points byref in case changes need to happen (meta.version gets upgraded every function if outdated)
		var updated = Upgrade0To1(meta, points);
		updated = Upgrade1To2(meta, points);
		updated = Upgrade2To3(meta, points);

		//NOTE(Kristof): Update json only if changes were made
		if (updated)
		{
			json = WriteUpgraded(meta, points);
		}

		return updated;
	}

	/// <summary>
	/// This version adds a version at the start of meta.json
	/// 
	/// Direction and rotation are no longer stored in meta.json.
	/// </summary>
	private static bool Upgrade0To1(MetaDataCompat meta, List<InteractionpointSerializeCompat> points)
	{
		//NOTE(Kristof): Check if we're indeed dealing with a version 0 json
		if (meta.version != 0)
		{
			return false;
		}

		//NOTE(Kristof): Set version to version 1
		meta.version = 1;

#if DEBUG_VERSION
		return false;
#else
		return true;
#endif
	}

	/// <summary>
	/// This version added VideoInteractions. 
	/// 
	/// It introduced an "extra" directory where all the extra files are stored by a GUID. 
	/// Perspectives are no longer stored in meta.json.
	/// </summary>
	private static bool Upgrade1To2(MetaDataCompat meta, List<InteractionpointSerializeCompat> points)
	{
		//NOTE(Kristof): Check if we're indeed dealing with a version 1 json
		if (meta.version != 1)
		{
			return false;
		}

		//NOTE(Kristof): Set version to version 2
		meta.version = 2;
		
		//NOTE(Kristof): Create variables for directory paths
		var projectDir = Path.Combine(Application.persistentDataPath, meta.guid.ToString());
		var extraDir = Path.Combine(projectDir, SaveFile.extraPath);
		//NOTE(Kristof): Dictionary is used to keep track of duplicate references to extras in the json 
		var newExtraDictionary = new Dictionary<string,string>();

		Directory.CreateDirectory(extraDir);

		//NOTE(Kristof): Iterate over all points and move extra files to the newly created "extra" directory. Files are renamed to a guid. 
		foreach (var point in points)
		{
			if (newExtraDictionary.ContainsKey(point.filename))
			{
				point.filename = newExtraDictionary[point.filename];
			}
			else if (!point.filename.Equals(""))
			{
				var newFilename = Path.Combine(SaveFile.extraPath, Editor.GenerateExtraGuid());
				newExtraDictionary.Add(point.filename, newFilename);
#if !DEBUG_VERSION
				File.Move(Path.Combine(projectDir, point.filename), Path.Combine(projectDir, newFilename));
#endif
				point.filename = newFilename;
			}
		}

#if !DEBUG_VERSION
		//NOTE(Kristof): Clean up leftover files
		foreach (var file in Directory.GetFiles(projectDir, "extra*"))
		{
			File.Delete(file);
		}
#endif

#if DEBUG_VERSION
		return false;
#else
		return true;
#endif
	}

	/// <summary>
	/// This version adds Multiplechoice Interaction
	/// </summary>
	private static bool Upgrade2To3(MetaDataCompat meta, List<InteractionpointSerializeCompat> points)
	{
		//NOTE(Kristof): Check if we're indeed dealing with a version 0 json
		if (meta.version != 2)
		{
			return false;
		}

		//NOTE(Kristof): Set version to version 1
		meta.version = 3;

#if DEBUG_VERSION
		return false;
#else
		return true;
#endif
	}

	private static string WriteUpgraded(MetaDataCompat meta, List<InteractionpointSerializeCompat> points)
	{
		//NOTE(Kristof): Build and return the updated string
		var sb = new StringBuilder();
		sb.Append("version:")
			.Append(meta.version)
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

		//NOTE(Kristof): Parse interactinpoints
		if (points.Count > 0)
		{
			foreach (var point in points)
			{
				var updatedPoint = new InteractionPointLatest(point);
				sb.Append(JsonUtility.ToJson(updatedPoint, true));
				sb.Append(",");
			}

			sb.Remove(sb.Length - 1, 1);
			sb.Append("]");
		}
		else
		{
			sb.Append("[]]");
		}
		
		return sb.ToString();
	}
}