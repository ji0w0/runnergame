using UnityEngine;

public class InkPainter : MonoBehaviour
{
    [Header("Ground (Plane)")]
    public Transform ground;                 // Plane Transform
    public Renderer groundRenderer;          // Plane Renderer
    public int resolution = 1024;            // 512~2048 적당
    public Material stampMat;                // 아래 셰이더로 만든 머티리얼
    public string groundInkTextureProperty = "_InkTex"; // 바닥 머티리얼 프로퍼티명
    public Texture2D brushTex;

    [Header("Stamp Settings")]
    public float stampRadius = 0.35f;        // 스탬프 반지름 (월드 단위)

    RenderTexture inkRT;
    Vector2 groundSize; // (X,Z) world size
    Color currentInkColor;

    static readonly int CenterId = Shader.PropertyToID("_Center");
    static readonly int RadiusId = Shader.PropertyToID("_Radius");
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    static readonly int InkColorId = Shader.PropertyToID("_InkColor");
    static readonly int StampColorId = Shader.PropertyToID("_StampColor");
    static readonly int BrushTexId = Shader.PropertyToID("_BrushTex");
    static readonly int RotationId = Shader.PropertyToID("_Rotation");

    void Awake()
    {
        if (ground == null || groundRenderer == null || stampMat == null)
        {
            Debug.LogError("[InkPainter] ground/groundRenderer/stampMat 세팅해라.");
            enabled = false;
            return;
        }

        groundSize = new Vector2(10f * ground.lossyScale.x, 10f * ground.lossyScale.z);

        inkRT = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32);
        inkRT.wrapMode = TextureWrapMode.Clamp;
        inkRT.filterMode = FilterMode.Bilinear;
        inkRT.Create();

        ClearInk();

        groundRenderer.material.SetTexture(groundInkTextureProperty, inkRT);
    }

    public void ClearInk()
    {
        var prev = RenderTexture.active;
        RenderTexture.active = inkRT;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = prev;
    }

    // worldRadius: 월드 단위 반지름(0.2~0.6 같은)
    public void Stamp(Vector3 worldPos, float worldRadius, Color color)
    {
        Stamp(worldPos, worldRadius, color, brushTex);
    }

    // 텍스처를 지정할 수 있는 오버로드
    public void Stamp(Vector3 worldPos, float worldRadius, Color color, Texture2D customBrushTex)
    {
        Vector2 uv = WorldToUV(worldPos);
        Debug.Log($"Stamp world={worldPos} uv={uv}");
        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return;

        // Plane이 정사각이 아닐 수 있어서 X/Z 각각 normalize 해줌
        // 셰이더는 radius를 "UV 반지름"으로 받는다고 가정.
        float uvRadiusX = worldRadius / groundSize.x;
        float uvRadiusY = worldRadius / groundSize.y;

        stampMat.SetVector(CenterId, new Vector4(uv.x, uv.y, uvRadiusX, uvRadiusY));
        stampMat.SetTexture(BrushTexId, customBrushTex != null ? customBrushTex : brushTex);
        stampMat.SetColor(StampColorId, color); // ✅ 스탬프마다 색 다르게

        var tmp = RenderTexture.GetTemporary(inkRT.descriptor);
        Graphics.Blit(inkRT, tmp);

        stampMat.SetTexture(MainTexId, tmp);
        Graphics.Blit(tmp, inkRT, stampMat);

        RenderTexture.ReleaseTemporary(tmp);
    }

    // 새로운 오버로드 (회전 지원)
    public void Stamp(Vector3 worldPos, float worldRadius, Color color, Texture2D customBrushTex, float rotationDegrees)
    {
        Vector2 uv = WorldToUV(worldPos);
        Debug.Log($"Stamp world={worldPos} uv={uv}");
        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return;

        // Plane이 정사각이 아닐 수 있어서 X/Z 각각 normalize 해줌
        // 셰이더는 radius를 "UV 반지름"으로 받는다고 가정.
        float uvRadiusX = worldRadius / groundSize.x;
        float uvRadiusY = worldRadius / groundSize.y;

        stampMat.SetVector(CenterId, new Vector4(uv.x, uv.y, uvRadiusX, uvRadiusY));
        stampMat.SetTexture(BrushTexId, customBrushTex != null ? customBrushTex : brushTex);
        stampMat.SetColor(StampColorId, color); // ✅ 스탬프마다 색 다르게
        stampMat.SetFloat(RotationId, rotationDegrees);// * Mathf.Deg2Rad);

        var tmp = RenderTexture.GetTemporary(inkRT.descriptor);
        Graphics.Blit(inkRT, tmp);

        stampMat.SetTexture(MainTexId, tmp);
        Graphics.Blit(tmp, inkRT, stampMat);

        RenderTexture.ReleaseTemporary(tmp);
    }

    Vector2 WorldToUV(Vector3 worldPos)
    {
        //Vector3 local = ground.InverseTransformPoint(worldPos);
        //// Plane은 중심이 (0,0,0)이고 XZ가 -size/2 ~ +size/2 라고 가정
        //float u = (local.x / groundSize.x) + 0.5f;
        //float v = (local.z / groundSize.y) + 0.5f;
        //return new Vector2(u, v);

        Bounds b = groundRenderer.bounds; // 월드 기준 AABB
        float u = Mathf.InverseLerp(b.min.x, b.max.x, worldPos.x);
        float v = Mathf.InverseLerp(b.min.z, b.max.z, worldPos.z);

        u = 1f - u;
        v = 1f - v;

        return new Vector2(u, v);
    }

    // 스테이지 끝날 때 1회 호출
    public float ComputeInkAreaWorld(byte alphaThreshold = 10)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = inkRT;

        var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
        tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        tex.Apply(false, false);

        RenderTexture.active = prev;

        var pixels = tex.GetPixels32();
        Destroy(tex);

        int inkCount = 0;
        for (int i = 0; i < pixels.Length; i++)
            if (pixels[i].a > alphaThreshold)
                inkCount++;

        float coverage01 = inkCount / (float)(pixels.Length * 10 / 14); // 0~1
        return coverage01 * 100f; // 0~100

        //var prev = RenderTexture.active;
        //RenderTexture.active = inkRT;

        //Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
        //tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        //tex.Apply(false, false);

        //RenderTexture.active = prev;

        //var pixels = tex.GetPixels32();
        //Destroy(tex);

        //int inkCount = 0;
        //for (int i = 0; i < pixels.Length; i++)
        //    if (pixels[i].a > alphaThreshold) inkCount++;

        //float coverage = inkCount / (float)pixels.Length;
        //float worldArea = groundSize.x * groundSize.y;
        //return coverage * worldArea;
    }

    public void SetInkColor(Color c)
    {
        currentInkColor = c;
        stampMat.SetColor(InkColorId, c);
        groundRenderer.material.SetColor(InkColorId, c);
    }
}
