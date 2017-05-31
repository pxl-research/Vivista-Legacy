using UnityEngine;

public static class WWWExtensions 
{
	public static int StatusCode(this WWW www)
	{
		return int.Parse(www.responseHeaders["STATUS"].Split(' ')[1]);
	}

	public static string StatusMessage(this WWW www)
	{
		return www.responseHeaders["STATUS"].Split(' ')[2];
	}
}
