using UnityEngine;

public class ShockwaveDamage : MonoBehaviour
{
    [Header("설정")]
    public float activeTime = 0.2f; // 판정이 살아있는 시간 (0.2초 뒤엔 안 아픔)
    public int damageAmount = 20;

    private BoxCollider2D col;

    void Start()
    {
        col = GetComponent<BoxCollider2D>();
        
        // 판정은 0.2초 뒤에 끔. (안 그러면 연기에 닿아도 아픔)
        Invoke("DisableCollider", activeTime);
    }

    void DisableCollider()
    {
        if (col) col.enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. PlayerHealth 컴포넌트를 찾기
            PlayerHealth health = other.GetComponent<PlayerHealth>();

            // 2. Health가 있다면 TakeDamage 호출
            if (health != null)
            {
                // 데미지를 주면서 '누가 때렸는지(transform)'를 같이 넘긴다.
                // 그래야 PlayerHealth가 넉백 방향(왼쪽/오른쪽)을 계산할 수 있음.
                health.TakeDamage(damageAmount, transform);
                
                DisableCollider(); // 한 대 때렸으니 판정 끄기
            }
        }
    }
}