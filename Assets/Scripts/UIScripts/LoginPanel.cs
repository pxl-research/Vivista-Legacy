using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LoginDetails
{
	public string email;
	public string password;
}

public class LoginResponse
{
	public string session;
	public string error;
}

public class LoginPanel : MonoBehaviour 
{
	public bool answered;

	public InputField loginEmail;
	public InputField loginPassword;
	public Toggle loginRemember;
	public Text loginError;

	public InputField registerUsername;
	public InputField registerEmail;
	public InputField registerPassword;
	public InputField registerPasswordConfirmation;
	public Text registerError;

	private string loginDataPath;

	public Color errorColor;

	void Start () 
	{
		loginDataPath = Path.Combine(Application.persistentDataPath, "save.dat");

		var loginDetails = GetSavedLogin();

		if (loginDetails != null)
		{
			loginEmail.text = loginDetails.email;
			loginPassword.text = loginDetails.password;
			loginRemember.isOn = true;
		}
	}

	public void Login() 
	{
		var email = loginEmail.text;
		var password = loginPassword.text;

		loginError.color = errorColor;
		if (String.IsNullOrEmpty(email))
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
				file.WriteLine(email);
				file.WriteLine(password);
			}
		}
		else
		{
			File.Delete(loginDataPath);
		}

		var (success, response) = SendLoginRequest(email, password);

		if (!success)
		{
			loginError.text = response;
			return;
		}

		answered = true;

		Toasts.AddToast(5, "Logged in");
	}

	public void Register() 
	{
		var username = registerUsername.text;
		var email = registerEmail.text;
		var password = registerPassword.text;
		var passwordConfirmation = registerPasswordConfirmation.text;

		registerError.color = errorColor;
		if (String.IsNullOrEmpty(username))
		{
			registerError.text = "Please fill in a username";
			return;
		}
		if (String.IsNullOrEmpty(email))
		{
			registerError.text = "Please fill in an email";
			return;
		}
		if (String.IsNullOrEmpty(password))
		{
			registerError.text = "Please fill in a password";
			return;
		}
		if (password.Length < Web.minPassLength)
		{
			registerError.text = $"Password should be at least {Web.minPassLength} characters long";
			return;
		}
		if (String.IsNullOrEmpty(passwordConfirmation))
		{
			registerError.text = "Please repeat your password";
			return;
		}
		if (password != passwordConfirmation)
		{
			registerError.text = "These passwords are not the same";
			return;
		}

		var form = new WWWForm();
		form.AddField("username", username);
		form.AddField("password", password);
		form.AddField("password-confirmation", passwordConfirmation);
		form.AddField("email", email);

		using (var www = UnityWebRequest.Post(Web.registerUrl, form))
		{
			www.SendWebRequest();
			//TODO(Simon): Async??
			while (!www.isDone) { }

			if (www.responseCode != 200)
			{
				registerError.text = www.downloadHandler.text;
				return;
			}
			if (www.isNetworkError || www.isHttpError)
			{
				registerError.text = www.error;
				return;
			}

			answered = true;
			var response = JsonUtility.FromJson<LoginResponse>(www.downloadHandler.text);
			Web.sessionCookie = response.session;

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
				details.email = file.ReadLine();
				details.password = file.ReadLine();
			}
		}
		else
		{
			return null;
		}

		return details;
	}

	//NOTE(Simon): Returns HTTP response code. 200 is good, anything else is bad
	public static (bool, string) SendLoginRequest(string email, string password)
	{
		var form = new WWWForm();
		form.AddField("email", email);
		form.AddField("password", password);

		using (var www = UnityWebRequest.Post(Web.loginUrl, form))
		{
			var request = www.SendWebRequest();
			//TODO(Simon): Async?
			while (!request.isDone) { }

			var response = JsonUtility.FromJson<LoginResponse>(www.downloadHandler.text);

			if (www.responseCode == 200)
			{
				Web.sessionCookie = response.session;
			}

			if (www.responseCode == 0)
			{
				response = new LoginResponse { error = www.error };
			}

			return (www.responseCode == 200, response.error);
		}
	}
}
