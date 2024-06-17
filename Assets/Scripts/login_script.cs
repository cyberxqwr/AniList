using AniListNet;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class login_script : MonoBehaviour
{
	public TMP_InputField tokenInputField;
	public Button logInButton;

	private void OnEnable()
	{
		Start();
	}
	// Start is called before the first frame update
	void Start()
	{
		tokenInputField.gameObject.SetActive(false);
		logInButton.onClick.RemoveAllListeners();
		logInButton.onClick.AddListener(delegate { openLogIn(); });
	}

	public void openLogIn()
	{
		Application.OpenURL("https://anilist.co/api/v2/oauth/authorize?client_id=15469&response_type=token");
		tokenInputField.gameObject.SetActive(true);
		logInButton.onClick.RemoveAllListeners();
		logInButton.onClick.AddListener(async delegate
		{
			var authenticated = await Main.logIn(tokenInputField.text);
			if (!authenticated)
			{
				Debug.LogError("Failed to authenticate user");
				return;
			}

			PlayerPrefs.SetString("authKey", tokenInputField.text);
			PlayerPrefs.Save();

			MenuManager.instance.loadMenu(1);
		});
	}
}
