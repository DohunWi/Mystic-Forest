using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    [Header("설정")]
    public float requiredHoldTime = 2.0f; // 몇 초 눌러야 하는가?
    private bool isPlayerInRange = false;
    private bool isSaving = false; // 현재 누르고 있는 중인가?
    private float currentHoldTimer = 0f; // 현재 누른 시간

    [Header("연결할 오브젝트들")]
    public GameObject interactUI;     // 안내 아이콘 
    public GameObject chargingEffect; // 충전 중 효과 (빛나는 원)
    public ParticleSystem completionEffect; // 완료 파티클 
    public SpriteRenderer treeRenderer; // 나무 본체 스프라이트


    void Update()
    {
        // 플레이어가 범위 밖에 있으면 아무것도 안 함
        if (!isPlayerInRange) return;

        // 1. 키를 누르기 시작했을 때 (충전 시작)
        // UpArrow(윗방향키) 또는 W키 지원
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            StartCharging();
        }

        // 2. 키를 누르고 있는 중 (충전 진행)
        if ((Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) && isSaving)
        {
            ProcessCharging();
        }

        // 3. 키를 뗐을 때 (충전 취소)
        if ((Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.W)) && isSaving)
        {
            CancelCharging();
        }
    }

    // --- 내부 로직 함수들 ---

    void StartCharging()
    {
        isSaving = true;
        currentHoldTimer = 0f;

        if (chargingEffect != null)
        {
            chargingEffect.SetActive(true); // 충전 이펙트 켜기
            chargingEffect.transform.localScale = Vector3.zero; // 크기 초기화
        }
        if (interactUI != null) interactUI.SetActive(false); // 안내 UI는 잠시 숨김
    }

    void ProcessCharging()
    {
        // 시간 더하기
        currentHoldTimer += Time.deltaTime;

        // 진행률 계산 (0.0 ~ 1.0)
        float progress = Mathf.Clamp01(currentHoldTimer / requiredHoldTime);

        // 시각 효과: 충전 이펙트 크기를 진행률에 맞춰 키움 (점점 커지는 연출!)
        if (chargingEffect != null)
        {
            chargingEffect.transform.localScale = Vector3.one * progress * 1.5f; // 1.5배까지 커짐
        }

        // 시간 다 채웠으면 저장
        if (currentHoldTimer >= requiredHoldTime)
        {
            CompleteSave();
        }
    }

    void CancelCharging()
    {
        isSaving = false;
        currentHoldTimer = 0f;

        if (chargingEffect != null) chargingEffect.SetActive(false); // 이펙트 끄기
        if (interactUI != null) interactUI.SetActive(true); // 안내 UI 다시 켜기
    }

    void CompleteSave()
    {
        isSaving = false; // 저장 끝

        // 1. 게임 매니저에 저장
        GameManager.Instance.lastCheckPointPos = transform.position;
        // 2. 파일로 저장 
        GameManager.Instance.SaveGame();

        Debug.Log("신비한 힘으로 저장이 완료되었습니다!");

        // 2. 시각 효과 마무리
        if (chargingEffect != null) chargingEffect.SetActive(false); // 충전 이펙트 끄기
        if (completionEffect != null) completionEffect.Play(); // 파티클 재생

    }

    // --- 트리거 이벤트 ---
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (interactUI != null && !isSaving) interactUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            CancelCharging(); // 나가면 충전 취소
            if (interactUI != null) interactUI.SetActive(false);
        }
    }
}