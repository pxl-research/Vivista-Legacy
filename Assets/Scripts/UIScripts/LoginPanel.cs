using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LoginDetails
{
	public string username;
	public string password;
}

public class LoginPanel : MonoBehaviour 
{
	public bool answered;
	public string answerToken;

	public InputField loginUsername;
	public InputField loginPassword;
	public Toggle loginRemember;
	public Text loginError;

	public InputField registerUsername;
	public InputField registerPassword;
	public InputField registerRepeatPassword;
	public Text registerError;

	private string loginDataPath;

	public Color errorColor;

	void Start () 
	{
		loginDataPath = Path.Combine(Application.persistentDataPath, "save.dat");

		var loginDetails = GetSavedLogin();

		if (loginDetails != null)
		{
			loginUsername.text = loginDetails.username;
			loginPassword.text = loginDetails.password;
			loginRemember.isOn = true;
		}
	}

	public void Login() 
	{
		var username = loginUsername.text;
		var password = loginPassword.text;

		loginError.color = errorColor;
		if (String.IsNullOrEmpty(username))
		{
			loginError.text = "Please fill in a username";
			return;
		}
		if (String.IsNullOrEmpty(password))
		{
			loginError.text = "Please fill in a password";
			return;
		}

		if (loginRemember.isOn)
		{
			using (var file = File.CreateText(loginDataPath))
			{
				file.WriteLine(username);
				file.WriteLine(password);
			}
		}
		else
		{
			File.Delete(loginDataPath);
		}

		var response = SendLoginRequest(username, password);

		if (response.Item1 == 401)
		{
			loginError.text = "Username does not exist, or password is wrong";
			return;
		}
		if (response.Item1 != 200)
		{
			loginError.text = "An error happened in the server. Please try again later";
			return;
		}

		answered = true;
		answerToken = response.Item2;

		Toasts.AddToast(5, "Logged in");
	}

	public void Register() 
	{
		var username = registerUsername.text;
		var password = registerPassword.text;
		var repeatPassword = registerRepeatPassword.text;

		registerError.color = errorColor;
		if (String.IsNullOrEmpty(username))
		{
			registerError.text = "Please fill in a username";
			return;
		}
		if (String.IsNullOrEmpty(password))
		{
			registerError.text = "Please fill in a password";
			return;
		}
		if (String.IsNullOrEmpty(repeatPassword))
		{
			registerError.text = "Please repeat your password";
			return;
		}
		if (password != repeatPassword)
		{
			registerError.text = "These passwords are not the same";
			return;
		}

		var form = new WWWForm();
		form.AddField("username", username);
		form.AddField("password", password);

		using (var www = new WWW(Web.registerUrl, form))
		{
			while (!www.isDone) { }

			var status = www.StatusCode();
			if (status == 409)
			{
				registerError.text = "This username is already taken.";
				return;
			}
			if (status != 200)
			{
				registerError.text = "An error happened in the server. Please try again later";
				return;
			}
			if (!String.IsNullOrEmpty(www.error))
			{
				registerError.text = www.error;
				return;
			}

			answered = true;
			answerToken = www.text;

			Toasts.AddToast(5, "Registered succesfully");
			Toasts.AddToast(5, "Logged in");
		}
	}

	public static LoginDetails GetSavedLogin()
	{
		var details = new LoginDetails();
		var loginDataPath = Path.Combine(Application.persistentDataPath, "save.dat");

		if (File.Exists(loginDataPath))
		{
			using (var file = File.OpenText(loginDataPath))
			{
				details.username = file.ReadLine();
				details.password = file.ReadLine();
			}
		}
		else
		{
			return null;
		}

		return details;
	}

	//NOTE(Simon): Returns HTTP response code . 200 is good, anything else is bad
	public static Tuple<int, string> SendLoginRequest(string username, string password)
	{
		using (var www = new UnityWebRequest($"{Web.loginUrl}?username={username}&password={password}", "POST", new DownloadHandlerBuffer(), null))
		{
			var request = www.SendWebRequest();
			while (!request.isDone) { }
			return new Tuple<int, string>((int)www.responseCode, www.downloadHandler.text);
		}
	}
}
