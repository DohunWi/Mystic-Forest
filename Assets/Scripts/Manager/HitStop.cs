using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    // 어디서든 쉽게 부르기 위한 싱글톤
    public static HitStop Instance;
    
    bool isStopping = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // duration: 멈출 시간 
    public void Stop(float duration)
    {
        // 이미 시간이 멈춰있거나(패링 중), 작동 중이면 무시
        if (isStopping || Time.timeScale < 0.1f) return;
        StartCoroutine(StopRoutine(duration));
    }

    IEnumerator StopRoutine(float duration)
    {
        isStopping = true;
        
        // 1. 시간 정지
        float originalScale = Time.timeScale; // 원래 속도 기억 (보통 1)
        Time.timeScale = 0.0f; 

        // ★ 중요: 시간이 0이므로 WaitForSecondsRealtime을 써야 함
        yield return new WaitForSecondsRealtime(duration); 

        // 2. 시간 복구
        Time.timeScale = originalScale;
        isStopping = false;
    }
}