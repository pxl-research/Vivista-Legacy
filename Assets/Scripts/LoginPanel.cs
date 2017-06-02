using System;
using System.Collections;
using System.Net;
using UnityEngine;
using UnityEngine.UI;

public class LoginPanel : MonoBehaviour 
{
	public InputField loginUsername;
	public InputField loginPassword;
	public Text loginError;

	public InputField registerUsername;
	public InputField registerPassword;
	public InputField registerRepeatPassword;
	public Text registerError;

	private enum State
	{
		Login,
		Register
	}
	private State state;

	void Start () 
	{
		
	}

	void Update () 
	{
	}

	public void Login() 
	{
		
	}

	public void Register() 
	{
		var username = registerUsername.text;
		var password = registerPassword.text;
		var repeatPassword = registerRepeatPassword.text;

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
		//form.headers.Add("Content-Type", "application/x-www-form-urlencoded");

		var www = new WWW("https://localhost/register", form);

		while (!www.isDone) {}
		
		if (www.size == 0)
		{
			registerError.text = "Couldn't connect to the server";
			return;
		}

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

		registerError.text = "";
	}
}
