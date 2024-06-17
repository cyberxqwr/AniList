using RenderHeads.Media.AVProVideo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TMP_Subtitles : MonoBehaviour
{
	[SerializeField] MediaPlayer _mediaPlayer = null;
	[SerializeField] TMP_Text _text = null;
	[SerializeField] Image _backgroundImage = null;
	[SerializeField] int _backgroundHorizontalPadding = 32;
	[SerializeField] int _backgroundVerticalPadding = 16;
	[SerializeField, Range(-1, 1024)] int _maxCharacters = 256;

	public MediaPlayer Player {
		set { ChangeMediaPlayer(value); }
		get { return _mediaPlayer; }
	}

	public TMP_Text Text {
		set { _text = value; }
		get { return _text; }
	}

	void Start () {
		ChangeMediaPlayer(_mediaPlayer);
	}

	void OnDestroy () {
		ChangeMediaPlayer(null);
	}

	void Update () {
		// TODO: Currently we need to call this each frame, as when it is called right after SetText() 
		// the ContentSizeFitter hasn't run yet, so effectively the box is a frame behind.
		UpdateBackgroundRect();
	}

	public void ChangeMediaPlayer (MediaPlayer newPlayer) {
		// When changing the media player, handle event subscriptions
		if (_mediaPlayer != null) {
			_mediaPlayer.Events.RemoveListener(OnMediaPlayerEvent);
			_mediaPlayer = null;
		}

		SetText(string.Empty);

		if (newPlayer != null) {
			newPlayer.Events.AddListener(OnMediaPlayerEvent);
			_mediaPlayer = newPlayer;
		}
	}

	private void SetText (string text) {
		_text.text = text;
		UpdateBackgroundRect();
	}

	private string PrepareText (string text) {
		// Crop text that is too long
		if (_maxCharacters >= 0 && text.Length > _maxCharacters) {
			text = text.Substring(0, _maxCharacters);
		}

		// Change RichText for Unity uGUI Text
		text = text.Replace("<font color=", "<color=");
		text = text.Replace("</font>", "</color>");
		text = text.Replace("<u>", string.Empty);
		text = text.Replace("</u>", string.Empty);
		return text;
	}

	private void UpdateBackgroundRect () {
		if (_backgroundImage) {
			if (string.IsNullOrEmpty(_text.text)) {
				_backgroundImage.enabled = false;
			} else {
				_backgroundImage.enabled = true;
				_backgroundImage.rectTransform.sizeDelta = _text.rectTransform.sizeDelta;
				_backgroundImage.rectTransform.anchoredPosition = _text.rectTransform.anchoredPosition;
				_backgroundImage.rectTransform.offsetMin -= new Vector2(_backgroundHorizontalPadding, _backgroundVerticalPadding);
				_backgroundImage.rectTransform.offsetMax += new Vector2(_backgroundHorizontalPadding, _backgroundVerticalPadding);
			}
		}
	}

	// Callback function to handle events
	private void OnMediaPlayerEvent (MediaPlayer mp, MediaPlayerEvent.EventType et, ErrorCode errorCode) {
		switch (et) {
			case MediaPlayerEvent.EventType.Closing: {
					SetText(string.Empty);
					break;
				}
			case MediaPlayerEvent.EventType.SubtitleChange: {
					SetText(PrepareText(_mediaPlayer.Subtitles.GetSubtitleText()));
					break;
				}
		}
	}
}
