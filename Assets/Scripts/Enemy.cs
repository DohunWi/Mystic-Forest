using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
    }

    // 외부(플레이어)에서 이 함수를 호출해서 데미지를 줌
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"적 피격! 남은 체력: {currentHealth}");

        // 피격 효과 (하얗게 깜빡임)
        StartCoroutine(FlashRoutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("적 사망!");
        // 나중에 사망 애니메이션이나 파티클 추가
        Destroy(gameObject);
    }

    private IEnumerator FlashRoutine()
    {
        // 0.1초 동안 빨간색(또는 투명)으로 변했다가 돌아옴
        spriteRenderer.color = Color.red; 
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }
}