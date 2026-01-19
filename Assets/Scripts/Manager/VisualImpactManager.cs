using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VisualImpactManager : MonoBehaviour
{
    public static VisualImpactManager Instance;

    [Header("연결 필요")]
    [SerializeField] private Volume globalVolume;

    [Header("효과 설정")]
    [SerializeField] private AnimationCurve impactCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(0.5f, 0.7f), new Keyframe(1, 0));
    [SerializeField] private float defaultIntensity = 4.0f; // 강도

    private ChromaticAberration chromaticAberration;
    private Coroutine currentRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        
        // Volume이 없거나 깨졌을 경우 방어
        if (globalVolume == null)
        {
            // 혹시 연결 안 했으면 같은 오브젝트에서라도 찾아봄
            globalVolume = GetComponent<Volume>();
        }

        if (globalVolume != null)
        {
            globalVolume.profile.TryGet(out chromaticAberration);
        }
    }

    public void TriggerImpact(float duration = 0.3f)
    {
        // 효과가 없거나 볼륨이 없으면 실행 X
        if (chromaticAberration == null || globalVolume == null) return;
        
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(EffectRoutine(duration));
    }

    IEnumerator EffectRoutine(float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            // 루프 도는 중에 게임이 꺼지거나 볼륨이 삭제되면 즉시 중단
            if (globalVolume == null || chromaticAberration == null) yield break;

            timer += Time.unscaledDeltaTime; 
            float t = timer / duration; 

            float currentIntensity = impactCurve.Evaluate(t) * defaultIntensity;
            chromaticAberration.intensity.value = currentIntensity;
            
            yield return null;
        }

        // 루프 끝난 후에도 살아있는지 체크
        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = 0f;
        }
        currentRoutine = null;
    }

    // 게임이 꺼지거나 객체가 비활성화될 때 강제 종료 및 초기화
    private void OnDisable()
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        
        // 게임 종료 시점에 접근하면 또 에러 날 수 있으므로 null 체크 필수
        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = 0f;
        }
    }
}