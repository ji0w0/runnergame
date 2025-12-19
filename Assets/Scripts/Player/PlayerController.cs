using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float forwardSpeed = 5f;        // 앞으로 가는 속도 (z+)
    public float horizontalSpeed = 10f;    // 드래그 시 좌우 이동 감도
    public float horizontalLimit = 3f;     // 좌우 이동 가능 범위

    float _lastPointerX;
    bool _isDragging;

    public bool IsFrozen { get; private set; }

    public void Freeze(bool freeze)
    {
        IsFrozen = freeze;
    }

    void Update()
    {
        if (IsFrozen) return;

        MoveForward();
        HandleDragHorizontal();
    }

    void MoveForward()
    {
        transform.Translate(0f, 0f, forwardSpeed * Time.deltaTime, Space.World);
    }

    void HandleDragHorizontal()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseDrag();
#endif

#if UNITY_ANDROID || UNITY_IOS
        HandleTouchDrag();
#endif
    }

    void HandleMouseDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _isDragging = true;
            _lastPointerX = Input.mousePosition.x;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            _isDragging = false;
        }

        if (!_isDragging)
            return;

        float currentX = Input.mousePosition.x;
        float deltaX = currentX - _lastPointerX;
        _lastPointerX = currentX;

        MoveHorizontal(deltaX);
    }

    void HandleTouchDrag()
    {
        if (Input.touchCount == 0)
        {
            _isDragging = false;
            return;
        }

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            _isDragging = true;
            _lastPointerX = touch.position.x;
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            _isDragging = false;
        }

        if (!_isDragging)
            return;

        float currentX = touch.position.x;
        float deltaX = currentX - _lastPointerX;
        _lastPointerX = currentX;

        MoveHorizontal(deltaX);
    }

    void MoveHorizontal(float deltaX)
    {
        float moveX = (deltaX / Screen.width) * horizontalSpeed;

        Vector3 pos = transform.position;
        pos.x += moveX;
        pos.x = Mathf.Clamp(pos.x, -horizontalLimit, horizontalLimit);
        transform.position = pos;
    }
}
