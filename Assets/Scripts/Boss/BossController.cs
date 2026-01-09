using UnityEngine;
using System.Collections;
using Unity.Cinemachine;
public class BossController : MonoBehaviour
{
    public enum BossState { Idle, Attacking, Dead }
    public Animator Anim;

    [Header("상태 정보")]
    public BossState currentState = BossState.Idle;
    public float maxHealth = 100f;
    private float currentHealth;
    public bool isEnraged = false;
    public bool isBattleStarted = false; // ★ 처음엔 false로 설정

    [Header("연결 (Assign)")]
    public Transform body;       // 본체
    public Transform handL;      // 왼손 
    public Transform handR;      // 오른손 
    public Transform magicSpawnPoint; // 마법 발사 위치 (본체 앞/입)
    public ParticleSystem chargeVFX; 
    public GameObject thunderVFX; // Thunder VFx
    public GameObject magicProjectilePrefab;

    [Header("이펙트 연결")] // 여기에 EnrageAura 파티클 연결
    public ParticleSystem[] enrageAura; // 배열로 받기

    [Header("이동 설정")]
    public float moveSpeed = 2.0f;
    public float floatSpeed = 2f;
    public float floatAmount = 0.5f;
    public Vector2 offsetFromPlayer = new Vector2(3f, 4f);

    [Header("이동 보정 (SmoothDamp)")]
    public float smoothTime = 0.3f; // 도달하는 데 걸리는 대략적인 시간 (작을수록 빠릿, 클수록 미끌)
    private Vector3 currentVelocity; // (내부 계산용) 현재 속도 저장 변수

    [Header("공격 설정")]
    public float attackCooldown = 3.5f; 
    private float lastAttackTime;

    [Header("Smash Sequence")]
    public GameObject groundSmashPrefab; // ★ 여기에 방금 만든 프리팹 연결
    public int waveCount = 3;        // 몇 번 터질지 (3번)
    public float waveDelay = 0.15f;  // 터지는 간격 (0.15초)
    public float waveSpacing = 3f; // 퍼지는 거리 간격 (1.5미터씩 벌어짐)

    [Header("Lightning Attack")]
    public GameObject lightningPrefab; // 번개 프리팹
    public int lightningCount = 5;     // 몇 번 쏠지
    public float lightningInterval = 1f; // 간격

    [Header("조준 보정")]
    public float targetOffsetY = 1.5f; // 플레이어 머리 높이 (1.0 ~ 1.5 정도로 조절)
    private Transform player;
    private SpriteRenderer bodySR;
    private Vector3 handL_OriginPos;
    private Vector3 handR_OriginPos;
    private Vector3 body_OriginPos; // 본체의 원래 위치 기억용
    private CinemachineImpulseSource impulseSource;
    void Start()
    {
        Anim = GetComponent<Animator>();
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player").transform;
        impulseSource = GetComponent<CinemachineImpulseSource>();
        
        if (body != null) bodySR = body.GetComponent<SpriteRenderer>();

        // 초기 위치 기억
        if (handL != null) handL_OriginPos = handL.localPosition;
        if (handR != null) handR_OriginPos = handR.localPosition;
        if (body != null) body_OriginPos = body.localPosition;
    }

    void Update()
    {
        // 전투가 시작되지 않았으면 아무것도 안 함
        if (!isBattleStarted) return;

        if (currentState == BossState.Dead || player == null) return;

        AnimateFloating();
        FacePlayer();

        switch (currentState)
        {
            case BossState.Idle:
                if (!Anim.GetCurrentAnimatorStateInfo(0).IsName("Boss_Idle"))
                {
                    Anim.Play("Boss_Idle");
                }
                MoveToTarget();
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    ChooseAttack();
                }
                break;
            case BossState.Attacking:
                break;
        }
    }

    // 외부(BossZone)에서 호출할 함수: 보스전 시작
    public void StartBossBattle()
    {
        if (isBattleStarted) return; // 이미 시작했으면 무시

        isBattleStarted = true;
        
        // 보스 체력바 on!
        if (UIManager.Instance != null) 
            UIManager.Instance.ShowBossHealth(true);

        // Anim.Play("Boss_Roar"); 
    }

    // ================== [이동 로직 복사본 (필요시 사용)] ==================
    void AnimateFloating()
    {
        if(body) 
        {
            // 원래 위치(y) + 사인파(y)
            float newY = body_OriginPos.y + (Mathf.Sin(Time.time * floatSpeed) * floatAmount);
            body.localPosition = new Vector3(body_OriginPos.x, newY, body_OriginPos.z);
        }
        if(currentState != BossState.Attacking)
        {
            if(handL) handL.localPosition = Vector3.Lerp(handL.localPosition, handL_OriginPos + Vector3.up * Mathf.Sin(Time.time * floatSpeed + 0.5f) * floatAmount, Time.deltaTime * 2f);
            if(handR) handR.localPosition = Vector3.Lerp(handR.localPosition, handR_OriginPos + Vector3.up * Mathf.Sin(Time.time * floatSpeed + 1.0f) * floatAmount, Time.deltaTime * 2f);
        }
    }
    void FacePlayer()
    {
        if (player.position.x > transform.position.x) transform.localScale = new Vector3(-1, 1, 1); 
        else transform.localScale = new Vector3(1, 1, 1);
    }
    void MoveToTarget()
    {
        // 1. 목표 위치 계산 (기존과 동일)
        Vector3 targetPos = player.position + new Vector3(0, offsetFromPlayer.y, 0);
        
        if (transform.position.x < player.position.x) 
            targetPos.x -= offsetFromPlayer.x;
        else 
            targetPos.x += offsetFromPlayer.x;

        // [핵심 변경] SmoothDamp 사용
        // 현재 위치에서 -> 목표 위치로 -> smoothTime 동안 부드럽게 이동하되 -> moveSpeed를 넘지 않음
        transform.position = Vector3.SmoothDamp(
            transform.position, // 현재 위치
            targetPos,          // 목표 위치
            ref currentVelocity,// 현재 속도 (참조 변수)
            smoothTime,         // 부드러움 정도 (0.3초 추천)
            moveSpeed           // ★ 최대 이동 속도 제한 (이게 있어서 Lerp처럼 급발진 안 함!)
        );
    }
    // ===================================================================


    void ChooseAttack()
    {
        float distance = Vector2.Distance(transform.position, player.position);
        if (distance >= 25.0f)
        {
            return; 
        }

        currentState = BossState.Attacking;

        int rand = Random.Range(0, 10);
        
        if (distance < 10.0f && rand < 4) 
        {
            StartCoroutine(SmashAttack());
        }
        else if (distance < 20.0f && rand < 7)
        {
            StartCoroutine(ThunderStorm());
        }
        else 
        {
            StartCoroutine(MagicAttack());
        }
    }

    // 패턴 1: 주먹 내려찍기 (양손 공용)
    // [수정] 공격 시작 코루틴 (아주 심플해짐)
    IEnumerator SmashAttack()
    {
        // 1. 애니메이션 발동!
        Anim.Play("Boss_Smash");

        // 2. 애니메이션 끝날 때까지 대기 (약 1.2초, 애니 길이만큼)
        // 실제 타격 판정은 아래 'OnSmashImpact' 함수가 해줌
        yield return new WaitForSeconds(1.2f); 
        lastAttackTime = Time.time;
        currentState = BossState.Idle;
    }

    // 애니메이션 이벤트가 호출하는 함수
    public void OnSmashImpact()
    {
        StartCoroutine(SmashWaveRoutine());
    }
    
    // 패턴 2: 본체 마법 발사
    IEnumerator MagicAttack()
    {
        Anim.Play("Boss_Casting");
        // 1. 마법진 ON
        if (chargeVFX != null) 
        {
            chargeVFX.gameObject.SetActive(true); 
            
        }

        // 3. 애니메이션이 끝날 때까지 대기
        yield return new WaitForSeconds(3.2f); 
        chargeVFX.gameObject.SetActive(false); 
        lastAttackTime = Time.time;

        currentState = BossState.Idle;
    }
    // ★ 연쇄 폭발 로직
    IEnumerator SmashWaveRoutine()
    {
        // 보스 발밑 높이 계산 (바닥에 딱 붙게)
        float floorY = transform.position.y; 
        Vector3 centerPos = new Vector3(transform.position.x, floorY, 0);

        for (int i = 0; i < waveCount; i++)
        {
            // 거리 계산: (i+1)을 곱해서 1칸, 2칸, 3칸... 점점 멀어지게 함
            float currentDist = waveSpacing * (i + 1);
            if (impulseSource != null)
            {
                // 힘(Velocity)을 넣어서 흔들기. (기본값은 Vector3.down * 힘)
                impulseSource.GenerateImpulse(3.0f); 
            }
            // --- 왼쪽 폭발 생성 ---
            Vector3 leftPos = centerPos + (Vector3.left * currentDist);
            if (groundSmashPrefab != null)
            {
                GameObject wave = Instantiate(groundSmashPrefab, leftPos, Quaternion.identity);
                wave.transform.localScale *= (1.0f + (i * 0.3f));
            }
            // --- 오른쪽 폭발 생성 ---
            Vector3 rightPos = centerPos + (Vector3.right * currentDist);
            if (groundSmashPrefab != null)
            {
                GameObject wave = Instantiate(groundSmashPrefab, rightPos, Quaternion.identity);
                wave.transform.localScale *= (1.0f + (i * 0.3f));
            }
            // --- 시간차 대기 ---
            yield return new WaitForSeconds(waveDelay);
        }
    }

    // ★ Animation Event가 호출하는 함수
    public void OnMagicFire()
    {
        if (impulseSource != null)
            {
                // 힘(Velocity)을 넣어서 흔들기
                impulseSource.GenerateImpulse(3.0f); 
            }

        // 1. 마법진 OFF
        if (chargeVFX != null) 
        {
            chargeVFX.gameObject.SetActive(false);
        }

        // 플레이어의 발(position) + 머리 높이(targetOffsetY)를 더해서 목표 지점 계산
        Vector3 playerHeadPos = player.position + (Vector3.up * targetOffsetY);

        // 2. 투사체 생성 및 발사
        if (magicProjectilePrefab != null && magicSpawnPoint != null)
        {
            GameObject magic = Instantiate(magicProjectilePrefab, magicSpawnPoint.position, Quaternion.identity);
            
            // 3. 플레이어 방향으로 회전 (조준)
            Vector3 dir = (playerHeadPos - magicSpawnPoint.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            
            // 투사체 회전 적용
            magic.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
    // 패턴 3: 번개 폭풍
    IEnumerator ThunderStorm()
    {
        Anim.Play("Boss_Thunder"); 
        yield return new WaitForSeconds(0.6f);

        for (int i = 0; i < lightningCount; i++)
        {
            if (lightningPrefab != null)
            {
                // 플레이어의 현재 위치(X)를 조준
                float targetY = player.position.y + 3.0f; 
                
                // 플레이어 위치 + 약간의 랜덤성 (너무 정확하면 피하기 힘듦)
                float targetX = player.position.x + Random.Range(-0.5f, 0.5f);
                
                Vector3 spawnPos = new Vector3(targetX, targetY, 0);
                
                Instantiate(lightningPrefab, spawnPos, Quaternion.identity);
            }

            // 다음 번개까지 대기
            yield return new WaitForSeconds(lightningInterval);
        }
        if (thunderVFX != null)
        {
            thunderVFX.gameObject.SetActive(false);
        }
        
        yield return new WaitForSeconds(1.0f); // 후딜레이
        lastAttackTime = Time.time;
        currentState = BossState.Idle;
    }
    public void OnThunderStorm()
    {
        if (thunderVFX != null) 
        {
            thunderVFX.gameObject.SetActive(true);
        }
    }
    // ================== [피격 및 사망] ==================

    public void TakeDamage(float damage)
    {
        if (currentState == BossState.Dead) return;

        currentHealth -= damage;
        // Boss HP Update
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealth(currentHealth, maxHealth);
        }
        
        StartCoroutine(HitFlash());

        // 광폭화 (체력 50% 이하)
        if (!isEnraged && currentHealth <= maxHealth * 0.5f)
        {
            Enrage();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Enrage()
    {
        isEnraged = true;
        Debug.Log("보스 광폭화!!");
        // 오라 파티클 색상 & 강도 변경
        // 배열에 있는 모든 파티클 시스템을 순회하며 적용
        if (enrageAura != null)
        {
            foreach (var aura in enrageAura)
            {
                if (aura == null) continue; // 비어있는 슬롯 방지

                // 1. 파티클 켜기
                if (!aura.isPlaying) aura.Play();

                // 2. 색상 변경 (붉은색)
                var main = aura.main;
                main.startColor = new Color(1f, 0.2f, 0.2f, 1f);
            }
        }
        // 능력치 강화
        moveSpeed *= 1.5f; 
        attackCooldown *= 0.6f;
        // 공격 패턴 강화
        waveCount += 1;
        lightningCount += 2;
        lightningInterval *= 0.7f;
    }

    IEnumerator HitFlash()
    {
        bodySR.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        bodySR.color = Color.white;
    }

    void Die()
    {
        currentState = BossState.Dead;
        // 사망 애니메이션, 파티클, 씬 전환 등 처리
        Destroy(gameObject, 2f);
    }
}