using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AniListNet;
using System.Threading.Tasks;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text.RegularExpressions;

public class Main : MonoBehaviour
{
    public static AniClient Client = new();

    public static Main instance;
	public static AniListNet.Objects.User user;

	private async void Awake()
	{
		instance = this;

		var auth = PlayerPrefs.GetString("authKey", string.Empty);

		if (auth != string.Empty)
		{
			await Client.TryAuthenticateAsync(auth);
			if (Client.IsAuthenticated)
			{
				user = await Client.GetAuthenticatedUserAsync();
			}
		}
	}

	public static async Task<bool> logIn(string token)
    {
        if (await Client.TryAuthenticateAsync(token))
		{
			user = await Client.GetAuthenticatedUserAsync();
			return true;

        } else return false;
    }

	public static string SplitCamelCase(string str)
	{
		return Regex.Replace(
			Regex.Replace(
				str,
				@"(\P{Ll})(\P{Ll}\p{Ll})",
				"$1 $2"
			),
			@"(\p{Ll})(\P{Ll})",
			"$1 $2"
		);
	}
}
