using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ClickAwayScript : MonoBehaviour {
    public GameObject Target;

    public UnityEvent OnClickAway;

    public bool _active;

#if !UNITY_EDITOR
	private void Start() {
		_active = false;
	}
#endif

	void LateUpdate () {
#if UNITY_EDITOR || !PLATFORM_ANDROID
		if (Input.GetMouseButtonDown(0) && Target.activeInHierarchy && _active) OnClick();

		if(Input.GetMouseButtonUp(0) && Target.activeInHierarchy && !_active) _active = true;
#else
        if (Input.touchCount < 1)
			return;
		Touch firstTouch = Input.GetTouch(0);

		switch (firstTouch.phase) {
			default:
				break;
			case TouchPhase.Ended:
				if (Target.activeInHierarchy && _active)
					OnClick();
				break;
			case TouchPhase.Began:
				_active = true;
				break;
		}

#endif
    }

    public void OnClick() {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
#if UNITY_EDITOR || !PLATFORM_ANDROID
		pointerEventData.position = Input.mousePosition;
#else
        pointerEventData.position = Input.GetTouch(0).position;
        _active = false;
#endif
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);
        foreach (RaycastResult result in raycastResults) {
            if (result.gameObject == Target) return;
        }

        OnClickAway.Invoke();
    }
}
