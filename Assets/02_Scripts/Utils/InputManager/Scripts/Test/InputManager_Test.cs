using GameModules;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class InputManager_Test : MonoBehaviour
{
    private void RegisterInputFunctions()
    {
        InputManager.Instance.OnTouchEvent += OnTouchEvent;
        InputManager.Instance.OnTapEvent += OnTapEvent;
        InputManager.Instance.OnLongTapEvent += OnLongTapEvent;
        InputManager.Instance.OnPinchZoomEvent += OnPinchZoomEvent;
        InputManager.Instance.OnSwipeEvent += OnSwipeEvent;
        InputManager.Instance.OnDragEvent += OnDragEvent;
        InputManager.Instance.OnHoldEvent += OnHoldEvent;
    }

    private void OnHoldEvent(HoldStatus status, Vector3 position)
    {
        Debug.LogWarning("OnHoldEvent: "+ status + " " + position);
    }

    private void OnTouchEvent(TouchStatus status,Vector3 position)
    {
        Debug.LogWarning("OnTouchEvent: " + status + " " + position);
    }

    private void OnTapEvent(Vector3 position)
    {
        Debug.LogWarning("OnTapEvent: " + position);
    }

    private void OnLongTapEvent(Vector3 position)
    {
        Debug.LogWarning("OnLongTapEvent: " + position);
    }

    private void OnPinchZoomEvent(float delta)
    {
        Debug.LogWarning("OnPinchZoomEvent: " + delta);
    }

    private void OnSwipeEvent(SwipeDirection direction, Vector3 realDirection)
    {
        Debug.LogWarning("OnSwipeEvent: " + direction+" : "+ realDirection);
    }

    private void OnDragEvent(DragStatus status, Vector3 position, Vector3 last)
    {
        Debug.LogWarning("OnDragEvent: " + status + " : " + position+" : "+last);
    }

    void Start ()
    {
        RegisterInputFunctions();
    }
}
