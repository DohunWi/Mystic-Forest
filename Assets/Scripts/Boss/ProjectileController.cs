using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    [Header("설정")]
    public float speed = 15f;       // 날아가는 속도
    public int damage = 10;      // 플레이어에게 줄 데미지
    public float lifetime = 4f;     // 4초 지나면 자동 삭제 (메모리 관리)

    [Header("이펙트 연결")]
    public GameObject hitVFXPrefab; // 벽이나 플레이어에 맞았을 때 터지는 이펙트

    [Header("반사 설정")]
    // 0:불, 1:얼음, 2:독, 3:번개
    public int reflectedFrameIndex = 3; // 반사되면 '3번(번개/노랑)'으로 변신
    public Color reflectedColor = Color.yellow; // 꼬리랑 빛깔도 노랗게!

    void Start()
    {
        // 아무것도 안 맞더라도 lifetime이 지나면 사라지게 예약
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // 로컬 좌표 기준 '오른쪽(Right)'으로 계속 이동
        // (보스가 발사할 때 플레이어 방향으로 회전을 시켜주므로, 여기선 직진만 하면 됨)
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    // 충돌 감지 (Trigger 필수)
    void OnTriggerEnter2D(Collider2D other)
    {
        // ---------------------------------------------------------
        // 1. 플레이어와 충돌 (패링 시도 확인)
        // ---------------------------------------------------------
        if (other.CompareTag("Player"))
        {
            // 플레이어 컨트롤러 가져오기
            PlayerController player = other.GetComponent<PlayerController>();

            // 플레이어가 존재하고 + 현재 '쳐내기(isParrying)' 판정 중이라면?
            if (player != null && player.isParrying)
            {
                // A. 패링 성공 처리 (이펙트, 시간 정지)
                player.OnParrySuccess(transform.position);

                // B. 투사체 반사 로직 (보스에게 되돌려줌)
                speed *= 1.5f;                // 속도  증가 
                transform.Rotate(0, 0, 180f); // 방향 180도 뒤집기
                gameObject.tag = "ReflectedProjectile"; // 별도 태그 설정

                // --- ★ 텍스처 프레임 변경 로직 ★ ---
                SwapTextureFrame();

                // E. 폭발하지 않고 함수 종료 (투사체는 계속 날아감)
                return; 
            }

            // 패링 실패 -> 플레이어 피격 처리
            Debug.Log("플레이어 피격!");
            if (player != null) player.OnHit(); // 플레이어 피격 모션/무적시간 등
            player.GetComponent<PlayerHealth>()?.TakeDamage(damage, transform); // 체력 깎기
            
            Explode(); // 투사체 폭발 및 삭제
        }
        else if (other.CompareTag("Boss"))
        {
            // 내가 반사된 상태("ReflectedProjectile")일 때만 보스 타격
            if (gameObject.CompareTag("ReflectedProjectile"))
            {
                Debug.Log("<color=red>보스에게 마법 반사 명중!</color>");
                
                BossController boss = other.GetComponent<BossController>();
                if (boss != null)
                {
                    boss.TakeDamage(damage * 0.5f); // 데미지 배율
                }
                
                Explode(); // 타격 후 폭발
            }
            // 반사된 게 아니라면(보스가 방금 쏜 것), 보스 몸체는 그냥 통과(무시)
        }
        // 2. 땅(Ground)이나 벽(Wall)과 충돌
        else if (other.CompareTag("Ground"))
        {
            Explode(); // 폭발 처리
        }
    }

    void SwapTextureFrame()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps == null) return;

        // -------------------------------------------------------
        // 1. 앞으로 나올 파티클 설정 변경 (텍스처 & 색상)
        // -------------------------------------------------------
        
        // A. 텍스처 변경 (새로 나오는 파티클부터 적용됨)
        var textureSheet = ps.textureSheetAnimation;
        textureSheet.startFrame = new ParticleSystem.MinMaxCurve(reflectedFrameIndex);

        // B. 메인 색상 변경
        var main = ps.main;
        main.startColor = reflectedColor;

        // C. 꼬리(Trail) 색상 변경
        var trails = ps.trails;
        if (trails.enabled)
        {
            trails.colorOverTrail = reflectedColor;
        }

        // -------------------------------------------------------
        // 2. 이미 살아있는 파티클들도 색깔 강제 변경
        // -------------------------------------------------------
        // Clear()를 쓰지 않고, 현재 날아가고 있는 파란색 파티클들을 잡아서
        // 강제로 노란색으로 칠해버립니다. (텍스처는 못 바꿔도 색이 바뀌면 티가 안 남)
        
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[main.maxParticles];
        int count = ps.GetParticles(particles); // 현재 살아있는 파티클 다 가져오기

        for (int i = 0; i < count; i++)
        {
            // 살아있는 파티클의 색상을 반사 색상으로 덮어씌움
            particles[i].startColor = reflectedColor;
            // 양수면 시계 방향, 음수면 반시계 방향
            particles[i].angularVelocity = 720f;
        }

        // 변경된 정보를 다시 파티클 시스템에 적용
        ps.SetParticles(particles, count);
    }

    // 폭발 및 삭제 처리
    void Explode()
    {
        // 1. 타격 이펙트(Hit VFX) 생성
        if (hitVFXPrefab != null)
        {
            Instantiate(hitVFXPrefab, transform.position, Quaternion.identity);
        }

        // 2. 투사체 삭제
        Destroy(gameObject);
    }
}