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

    [Header("Rotation")]
    public float maxRotationAngle = 20f;   // 최대 회전 각도 (좌우)
    public float rotationSpeed = 5f;       // 회전 속도 (부드러운 전환)

    [Header("Ink Painting")]
    public InkPainter painter;             // InkPainter 참조
    public Color playerInkColor = Color.blue; // 플레이어 잉크 색상
    public Texture2D playerBrushTexture;   // 플레이어 브러시 텍스처 (null이면 기본 텍스처 사용)
    public float playerStampRadius = 0.35f; // 플레이어 스탬프 반지름 (월드 단위)
    public float stampInterval = 0.1f;     // 스탬프 찍는 간격 (초)
    public LayerMask groundMask;           // 바닥 레이어
    public float stampOffsetX = 0.1f;        // 스탬프 좌우 오프셋
    public bool stampFlipFlop;

    float _lastStampTime;
    float _currentHorizontalVelocity;      // 현재 좌우 이동 속도
    float _targetRotationY;                // 목표 Y 회전값

    public bool IsFrozen { get; private set; }

    void Start()
    {
        SetupAnimator();

        _lastStampTime = -0.2f; // 시작하자마자 바로 찍히도록 약간 보정
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
        UpdateRotation();
    }

    void MoveForward()
    {
        transform.Translate(0f, 0f, forwardSpeed * Time.deltaTime, Space.World);
    }

    void HandleDragHorizontal()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#endif

#if UNITY_ANDROID || UNITY_IOS
        HandleTouchInput();
#endif
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButton(0))
        {
            float screenCenterX = Screen.width * 0.5f;
            float mouseX = Input.mousePosition.x;
            
            // 화면 중심을 기준으로 좌우 이동
            MoveTowardsScreenPosition(mouseX, screenCenterX);
        }
        else
        {
            // 클릭하지 않으면 속도를 0으로
            _currentHorizontalVelocity = 0f;
        }
    }

    void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            float screenCenterX = Screen.width * 0.5f;
            float touchX = touch.position.x;
            
            // 화면 중심을 기준으로 좌우 이동
            MoveTowardsScreenPosition(touchX, screenCenterX);
        }
        else
        {
            _currentHorizontalVelocity = 0f;
        }
    }

    void MoveTowardsScreenPosition(float inputX, float screenCenterX)
    {
        // 화면 중심에서 입력 위치까지의 거리 (-1 ~ +1로 정규화)
        float offsetFromCenter = (inputX - screenCenterX) / (Screen.width * 0.5f);
        
        // 목표 X 위치 계산
        float targetX = offsetFromCenter * horizontalLimit;
        
        // 현재 위치에서 목표 위치로 이동
        Vector3 pos = transform.position;
        float moveX = (targetX - pos.x) * horizontalSpeed * Time.deltaTime;
        
        // 현재 좌우 이동 속도 저장 (회전 계산용)
        _currentHorizontalVelocity = moveX / Time.deltaTime;
        
        pos.x += moveX;
        pos.x = Mathf.Clamp(pos.x, -horizontalLimit, horizontalLimit);
        transform.position = pos;
    }

    void UpdateRotation()
    {
        // 좌우 이동 속도에 따라 목표 회전값 계산
        // 속도를 정규화하여 -1 ~ +1 범위로 변환
        float normalizedVelocity = Mathf.Clamp(_currentHorizontalVelocity / horizontalSpeed, -1f, 1f);
        
        // 목표 Y 회전값 계산 (오른쪽으로 갈수록 양수)
        _targetRotationY = normalizedVelocity * maxRotationAngle;

        // 현재 회전값을 목표값으로 부드럽게 전환
        Vector3 currentEuler = transform.localEulerAngles;
        
        // 0~360 범위를 -180~180 범위로 변환
        float currentY = currentEuler.y;
        if (currentY > 180f) currentY -= 360f;

        // Lerp로 부드럽게 회전
        float newY = Mathf.Lerp(currentY, _targetRotationY, rotationSpeed * Time.deltaTime);
        
        transform.localEulerAngles = new Vector3(currentEuler.x, newY, currentEuler.z);
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
            // 캐릭터의 Y 회전 각도 가져오기
            float rotationY = transform.localEulerAngles.y;
            
            // 0~360 범위를 -180~180 범위로 변환
            if (rotationY > 180f) rotationY -= 360f;

            hit.point += Vector3.right * stampOffsetX * (stampFlipFlop ? 1 : -1); //왼발오른발
            hit.point += Vector3.left * playerStampRadius * Mathf.Sin(rotationY) * 1 / 4;

            // 커스텀 텍스처, 반지름, 회전 각도 사용
            painter.Stamp(hit.point, playerStampRadius, playerInkColor, playerBrushTexture, rotationY);
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
