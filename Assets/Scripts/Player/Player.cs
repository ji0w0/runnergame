using UnityEngine;

[RequireComponent(typeof(PlayerController))]
//[RequireComponent(typeof(PlayerInventory))]
public class Player : MonoBehaviour
{
    public PlayerController Controller { get; private set; }
    public PlayerInventory Inventory { get; private set; }

    Vector3 _startPosition;
    Vector3 _originalScale;  // 원래 크기 저장

    void Awake()
    {
        Controller = GetComponent<PlayerController>();
        Inventory = GetComponent<PlayerInventory>();
        
        // 초기 위치 및 크기 저장
        _startPosition = transform.position;
        _originalScale = transform.localScale;
    }

    /// <summary>
    /// 플레이어를 초기 상태로 리셋합니다.
    /// </summary>
    public void ResetPlayer()
    {
        // 위치 초기화
        transform.position = _startPosition;
        
        // 크기 초기화
        transform.localScale = _originalScale;
        
        // 스탬프 반지름 초기화
        if (Controller != null)
        {
            Controller.ResetStampRadius();
        }
        
        // Controller Freeze 해제
        if (Controller != null)
        {
            Controller.Freeze(false);
        }
        
        // Inventory 초기화 (필요한 경우)
        if (Inventory != null)
        {
            // Inventory.Clear();
        }
    }

    /// <summary>
    /// 플레이어를 정지시킵니다.
    /// </summary>
    public void FreezePlayer()
    {
        if (Controller != null)
        {
            Controller.Freeze(true);
        }
    }

    /// <summary>
    /// 플레이어 크기를 배율만큼 변경합니다.
    /// </summary>
    /// <param name="scaleMultiplier">크기 배율 (1.5 = 1.5배)</param>
    public void ScalePlayer(float scaleMultiplier)
    {
        transform.localScale *= scaleMultiplier;
        
        // 스탬프 반지름도 같은 배율로 조정
        if (Controller != null)
        {
            Controller.ScaleStampRadius(scaleMultiplier);
        }
    }

    /// <summary>
    /// 플레이어 크기를 원래대로 되돌립니다.
    /// </summary>
    public void ResetScale()
    {
        transform.localScale = _originalScale;
        
        // 스탬프 반지름도 원래대로
        if (Controller != null)
        {
            Controller.ResetStampRadius();
        }
    }
}
