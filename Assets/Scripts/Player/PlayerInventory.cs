using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [Header("Counts")]
    public int money;
    public int trash;
    public int hamburger;

    [Header("Pile (Stack Visual)")]
    public Transform pileTransform;      // 손 위에 쌓일 위치
    public Vector3 pileLocalOffset = Vector3.zero; // pileTransform 기준 추가 오프셋
    public bool keepWorldRotation = true; // 줍고 나서 회전 유지할지

    readonly List<Interactable> _piled = new(); // 쌓인 아이템들

    public void Add(ItemType type, int amount = 1)
    {
        if (amount <= 0) return;

        switch (type)
        {
            case ItemType.Money:
                money += amount;
                break;
            case ItemType.Trash:
                trash += amount;
                break;
            case ItemType.Hamburger:
                hamburger += amount;
                break;
        }

        // 필요하면 여기서 UI 업데이트 이벤트 호출
        // OnChanged?.Invoke();
    }

    public void PickupAndPile(Interactable item)
    {
        if (item == null) return;

        if (pileTransform == null)
        {
            Debug.LogError("[PlayerInventory] pileTransform is null. Set it in Inspector.", this);
            return;
        }

        // 1) 수량 처리
        Add(item.type, item.amount);

        // 2) 다시 못 줍게 처리(콜라이더/리짓바디 비활성)
        item.OnPickedUp();


        // ✅ 아이템별 pileStepY를 누적합으로 계산
        float y = 0f;
        for (int i = 0; i < _piled.Count; i++)
            y += Mathf.Max(0f, _piled[i].pileStepY);


        // 3) 쌓기(부모 지정 + 위치)
        _piled.Add(item);

        Transform t = item.transform;

        // 부모 지정 (월드 회전 유지 옵션)
        t.SetParent(pileTransform, worldPositionStays: keepWorldRotation);

        // pileTransform 기준으로 원하는 위치에 고정
        t.localPosition = pileLocalOffset + Vector3.up * y;

        // 회전도 고정하고 싶으면 원하는 값으로 설정 가능
        // t.localRotation = Quaternion.identity;
    }

    // ✅ Boss에게 하나씩 건네주기
    public IEnumerator GiveAllToBoss(
            Transform bossItemReturnTransform,
            float moveDuration = 0.35f,
            float interval = 0.1f
        )
    {
        if (bossItemReturnTransform == null)
        {
            Debug.LogError("[PlayerInventory] bossItemReturnTransform is null.", this);
            yield break;
        }

        float bossStackY = 0f;

        while (_piled.Count > 0)
        {
            int last = _piled.Count - 1;
            var item = _piled[last];
            _piled.RemoveAt(last);

            if (item != null)
            {
                Transform t = item.transform;

                // 부모 먼저 Boss로 변경 (월드 좌표 유지)
                t.SetParent(bossItemReturnTransform, worldPositionStays: true);

                Vector3 targetWorldPos =
                    bossItemReturnTransform.position + Vector3.up * bossStackY;

                // DOTween 이동 연출
                t.DOMove(targetWorldPos, moveDuration)
                 .SetEase(Ease.OutBack); // 👈 쫀득 포인트

                // 회전도 같이 정리하고 싶으면
                t.DORotateQuaternion(
                    bossItemReturnTransform.rotation,
                    moveDuration * 0.8f
                );

                bossStackY += Mathf.Max(0f, item.pileStepY);
            }

            yield return new WaitForSeconds(interval);
        }
    }

    public bool HasPiledItems() => _piled.Count > 0;
}
