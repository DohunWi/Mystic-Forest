using UnityEngine;
using System.Collections;
using TMPro; // 텍스트 띄울 때 필요

public class BossZone : MonoBehaviour
{
    [Header("연결")]
    public BossController boss;       // 깨울 보스
    public GameObject warningUI;      // "BOSS BATTLE START" 텍스트 (UI 패널)
    public GameObject entryWall;      // 들어오면 닫힐 문 (선택사항)
    [SerializeField] private AudioClip bossBGM; // 이 맵에서 틀 음악 파일
    private bool hasTriggered = false; // 한 번만 발동하게

    void Start()
    {
        // 시작할 때 경고 UI와 문은 숨겨둠
        if(warningUI != null) warningUI.SetActive(false);
        if(entryWall != null) entryWall.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 이미 발동했거나, 플레이어가 아니면 무시
        if (hasTriggered || !other.CompareTag("Player")) return;

        StartCoroutine(StartSequence());
    }

    IEnumerator StartSequence()
    {
        hasTriggered = true;

        // 1. 문 닫기 
        if(entryWall != null) entryWall.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        // 2. 보스 bgm 재생
        if (SoundManager.Instance != null && bossBGM != null)
        {
            SoundManager.Instance.PlayBGM(bossBGM);
        }

        // 3. 경고 문구 띄우기 ("WARNING!")
        if (warningUI != null)
        {
            warningUI.SetActive(true);
            
            // 2초 정도 보여주고 끄기
            yield return new WaitForSeconds(2.0f);
            warningUI.SetActive(false);
        }

        // 4. 보스 깨우기
        if (boss != null)
        {
            boss.StartBossBattle();
        }
    }
}