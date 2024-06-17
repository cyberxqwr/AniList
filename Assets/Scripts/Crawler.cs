using AniListNet;
using Juro.Core.Models.Anime;
using Juro.Core.Models.Movie;
using Juro.Providers.Anime;
using RenderHeads.Media.AVProVideo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;
using static UnityEngine.EventSystems.EventTrigger;

public class Crawler : MonoBehaviour
{
	#region Variables
	public static Crawler instance;

	public Image logOutButton;
	public TMP_Text homePage;
	public TMP_Text browsePage;
	public TMP_Text AnimeListPage;

	[Header("Header")]
	public RectTransform searchBar;
	public RectTransform searchIcon;
	public RectTransform searchClose;
	public Image accountPfp;
	[Space(10)]
	public CanvasGroup searchBoxDim;
	[Space(10)]
	public float animSearchBarSpeed = 0.5f;
	private LTDescr activeAnimSearch;
	public Coroutine SearchEnumerator;

	[Header("Home Menu")]
	public GameObject ActivityListObject;
	public GameObject AnimeInProgressListObject;
	[Space(10)]
	public RectTransform ActivityList;
	public RectTransform AnimeInProgressList;
	[Space(10)]
	public ActivityItem ActivityItem;
	public List<AniListNet.Objects.MediaEntry> ActivitiesList = new();
	public Transform ActivityItemParent;

	[Header("Browse Page")]
	public GameObject browseMenu;
	[Space(10)]
	public RectTransform trendingNowList;
	public RectTransform popularThisSeasonList;
	public RectTransform upcomingNextSeasonList;
	public RectTransform top50List;

	[Header("Anime List Page")]
	public GameObject FilterListObject;
	public Transform All;
	public Transform Watching;
	public Transform Completed;
	public Transform Paused;
	public Transform Dropped;
	public Transform Planning;
    public GameObject AnimeListObject;
	public List<AniListNet.Objects.MediaEntry> EntryList = new();
	public Transform AnimeList;
	public ListItem Bookmarked;

	[Header("Anime Data")]

	public TMP_Text Status;
    public List<AniListNet.Objects.CharacterEdge> CharacterList = new();
	public Transform CharacterListObj;
	public ListCharacter Character;
	public Transform StatusButtonList;
	public AniListNet.Objects.MediaEntry currentEntry;

    [Header("Anime Page")]
	public GameObject AnimeBannerObject;
	public GameObject AnimeStatsObject;
	public GameObject EpisodeListObject;
	public GameObject SubDubListObject;
	public GameObject SelectedTaskOverview;
	public GameObject SelectedTaskWatch;
	public GameObject SelectedTaskCharacters;
	[Space(10)]
	public Transform SubButton;
    public Transform DubButton;
	public SubDub titlesValue = SubDub.Sub;
    [Space(10)]
	public RectTransform AnimeTitleItemList;
	public RectTransform AnimeTagsList;
	public RectTransform AnimeRelationsList;
	public RectTransform AnimeCharactersList;
	public RectTransform AnimeRecommendationsList;
	public RectTransform AnimeCharactersPageList;
	public RectTransform AnimeEpisodesList;
	[Space(10)]
	public Image AnimeBanner;
	public Image AnimeThumbnail;
	public GameObject AnimeDataObject;
	public TMP_Text AnimeStatsText;
	public TMP_Text AnimeTitleText;
	public TMP_Text AnimeDescriptionText;
	public Sprite _defaultImage;
	public static Sprite defaultImage;
	[Space(10)]
	public TMP_Text overviewText;
	public TMP_Text watchText;
	public TMP_Text charactersText;

	[Header("Anime player")]
	public MediaPlayer AnimePlayer;
	public RectTransform SeekerBar;
	public LTDescr SeekerBarAnim;
	[Space(10)]
	public RectTransform fullScreenRect;
	public RectTransform miniPlayerRect;
	[Space(10)]
	public TMP_Text Subtitles;
	[Space(10)]
	public Sprite playImage;
	public Sprite pauseImage;
	public Image pausePlayButton;
	[Space(10)]
	public Sprite mute;
	public Sprite unmute;
	public Image muteUnmuteButton;
	public float videoVolume = 0.80f;
	[Space(10)]
	public Slider volumeSlider;
	private float unmutedValue = 0.80f;
	public Slider seekSlider;
	public TMP_Text seekText;

	[Header("Prefabs")]
	public GameObject AnimeTitlePrefab;
	public GameObject TagPrefab;
	public GameObject RelationPrefab;
	public GameObject CharacterPrefab;
	public GameObject RecommendationPrefab;
	public GameObject CharacterPagePrefab;
	public GameObject EpisodePrefab;
	public GameObject mediaPrefab;
	public GameObject topAnimeItemPrefab;

	private Kaido watchClient = new Kaido();
	private AniListNet.Objects.Media currentAnime;
	private int currentEpisode = -1;
	private bool seeking = false;
    private Coroutine DoubleClickSeekRoutine = null;
    public static Dictionary<string, Sprite> cachedSprites = new();
	#endregion

	#region UnityMethods
	private void Awake()
	{
		instance = this;
	}

	private void OnEnable()
	{
		loadHomePage();
	}

	void Update()
	{
		if (AnimePlayer.Control == null)
			return;

		var clickHappened = false;

		if (Input.GetMouseButtonDown(0) && !seeking && CheckInput()) seeking = true;

		if (Input.GetMouseButtonUp(0)) clickHappened = true;

		if (clickHappened && seeking)
			seeking = false;

		if (AnimePlayer.Control.IsPlaying() && !seeking)
		{
			pausePlayButton.sprite = pauseImage;

			var duration = (float)AnimePlayer.Info.GetDuration();
			var current = (float)AnimePlayer.Control.GetCurrentTime();

            seekSlider.maxValue = duration;
			seekSlider.value = current;

			var currentTime = new TimeSpan(0, 0, 0, (int)(current), 0);
			var durationTime = new TimeSpan(0, 0, 0, (int)(duration), 0);

			seekText.text = currentTime.ToString(@"h\:mm\:ss") + " / " + durationTime.ToString(@"h\:mm\:ss");
		}
		else if (!AnimePlayer.Control.IsPlaying())
		{
			pausePlayButton.sprite = playImage;
		}

		if (seeking)
		{
			AnimePlayer.Control.SeekFast(seekSlider.value);

			var duration = (float)AnimePlayer.Info.GetDuration();
            var current = seekSlider.value;

			var currentTime = new TimeSpan(0, 0, 0, (int)(current), 0);
			var durationTime = new TimeSpan(0, 0, 0, (int)(duration), 0);

			seekText.text = currentTime.ToString(@"h\:mm\:ss") + " / " + durationTime.ToString(@"h\:mm\:ss");
		}


		if (Input.GetMouseButtonUp(0))
		{
			PointerEventData eventData = new PointerEventData(EventSystem.current);
			eventData.position = Input.mousePosition;
			List<RaycastResult> raycastResults = new List<RaycastResult>();
			EventSystem.current.RaycastAll(eventData, raycastResults);

			bool onDisplay = false;

			foreach (var hit in raycastResults)
			{
				if (hit.gameObject.CompareTag("NoRaycast") && !seeking)
					return;

				if (hit.gameObject.CompareTag("Screen"))
					onDisplay = true;
			}

			if (onDisplay)
				pausePlay();

			seeking = false;
		}
	}

    private void LateUpdate()
    {
        if (AnimePlayer.Control == null)
            return;

        var clickHappened = false;

        if (Input.GetMouseButtonUp(1)) clickHappened = true;

        if (clickHappened) HandleBars();
    }

    public bool CheckInput(string Tag)
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);

        pointerEventData.position = Input.mousePosition;

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);
        foreach (RaycastResult result in raycastResults)
        {
            if (result.gameObject.CompareTag(Tag))
                return true;
        }

        return false;
    }

    public async void HandleBars(bool resetTimer = false)
    {
        if (CheckInput("NoRaycast") && !resetTimer)
            return;

		if (Mathf.RoundToInt(SeekerBar.anchoredPosition.y) < 0 || resetTimer)
		{
			if (!resetTimer)
				ExtendBars();

			var exit = false;

			var waitForClick = StartCoroutine(CheckForClick(delegate (bool b) { exit = b; }));

			await Task.Delay(5 * 1000);

			if (exit)
				return;

			StopCoroutine(waitForClick);

			CollapseBars();
		}
		else
		{
			CollapseBars();
		}
	}

    public IEnumerator CheckForClick(Action<bool> onInput)
    {
        yield return null;

        while (!catchClick())
        {
            yield return null;
        }

        onInput.Invoke(true);
    }

    public bool catchClick()
    {

        if (Input.GetMouseButton(0))
            return true;

        return false;
    }

    public void ExtendBars()
    {

        if (SeekerBarAnim != null)
            LeanTween.cancel(SeekerBarAnim.id);

        SeekerBarAnim = LeanTween.value(SeekerBar.gameObject,
            delegate (float f) {
                SeekerBar.anchoredPosition = new Vector2(SeekerBar.anchoredPosition.x, f);
            },
            SeekerBar.anchoredPosition.y,
            0,
            0.01f
        ).setEaseOutQuart();
    }

    public void CollapseBars()
    {

        if (SeekerBarAnim != null)
            LeanTween.cancel(SeekerBarAnim.id);

        SeekerBarAnim = LeanTween.value(SeekerBar.gameObject,
            delegate (float f) {
                SeekerBar.anchoredPosition = new Vector2(SeekerBar.anchoredPosition.x, f);
            },
            SeekerBar.anchoredPosition.y,
            -50,
            0.01f
        ).setEaseOutQuart();
    }
    #endregion

    #region MediaPlayer
    public void toggleFullScreen()
	{
		if (AnimePlayer.transform.parent.parent == miniPlayerRect)
		{
			AnimePlayer.transform.parent.SetParent(fullScreenRect);
			AnimePlayer.GetComponent<LayoutElement>().preferredHeight = 1080;
		}
		else
		{
			AnimePlayer.transform.parent.SetParent(miniPlayerRect);
			AnimePlayer.GetComponent<LayoutElement>().preferredHeight = 638;
		}
	}

	public void pausePlay(bool ignorePlaying = false)
	{
		if (AnimePlayer.Control.IsPlaying())
			AnimePlayer.Control.Pause();
		else
			AnimePlayer.Control.Play();
	}

	public void muteUnmute()
	{
		if (AnimePlayer.Control.IsMuted())
		{
			silentSetSliderValue(volumeSlider, unmutedValue);
			setVolume(unmutedValue);
		}
		else
		{
			unmutedValue = volumeSlider.value;

			silentSetSliderValue(volumeSlider, 0);
			setVolume(0, unmutedValue);
		}
	}

	public void setVolume(float value)
	{
		setVolume(value, 0.1f);
	}

	public void setVolume(float value, float restoreValue)
	{
		if (value == 0)
		{
			AnimePlayer.Control.MuteAudio(true);
			unmutedValue = restoreValue;
		}
		else
		{
			AnimePlayer.Control.MuteAudio(false);
		}

		muteUnmuteButton.sprite = AnimePlayer.Control.IsMuted() ? unmute : mute;

		AnimePlayer.Control.SetVolume(value);
		videoVolume = value;
	}

	public void silentSetSliderValue(Slider slider, float value)
	{
		var temp = slider.onValueChanged;
		slider.onValueChanged = new Slider.SliderEvent();

		slider.value = value;

		slider.onValueChanged = temp;
	}
	public bool CheckInput()
	{
		PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
		pointerEventData.position = Input.mousePosition;
		List<RaycastResult> raycastResults = new List<RaycastResult>();
		EventSystem.current.RaycastAll(pointerEventData, raycastResults);
		foreach (RaycastResult result in raycastResults)
		{
			if (result.gameObject == seekSlider.gameObject)
				return true;
		}

		return false;
	}
    #endregion

    #region AnimeLists

    public async Task LoadEntries(AniListNet.Objects.MediaEntryStatus? status = null)
	{

		EntryList.Clear();

        var entries = await Main.Client.GetUserEntriesAsync(Main.user.Id, new AniListNet.Parameters.MediaEntryFilter
        {
            Sort = AniListNet.Objects.MediaEntrySort.Score,
            SortDescending = true,
            Type = AniListNet.Objects.MediaType.Anime,
			Status = status
        }, new AniPaginationOptions(1, 50));

        EntryList.AddRange(entries.Data);

        for (int i = 2; i <= entries.LastPageIndex; i++)
        {

            entries = await Main.Client.GetUserEntriesAsync(Main.user.Id, new AniListNet.Parameters.MediaEntryFilter
            {
                Sort = AniListNet.Objects.MediaEntrySort.Score,
                SortDescending = true,
                Type = AniListNet.Objects.MediaType.Anime,
                Status = status
            }, new AniPaginationOptions(i, 50));

            EntryList.AddRange(entries.Data);
        }

    }

	public void ReloadUI()
	{
		foreach (Transform item in AnimeList)
		{
			if (!item.CompareTag("NoDelete")) Destroy(item.gameObject);
		}

        ResetFilters();

        foreach (var entry in EntryList)
		{
            var listObj = Instantiate(Bookmarked, AnimeList);
            ApplyImageBackground(entry.Media.Cover.ExtraLargeImageUrl.AbsoluteUri, listObj.image, new Vector2(40, 40));
            listObj.title.text = entry.Media.Title.PreferredTitle;
            listObj.score.text = entry.Score.ToString();
            listObj.progress.text = entry.Progress + "/" + entry.MaxProgress ?? "?";
            listObj.type.text = entry.Media.Format.ToString();

			listObj.title.GetComponent<Button>().onClick.RemoveAllListeners();
			listObj.title.GetComponent<Button>().onClick.AddListener(delegate { LoadAnimePage(entry.Media.Id); });
        }

	}

	public void ResetFilters()
	{

        All.GetChild(0).GetComponent<TMP_Text>().text = "All";
        Watching.GetChild(0).GetComponent<TMP_Text>().text = "Watching";
        Completed.GetChild(0).GetComponent<TMP_Text>().text = "Completed";
        Paused.GetChild(0).GetComponent<TMP_Text>().text = "Paused";
        Dropped.GetChild(0).GetComponent<TMP_Text>().text = "Dropped";
        Planning.GetChild(0).GetComponent<TMP_Text>().text = "Planning";
    }

    #endregion

    #region Header
    public void openSearch()
	{
		if (activeAnimSearch != null)
		{
			LeanTween.cancel(activeAnimSearch.id);
		}

		var t = getLerpT(116, searchBar.anchoredPosition.x, 0);

		activeAnimSearch = LeanTween.value(searchBar.gameObject, animateSearhBar, t, 1, animSearchBarSpeed * t != 0 ? 1 - t : 1).setEaseOutQuad();
	}

	public void closeSearch()
	{
		if (activeAnimSearch != null)
		{
			LeanTween.cancel(activeAnimSearch.id);
		}

		searchBar.GetComponent<TMP_InputField>().text = "";

		var t = getLerpT(116, searchBar.anchoredPosition.x, 0);

		activeAnimSearch = LeanTween.value(searchBar.gameObject, animateSearhBar, t, 0, animSearchBarSpeed * t != 0 ? t : 1).setEaseOutQuad();
	}

	public void animateSearhBar(float progress)
	{
		searchBar.anchoredPosition = Vector2.Lerp(new Vector2(116, -40), new Vector2(0, -118), progress);
		searchBar.sizeDelta = Vector2.Lerp(new Vector2(500, 50), new Vector2(1000, 80), progress);

		searchIcon.anchoredPosition = Vector2.Lerp(new Vector2(19, 0), new Vector2(13, 0), progress);
		searchIcon.sizeDelta = Vector2.Lerp(new Vector2(30, 30), new Vector2(40, 40), progress);

		searchClose.anchoredPosition = Vector2.Lerp(new Vector2(-19, 0), new Vector2(-13, 0), progress);
		searchClose.sizeDelta = Vector2.Lerp(new Vector2(30, 30), new Vector2(40, 40), progress);

		searchBoxDim.alpha = progress;
		searchBoxDim.interactable = progress != 0;
		searchBoxDim.blocksRaycasts = progress != 0;
	}

	public float getLerpT(float startValue, float CurrentValue, float EndValue)
	{
		return (startValue - CurrentValue) / -(EndValue - startValue);
	}

	public async void LoadAnimeList(string search)
	{
		if (SearchEnumerator != null)
			StopCoroutine(SearchEnumerator);

		await Task.Delay(500);

		if (search != searchBar.GetComponent<TMP_InputField>().text)
			return;

		var animes = await Main.Client.SearchMediaAsync(new AniListNet.Parameters.SearchMediaFilter
		{
			Query = search,
			Sort = AniListNet.Objects.MediaSort.Relevance,
			Type = AniListNet.Objects.MediaType.Anime
		});

		SearchEnumerator = StartCoroutine(LoadAnimeList(new List<AniListNet.Objects.Media>(animes.Data), AnimeTitleItemList));
	}

	public IEnumerator LoadAnimeList(List<AniListNet.Objects.Media> animes, RectTransform itemList)
	{
		yield return new WaitForEndOfFrame();

		foreach (Transform item in itemList)
		{
			if (item == null)
				yield break;

			Destroy(item.gameObject);
		}

		foreach (var anime in animes)
		{
			if (animes.IndexOf(anime) > 6)
				break;

			var item = Instantiate(AnimeTitlePrefab, AnimeTitleItemList);

			item.name = anime.Title.PreferredTitle;

			item.GetComponent<Button>().onClick.AddListener(
				async delegate
				{
					LoadAnimePage(anime.Id);
				}
			);

			var image = item.transform.GetChild(0).GetChild(0).GetComponent<Image>();
			var title = item.transform.GetChild(1).GetChild(0).GetComponent<TMP_Text>();
			var type = item.transform.GetChild(1).GetChild(1).GetComponent<TMP_Text>();

			ApplyImageBackground(anime.Cover.ExtraLargeImageUrl.AbsoluteUri, image, new Vector2(64, 64));

			title.text = anime.Title.PreferredTitle;
			type.text = anime.Format.ToString();
		}

		SearchEnumerator = null;
	}
	#endregion

	#region ImageLoader
	public static async void ApplyImageBackground(string url, Image image, Vector2 requiredSize)
	{
		var sprite = await LoadImage(url);

		if (image == null)
			return;

		var size = CalculateImageSize(sprite.texture.width, sprite.texture.height, requiredSize);

		image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
		image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
		image.rectTransform.anchoredPosition = Vector2.zero;
		image.sprite = sprite;
	}

	public static async Task<Sprite> LoadImage(string url)
	{
		if (url == null)
			return defaultImage;

		if (cachedSprites.TryGetValue(url, out var temp)) { await Task.Delay(100); return temp; }

		UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);

		Sprite sprite = null;

		www.SendWebRequest();

		while (!www.isDone)
		{
			await Task.Delay(1);
		}

		if (www.result != UnityWebRequest.Result.Success)
		{
			Debug.Log(www.error);
		}
		else
		{
			var texture = DownloadHandlerTexture.GetContent(www);

			sprite = GetSprite(texture);

			cachedSprites[url] = sprite;
		}

		www.Dispose();

		return sprite;
	}

	public static Sprite GetSprite(Texture2D texture)
	{
		return Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);
	}

	public static Vector2 CalculateImageSize(float x, float y, float finalSize)
	{
		if (x > y)
		{
			y = y * finalSize / x;
			x = finalSize;
		}
		else if (x < y)
		{
			x = x * finalSize / y;
			y = finalSize;
		}
		else
		{
			x = y = finalSize;
		}

		return new Vector2(x, y);
	}

	public static Vector2 CalculateImageSize(float x, float y, Vector2 finalSize)
	{
		var xtemp = x;
		var ytemp = y;

		if (x > y)
		{
			y = y * finalSize.x / x;
			x = finalSize.x;
		}
		else if (x < y)
		{
			x = x * finalSize.y / y;
			y = finalSize.y;
		}
		else
		{
			x = y = finalSize.x;
		}

		if (x < finalSize.x || y < finalSize.y)
		{
			x = xtemp;
			y = ytemp;

			if (x < y)
			{
				y = y * finalSize.x / x;
				x = finalSize.x;
			}
			else if (x > y)
			{
				x = x * finalSize.y / y;
				y = finalSize.y;
			}
			else
			{
				x = finalSize.x;
				y = finalSize.y;
			}
		}

		return new Vector2(x, y);
	}
	public static void ResizeImage(Image image, Vector2 size)
	{
		image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
		image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
	}
	#endregion

	#region AnimePage
	public async void LoadAnimePage(int titleID)
	{
		DisableAllMenus();

		transform.GetChild(1).GetChild(0).GetComponent<Scrollbar>().value = 1;

		AnimeBannerObject.SetActive(true);
		AnimeStatsObject.SetActive(true);
		AnimeDataObject.SetActive(true);

		currentAnime = await Main.Client.GetMediaAsync(titleID);
		await CheckAnimeStatus(titleID);
		currentEpisode = -1;

		LoadTitleStats();
		LoadOverviewPage();

		AnimeTitleText.text = currentAnime.Title.PreferredTitle;
		AnimeDescriptionText.text = currentAnime.Description;

		AnimeBanner.sprite = await LoadImage(currentAnime.BannerImageUrl.AbsoluteUri);
		AnimeThumbnail.sprite = await LoadImage(currentAnime.Cover.ExtraLargeImageUrl.AbsoluteUri);

		ResizeImage(AnimeBanner, CalculateImageSize(AnimeBanner.sprite.texture.width, AnimeBanner.sprite.texture.height, new Vector2(1920, 400)));
		ResizeImage(AnimeThumbnail, CalculateImageSize(AnimeThumbnail.sprite.texture.width, AnimeThumbnail.sprite.texture.height, new Vector2(258, 326.4f)));
	}

	public async void LoadOverviewPage()
	{
		DisableAllSideMenus();
		overviewText.text = "<u>Overview</u>";

		SelectedTaskOverview.SetActive(true);
		AnimeStatsObject.SetActive(true);
	}

	public async void LoadWatchPage()
	{

		if (await ServersAvailable(SubDub.Sub)) SubButton.gameObject.SetActive(true);
		else
		{
			titlesValue = SubDub.Dub;
			SubButton.gameObject.SetActive(false);
		}
		if (await ServersAvailable(SubDub.Dub)) DubButton.gameObject.SetActive(true);
        else
        {
			titlesValue = SubDub.Sub;
            DubButton.gameObject.SetActive(false);
        }

        DisableAllSideMenus();
		watchText.text = "<u>Watch</u>";
        if (titlesValue == SubDub.Sub)
		{
            SubButton.GetChild(0).GetComponent<TMP_Text>().text = "<u>Sub</u>";
            DubButton.GetChild(0).GetComponent<TMP_Text>().text = "Dub";
			Subtitles.enabled = true;
        } else
		{
            DubButton.GetChild(0).GetComponent<TMP_Text>().text = "<u>Dub</u>";
            SubButton.GetChild(0).GetComponent<TMP_Text>().text = "Sub";
			Subtitles.enabled = false;
        }

        SelectedTaskWatch.SetActive(true);
		EpisodeListObject.SetActive(true);
		SubDubListObject.SetActive(true);

		var animes = await watchClient.SearchAsync(currentAnime.Title.RomajiTitle);

		
		LoadEpisodesList(animes[0]);
		LoadEpisode(animes[0], 0);
	}

	public async void LoadCharactersPage()
	{
		DisableAllSideMenus();
		charactersText.text = "<u>Characters</u>";

		SelectedTaskCharacters.SetActive(true);
		AnimeStatsObject.SetActive(true);
		await LoadCharacters();

	}

	public void SetType(int option)
	{

		switch (option)
		{
			case 0:
				titlesValue = SubDub.Sub;
				SubButton.GetChild(0).GetComponent<TMP_Text>().text = "<u>Sub</u>";
                DubButton.GetChild(0).GetComponent<TMP_Text>().text = "Dub";
                LoadWatchPage();
                
                break;
			case 1:
                titlesValue = SubDub.Dub;
                DubButton.GetChild(0).GetComponent<TMP_Text>().text = "<u>Dub</u>";
                SubButton.GetChild(0).GetComponent<TMP_Text>().text = "Sub";
				LoadWatchPage();
                break;
		}
	}

	public async Task<bool> ServersAvailable(SubDub subdub)
	{
        var animes = await watchClient.SearchAsync(currentAnime.Title.RomajiTitle);
        var episodes = await watchClient.GetEpisodesAsync(animes[0].Id);
        var servers = await watchClient.GetVideoServersAsync(episodes[0].Id, subdub);

		if (servers.Count == 0) return false;
		else return true;
    }

	public async void LoadEpisodesList(IAnimeInfo anime, int selectedEpisode = 1)
	{
		var episodes = await watchClient.GetEpisodesAsync(anime.Id);

		foreach (Transform item in AnimeEpisodesList)
		{
			Destroy(item.gameObject);
		}

		episodes.Reverse();

		foreach (var episode in episodes)
		{
			var item = Instantiate(EpisodePrefab, AnimeEpisodesList);

			var index = episodes.IndexOf(episode) + 1;

			item.name = "Episode_" + index;

			var button = item.GetComponent<Button>();
			var text = item.GetComponentInChildren<TMP_Text>();

			text.text = (index == selectedEpisode ? "<u>" : "") + index.ToString();

			button.onClick.AddListener(
				delegate 
				{
					LoadEpisodesList(anime, index);
					LoadEpisode(anime, index - 1);

				}
			);
		}
	}

	public async void LoadEpisode(IAnimeInfo anime, int episode)
	{
		var episodes = await watchClient.GetEpisodesAsync(anime.Id);

		var servers = await watchClient.GetVideoServersAsync(episodes[episode].Id, titlesValue);

		float videoProgress = seekSlider.value / seekSlider.maxValue;

        if (currentEntry != null && episode - currentEntry.Progress == 1 && videoProgress >= 0.7f)
		{

            currentEntry = await Main.Client.SaveMediaEntryAsync(currentAnime.Id, new AniListNet.Parameters.MediaEntryMutation
            {
                Progress = currentEntry.Progress + 1
            });
        }

        currentEpisode = episode;

        var link = "";
		var subtitlesUrl = "";

		foreach(var server in servers)
		{
			var videos = await watchClient.GetVideosAsync(server);

			foreach(var video in videos)
			{
				if (video.VideoUrl != null && video.VideoUrl != "")
				{
					link = video.VideoUrl;

					if (titlesValue == SubDub.Sub)
					{

						foreach (var subtitle in video.Subtitles)
						{

							if (subtitle.Language.Equals("English"))
							{

								subtitlesUrl = subtitle.Url;
							}
							if (subtitlesUrl != string.Empty) break;
						}
					}
				}
					
					

				if (link != "")
					break;
			}

			if (link != "")
                break;
        }

		AnimePlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, link, true);
		if (subtitlesUrl != string.Empty) AnimePlayer.EnableSubtitles(new MediaPath(subtitlesUrl, MediaPathType.AbsolutePathOrURL));

        var duration = (float) AnimePlayer.Info.GetDuration();
		var time = (float) AnimePlayer.Control.GetCurrentTime();

		seekSlider.maxValue = duration;
		seekSlider.minValue = time;

		pausePlayButton.sprite = playImage;

		volumeSlider.value = AnimePlayer.Control.GetVolume();
	}

    public static List<Subtitle> ParseSubtitlesVTT(string data)
    {
        List<Subtitle> result = new();

        byte[] byteArray = Encoding.ASCII.GetBytes(data);
        MemoryStream stream = new MemoryStream(byteArray);

        var parser = new SubtitlesParser.Classes.Parsers.VttParser();
        var items = parser.ParseStream(stream, new UTF7Encoding());

        foreach (var item in items)
        {
            var subtitle = new Subtitle();

            var text = "";

            foreach (var line in item.Lines)
            {
                text += line + "\n";
            }

            text = text.TrimEnd('\n');

            subtitle.index = items.IndexOf(item);
            subtitle.text = text;
            subtitle.timeStart = ((double)item.StartTime) / 1000;
            subtitle.timeEnd = ((double)item.EndTime) / 1000;

            result.Add(subtitle);
        }

        return result;
    }

    public async void LoadTitleStats()
	{
		var stats =
			"<b>STATUS</b>\n" +
			"{0}\n\n" +
			"<b>SOURCE</b>\n" +
			"{1}\n\n" +
			"<b>GENRES</b>\n" +
			"{2}\n" +
			"<b>SYNONYMS</b>\n" +
			"{3}\n";

		var genres = "";

		foreach (var genre in currentAnime.Genres)
		{
			genres += genre + "\n";
		}

		var synonyms = "";

		foreach (var synonym in currentAnime.Synonyms)
		{
			synonyms += synonym + "\n";
		}

		AnimeStatsText.text = string.Format(stats, new object[4] { currentAnime.Status.ToString(), currentAnime.Source.ToString(), genres, synonyms });

		foreach(RectTransform child in AnimeTagsList)
		{
			Destroy(child.gameObject);
		}

		var tags = await currentAnime.GetTagsAsync();

		foreach (var tag in tags)
		{
			var item = Instantiate(TagPrefab, AnimeTagsList);

			item.name = tag.Name;

			item.GetComponentInChildren<TMP_Text>().text = tag.Name;
		}
	}
	
	public async Task CheckAnimeStatus(int titleID)
	{

		await LoadEntries();
		bool found = false;
        foreach (var entry in EntryList)
        {
            if (entry.Media.Id == titleID)
            {
				currentEntry = entry;
                Status.text = entry.Status.ToString();
				Status.GetComponent<Button>().interactable = false;
                found = true;
            }
        }

		if (!found) {

			AniListNet.Objects.MediaEntry newEntry = new AniListNet.Objects.MediaEntry();

            Status.text = "Add to List";
            Status.GetComponent<Button>().interactable = true;
            Status.GetComponent<Button>().onClick.RemoveAllListeners();
            Status.GetComponent<Button>().onClick.AddListener(async delegate
            {
                newEntry = await Main.Client.SaveMediaEntryAsync(currentAnime.Id, new AniListNet.Parameters.MediaEntryMutation
                {
                    Status = AniListNet.Objects.MediaEntryStatus.Current,
                    Progress = 0,
                    Score = 0
                });

				currentEntry = newEntry;
				Status.text = "Watching";
                Status.GetComponent<Button>().interactable = false;
            });
        }
		if (Status.text == "Current") Status.text = "Watching";
    }

	public void StatusButtonAction()
	{
        if (Mathf.RoundToInt(StatusButtonList.GetComponent<CanvasGroup>().alpha) != 0) return;

        StatusButtonList.gameObject.SetActive(true);
		LeanTween.value(StatusButtonList.gameObject, delegate (float f) { StatusButtonList.GetComponent<CanvasGroup>().alpha = f; }, 0, 1, 0.2f).setEaseOutCirc();
		StatusButtonList.GetComponent<CanvasGroup>().interactable = true;
		StatusButtonList.GetComponent<CanvasGroup>().blocksRaycasts = true;

    }

    public void StatusButtonClose()
    {

		if (Mathf.RoundToInt(StatusButtonList.GetComponent<CanvasGroup>().alpha) != 1) return;

        LeanTween.value(StatusButtonList.gameObject, delegate (float f) { StatusButtonList.GetComponent<CanvasGroup>().alpha = f; }, 1, 0, 0.2f).setEaseInCirc()
			.setOnComplete(delegate () { StatusButtonList.gameObject.SetActive(false); });
        StatusButtonList.GetComponent<CanvasGroup>().interactable = false;
        StatusButtonList.GetComponent<CanvasGroup>().blocksRaycasts = false;

    }

	public async void StatusChange(int option)
	{

        switch (option)
		{
			case 0:
				currentEntry = await Main.Client.SaveMediaEntryAsync(currentAnime.Id, new AniListNet.Parameters.MediaEntryMutation { 
					Status = AniListNet.Objects.MediaEntryStatus.Completed, 
					Progress = currentEntry.MaxProgress });
				Status.text = "Completed";
				break;
			case 1:
                currentEntry = await Main.Client.SaveMediaEntryAsync(currentAnime.Id, new AniListNet.Parameters.MediaEntryMutation { Status = AniListNet.Objects.MediaEntryStatus.Planning });
				Status.text = "Planning";
                break;
		}

		StatusButtonClose();
	}

    public async Task LoadCharacters()
    {

        foreach (Transform item in CharacterListObj)
        {
            if (!item.CompareTag("NoDelete")) Destroy(item.gameObject);
        }

        CharacterList.Clear();

        var entries = await Main.Client.GetMediaCharactersAsync(currentAnime.Id, new AniPaginationOptions(1, 50));

        CharacterList.AddRange(entries.Data);

        for (int i = 2; i <= entries.LastPageIndex; i++)
        {

            entries = await Main.Client.GetMediaCharactersAsync(currentAnime.Id, new AniPaginationOptions(i, 50));

            CharacterList.AddRange(entries.Data);
        }

		foreach (var entry in CharacterList)
		{
			var charObj = Instantiate(Character, CharacterListObj);
            ApplyImageBackground(entry.Character.Image.LargeImageUrl.AbsoluteUri, charObj.banner, new Vector2(80, 100));
			charObj.name.text = entry.Character.Name.FullName.ToString();
			charObj.role.text = entry.Role.ToString();

        }

    }
    #endregion

    #region PageHandler
    public void DisableAllMenus()
	{
		homePage.text = "Home";
		browsePage.text = "Browse";
		AnimeListPage.text = "Anime List";
		browseMenu.SetActive(false);

		ActivityListObject.SetActive(false);
		AnimeInProgressListObject.SetActive(false);
		FilterListObject.SetActive(false);
		AnimeListObject.SetActive(false);
		AnimeBannerObject.SetActive(false);
		AnimeDataObject.SetActive(false);
		AnimePlayer.CloseMedia();

		DisableAllSideMenus();

		//
	}
	public void DisableAllSideMenus()
	{
		SelectedTaskOverview.SetActive(false);
		SelectedTaskWatch.SetActive(false);
		SelectedTaskCharacters.SetActive(false);
		AnimeStatsObject.SetActive(false);
		EpisodeListObject.SetActive(false);
		SubDubListObject.SetActive(false);
		

		overviewText.text = "Overview";
		watchText.text = "Watch";
		charactersText.text = "Characters";
	}
	public void logOut()
	{
		PlayerPrefs.DeleteKey("authKey");
		PlayerPrefs.Save();

		MenuManager.instance.loadMenu(0);

		Main.Client = new AniClient();
	}

	public void loadHomePage()
	{
		DisableAllMenus();

		homePage.text = "<u>Home</u>";

		ActivityListObject.SetActive(true);
		AnimeInProgressListObject.SetActive(true);

		updateAnimeInProgress();
		updateActivity();
	}

	public void loadBrowsePage()
	{
		DisableAllMenus();
		browseMenu.SetActive(true);

		browsePage.text = "<u>Browse</u>";

		updateTrendingNow();
		updatePopularThisSeason();
		updateUpcomingNextSeason();
		updateTop50();
	}

	public async void loadAnimeListPage()
	{
		DisableAllMenus();

		AnimeListPage.text = "<u>Anime List</u>";

		FilterListObject.SetActive(true);
		AnimeListObject.SetActive(true);
        await LoadEntries();
		ReloadUI();

		int allCount = 0;
		int watchingCount = 0;
		int completedCount = 0;
		int pausedCount = 0;
		int droppedCount = 0;
		int planningCount = 0;

        foreach (var entry in EntryList)
        {

			allCount++;
			switch (entry.Status)
			{
				case AniListNet.Objects.MediaEntryStatus.Current:
					watchingCount++;
					break;
                case AniListNet.Objects.MediaEntryStatus.Completed:
                    completedCount++;
                    break;
                case AniListNet.Objects.MediaEntryStatus.Paused:
                    pausedCount++;
                    break;
                case AniListNet.Objects.MediaEntryStatus.Dropped:
                    droppedCount++;
                    break;
                case AniListNet.Objects.MediaEntryStatus.Planning:
                    planningCount++;
                    break;
            }
        }

		All.GetChild(1).GetComponent<TMP_Text>().text = allCount.ToString();
		Watching.GetChild(1).GetComponent<TMP_Text>().text = watchingCount.ToString();
        Completed.GetChild(1).GetComponent<TMP_Text>().text = completedCount.ToString();
		Paused.GetChild(1).GetComponent<TMP_Text>().text = pausedCount.ToString();
		Dropped.GetChild(1).GetComponent<TMP_Text>().text = droppedCount.ToString();
		Planning.GetChild(1).GetComponent<TMP_Text>().text = planningCount.ToString();

		All.GetChild(0).GetComponent<Button>().onClick.RemoveAllListeners();
        All.GetChild(0).GetComponent<Button>().onClick.AddListener(async delegate
        {
            await LoadEntries(); 
			ReloadUI();
            All.GetChild(0).GetComponent<TMP_Text>().text = "<u>All</u>";
        });

        Watching.GetChild(0).GetComponent<Button>().onClick.RemoveAllListeners();
        Watching.GetChild(0).GetComponent<Button>().onClick.AddListener(async delegate
        {
            await LoadEntries(AniListNet.Objects.MediaEntryStatus.Current);
            ReloadUI();
            Watching.GetChild(0).GetComponent<TMP_Text>().text = "<u>Watching</u>";
        });

        Completed.GetChild(0).GetComponent<Button>().onClick.RemoveAllListeners();
        Completed.GetChild(0).GetComponent<Button>().onClick.AddListener(async delegate
        {
            await LoadEntries(AniListNet.Objects.MediaEntryStatus.Completed);
            ReloadUI();
            Completed.GetChild(0).GetComponent<TMP_Text>().text = "<u>Completed</u>";
        });

        Paused.GetChild(0).GetComponent<Button>().onClick.RemoveAllListeners();
        Paused.GetChild(0).GetComponent<Button>().onClick.AddListener(async delegate
        {
            await LoadEntries(AniListNet.Objects.MediaEntryStatus.Paused);
            ReloadUI();
            Paused.GetChild(0).GetComponent<TMP_Text>().text = "<u>Paused</u>";
        });

        Dropped.GetChild(0).GetComponent<Button>().onClick.RemoveAllListeners();
        Dropped.GetChild(0).GetComponent<Button>().onClick.AddListener(async delegate
        {
            await LoadEntries(AniListNet.Objects.MediaEntryStatus.Dropped);
            ReloadUI();
            Dropped.GetChild(0).GetComponent<TMP_Text>().text = "<u>Dropped</u>";
        });

        Planning.GetChild(0).GetComponent<Button>().onClick.RemoveAllListeners();
        Planning.GetChild(0).GetComponent<Button>().onClick.AddListener(async delegate
        {
            await LoadEntries(AniListNet.Objects.MediaEntryStatus.Planning);
            ReloadUI();
            Planning.GetChild(0).GetComponent<TMP_Text>().text = "<u>Planning</u>";
        });
    }

		
	#endregion

	#region Updaters
	public async void updateTrendingNow()
	{
		foreach (RectTransform child in trendingNowList)
		{
			Destroy(child.gameObject);
		}

		var trending = await Main.Client.SearchMediaAsync(new AniListNet.Parameters.SearchMediaFilter { Sort = AniListNet.Objects.MediaSort.Trending, SortDescending = true, Type = AniListNet.Objects.MediaType.Anime });

		for (int i = 0; i < 5; i++)
		{
			var mediaObject = Instantiate(mediaPrefab, trendingNowList);

			var media = trending.Data[i];

			mediaObject.name = media.Title.EnglishTitle;
			var image = mediaObject.transform.GetChild(0).GetChild(0).GetComponent<Image>();
			var text = mediaObject.transform.GetChild(1).GetComponent<TMP_Text>();

			Crawler.ApplyImageBackground(media.Cover.ExtraLargeImageUrl.AbsoluteUri, image, new Vector2(230, 310));
			text.text = media.Title.PreferredTitle;

			mediaObject.GetComponent<Button>().onClick.AddListener(delegate { Crawler.instance.LoadAnimePage(media.Id);});
		}
	}

	public async void updatePopularThisSeason()
	{
		foreach (RectTransform child in popularThisSeasonList)
		{
			Destroy(child.gameObject);
		}

		var popular = await Main.Client.SearchMediaAsync(new AniListNet.Parameters.SearchMediaFilter
		{
			Type = AniListNet.Objects.MediaType.Anime,
			SortDescending = true,
			Sort = AniListNet.Objects.MediaSort.Popularity,
			Season = (DateTime.Today.Month < 3 || DateTime.Today.Month == 12 ? AniListNet.Objects.MediaSeason.Winter :
			(DateTime.Today.Month < 6 ? AniListNet.Objects.MediaSeason.Spring :
			(DateTime.Today.Month < 9 ? AniListNet.Objects.MediaSeason.Summer : AniListNet.Objects.MediaSeason.Fall)))
		}, new AniPaginationOptions(1, 5));

		foreach (var media in popular.Data)
		{
			createMedia(media, popularThisSeasonList);
		}
	}

	public async void updateUpcomingNextSeason()
	{
		foreach (RectTransform child in upcomingNextSeasonList)
		{
			Destroy(child.gameObject);
		}

		var upcoming = await Main.Client.SearchMediaAsync(new AniListNet.Parameters.SearchMediaFilter
		{
			Type = AniListNet.Objects.MediaType.Anime,
			SortDescending = true,
			Sort = AniListNet.Objects.MediaSort.Popularity,
			Season = (DateTime.Today.Month < 3 || DateTime.Today.Month == 12 ? AniListNet.Objects.MediaSeason.Spring :
			(DateTime.Today.Month < 6 ? AniListNet.Objects.MediaSeason.Summer :
			(DateTime.Today.Month < 9 ? AniListNet.Objects.MediaSeason.Fall : AniListNet.Objects.MediaSeason.Winter))),
			Status = new Dictionary<AniListNet.Objects.MediaStatus, bool> { { AniListNet.Objects.MediaStatus.NotYetReleased, true } }
		}, new AniPaginationOptions(1, 5));

		foreach (var media in upcoming.Data)
		{
			createMedia(media, upcomingNextSeasonList);
		}
	}

	public void createMedia(AniListNet.Objects.Media media, RectTransform list)
	{
		var mediaObject = Instantiate(mediaPrefab, list);

		mediaObject.name = media.Title.EnglishTitle;
		var image = mediaObject.transform.GetChild(0).GetChild(0).GetComponent<Image>();
		var text = mediaObject.transform.GetChild(1).GetComponent<TMP_Text>();

		Crawler.ApplyImageBackground(media.Cover.ExtraLargeImageUrl.AbsoluteUri, image, new Vector2(230, 310));
		text.text = media.Title.PreferredTitle;

		mediaObject.GetComponent<Button>().onClick.AddListener(delegate { Crawler.instance.LoadAnimePage(media.Id); });
	}

	public async void updateTop50()
	{
		foreach (RectTransform child in top50List)
		{
			Destroy(child.gameObject);
		}

		var top50 = await Main.Client.SearchMediaAsync(new AniListNet.Parameters.SearchMediaFilter
		{
			Type = AniListNet.Objects.MediaType.Anime,
			SortDescending = true,
			Sort = AniListNet.Objects.MediaSort.Score
		}, new AniPaginationOptions(1, 50));

		foreach (var media in top50.Data)
		{
			var mediaObject = Instantiate(topAnimeItemPrefab, top50List);

			mediaObject.name = media.Title.EnglishTitle;
			var listNumber = mediaObject.transform.GetChild(0).GetComponent<TMP_Text>();
			var image = mediaObject.transform.GetChild(1).GetChild(0).GetChild(0).GetComponent<Image>();
			var title = mediaObject.transform.GetChild(1).GetChild(1).GetComponent<TMP_Text>();
			var score = mediaObject.transform.GetChild(1).GetChild(2).GetComponent<TMP_Text>();
			var type = mediaObject.transform.GetChild(1).GetChild(3).GetChild(0).GetComponent<TMP_Text>();
			var lenght = mediaObject.transform.GetChild(1).GetChild(3).GetChild(1).GetComponent<TMP_Text>();
			var season = mediaObject.transform.GetChild(1).GetChild(4).GetChild(0).GetComponent<TMP_Text>();
			var status = mediaObject.transform.GetChild(1).GetChild(4).GetChild(1).GetComponent<TMP_Text>();

			listNumber.text = "<color=#748899cc><size=85%>#</size></color>" + (Array.IndexOf(top50.Data, media) + 1);
			Crawler.ApplyImageBackground(media.Cover.ExtraLargeImageUrl.AbsoluteUri, image, new Vector2(230, 310));
			title.text = media.Title.PreferredTitle;
			score.text = media.AverageScore + "%";
			type.text = Main.SplitCamelCase(media.Format.ToString());
			switch (media.Format)
			{
				case AniListNet.Objects.MediaFormat.Movie:
					lenght.text = media.Duration.ToString();

					break;

				default:
					if (media.Episodes != null)
						lenght.text = media.Episodes + " episodes";

					break;
			}

			if (media.Status == AniListNet.Objects.MediaStatus.Finished)
				season.text = media.Season.ToString() + " " + media.SeasonYear;
			else
				season.text = "Airing Since " + media.SeasonYear;
			status.text = Main.SplitCamelCase(media.Status.ToString());

			mediaObject.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(delegate { Crawler.instance.LoadAnimePage(media.Id); });
		}
	}

	public async void updateActivity()
	{

		ActivitiesList.Clear();

		//     if (Main.user == null)
		//     {
		//         var user = await Main.Client.GetAuthenticatedUserAsync();
		//Main.user = user;
		//     }

		foreach (Transform item in ActivityItemParent)
		{
			if (!item.CompareTag("NoDelete")) Destroy(item.gameObject);
		}

		while (Main.user == null) await Task.Yield();

		var activityItems = await Main.Client.GetUserEntriesAsync(Main.user.Id, new AniListNet.Parameters.MediaEntryFilter
		{
			Sort = AniListNet.Objects.MediaEntrySort.StartedDate,
			SortDescending = true,
			Type = AniListNet.Objects.MediaType.Anime,
		}, new AniPaginationOptions(1, 50));

		ActivitiesList.AddRange(activityItems.Data);

		//for (int i = 2; i <= activityItems.LastPageIndex; i++)
		//{

		//	activityItems = await Main.Client.GetUserEntriesAsync(Main.user.Id, new AniListNet.Parameters.MediaEntryFilter
		//	{
		//		Sort = AniListNet.Objects.MediaEntrySort.StartedDate,
		//		SortDescending = true,
		//		Type = AniListNet.Objects.MediaType.Anime,
		//	}, new AniPaginationOptions(i, 50));

		//	ActivitiesList.AddRange(activityItems.Data);
		//}

		if (ActivitiesList.Count == activityItems.Data.Length)
		{

			foreach (var activity in ActivitiesList)
			{
				var activityObj = Instantiate(ActivityItem, ActivityItemParent);
				activityObj.accountName.text = Main.user.Name;
				ApplyImageBackground(Main.user.Avatar.MediumImageUrl.AbsoluteUri, activityObj.accountPicture, new Vector2(55, 55));
				ApplyImageBackground(activity.Media.Cover.ExtraLargeImageUrl.AbsoluteUri, activityObj.animeBanner, new Vector2(100, 150));
				activityObj.accountActivity.text = Main.user.Name + interpretStatus(activity.Status) + activity.Media.Title.PreferredTitle;
				activityObj.postTime.text = interpretDate(activity);

			}
		}

	}

	public string interpretStatus(AniListNet.Objects.MediaEntryStatus? status = null)
	{
		switch (status)
		{
			case AniListNet.Objects.MediaEntryStatus.Current:
				return " has now started watching ";
			case AniListNet.Objects.MediaEntryStatus.Planning:
				return " is now planning to watch ";
			case AniListNet.Objects.MediaEntryStatus.Completed:
				return " has completed watching ";
			case AniListNet.Objects.MediaEntryStatus.Dropped:
				return " has decided to drop ";
			case AniListNet.Objects.MediaEntryStatus.Paused:
				return " decided to pause watching ";
			case AniListNet.Objects.MediaEntryStatus.Repeating:
				return " is now repeating ";
			default:
				return " has added to their list an anime called ";
		}
	}

	public string interpretDate(AniListNet.Objects.MediaEntry activity)
	{

		if (activity.StartDate.Day == DateTime.Now.Day)
		{
			return "Today";
		}
		else if (activity.StartDate.Day == DateTime.Now.Day - 1)
		{
			return "Yesterday";
		}
		else if (activity.StartDate.Year == DateTime.Now.Year && activity.StartDate.Month == DateTime.Now.Month && DateTime.Now.Day - activity.StartDate.Day <= 7)
		{
			return "This week";
		}
		else if (activity.StartDate.Year == DateTime.Now.Year && activity.StartDate.Month == DateTime.Now.Month)
		{
			return "This month";
		}
		else if (activity.StartDate.Year == DateTime.Now.Year)
		{
			return "This year";
		}
		else if (activity.StartDate.Year == DateTime.Now.Year - 1)
		{
			return "Last year";
		}
		else return new DateTime((int)activity.StartDate.Year, (int)activity.StartDate.Month, (int)activity.StartDate.Day).ToString("yyyy/mm/dd");
	}

	public async void updateAnimeInProgress()
	{
		foreach (Transform child in AnimeInProgressList)
		{
			Destroy(child.gameObject);
		}

		if (!Main.Client.IsAuthenticated)
			return;

		while (Main.user == null) await Task.Yield();

		var watchingTitles = await Main.Client.GetUserEntriesAsync(
			Main.user.Id, 
			new AniListNet.Parameters.MediaEntryFilter { 
				Status = AniListNet.Objects.MediaEntryStatus.Current, 
				Type = AniListNet.Objects.MediaType.Anime });

		foreach (var title in watchingTitles.Data)
		{
			var titleObj = Instantiate(RecommendationPrefab, AnimeInProgressList);

			titleObj.name = title.Media.Title.PreferredTitle;

			var img = titleObj.transform.GetChild(0).GetChild(0).GetComponent<Image>();
			var txt = titleObj.transform.GetChild(1).GetComponent<TMP_Text>();

			titleObj.GetComponent<Button>().onClick.AddListener(delegate { LoadAnimePage(title.Media.Id); });

			ApplyImageBackground(title.Media.Cover.ExtraLargeImageUrl.AbsoluteUri, img, new Vector2(110, 150));
			txt.text = title.Media.Title.PreferredTitle;
		}
	}
	public async void updatePfp()
	{
		var profile = await Main.Client.GetAuthenticatedUserAsync();

		ApplyImageBackground(profile.Avatar.LargeImageUrl.AbsoluteUri, accountPfp, (accountPfp.transform.parent as RectTransform).sizeDelta);
	}
	#endregion
}
