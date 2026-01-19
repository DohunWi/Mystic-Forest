using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public Transform posA, posB;
    public float speed = 2f;
    
    private Vector3 targetPos;
    private Transform playerTransform; // 플레이어를 기억해둘 변수

    void Start()
    {
        targetPos = posB.position;
    }

    // 물리 연산에 맞춰서 이동 (부드러움 + 동기화 해결)
    void FixedUpdate()
    {
        // 1. 이번 프레임에 이동해야 할 목표 위치 계산
        Vector3 nextPos = Vector3.MoveTowards(transform.position, targetPos, speed * Time.fixedDeltaTime);
        
        // 2. 실제로 이동하게 될 거리(Delta) 구하기
        Vector3 moveDelta = nextPos - transform.position;

        // 3. 발판 이동
        transform.position = nextPos;

        // 4. ★ 핵심: 플레이어가 위에 있다면, 발판이 간 만큼 똑같이 밀어줌
        if (playerTransform != null)
        {
            playerTransform.position += moveDelta;
        }

        // 5. 목적지 도착 체크
        if (Vector2.Distance(transform.position, targetPos) < 0.01f)
        {
            if (targetPos == posB.position) targetPos = posA.position;
            else targetPos = posB.position;
        }
    }

    // 플레이어가 탔다 (기억하기)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 발판 위쪽을 밟았을 때만 인식 (옆구리 충돌 방지)
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f) 
                {
                    playerTransform = collision.transform;
                    break;
                }
            }
        }
    }

    // 플레이어가 내렸다 (까먹기)
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 플레이어가 맞다면 변수 비우기
            if (playerTransform == collision.transform)
            {
                playerTransform = null;
            }
        }
    }
}