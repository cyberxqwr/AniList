using AniListNet;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
	public static MenuManager instance;
	public List<GameObject> Menus = new List<GameObject>();

	private async void Awake()
	{
		instance = this;

		loadMenu(0);

		if (PlayerPrefs.HasKey("authKey") && await Main.Client.TryAuthenticateAsync(PlayerPrefs.GetString("authKey")))
		{
			loadMenu(1);
		}
	}

	public void loadMenu(int menuID)
	{
		foreach(var menu in Menus)
		{
			menu.SetActive(false);
		}

		Menus[menuID].SetActive(true);

		if(menuID == 1)
		{
			Crawler.instance.updatePfp();
		}
	}
}
