using UnityEngine; 
using System.Collections;
using Unity.Cinemachine;

public class LightningController : MonoBehaviour
{
    // 번개폭풍 프리팹 제어
    [Header("설정")]
    public float warningTime = 1.2f; // 마법진 떠있는 시간
    public int damage = 15;

    [Header("연결")]
    public ParticleSystem warningParticle; // 전조 마법진 (파티클)
    public Animator lightningAnim;         // 번개 애니메이터

    [Header("Audio")]
    [SerializeField] private AudioClip lightningSound;
  
    private BoxCollider2D col;
    private CinemachineImpulseSource impulse;

    void Start()
    {
        col = GetComponent<BoxCollider2D>();
        impulse = GetComponent<CinemachineImpulseSource>();

        // 초기화: 판정 끄기
        col.enabled = false;
        
        StartCoroutine(SequenceRoutine());
    }

    IEnumerator SequenceRoutine()
    {
        // 1. 전조 단계: 파티클 재생
        if(warningParticle != null) warningParticle.Play();
        
        // 2. 대기 (플레이어가 피할 시간)
        yield return new WaitForSeconds(warningTime);

        if(lightningAnim != null)
        {
            // 클립 직접 실행.
            lightningAnim.Play("Lightning"); 
        }
    }

    // 애니메이션 이벤트가 호출할 함수 
    public void OnStrikeHit()
    {
        // 1. 공격 판정 켜기
        col.enabled = true;

        // 2. 화면 흔들림
        if (impulse) impulse.GenerateImpulse(1.5f);
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(lightningSound);
        // 3. 짧은 시간 뒤에 판정 끄기 (0.1초)
        Invoke("DisableCollider", 0.1f);
    }

    // 혹은 애니메이션 길이에 맞춰 자동 삭제
    public void OnAnimEnd()
    {
        Destroy(gameObject);
    }

    void DisableCollider()
    {
        col.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, transform);
                col.enabled = false; // 한 번만 때리기
            }
        }
    }
}