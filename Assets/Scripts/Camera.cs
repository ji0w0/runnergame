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

    void LateUpdate()
    {
        if (player == null)
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
}
