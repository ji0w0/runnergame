using System;
using System.Collections;
using UnityEngine;

public class Boss : MonoBehaviour
{
    [Header("Final Area Trigger (앞쪽 영역 Collider)")]
    public Collider finalArea; // isTrigger = true

    [Header("Item Return")]
    public Transform itemReturnTransform;

    [Header("Give Settings")]
    public float giveMoveDuration = 0.25f;
    public float giveInterval = 0.1f;

    public UI_Victory victoryUI;
    public Stage stage;

    public Action arrivalCallback;

    bool _processing;

    void Reset()
    {
        // 실수 방지: finalArea를 자기 자신에서 찾고 Trigger로
        if (finalArea == null) finalArea = GetComponent<Collider>();
        if (finalArea != null) finalArea.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // Boss 오브젝트에 finalArea 콜라이더를 붙여쓰는 방식
        if (_processing) return;

        var player = other.GetComponentInParent<Player>();
        if (player == null) return;

        //StartCoroutine(ProcessPlayerArrival(player));
        OnPlayerArrival(player);
    }

    void OnPlayerArrival(Player player)
    {
        if (player.Controller != null)
            player.Controller.Freeze(true);

        arrivalCallback?.Invoke();
    }

    public void ResetStage()
    {
        Reset();
    }

    //IEnumerator ProcessPlayerArrival(Player player)
    //{
    //    _processing = true;

    //    // 1) 플레이어 멈추기
    //    if (player.Controller != null)
    //        player.Controller.Freeze(true);

    //    // 2) 아이템 하나씩 건네주기
    //    if (player.Inventory != null && player.Inventory.HasPiledItems())
    //    {
    //        yield return player.Inventory.GiveAllToBoss(
    //            itemReturnTransform,
    //            giveMoveDuration,
    //            giveInterval
    //        );
    //    }

    //    // 제출 끝난 뒤
    //    if (victoryUI != null && player.Inventory != null)
    //    {
    //        victoryUI.Show(
    //            player.Inventory.hamburger,
    //            player.Inventory.money,
    //            player.Inventory.trash
    //        );
    //    }
    //    else
    //    {
    //        Debug.LogWarning("[Boss] victoryUI or player.Inventory is missing.");
    //    }

    //    _processing = false;
    //}
}
