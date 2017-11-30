using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

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

		if (File.Exists(loginDataPath))
		{
			using (var file = File.OpenText(loginDataPath))
			{
				loginUsername.text = file.ReadLine();
				loginPassword.text = file.ReadLine();
				loginRemember.isOn = true;
			}
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

		var form = new WWWForm();
		form.AddField("username", username);
		form.AddField("password", password);
		//form.headers.Add("Content-Type", "application/x-www-form-urlencoded");

		using (var www = new WWW("http://localhost/login", form))
		{

			while (!www.isDone) { }

			var status = www.StatusCode();
			if (status == 401)
			{
				loginError.text = "Username does not exist, or password is wrong";
				return;
			}
			if (status != 200)
			{
				loginError.text = "An error happened in the server. Please try again later";
				return;
			}
			if (!String.IsNullOrEmpty(www.error))
			{
				loginError.text = www.error;
				return;
			}

			answered = true;
			answerToken = www.text;

			Toasts.AddToast(5, "Logged in");
		}
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

		using (var www = new WWW("http://localhost/register", form))
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
}
