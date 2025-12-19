using UnityEngine;

public class StageEndScoring : MonoBehaviour
{
    public InkPainter painter;

    public void OnStageEnd()
    {
        float area = painter.ComputeInkAreaWorld();
        int score = Mathf.RoundToInt(area * 10f); // 배수는 너가 정해
        Debug.Log($"Ink Area: {area:F2} m^2, Score: {score}");
    }
}
