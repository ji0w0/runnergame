using UnityEngine;

public class GameFlow : MonoBehaviour
{
    public UI_Victory victoryUI;
    public Stage stage;

    void Awake()
    {
        if (victoryUI != null)
            victoryUI.OnContinue += HandleContinue;
    }

    void HandleContinue()
    {
        victoryUI.Hide();

        if (stage != null)
            stage.ResetStage();
    }
}
