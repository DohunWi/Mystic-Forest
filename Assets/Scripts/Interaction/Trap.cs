using UnityEngine;

public class Trap : MonoBehaviour
{
    [Header("함정 설정")]
    public int damage = 1; // 닿으면 깎일 체력

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 플레이어 스크립트를 찾아서 데미지 주기
            PlayerHealth player = collision.GetComponent<PlayerHealth>();
            
            if (player != null)
            {
                player.TakeDamage(damage, transform);
            }
        }
    }
}