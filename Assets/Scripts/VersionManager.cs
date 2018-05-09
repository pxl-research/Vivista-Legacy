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
	public const int VERSION = 2;

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

			result = SaveFile.JsonGetValueFromLine(jsonString, result.endindex);
			metaCompat.perspective = (Perspective) Enum.Parse(typeof(Perspective), result.value);

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

		//NOTE(Kristof): Update json only if changes were made
		if (updated)
		{
			json = WriteUpgraded(meta, points);
		}

		return updated;
	}

	private static bool Upgrade0To1(MetaDataCompat meta, List<InteractionpointSerializeCompat> points)
	{
		//NOTE(Kristof): Check if we're indeed dealing with a version 0 json
		if (meta.version != 0)
		{
			return false;
		}

		//NOTE(Kristof: Increment version by one to version 1
		meta.version++;
		return true;
	}

	private static bool Upgrade1To2(MetaDataCompat meta, List<InteractionpointSerializeCompat> points)
	{

		if (meta.version != 1)
		{
			return false;
		}

		var path = Path.combine(meta.guid, SaveFile.extraPath);
		Directory.CreateDirectory(path);

		return true;
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

		sb.Append("perspective:")
			.Append(meta.perspective)
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