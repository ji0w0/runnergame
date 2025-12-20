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
    public Vector3 playerStartPosition = new Vector3(0, 0, 0); // 플레이어 시작 위치

    void Awake()
    {
        if (victoryUI != null)
            victoryUI.OnContinue += HandleContinue;

        if (boss != null)
            boss.arrivalCallback = OnStageEnd;

        // 플레이어 초기 위치 저장
        if (player != null)
        {
            playerStartPosition = player.transform.position;
        }
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

        // 플레이어 초기 위치로 이동
        if (player != null)
        {
            player.transform.position = playerStartPosition;
            
            // PlayerController의 Freeze 해제
            if (player.Controller != null)
            {
                player.Controller.Freeze(false);
            }
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
        if (player != null && player.Controller != null)
        {
            player.Controller.Freeze(true);
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
