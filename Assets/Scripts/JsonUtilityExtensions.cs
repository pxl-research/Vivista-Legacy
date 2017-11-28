using System;
using UnityEngine;

public class JsonHelper
{
	public static T[] ToArray<T>(string json)
	{
		string newJson = "{ \"array\": " + json + "}";
		Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>> (newJson);
		return wrapper.array;
	}

	public static string FromArray<T>(T[] array)
	{
		Wrapper<T> wrapper = new Wrapper<T> {array = array};
		return JsonUtility.ToJson (wrapper);
	}
 
	[Serializable]
	private class Wrapper<T>
	{
		public T[] array;
	}
}
