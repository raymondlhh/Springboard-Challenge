using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Pinch Zoom Settings")]
    [SerializeField] private float maxForwardOffset = 3f;
    [SerializeField] private float pinchSensitivity = 0.01f;
    [SerializeField] private float positionLerpSpeed = 10f;
    [SerializeField] private float minYPosition = 0.3f;
    [SerializeField] private float focusPlaneHeight = 0f;
    [SerializeField] private float focusLerpSpeed = 10f;
    [SerializeField] private float panActivationThreshold = 0.05f;
#if UNITY_EDITOR
    [SerializeField] private bool enableMouseWheelZoom = true;
    [SerializeField] private float scrollSensitivity = 1f;
#endif

    private Vector3 _defaultPosition;
    private float _currentZoomOffset;
    private float _targetZoomOffset;
    private Vector3 _currentFocusOffset;
    private Vector3 _targetFocusOffset;
    private Camera _camera;
    private bool _isPanning;
    private Vector3 _lastPanPoint;

    private void Start()
    {
        _defaultPosition = transform.position;
        _currentZoomOffset = 0f;
        _targetZoomOffset = 0f;
        _currentFocusOffset = Vector3.zero;
        _targetFocusOffset = Vector3.zero;
        _camera = GetComponent<Camera>() ?? Camera.main;
    }

    private void Update()
    {
        HandlePinchZoom();
        HandlePan();
#if UNITY_EDITOR
        if (enableMouseWheelZoom)
        {
            HandleScrollZoom();
        }
#endif
        UpdateCameraPosition();
    }

    private void HandlePinchZoom()
    {
        if (Input.touchCount < 2)
        {
            return;
        }

        Touch touchZero = Input.GetTouch(0);
        Touch touchOne = Input.GetTouch(1);

        Vector2 previousPositionZero = touchZero.position - touchZero.deltaPosition;
        Vector2 previousPositionOne = touchOne.position - touchOne.deltaPosition;

        float previousMagnitude = (previousPositionZero - previousPositionOne).magnitude;
        float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

        float magnitudeDelta = previousMagnitude - currentMagnitude;
        float zoomDelta = -magnitudeDelta * pinchSensitivity;

        // Positive delta moves the camera forward, negative moves it back out.
        if (Mathf.Approximately(zoomDelta, 0f))
        {
            return;
        }

        _targetZoomOffset = Mathf.Clamp(_targetZoomOffset + zoomDelta, 0f, maxForwardOffset);
        UpdateTargetFocusOffset(touchZero, touchOne);
        _isPanning = false;
    }

    private void UpdateCameraPosition()
    {
        _currentZoomOffset = Mathf.Lerp(_currentZoomOffset, _targetZoomOffset, positionLerpSpeed * Time.deltaTime);
        bool hasZoom = _targetZoomOffset > panActivationThreshold || _currentZoomOffset > panActivationThreshold;

        Vector3 targetFocus = hasZoom ? _targetFocusOffset : Vector3.zero;
        _currentFocusOffset = Vector3.Lerp(_currentFocusOffset, targetFocus, focusLerpSpeed * Time.deltaTime);

        Vector3 horizontalOffset = hasZoom ? _currentFocusOffset : Vector3.zero;
        Vector3 desiredPosition = _defaultPosition + horizontalOffset + transform.forward * _currentZoomOffset;
        desiredPosition.y = Mathf.Max(desiredPosition.y, minYPosition);
        transform.position = desiredPosition;
    }

#if UNITY_EDITOR
    private void HandleScrollZoom()
    {
        float scrollValue = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scrollValue, 0f))
        {
            return;
        }

        float zoomDelta = scrollValue * scrollSensitivity;
        _targetZoomOffset = Mathf.Clamp(_targetZoomOffset + zoomDelta, 0f, maxForwardOffset);
        if (!Mathf.Approximately(zoomDelta, 0f))
        {
            _isPanning = false;
        }
    }
#endif

    private void UpdateTargetFocusOffset(Touch touchZero, Touch touchOne)
    {
        if (_camera == null)
        {
            return;
        }

        Vector2 midpoint = (touchZero.position + touchOne.position) * 0.5f;
        if (TryGetPointOnFocusPlane(midpoint, out Vector3 focusPoint))
        {
            Vector3 referencePoint = new Vector3(_defaultPosition.x, focusPlaneHeight, _defaultPosition.z);
            Vector3 focusOffset = focusPoint - referencePoint;
            focusOffset.y = 0f;
            _targetFocusOffset = focusOffset;
        }
    }

    private bool TryGetPointOnFocusPlane(Vector2 screenPosition, out Vector3 worldPoint)
    {
        Plane focusPlane = new Plane(Vector3.up, new Vector3(0f, focusPlaneHeight, 0f));
        Ray ray = _camera.ScreenPointToRay(screenPosition);
        if (focusPlane.Raycast(ray, out float enter))
        {
            worldPoint = ray.GetPoint(enter);
            return true;
        }

        worldPoint = Vector3.zero;
        return false;
    }

    private void HandlePan()
    {
        if (_camera == null)
        {
            return;
        }

        if (_currentZoomOffset <= panActivationThreshold && _targetZoomOffset <= panActivationThreshold)
        {
            _isPanning = false;
            return;
        }

        if (Input.touchCount != 1)
        {
            _isPanning = false;
            return;
        }

        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                _isPanning = TryGetPointOnFocusPlane(touch.position, out _lastPanPoint);
                break;
            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (!_isPanning)
                {
                    _isPanning = TryGetPointOnFocusPlane(touch.position, out _lastPanPoint);
                    break;
                }

                if (TryGetPointOnFocusPlane(touch.position, out Vector3 currentPoint))
                {
                    Vector3 delta = currentPoint - _lastPanPoint;
                    delta.y = 0f;
                    _targetFocusOffset -= delta;
                    _lastPanPoint = currentPoint;
                }
                break;
            default:
                _isPanning = false;
                break;
        }
    }
}