using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 5;
    [SerializeField] public int currentHealth;

    [Header("Hit Settings")]
    [SerializeField] private float invincibilityDuration = 1.5f; // 무적 지속 시간
    [SerializeField] private float flashDelay = 0.1f; // 깜빡이는 속도
    [SerializeField] private Vector2 knockbackPower = new Vector2(10f, 10f); // 넉백 파워 (X, Y)
    [SerializeField] private float knockbackDuration = 0.3f; // 조작 불능 시간

    // 내부 변수
    private bool isInvincible = false;
    private PlayerController controller;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        
    }
    void Start()
    {
        // ★ 1. 게임 매니저와 연동 (위치 및 체력 동기화)
        if (GameManager.Instance != null)
        {
            // 위치 불러오기
            if (GameManager.Instance.lastCheckPointPos != Vector3.zero)
            {
                transform.position = GameManager.Instance.lastCheckPointPos;
            }

            // 체력 불러오기
            // 매니저가 기억하고 있는 체력을 내 체력으로 설정함
            currentHealth = GameManager.Instance.currentHealth;
        }
        else
        {
            // 매니저가 없으면 그냥 풀피로 시작 
            currentHealth = maxHealth;
        }
        // 시작하자마자 UI 갱신
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthUI(currentHealth);
        }
    }

    // 적(Enemy)이 호출하는 함수
    public void TakeDamage(int damage, Transform enemyTransform)
    {
        // 1. 무적 상태거나 이미 죽었으면 무시
        if (isInvincible || currentHealth <= 0) return;

        // 2. 체력 감소
        currentHealth -= damage;

        Debug.Log($"플레이어 체력: {currentHealth}/{maxHealth}");
        
        // UI 업데이트
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthUI(currentHealth);
        }
        else
        {
            Debug.Log("UIManager를 못 찾았어요!"); // 이게 뜨면 싱글톤 문제
        }

        // 3. 사망 체크
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // 4. 피격 루틴 시작 (넉백 + 무적)
        StartCoroutine(HitRoutine(enemyTransform));
    }

    private IEnumerator HitRoutine(Transform enemyTransform)
    {
        isInvincible = true;

        // 컨트롤러에게 피격 사실 알림 (애니메이션 재생 & 조작 차단)
        if (controller != null) controller.OnHit();

        // --- 넉백 물리 적용 ---
        // 적이 내 오른쪽에 있으면 왼쪽(-1)으로, 왼쪽에 있으면 오른쪽(1)으로 튕겨남
        int dir = (transform.position.x < enemyTransform.position.x) ? -1 : 1;
        
        rb.linearVelocity = Vector2.zero; // 기존 속도 초기화 
        rb.AddForce(new Vector2(dir * knockbackPower.x, knockbackPower.y), ForceMode2D.Impulse);

        // --- 넉백 시간 동안 대기 ---
        yield return new WaitForSeconds(knockbackDuration);

        // 넉백 끝남 -> 조작 다시 허용
        if (controller != null) controller.isHit = false;

        // --- 무적 시간(깜빡임) 처리 ---
        // 넉백이 끝나도 무적 시간은 남았으므로 계속 깜빡거림
        float elapsed = knockbackDuration;
        while (elapsed < invincibilityDuration)
        {
            sr.color = new Color(1, 1, 1, 0.4f); // 반투명
            yield return new WaitForSeconds(flashDelay);
            sr.color = Color.white; // 원상복구
            yield return new WaitForSeconds(flashDelay);
            elapsed += flashDelay * 2;
        }

        // 모든 상황 종료
        sr.color = Color.white;
        isInvincible = false;
    }

    private void Die()
    {
        Debug.Log("Game Over");
        
        GameManager.Instance.GameOver(); 
        // 조작 영구 차단
        if (controller != null) controller.enabled = false;
        
        // 물리 정지
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static; // 공중에 멈춤
        
        // 사망 애니메이션 재생 
        // GetComponent<Animator>().Play("Player_Die");
    }
}