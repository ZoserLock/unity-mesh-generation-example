using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace GameModules
{
    public enum DragStatus
    {
        Begin,
        Moving,
        End,
    }

    public enum HoldStatus
    {
        Begin,
        End,
    }

    public enum TouchStatus
    {
        Down,
        Hold,
        Up,
    }

    [System.Flags]
    public enum SwipeDirection
    {
        None = 0,
        Left = 0x1,
        Up = 0x2,
        Right = 0x4,
        Down = 0x8,

        Horizontal = Left | Right,
        Vertical = Up | Down,
        Full = Horizontal | Vertical
    }

    public sealed class InputManager : MonoSingleton<InputManager>
    {
        private class TouchInfo
        {
            public int id;
            public float beginTime;
            public Vector3 beginPosition;
            public bool valid = true;
            public bool holdProcessed = false;
        }

        [Header("Touch Setup")]
        [SerializeField]
        private float _tapMaxTime = 0.5f;

        [SerializeField]
        private float _swipeMaxTime = 0.4f;

        [SerializeField]
        private float _dragDeadZone = 15;

        [SerializeField]
        private float _swipeDeadZone = 15;

        [SerializeField]
        private float _dragAngleTreshold = 25;

        [SerializeField]
        private float _holdTime = 2.0f;

        [SerializeField]
        private bool _mouseSimulation = true;

        [SerializeField]
        private bool _continuousDragDetection = true;

        public delegate void OnTouchDelegate(TouchStatus touchStatus, Vector3 position);
        public event OnTouchDelegate OnTouchEvent;

        public delegate void OnTapDelegate(Vector3 position);
        public event OnTapDelegate OnTapEvent;

        public delegate void OnLongTapDelegate(Vector3 position);
        public event OnLongTapDelegate OnLongTapEvent;

        public delegate void OnHoldDelegate(HoldStatus status, Vector3 position);
        public event OnHoldDelegate OnHoldEvent;

        public delegate void OnDragDelegate(DragStatus status, Vector3 position, Vector3 last);
        public event OnDragDelegate OnDragEvent;

        public delegate void OnSwipeDelegate(SwipeDirection direction, Vector3 realDirection);
        public event OnSwipeDelegate OnSwipeEvent;

        public delegate void OnPinchZoomDelegate(float delta);
        public event OnPinchZoomDelegate OnPinchZoomEvent;

        private Dictionary<int, TouchInfo> _touchInfoList = new Dictionary<int, TouchInfo>(10);

        private TouchInfo _currentDragTouch;
        private Vector3 _lastMousePosition;

        private StaticObjectPool<TouchInfo> _infoPool = new StaticObjectPool<TouchInfo>(16);

        public SwipeDirection GetSwipeDirection(Vector3 currentTouchPosition, Vector3 beginPosition)
        {
            SwipeDirection direction = SwipeDirection.None;
            Vector3 to = currentTouchPosition - beginPosition;
            float angle = Vector2.Angle(Vector2.right, to);
            angle = Mathf.Sign(Vector3.Cross(Vector2.right, to).z) < 0 ? (360 - angle) % 360 : angle;

            if (IsAngleBetween(angle, 360 - _dragAngleTreshold, _dragAngleTreshold))
            {
                direction = SwipeDirection.Right;
            }
            else if (IsAngleBetween(angle, _dragAngleTreshold, 90 - _dragAngleTreshold))
            {
                direction = SwipeDirection.Right | SwipeDirection.Up;
            }
            else if (IsAngleBetween(angle, 90 - _dragAngleTreshold, 90 + _dragAngleTreshold))
            {
                direction = SwipeDirection.Up;
            }
            else if (IsAngleBetween(angle, 90 + _dragAngleTreshold, 180 - _dragAngleTreshold))
            {
                direction = SwipeDirection.Up | SwipeDirection.Left;
            }
            else if (IsAngleBetween(angle, 180 - _dragAngleTreshold, 180 + _dragAngleTreshold))
            {
                direction = SwipeDirection.Left;
            }
            else if (IsAngleBetween(angle, 180 + _dragAngleTreshold, 270 - _dragAngleTreshold))
            {
                direction = SwipeDirection.Left | SwipeDirection.Down;
            }
            else if (IsAngleBetween(angle, 270 - _dragAngleTreshold, 270 + _dragAngleTreshold))
            {
                direction = SwipeDirection.Down;
            }
            else if (IsAngleBetween(angle, 270 + _dragAngleTreshold, 360 - _dragAngleTreshold))
            {
                direction = SwipeDirection.Down | SwipeDirection.Right;
            }

            return direction;
        }

        protected override bool DestroyOnLoad()
        {
            return false;
        }

        void Update()
        {
            // Tap / Drag / Swipe
            for (int a = 0; a < Input.touchCount; ++a)
            {
                Touch touch = Input.GetTouch(a);
                HandleTouch(touch.fingerId, touch.position, touch.deltaPosition, touch.phase);
            }

            // Pinch Zoom
            if (Input.touchCount == 2)
            {
                if (OnPinchZoomEvent != null)
                {
                    Touch touchZero = Input.GetTouch(0);
                    Touch touchOne = Input.GetTouch(1);

                    Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                    Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

                    float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                    float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                    float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

                    OnPinchZoomEvent(deltaMagnitudeDiff);
                }
            }

            // Mouse Touch Simulation
            if (_mouseSimulation)
            {
                if (Input.touchCount == 0)
                {
                    for (int a = 0; a < 3; ++a)
                    {
                        if (Input.GetMouseButtonDown(a))
                        {
                            HandleTouch(- (1 + a), Input.mousePosition, Input.mousePosition - _lastMousePosition, TouchPhase.Began);
                        }

                        if (Input.GetMouseButton(a))
                        {
                            HandleTouch(- (1 + a), Input.mousePosition, Input.mousePosition - _lastMousePosition, TouchPhase.Moved);
                        }

                        if (Input.GetMouseButtonUp(a))
                        {
                            HandleTouch(- (1 + a), Input.mousePosition, Input.mousePosition - _lastMousePosition, TouchPhase.Ended);
                        }
                    }

                    _lastMousePosition = Input.mousePosition;

                    if (OnPinchZoomEvent != null)
                    {
                        if (Input.GetAxis("Mouse ScrollWheel") > 0)
                        {
                            OnPinchZoomEvent(-1);
                        }

                        if (Input.GetAxis("Mouse ScrollWheel") < 0)
                        {
                            OnPinchZoomEvent(1);
                        }
                    }
                }
            }
        }

        private void ProcessTouchHold(Vector3 touchPosition)
        {
            if (OnTouchEvent != null)
            {
                OnTouchEvent(TouchStatus.Hold, touchPosition);
            }
        }

        private void ProcessTouchDown(Vector3 touchPosition)
        {
            if (OnTouchEvent != null)
            {
                OnTouchEvent(TouchStatus.Down,touchPosition);
            }
        }

        private void ProcessTouchUp(Vector3 touchPosition)
        {
            if (OnTouchEvent != null)
            {
                OnTouchEvent(TouchStatus.Up, touchPosition);
            }
        }

        private void ProcessTap(Vector3 touchPosition)
        {
            if (OnTapEvent != null)
            {
                OnTapEvent(touchPosition);
            }
        }

        private void ProcessOnHold(HoldStatus status, Vector3 touchPosition)
        {
            if (OnHoldEvent != null)
            {
                OnHoldEvent(status, touchPosition);
            }
        }

        private void ProcessLongTap(Vector3 touchPosition)
        {
            if (OnLongTapEvent != null)
            {
                OnLongTapEvent(touchPosition);
            }
        }

        private void ProcessDrag(DragStatus status, Vector3 position, Vector3 last)
        {
            if (OnDragEvent != null)
            {
                OnDragEvent(status, position, last);
            }
        }

        private void ProcessSwipe(SwipeDirection direction, Vector3 realDirection)
        {
            if (OnSwipeEvent != null)
            {
                OnSwipeEvent(direction, realDirection);
            }
        }

        private void HandleTouch(int touchFingerId, Vector3 touchPosition, Vector3 delta, TouchPhase touchPhase)
        {
            TouchInfo info = null;

            if (!_touchInfoList.TryGetValue(touchFingerId, out info))
            {
                info = _infoPool.GetInstance();
                _touchInfoList.Add(touchFingerId, info);
            }

            switch (touchPhase)
            {
                case TouchPhase.Began:

                    info.id = touchFingerId;
                    info.beginTime = Time.time;
                    info.beginPosition = touchPosition;
                    info.valid = true;
                    info.holdProcessed = false;

                    if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(info.id))
                    {
                        ProcessTouchDown(touchPosition);
                    }
                    else
                    {
                        info.valid = false;
                    }

                    break;
                case TouchPhase.Stationary:
                case TouchPhase.Moved:

                    if (info.valid)
                    {
                        ProcessTouchHold(touchPosition);
                    }

                    if (_currentDragTouch == null)
                    {
                        if (Vector3.Distance(touchPosition, info.beginPosition) > _dragDeadZone)
                        {
                            _currentDragTouch = info;
                            if (info.valid)
                            {
                                ProcessDrag(DragStatus.Begin, touchPosition, delta);
                            }
                        }
                        else
                        {
                            if (info.valid)
                            {
                                float elapsed = Time.time - info.beginTime;
                                if (!info.holdProcessed && elapsed > _holdTime)
                                {
                                    ProcessOnHold(HoldStatus.Begin, touchPosition);
                                    info.holdProcessed = true;
                                }
                            }
                        }
                    }
                    else if (_currentDragTouch == info)
                    {
                        if (info.valid)
                        {
                            if (_continuousDragDetection || delta.magnitude > 0.1f)
                            {
                                ProcessDrag(DragStatus.Moving, touchPosition, delta);
                            }
                        }
                    }

                    break;
                case TouchPhase.Ended:

                    if (_currentDragTouch != null && _currentDragTouch == info)
                    {
                        if (info.valid)
                        {
                            ProcessDrag(DragStatus.End, touchPosition, delta);
                        }
                        _currentDragTouch = null;
                    }

                    if (Vector3.Distance(touchPosition, info.beginPosition) > _swipeDeadZone)
                    {
                        if ((Time.time - info.beginTime) < _swipeMaxTime)
                        {
                            if (OnSwipeEvent != null)
                            {
                                SwipeDirection direction = GetSwipeDirection(touchPosition, info.beginPosition);
                                if (info.valid && direction != SwipeDirection.None)
                                {
                                    ProcessSwipe(direction, (touchPosition - info.beginPosition).normalized);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(info.id))
                        {
                            float elapsed = Time.time - info.beginTime;
                            if (elapsed < _tapMaxTime)
                            {
                                ProcessTap(touchPosition);
                            }
                            else
                            {
                                ProcessLongTap(touchPosition);
                            }
                        }
                    }

                    if (info.valid)
                    {
                        if (info.holdProcessed)
                        {
                            ProcessOnHold(HoldStatus.End, touchPosition);
                        }
                        ProcessTouchUp(touchPosition);
                    }

                    _infoPool.ReleaseInstance(info);
                    _touchInfoList.Remove(touchFingerId);

                    break;
                case TouchPhase.Canceled:
                    if (_currentDragTouch != null && _currentDragTouch == info)
                    {
                        if (info.valid)
                        {
                            ProcessDrag(DragStatus.End, touchPosition, delta);
                        }
                        _currentDragTouch = null;
                    }

                    if (info.valid)
                    {
                        if (info.holdProcessed)
                        {
                            ProcessOnHold(HoldStatus.End, touchPosition);
                        }
                    }

                    _infoPool.ReleaseInstance(info);
                    _touchInfoList.Remove(touchFingerId);
                    break;
            }
        }

        /// <summary>
        /// Check if the given angle is between 2 angles. 360 degress check.
        /// </summary>
        private bool IsAngleBetween(float angle, float minAngle, float maxAngle)
        {
            if (minAngle > maxAngle)
            {
                if (angle > minAngle)
                {
                    return true;
                }
                else if (angle < maxAngle)
                {
                    return true;
                }
            }
            return angle > minAngle && angle < maxAngle;
        }
    }
}