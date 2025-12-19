using UnityEngine;

public class InkOnSphereHit : MonoBehaviour
{
    public InkPainter painter;
    public LayerMask groundMask;

    Color myColor;
    Renderer sphereRenderer;

    private void Start()
    {
        myColor = GetRandomVividColor();
        sphereRenderer = GetComponent<Renderer>();
        
        // 머테리얼 색상을 myColor로 설정
        if (sphereRenderer != null)
        {
            sphereRenderer.material.color = myColor;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Trigger에는 충돌점이 없으므로,
        // sphere 중심에서 아래로 레이캐스트
        Vector3 origin = transform.position + Vector3.up * 0.2f;

        if (Physics.Raycast(origin, Vector3.down, out var hit, 2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            painter.SetInkColor(myColor);
            painter.Stamp(hit.point, painter.stampRadius, myColor);
            Debug.DrawRay(hit.point, Vector3.up * 0.5f, Color.magenta, 1f);
        }
    }

    //    public void SetRandomInkColor()
    //    {
    //        currentInkColor = GetRandomVividColor();
    //        stampMat.SetColor(InkColorId, currentInkColor);
    //    }

    Color GetRandomVividColor()
    {
        float h = Random.value;                  // 색상: 자유
        float s = Random.Range(0.6f, 1.0f);      // 채도 하한
        float v = Random.Range(0.7f, 1.0f);      // 명도 하한
        return Color.HSVToRGB(h, s, v);
    }
}