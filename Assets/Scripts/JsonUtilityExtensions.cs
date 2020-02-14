using System;
using UnityEngine;

public class JsonHelper
{
	public static T[] ToArray<T>(string json)
	{
		Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>> (json);
		return wrapper.array;
	}

	public static string ToJson<T>(T[] array)
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
