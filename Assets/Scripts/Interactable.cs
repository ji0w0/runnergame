using Unity.AppUI.UI;
using UnityEngine;

public enum ItemType
{
    Hamburger,
    Money,
    Trash,
    Ink,

    Item_startIndex = 10,
    Item_scaleUp = 11,
    Item_scaleDown = 12,
}

public class Interactable : MonoBehaviour
{
    public ItemType type;
    public int amount = 1;

    [Header("Pile Settings")]
    public float pileStepY = 0.15f; // ✅ 종류(프리팹)별로 다르게 설정

    [Header("Item Rotation")]
    public float rotationSpeed = 100f; // 회전 속도 (도/초)

    [Header("Optional")]
    public AudioClip pickupSfx;
    public GameObject pickupVfxPrefab;

    [Header("PileStepY Gizmo (Editor Only)")]
    public bool showPileStepGizmo = true;
    public float gizmoWidth = 0.35f;   // 노란 영역 가로
    public float gizmoDepth = 0.35f;   // 노란 영역 세로
    public float gizmoYOffset = 0.0f;  // 필요하면 기준 높이 살짝 올리기/내리기

    public bool _picked; // 중복 줍기 방지

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void Update()
    {
        // ItemType이 Item_startIndex보다 크면 회전
        if ((int)type > (int)ItemType.Item_startIndex && !_picked)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_picked) return;

        // Player 태그가 아니면 무시
        if (!other.CompareTag("Player"))
            return;

        // 플레이어 루트에서 Player를 찾는다 (자식 콜라이더여도 OK)
        var player = other.GetComponentInParent<Player>();
        if (player == null) return;

        _picked = true;

        // VFX와 SFX 재생
        if (pickupVfxPrefab != null)
            Instantiate(pickupVfxPrefab, transform.position, Quaternion.identity);

        if (pickupSfx != null)
            AudioSource.PlayClipAtPoint(pickupSfx, transform.position);

        // ItemA 타입이면 플레이어 크기 1.5배로 키우기
        if (type == ItemType.Item_scaleUp)
        {
            player.ScalePlayer(1.2f);
        }
        else if (type == ItemType.Item_scaleDown)
        {
            player.ScalePlayer(0.8f);
        }

        // Stage의 오브젝트 풀로 반환
        var stage = FindAnyObjectByType<Stage>();
        if (stage != null)
        {
            stage.ReturnObjectToPool(gameObject);
        }
        else
        {
            // Stage를 찾지 못했으면 그냥 비활성화
            gameObject.SetActive(false);
        }
    }

    // PlayerInventory에서 호출: "줍힌 뒤 상태 전환"
    public void OnPickedUp()
    {
        _picked = true;

        // Trigger 다시 안 들어오게 콜라이더 끄기
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 물리 영향 끄기
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // (선택) Layer 바꿔서 충돌/레이캐스트 제외하고 싶으면 여기서 변경
        // gameObject.layer = LayerMask.NameToLayer("Collected");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!showPileStepGizmo) return;

        float h = Mathf.Max(0f, pileStepY);
        if (h <= 0f) return;

        // 노란색(반투명) 박스로 "이 아이템이 차지하는 높이"를 표시
        Color fill = new Color(1f, 1f, 0f, 0.18f); // yellow, alpha
        Color wire = new Color(1f, 1f, 0f, 0.9f);

        Vector3 basePos = transform.position + Vector3.up * gizmoYOffset;

        // 박스 중심은 높이의 중간으로
        Vector3 center = basePos + Vector3.up * (h * 0.5f);
        Vector3 size = new Vector3(gizmoWidth, h, gizmoDepth);

        Gizmos.matrix = Matrix4x4.identity;

        Gizmos.color = fill;
        Gizmos.DrawCube(center, size);

        Gizmos.color = wire;
        Gizmos.DrawWireCube(center, size);

        // 기준선(바닥)과 상단선도 같이 보여줌
        Gizmos.DrawLine(basePos, basePos + Vector3.up * h);
        Gizmos.DrawSphere(basePos, 0.015f);
        Gizmos.DrawSphere(basePos + Vector3.up * h, 0.015f);
    }
#endif
}
