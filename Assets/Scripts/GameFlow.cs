using UnityEngine;

public class GameFlow : MonoBehaviour
{
    [Header("UI")]
    public UI_Victory victoryUI;

    [Header("Game Systems")]
    public InkPainter painter;
    public Stage stage;
    public Boss boss;

    [Header("Player")]
    public Player player;

    [Header("Camera")]
    public Camera gameCamera;
    public Vector3 endCameraPosition = new Vector3(0, 10, -5); // 스테이지 종료 시 카메라 위치
    public Vector3 endCameraRotation = new Vector3(45, 0, 0);  // 스테이지 종료 시 카메라 회전 (Euler)
    public float cameraTransitionDuration = 1f; // 카메라 이동 시간 (0 = 즉시)

    void Awake()
    {
        if (victoryUI != null)
            victoryUI.OnContinue += HandleContinue;

        if (boss != null)
            boss.arrivalCallback = OnStageEnd;
    }

    void HandleContinue()
    {
        // UI 숨기기
        victoryUI.Hide();

        // 잉크 초기화
        if (painter != null)
        {
            painter.ClearInk();
        }

        // 스테이지 리셋
        if (stage != null)
        {
            stage.ResetStage();
        }

        // 플레이어 리셋
        if (player != null)
        {
            player.ResetPlayer();
        }

        // 카메라를 초기 위치로 되돌리고 플레이어 추적 재개
        if (gameCamera != null)
        {
            gameCamera.ResetToInitial();
            gameCamera.StartFollowing();
        }

        // Boss 리셋 (필요한 경우)
        if (boss != null)
        {
            // Boss에 리셋 메서드가 있다면 호출
            //boss.ResetStage();
        }
    }

    public void OnStageEnd()
    {
        // 플레이어 정지
        if (player != null)
        {
            player.FreezePlayer();
        }

        // 카메라를 특정 위치로 이동하고 플레이어 추적 멈춤
        if (gameCamera != null)
        {
            Quaternion targetRotation = Quaternion.Euler(endCameraRotation);
            gameCamera.MoveTo(endCameraPosition, targetRotation, cameraTransitionDuration);
        }

        float area = painter.ComputeInkAreaWorld();
        int score = Mathf.RoundToInt(area); // 배수는 너가 정해
        Debug.Log($"Ink Area: {area:F2} m^2, Score: {score}");

        // Victory UI 표시
        if (victoryUI != null)
        {
            victoryUI.Show(score);
        }
    }
}
