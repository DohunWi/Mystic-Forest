using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine; 
using UnityEngine.Rendering; // 포스트 프로세싱용
using UnityEngine.Rendering.Universal; 

public class PlayerController : MonoBehaviour
{
    [Header("1. Movement & Jump")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 16f;
    [Range(0, 1)] [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float gravityScale = 4.5f;
    [SerializeField] private float fallGravityMult = 1.5f;

    [Header("2. Dash")]
    [SerializeField] private float dashPower = 24f;
    [SerializeField] private float dashTime = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("3. Advanced Physics")]
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform footPos;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.2f);

    [Header("Combat")]
    [SerializeField] private GameObject[] attackVFXs; // 1타, 2타, 3타 이펙트
    [SerializeField] private float attackDuration = 0.2f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask enemyLayer;
    public float basicDamage = 5.0f;

    // 패링 관련 변수 
    [Header("Parry System")]
    [SerializeField] private float parryWindow = 0.2f; // 패링 유효 시간 
    [SerializeField] private GameObject parryVFX;      // 패링 성공 시 터질 이펙트
    public bool isParrying = false;                    // 투사체가 확인할 변수 (외부에서 접근 가능)

    [Header("Combo Settings")]
    [SerializeField] private float comboResetTime = 1.0f; // 이 시간 안에 다음 공격해야 콤보 유지
    private int comboStep = 0; // 현재 콤보 단계 (0, 1, 2...)
    private float lastAttackTime; // 마지막 공격이 끝난 시간

    [Header("Audio")]
    [SerializeField] private AudioClip[] attackSounds;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip dashSound;
    [SerializeField] private AudioClip[] footstepSounds; // 배열로 만들어서 랜덤 재생
    [SerializeField] private float footstepRate = 0.3f; // 발소리 간격 
    private float footstepTimer;

    [Header("State Checks")]
    public bool isGrounded;
    public bool isAttacking;
    public bool isDashing;
    [HideInInspector] public bool isHit; // PlayerHealth에서 제어

    [Header("Parry Juice (연출)")]
    [SerializeField] private CinemachineCamera virtualCam; // 시네머신 카메라 연결
    [SerializeField] private Volume globalVolume; // 글로벌 볼륨 연결
    [SerializeField] private float zoomAmount = 5f; // 줌인 목표 사이즈 (기본보다 작게)
    [SerializeField] private float zoomDuration = 0.15f; // 줌 유지 시간

    private float defaultLensSize; // 원래 카메라 크기 저장용
    private ChromaticAberration chromaticAberration; // 색수차 효과 제어용
    private CinemachineImpulseSource impulseSource;

    // 내부 변수
    private Rigidbody2D rb;
    private Animator anim;
    private Vector2 moveInput;
    private string currentAnimName; // 중복 재생 방지용

    // 타이머 및 상태
    private bool canDash = true;
    private bool isJumping; // 점프 중인지 체크 (코요테 타임 로직용)
    private float lastGroundedTime;
    private float lastJumpPressedTime;

    // --- 애니메이션 이름 상수 (대소문자 정확하게) ---
    const string ANIM_IDLE = "Player_idle";
    const string ANIM_RUN = "Player_run";
    const string ANIM_JUMP = "Player_jump";
    const string ANIM_DASH = "Player_dash"; 
    const string ANIM_HIT = "Player_Hit";

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.gravityScale = gravityScale;
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }
    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.lastCheckPointPos != Vector3.zero)
        {
            transform.position = GameManager.Instance.lastCheckPointPos;
        }
        // 1. 카메라 원래 크기 저장
        if (virtualCam != null)
            defaultLensSize = virtualCam.Lens.OrthographicSize;

        // 2. 포스트 프로세싱 효과 가져오기
        if (globalVolume != null && globalVolume.profile.TryGet(out ChromaticAberration ca))
        {
            chromaticAberration = ca;
        }
    }
    private void Update()
    {
        if (isHit) return; // 피격 중엔 조작 불가

        // 타이머 업데이트
        lastGroundedTime -= Time.deltaTime;
        lastJumpPressedTime -= Time.deltaTime;

        if (isDashing) return; // 대시 중엔 아래 로직 무시

        // 1. 접지 판정
        if (Physics2D.OverlapBox(footPos.position, groundCheckSize, 0f, groundLayer))
        {
            isGrounded = true;
            // 낙하 중이거나 바닥에 있을 때 코요테 타임 갱신
            if (rb.linearVelocity.y <= 0)
            {
                lastGroundedTime = coyoteTime;
                isJumping = false;
            }
        }
        else
        {
            isGrounded = false;
        }

        // 2. 점프 실행 (버퍼 && 코요테)
        if (lastJumpPressedTime > 0 && lastGroundedTime > 0 && !isJumping)
        {
            PerformJump();
        }

        // 3. 방향 전환
        if (moveInput.x != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(moveInput.x), 1, 1);
        }

        // 4. 낙하 시 중력 조절
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = gravityScale * fallGravityMult;
        }
        else
        {
            rb.gravityScale = gravityScale;
        }

        HandleFootsteps();
        // 5. 애니메이션 갱신
        HandleAnimations();
    }

    private void FixedUpdate()
    {
        if (isDashing || isHit) return;

        // 이동 적용
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }
    private void HandleFootsteps()
    {
        // 조건: 땅에 닿아있음 && 움직이는 중 (속도가 있음) && 대시 중이 아님
        if (isGrounded && Mathf.Abs(rb.linearVelocity.x) > 0.1f && !isDashing)
        {
            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0)
            {
                // 소리 재생
                if (footstepSounds.Length > 0 && SoundManager.Instance != null)
                {
                    // 랜덤한 발소리 골라서 재생 - 자연스럽게
                    int randIndex = Random.Range(0, footstepSounds.Length);
                    SoundManager.Instance.PlaySFX(footstepSounds[randIndex]);
                }

                // 타이머 리셋
                footstepTimer = footstepRate;
            }
        }
        else
        {
            // 멈추거나 공중에 뜨면 타이머를 0으로 만들어서, 착지하자마자 소리가 나게 함
            footstepTimer = 0; 
        }
    }
    // 애니메이션 우선순위 관리
    private void HandleAnimations()
    {
        // 1순위: 대시
        if (isDashing)
        {
            PlayAnim(ANIM_DASH);
            return;
        }

        // 2순위: 공중 (점프/낙하)
        if (!isGrounded)
        {
            PlayAnim(ANIM_JUMP);
            return;
        }

        // 3순위: 달리기 (이동 입력이 있고 && 공격 중이 아닐 때)
        if (Mathf.Abs(moveInput.x) > 0.01f && !isAttacking)
        {
            PlayAnim(ANIM_RUN);
        }
        else
        {
            // 그 외: 대기
            PlayAnim(ANIM_IDLE);
        }
    }

    public void PlayAnim(string animName)
    {
        if (currentAnimName == animName) return;
        anim.Play(animName);
        currentAnimName = animName;
    }

    private void PerformJump()
    {
        isJumping = true;
        lastGroundedTime = 0;
        lastJumpPressedTime = 0;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // Y속도 초기화 후 점프
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        
        // 점프 소리는 여기서 한 번만 (OnJump에서 중복 재생 방지)
        if(SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(jumpSound);
    }

    // --- Input Events ---

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            lastJumpPressedTime = jumpBufferTime;
        }
        // 가변 점프 (키 뗐을 때)
        else if (rb.linearVelocity.y > 0 && isJumping)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed && canDash && !isDashing && !isHit)
        {
            StartCoroutine(DashRoutine());
        }
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed && !isAttacking && !isDashing && !isHit)
        {
            // 1. 콤보 유지 시간 체크
            // (마지막 공격 끝난 후 1초가 지났으면 콤보 초기화)
            if (Time.time - lastAttackTime > comboResetTime)
            {
                comboStep = 0;
            }

            StartCoroutine(AttackRoutine());
        }
    }

    // --- Coroutines ---

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        
        // 공격 시작과 동시에 패링 판정 켜기
        StartCoroutine(ParryWindowRoutine());

        // --- 1. 콤보 단계에 맞는 리소스 선택 ---
        // 배열 크기를 벗어나지 않게 안전장치 (나머지 연산 %)
        // 예: 이펙트가 3개인데 4타째가 되면 다시 0번으로
        int stepIndex = comboStep % attackVFXs.Length;

        // --- 2. 소리 재생 ---
        if (SoundManager.Instance != null && attackSounds.Length > 0) 
        {
            // 배열 인덱스 보호 (소리가 이펙트보다 적을 수도 있으니)
            int soundIndex = comboStep % attackSounds.Length;
            SoundManager.Instance.PlaySFX(attackSounds[soundIndex]);
        }

        // --- 3. VFX 켜기 ---
        GameObject currentVFX = attackVFXs[stepIndex];
        if (currentVFX != null) currentVFX.SetActive(true);

        // --- 4. 데미지 판정 (콤보마다 데미지 증가 가능) ---
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);
        foreach (Collider2D enemyCollider in hitEnemies)
        {
            float damage = basicDamage;
            if (stepIndex == 2) damage *= 2; // 3번째 공격은 2배데미지

            if (enemyCollider.CompareTag("Enemy"))
            {
                EnemyAI enemyAI = enemyCollider.GetComponent<EnemyAI>();
                if (enemyAI != null)
                {
                    enemyAI.TakeDamage(damage, transform); 
                }
            }
            else if(enemyCollider.CompareTag("Boss"))
            {
                BossController bossController = enemyCollider.GetComponent<BossController>();
                if (bossController != null)
                {
                    bossController.TakeDamage((float)damage); 
                }
            }
        }

        // --- 5. 대기 ---
        yield return new WaitForSeconds(attackDuration);

        // --- 6. 정리 ---
        if (currentVFX != null) currentVFX.SetActive(false);
        
        isAttacking = false;
        
        // 콤보 다음 단계로 증가
        comboStep++;
        // 마지막 공격 끝난 시간 기록 (이 시간부터 1초 카운트 시작)
        lastAttackTime = Time.time; 
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        canDash = false;

        PlayAnim(ANIM_DASH);
        
        if(SoundManager.Instance != null) 
            SoundManager.Instance.PlaySFX(dashSound);

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        // 보고 있는 방향으로 대시
        rb.linearVelocity = new Vector2(transform.localScale.x * dashPower, 0f);

        yield return new WaitForSeconds(dashTime);

        rb.gravityScale = originalGravity;
        rb.linearVelocity = Vector2.zero;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
    private IEnumerator ParryWindowRoutine()
    {
        isParrying = true;  // 무적/반사 판정 ON
        yield return new WaitForSeconds(parryWindow); // 0.2초 대기
        isParrying = false; // 판정 OFF
    }

    // 패링 성공 시 호출될 함수 (투사체 스크립트에서 부름)
    public void OnParrySuccess(Vector3 hitPoint)
    {
        // 1. 이펙트 생성 (노란색 소용돌이 등)
        if (parryVFX != null) 
        {
            GameObject vfx = Instantiate(parryVFX, hitPoint, Quaternion.identity);
            Destroy(vfx, 0.2f);
        }


        // ★ 3. 연출 종합 코루틴 실행
        StartCoroutine(ParryJuiceRoutine());
        
        // 3. 로그
        Debug.Log("<color=yellow>공격 패링 성공!</color>");
    }

    // 역경직 (타격감)
    IEnumerator HitStop()
    {
        Time.timeScale = 0.05f; // 시간을 아주 느리게
        yield return new WaitForSecondsRealtime(0.25f); // 현실 시간 0.25초
        Time.timeScale = 1.0f; // 정상화
    }
    IEnumerator ParryJuiceRoutine()
    {
        // --- A. 임팩트 순간 (정지 & 왜곡) ---
        
        // 1. 시간 완전 정지
        Time.timeScale = 0.01f; 

        // 2. 카메라 줌 인 (순간이동)
        if (virtualCam != null)
            virtualCam.Lens.OrthographicSize = zoomAmount; // 확대!

        // 1. 기본 설정대로 흔들기
        if (impulseSource != null)
        {
            // 힘(Velocity)을 넣어서 흔들기. (기본값은 Vector3.down * 힘)
            // 패링은 강하니까 힘을 좀 세게(2.0f) 줍니다.
            impulseSource.GenerateImpulse(2.0f); 
        }

        // 3. 색수차(글리치) 최대
        if (chromaticAberration != null)
            chromaticAberration.intensity.value = 1.0f; // 화면 찢어짐!

        // 정지 상태로 현실 시간 0.15초 대기 (플레이어가 상황 인식)
        yield return new WaitForSecondsRealtime(0.15f);


        // --- B. 풀리는 과정 (슬로우 -> 정상) ---

        // 1. 시간: 약간 슬로우로 시작해서 복구
        Time.timeScale = 0.3f; 

        float timer = 0f;
        while (timer < zoomDuration)
        {
            timer += Time.unscaledDeltaTime; // timeScale 영향을 안 받는 시간 사용
            float t = timer / zoomDuration;

            // 2. 카메라: 부드럽게 원래대로 복구 (Lerp)
            if (virtualCam != null)
                virtualCam.Lens.OrthographicSize = Mathf.Lerp(zoomAmount, defaultLensSize, t);

            // 3. 색수차: 부드럽게 0으로 복구
            if (chromaticAberration != null)
                chromaticAberration.intensity.value = Mathf.Lerp(1.0f, 0f, t);

            yield return null;
        }

        // --- C. 원상 복구 확인 ---
        Time.timeScale = 1.0f;
        if (virtualCam != null) virtualCam.Lens.OrthographicSize = defaultLensSize;
        if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;
    }

    
    // 피격 시 호출할 함수
    public void OnHit()
    {
        isHit = true;
    }

    private void OnDrawGizmos()
    {
        if (footPos != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(footPos.position, groundCheckSize);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}