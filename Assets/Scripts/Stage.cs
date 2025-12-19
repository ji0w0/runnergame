using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Stage : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public Interactable prefab;
        [Min(0f)] public float weight = 1f;
    }

    [Header("Spawn Table")]
    public List<SpawnEntry> spawnTable = new();

    [Header("Stage Layout")]
    [Min(1)] public int itemsPerRow = 3;     // 한 줄에 몇 개 배치할지
    [Min(0.1f)] public float spawnWidth = 6f; // 가로 폭 (x 방향)
    [Min(0.1f)] public float itemSpacing = 3f; // 줄 간격 (z 방향)
    [Min(0.1f)] public float stageLength = 100f; // 총 스테이지 길이

    [Header("Row Generation")]
    [Min(0.01f)] public float rowTickSeconds = 0.3f; // "주기적으로" 한 줄씩 만드는 템포(필요 최소치)
    public bool loopStage = true;      // 끝까지 만들면 다시 처음부터 반복할지
    public bool centerRows = true;     // row를 0 기준 대칭으로 배치할지
    public bool randomizeXInRow = false; // true면 각 칸의 x를 약간 랜덤
    [Range(0f, 1f)] public float spawnProbability = 1f; // 각 위치에서 스폰될 확률 (0~1)

    [Header("Object Pooling")]
    [Min(5)] public int initialPoolSize = 20; // 각 프리팹당 초기 풀 크기

    [Header("Hierarchy")]
    public Transform spawnRoot; // 생성된 아이템 parent

    // 내부 상태
    Coroutine _co;
    float _cursorZ; // 현재 생성할 row의 z 위치 (stage 로컬 기준)
    readonly List<GameObject> _spawned = new();

    // 오브젝트 풀: 프리팹별로 비활성 오브젝트 큐
    readonly Dictionary<Interactable, Queue<GameObject>> _pool = new();

    void Awake()
    {
        InitializePool();
    }

    void OnEnable() => StartSpawning();
    void OnDisable() => StopSpawning();

    void InitializePool()
    {
        if (spawnTable == null || spawnTable.Count == 0) return;

        foreach (var entry in spawnTable)
        {
            if (entry.prefab == null) continue;
            if (_pool.ContainsKey(entry.prefab)) continue;

            var queue = new Queue<GameObject>();

            // 초기 풀 생성
            for (int i = 0; i < initialPoolSize; i++)
            {
                var go = Instantiate(entry.prefab.gameObject, spawnRoot);
                go.SetActive(false);
                queue.Enqueue(go);
            }

            _pool[entry.prefab] = queue;
        }
    }

    GameObject GetFromPool(Interactable prefab)
    {
        if (prefab == null) return null;

        // 풀이 없으면 생성
        if (!_pool.ContainsKey(prefab))
        {
            _pool[prefab] = new Queue<GameObject>();
        }

        var queue = _pool[prefab];

        // 풀에서 가져오기 (사용 가능한 오브젝트 찾기)
        while (queue.Count > 0)
        {
            var go = queue.Dequeue();
            if (go != null)
            {
                go.SetActive(true);
                return go;
            }
        }

        // 풀이 비었으면 새로 생성
        var newObj = Instantiate(prefab.gameObject, spawnRoot);
        newObj.SetActive(true);
        return newObj;
    }

    void ReturnToPool(GameObject go, Interactable prefab)
    {
        if (go == null || prefab == null) return;

        // Interactable 상태 초기화
        var interactable = go.GetComponent<Interactable>();
        if (interactable != null)
        {
            interactable._picked = false;

            // 콜라이더 다시 활성화
            var col = go.GetComponent<Collider>();
            if (col != null) col.enabled = true;

            // Rigidbody 상태 복원
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        go.SetActive(false);
        go.transform.SetParent(spawnRoot);

        if (!_pool.ContainsKey(prefab))
        {
            _pool[prefab] = new Queue<GameObject>();
        }

        _pool[prefab].Enqueue(go);
    }

    public void StartSpawning()
    {
        if (_co != null) return;
        _co = StartCoroutine(RowLoop());
    }

    public void StopSpawning()
    {
        if (_co == null) return;
        StopCoroutine(_co);
        _co = null;
    }

    IEnumerator RowLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(rowTickSeconds);

            // 스테이지 끝 도달 체크
            if (_cursorZ > stageLength)
            {
                if (!loopStage)
                    yield break;

                _cursorZ = 0f;
            }

            SpawnRow(_cursorZ);
            _cursorZ += itemSpacing;

            CleanupInactiveObjects();
        }
    }

    void SpawnRow(float localZ)
    {
        if (itemsPerRow <= 0) return;

        // x 배치 계산
        // itemsPerRow=1이면 x=0
        // itemsPerRow>1이면 -width/2 ~ +width/2 사이 균등 분배
        for (int i = 0; i < itemsPerRow; i++)
        {
            // 스폰 확률 체크
            if (Random.value > spawnProbability)
                continue; // 이 위치는 스폰 건너뛰기

            var prefab = PickPrefab();
            if (prefab == null) return;

            float t = (itemsPerRow == 1) ? 0.5f : (i / (float)(itemsPerRow - 1));
            float x = Mathf.Lerp(-spawnWidth * 0.5f, spawnWidth * 0.5f, t);

            if (!centerRows)
                x = Mathf.Lerp(0f, spawnWidth, t); // 0~width 로 배치

            if (randomizeXInRow && itemsPerRow > 1)
            {
                float cell = spawnWidth / (itemsPerRow - 1);
                x += Random.Range(-cell * 0.25f, cell * 0.25f);
            }

            // Stage 로컬에서 (x, 0, z) -> 월드로 변환
            Vector3 worldPos = transform.TransformPoint(new Vector3(x, 0f, localZ));

            // 오브젝트 풀에서 가져오기
            var go = GetFromPool(prefab);
            if (go != null)
            {
                go.transform.position = worldPos;
                go.transform.rotation = prefab.transform.rotation;
                _spawned.Add(go);
            }
        }
    }

    Interactable PickPrefab()
    {
        if (spawnTable == null || spawnTable.Count == 0) return null;

        float total = 0f;
        for (int i = 0; i < spawnTable.Count; i++)
        {
            var e = spawnTable[i];
            if (e.prefab == null) continue;
            total += Mathf.Max(0f, e.weight);
        }
        if (total <= 0f) return null;

        float r = Random.value * total;
        float acc = 0f;

        for (int i = 0; i < spawnTable.Count; i++)
        {
            var e = spawnTable[i];
            if (e.prefab == null) continue;

            acc += Mathf.Max(0f, e.weight);
            if (r <= acc) return e.prefab;
        }

        return spawnTable[spawnTable.Count - 1].prefab;
    }

    void CleanupInactiveObjects()
    {
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            var go = _spawned[i];

            // null이거나 비활성화된 오브젝트 제거
            if (go == null || !go.activeInHierarchy)
            {
                _spawned.RemoveAt(i);
            }
        }
    }

    // 오브젝트를 풀로 반환하는 public 메서드
    public void ReturnObjectToPool(GameObject go)
    {
        if (go == null) return;

        // 어떤 프리팹에서 나온 것인지 찾기
        var interactable = go.GetComponent<Interactable>();
        if (interactable != null)
        {
            foreach (var entry in spawnTable)
            {
                if (entry.prefab != null && entry.prefab.GetType() == interactable.GetType())
                {
                    ReturnToPool(go, entry.prefab);
                    _spawned.Remove(go);
                    return;
                }
            }
        }

        // 프리팹을 찾지 못했으면 그냥 파괴
        Destroy(go);
    }

    // ✅ 스테이지 리셋: 생성된 아이템 정리 + 커서 초기화 + 다시 스폰
    public void ResetStage()
    {
        StopSpawning();

        // 모든 활성 오브젝트를 풀로 반환
        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            var go = _spawned[i];
            if (go != null)
            {
                var interactable = go.GetComponent<Interactable>();
                if (interactable != null)
                {
                    foreach (var entry in spawnTable)
                    {
                        if (entry.prefab != null && entry.prefab.GetType() == interactable.GetType())
                        {
                            ReturnToPool(go, entry.prefab);
                            break;
                        }
                    }
                }
                else
                {
                    Destroy(go);
                }
            }
        }

        _spawned.Clear();
        _cursorZ = 0f;

        StartSpawning();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 설정값이 이상하면 그리지 않음
        if (itemsPerRow <= 0) return;
        if (spawnWidth <= 0f || itemSpacing <= 0f || stageLength <= 0f) return;

        // "얼마나 촘촘히 찍을지" (너무 많으면 씬뷰가 무거워져서 상한)
        int rowCount = Mathf.FloorToInt(stageLength / itemSpacing) + 1;
        rowCount = Mathf.Clamp(rowCount, 0, 500); // 안전 상한

        // 원(디스크) 반지름: 칸 간격/폭 기준으로 적당히 자동
        float discRadius = Mathf.Max(0.05f, Mathf.Min(itemSpacing, spawnWidth / Mathf.Max(1, itemsPerRow)) * 0.12f);

        // 디스크 색
        Handles.color = new Color(1f, 1f, 0f, 0.9f);

        // 각 row마다, 각 column마다 스폰 위치 계산해서 원 그리기
        for (int r = 0; r < rowCount; r++)
        {
            float localZ = r * itemSpacing;

            for (int i = 0; i < itemsPerRow; i++)
            {
                float t = (itemsPerRow == 1) ? 0.5f : (i / (float)(itemsPerRow - 1));
                float x = centerRows
                    ? Mathf.Lerp(-spawnWidth * 0.5f, spawnWidth * 0.5f, t)
                    : Mathf.Lerp(0f, spawnWidth, t);

                Vector3 localPos = new Vector3(x, 0f, localZ);
                Vector3 worldPos = transform.TransformPoint(localPos);

                // y=0 평면(스테이지 로컬 up 기준) 위에 원 그리기
                Vector3 normal = transform.up;
                Handles.DrawWireDisc(worldPos, normal, discRadius);
            }
        }
    }
#endif
}
