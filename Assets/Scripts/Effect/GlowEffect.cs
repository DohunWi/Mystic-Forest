using UnityEngine;

public class NaturalGlow : MonoBehaviour
{
    private SpriteRenderer sr;
    
    [Header("밝기 범위")]
    public float minAlpha = 0.3f; // 너무 어두워지지 않게
    public float maxAlpha = 0.7f; // 너무 밝아지지 않게
    
    [Header("속도 및 랜덤성")]
    public float speed = 1.0f;    // 빛이 변하는 전반적인 속도
    private float randomOffset;   // 랜턴마다 다르게 깜빡이게 하기 위한 난수

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        // 랜턴마다 서로 다른 타이밍으로 시작하게 랜덤 값 부여
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        // Mathf.PerlinNoise: 시간이 지남에 따라 0~1 사이의 부드러운 난수를 생성
        float noise = Mathf.PerlinNoise(Time.time * speed, randomOffset);

        // 노이즈 값(0~1)을 우리가 원하는 밝기 범위(min~max)로 변환
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, noise);
        
        // 적용
        Color color = sr.color;
        color.a = alpha;
        sr.color = color;
    }
}