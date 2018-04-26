using System;
using System.Collections.Generic;
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

public class VersionManager
{
	public MetaDataCompat metaCompat = new MetaDataCompat();
	public List<InteractionpointSerializeCompat> pointList = new List<InteractionpointSerializeCompat>();
	public bool updated;

	private string json;

	public VersionManager(string jsonString)
	{
		//Note(kristof): Init
		json = jsonString;
		metaCompat.version = 0;
		updated = false;
	}

	public string CheckAndUpgradeVersion()
	{
		//NOTE(Kristof): First check if the json contains a version
		if (!json.StartsWith("version:"))
		{
			metaCompat.version = 0;
		}
		else
		{
			var result = SaveFile.JsonGetValueFromLine(json, 0);
			metaCompat.version = Convert.ToInt32(result.value);
		}

		//NOTE(Kristof): Build objects from json
		{
			var result = new SaveFile.ParsedJsonLine();

			if (metaCompat.version > 0)
			{
				result = SaveFile.JsonGetValueFromLine(json, result.endindex);
				metaCompat.version = Convert.ToInt32(result.value);
			}

			result = SaveFile.JsonGetValueFromLine(json, result.endindex);
			metaCompat.guid = new Guid(result.value);

			result = SaveFile.JsonGetValueFromLine(json, result.endindex);
			metaCompat.title = result.value;

			result = SaveFile.JsonGetValueFromLine(json, result.endindex);
			metaCompat.description = result.value;

			result = SaveFile.JsonGetValueFromLine(json, result.endindex);
			metaCompat.perspective = (Perspective)Enum.Parse(typeof(Perspective), result.value);

			result = SaveFile.JsonGetValueFromLine(json, result.endindex);
			metaCompat.length = Convert.ToSingle(result.value);

			foreach (var obj in SaveFile.ParseInteractionPoints(json, result.endindex))
			{
				pointList.Add(JsonUtility.FromJson<InteractionpointSerializeCompat>(obj));
			}
		}

		//NOTE(Kristof): Json gets updated with changes if not latest version
		UpgradeSaveFile(metaCompat, pointList, ref json, ref updated);

		//NOTE(Kristof): Pass back json, with any potentional changes
		return json;
	}

	private static void UpgradeSaveFile(MetaDataCompat meta, List<InteractionpointSerializeCompat> points, ref string json, ref bool updated)
	{
		//NOTE(Kristof): Pass meta and points byref in case changes need to happen (meta.version gets upgraded every function if outdated)
		var updatedJson = Upgrade0To1(ref meta, ref points, ref updated);

		//NOTE(Kristof): Update json only if changes were made
		if (updated)
		{
			json = updatedJson;
		}
	}

	private static string Upgrade0To1(ref MetaDataCompat meta, ref List<InteractionpointSerializeCompat> points, ref bool updated)
	{
		//NOTE(Kristof): Check if we're indeed dealing with a version 0 json
		if (meta.version != 0)
		{
			return "";
		}

		//NOTE(Kristof: Increment version by one to version 1
		meta.version++;

		//NOTE(Kristof): Create a version 1 meta.json header
		var metaV1 = new
		{
			meta.version,
			meta.guid,
			meta.title,
			meta.description,
			meta.perspective,
			meta.length
		};

		//NOTE(Kristof): Create a list of version one interaction point json strings
		var listV1 = new List<VersionedInteractionPoint.InteractionPointVersion1>();
		foreach (var point in points)
		{
			var pointV1 = new VersionedInteractionPoint.InteractionPointVersion1(point);
			listV1.Add(pointV1);
		}


		//NOTE(Kristof): Build and return the json.meta string
		var sb = new StringBuilder();
		sb.Append("version:")
			.Append(metaV1.version)
			.Append(",\n");

		sb.Append("uuid:")
			.Append(metaV1.guid)
			.Append(",\n");

		sb.Append("title:")
			.Append(metaV1.title)
			.Append(",\n");

		sb.Append("description:")
			.Append(metaV1.description)
			.Append(",\n");

		sb.Append("perspective:")
			.Append(metaV1.perspective)
			.Append(",\n");

		sb.Append("length:")
			.Append(metaV1.length)
			.Append(",\n");

		sb.Append("[");

		foreach (var point in listV1)
		{
			sb.Append(JsonUtility.ToJson(point, true));
			sb.Append(",");
		}

		sb.Remove(sb.Length - 1, 1);
		sb.Append("]");

		updated = true;
		return sb.ToString();
	}
}