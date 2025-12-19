using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float forwardSpeed = 5f;        // 앞으로 가는 속도 (z+)
    public float horizontalSpeed = 10f;    // 드래그 시 좌우 이동 감도
    public float horizontalLimit = 3f;     // 좌우 이동 가능 범위

    [Header("Animation")]
    public Animator animator;              // 플레이어 Animator
    public float animationSpeed = 2f;      // 애니메이션 속도 배율

    [Header("Ink Painting")]
    public InkPainter painter;             // InkPainter 참조
    public Color playerInkColor = Color.blue; // 플레이어 잉크 색상
    public Texture2D playerBrushTexture;   // 플레이어 브러시 텍스처 (null이면 기본 텍스처 사용)
    public float playerStampRadius = 0.35f; // 플레이어 스탬프 반지름 (월드 단위)
    public float stampInterval = 0.1f;     // 스탬프 찍는 간격 (초)
    public LayerMask groundMask;           // 바닥 레이어
    public float stampOffsetX = 0.1f;        // 스탬프 좌우 오프셋
    public bool stampFlipFlop;

    float _lastPointerX;
    bool _isDragging;
    float _lastStampTime;

    public bool IsFrozen { get; private set; }

    void Start()
    {
        SetupAnimator();

        //_lastStampTime = stampInterval - 0.02f; // 시작하자마자 바로 찍히도록 약간 보정
    }

    void SetupAnimator()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator != null)
        {
            animator.speed = animationSpeed;
        }
    }

    public void Freeze(bool freeze)
    {
        IsFrozen = freeze;

        // 얼렸을 때 애니메이션도 정지
        if (animator != null)
        {
            animator.speed = freeze ? 0f : animationSpeed;
        }
    }

    void Update()
    {
        if (IsFrozen) return;

        MoveForward();
        HandleDragHorizontal();
        HandleInkStamping();
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

    void HandleInkStamping()
    {
        if (painter == null) return;

        // 일정 간격으로만 스탬프 찍기
        if (Time.time - _lastStampTime < stampInterval)
            return;

        // 플레이어 위치에서 아래로 레이캐스트
        Vector3 origin = transform.position + Vector3.up * 5f;

        if (Physics.Raycast(origin, Vector3.down, out var hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
        {
            hit.point += Vector3.right * stampOffsetX * (stampFlipFlop ? 1 : -1);

            // 커스텀 텍스처와 반지름 사용
            painter.Stamp(hit.point, playerStampRadius, playerInkColor, playerBrushTexture);
            stampFlipFlop = !stampFlipFlop;
            _lastStampTime = Time.time;
        }
    }

    // 애니메이션 속도 동적 변경
    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = speed;
        if (animator != null && !IsFrozen)
        {
            animator.speed = animationSpeed;
        }
    }

    // 잉크 색상 변경
    public void SetInkColor(Color color)
    {
        playerInkColor = color;
    }

    // 브러시 텍스처 변경
    public void SetBrushTexture(Texture2D texture)
    {
        playerBrushTexture = texture;
    }

    // 스탬프 반지름 변경
    public void SetStampRadius(float radius)
    {
        playerStampRadius = radius;
    }
}
