using UnityEngine;

public class Camera : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Offset")]
    public Vector3 offset = new Vector3(0f, 5f, -8f);

    [Header("Follow Settings")]
    public float followSpeed = 8f;

    [Header("Follow Axis")]
    public bool followX = true;
    public bool followY = true;
    public bool followZ = true;

    Vector3 _initialPosition;
    Quaternion _initialRotation;
    bool _isFollowing = true;

    void Awake()
    {
        // 초기 위치와 회전 저장
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
    }

    void LateUpdate()
    {
        if (player == null || !_isFollowing)
            return;

        Vector3 targetPos = transform.position;  // 기본 현재 위치 유지

        // 플레이어 위치 + offset 계산
        Vector3 desired = player.position + offset;

        // 선택된 축만 따라간다
        if (followX) targetPos.x = desired.x;
        if (followY) targetPos.y = desired.y;
        if (followZ) targetPos.z = desired.z;

        // 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
    }

    /// <summary>
    /// 플레이어 추적을 시작합니다.
    /// </summary>
    public void StartFollowing()
    {
        _isFollowing = true;
    }

    /// <summary>
    /// 플레이어 추적을 멈춥니다.
    /// </summary>
    public void StopFollowing()
    {
        _isFollowing = false;
    }

    /// <summary>
    /// 카메라를 특정 위치와 회전으로 이동시킵니다.
    /// </summary>
    /// <param name="position">목표 위치</param>
    /// <param name="rotation">목표 회전</param>
    /// <param name="duration">이동 시간 (0이면 즉시 이동)</param>
    public void MoveTo(Vector3 position, Quaternion rotation, float duration = 0f)
    {
        StopFollowing();

        if (duration <= 0f)
        {
            // 즉시 이동
            transform.position = position;
            transform.rotation = rotation;
        }
        else
        {
            // 부드럽게 이동 (코루틴 사용)
            StartCoroutine(MoveToCoroutine(position, rotation, duration));
        }
    }

    /// <summary>
    /// 카메라를 초기 위치와 회전으로 되돌립니다.
    /// </summary>
    public void ResetToInitial()
    {
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;
    }

    System.Collections.IEnumerator MoveToCoroutine(Vector3 targetPosition, Quaternion targetRotation, float duration)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        transform.position = targetPosition;
        transform.rotation = targetRotation;
    }
}
