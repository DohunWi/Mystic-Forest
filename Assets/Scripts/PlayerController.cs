using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("1. Movement & Jump")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 16f;
    [Range(0, 1)] [SerializeField] private float jumpCutMultiplier = 0.5f; // 점프 키 뗐을 때 감속 (낮은 점프)
    [SerializeField] private float gravityScale = 4.5f; // 기본 중력 (떨어질 때 빠르 게)
    [SerializeField] private float fallGravityMult = 1.5f; // 낙하 시 추가 가속 (묵직한 느낌)

    [Header("2. Dash")]
    [SerializeField] private float dashPower = 24f;
    [SerializeField] private float dashTime = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("3. Advanced Physics (Feel)")]
    [SerializeField] private float coyoteTime = 0.1f; // 땅에서 떨어져도 점프 가능한 시간
    [SerializeField] private float jumpBufferTime = 0.1f; // 땅에 닿기 전 점프 미리 입력 허용 시간
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform footPos; // 발바닥 위치 (빈 오브젝트)
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.2f); // 접지 판정 박스 크기

    [Header("Combat")]
    [SerializeField] private GameObject attackVFX; // 자식 오브젝트를 연결할 변수
    [SerializeField] private float attackDuration = 0.2f; // 공격 이펙트가 보여질 시간 (애니메이션 길이와 맞춰주세요)
    [SerializeField] private Transform attackPoint; // 공격 중심점
    [SerializeField] private float attackRange = 0.5f; // 공격 반경
    [SerializeField] private LayerMask enemyLayer; // 적 레이어 (선택 사항, 없으면 다 때림)

    [Header("Audio")]
    [SerializeField] private AudioClip attackSound; // 공격 소리 파일
    [SerializeField] private AudioClip jumpSound;   // 점프 소리 파일
    [SerializeField] private AudioClip dashSound;   // 대쉬 소리 파일

    // 내부 변수
    private Rigidbody2D rb;
    private Vector2 moveInput;
    
    // 상태 플래그
    private bool isDashing;
    private bool canDash = true;
    private bool isJumping;
    private bool isGrounded;
    private bool isAttacking;
    // 애니메이터 변수
    private Animator anim;

    // 고급 점프용 타이머
    private float lastGroundedTime;
    private float lastJumpPressedTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // 중력 스케일 초기 설정
        rb.gravityScale = gravityScale;
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        // 타이머 업데이트
        lastGroundedTime -= Time.deltaTime;
        lastJumpPressedTime -= Time.deltaTime;

        if (isDashing) return;

        // 1. 접지 판정 (BoxCast 이용 - Raycast보다 안정적)
        // 발바닥 위치에서 아래로 쏘는 사각형 레이캐스트
        isGrounded = Physics2D.OverlapBox(footPos.position, groundCheckSize, 0f, groundLayer);
        
        // 땅에 닿아있으면 코요테 타임 갱신 (항상 점프 가능 상태로 유지)
        if (isGrounded && rb.linearVelocity.y <= 0) // 올라가는 중이 아닐 때만
        {
            lastGroundedTime = coyoteTime;
            isJumping = false;
        }

        // 2. 점프 실행 로직 (코요테 타임 > 0  AND  점프 버퍼 > 0)
        if (lastJumpPressedTime > 0 && lastGroundedTime > 0 && !isJumping)
        {
            PerformJump();
        }

        // 3. 방향 전환
        if (moveInput.x != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(moveInput.x), 1, 1);
        }

        // 4. 낙하 시 중력 조절 (더 묵직한 조작감)
        if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = gravityScale * fallGravityMult;
        }
        else
        {
            rb.gravityScale = gravityScale;
        }

        // 에니메이션 업데이트
        UpdateAnimationState();
    }

    private void FixedUpdate()
    {
        if (isDashing) return;

        // 이동 적용
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
    }

    //애니메이션 상태 관리 함수
    private void UpdateAnimationState()
    {
        if (anim == null) return;

        // 1. 달리기 상태 (입력값이 0이 아니면 달리는 중)
        bool running = Mathf.Abs(moveInput.x) > 0;
        anim.SetBool("isRunning", running);

        // 2. 바닥 상태
        // isGrounded 변수는 Update에서 이미 계산되고 있다고 가정
        anim.SetBool("isGrounded", isGrounded); // 혹은 lastGroundedTime > 0

        // 3. 수직 속도 (점프 vs 낙하 구분용)
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
        
    }

    private void PerformJump()
    {
        isJumping = true;
        lastGroundedTime = 0; // 점프 했으니 코요테 타임 즉시 소멸
        lastJumpPressedTime = 0; // 점프 했으니 버퍼 즉시 소멸

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0); // 기존 Y 속도 초기화
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    // --- Input System Events ---

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        // 키를 누른 순간: 버퍼 타임 설정
        if (value.isPressed)
        {
            lastJumpPressedTime = jumpBufferTime;
            // 점프 소리 재생
            if (isGrounded)
            {
                SoundManager.Instance.PlaySFX(jumpSound);
            }
        }
        // 키를 뗐을 때 (가변 점프): 점프 중이라면 속도를 깎아서 낮게 점프
        else if (rb.linearVelocity.y > 0 && isJumping)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed && canDash && !isDashing)
        {
            StartCoroutine(DashRoutine());
        }
    }

    public void OnAttack(InputValue value)
    {
        // 키를 눌렀고, 공격 중이 아니고, 대쉬 중이 아닐 때 실행
        if (value.isPressed && !isAttacking && !isDashing)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    // --- Coroutines ---
    private IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // 1. 플레이어 몸체 애니메이션 실행 (만약 있다면)
        if (anim != null) anim.SetTrigger("Attack");

        // 공격 소리 재생
        SoundManager.Instance.PlaySFX(attackSound);

        // 2. 이펙트 오브젝트 켜기! (이때 이펙트 애니메이션이 자동 재생됨)
        if (attackVFX != null)
        {
            attackVFX.SetActive(true);
        }

        // 공격 판정
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);

        foreach (Collider2D enemy in hitEnemies)
        {
            // 'Enemy' 태그가 붙은 놈만 때림
            if (enemy.CompareTag("Enemy"))
            {
                // 상대방의 Enemy 스크립트를 가져와서 데미지 함수 실행
                Enemy enemyScript = enemy.GetComponent<Enemy>();
                if (enemyScript != null)
                {
                    enemyScript.TakeDamage(1);
                }
            }
        }

        // 3. 공격 애니메이션 시간만큼 대기
        yield return new WaitForSeconds(attackDuration);

        // 4. 이펙트 끄기
        if (attackVFX != null)
        {
            attackVFX.SetActive(false);
        }

        isAttacking = false;
        
        
    }
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
    private IEnumerator DashRoutine()
    {
        isDashing = true;
        anim.SetBool("isDashing", isDashing);
        SoundManager.Instance.PlaySFX(dashSound);
        canDash = false;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(transform.localScale.x * dashPower, 0f);

        yield return new WaitForSeconds(dashTime);

        rb.gravityScale = originalGravity;
        rb.linearVelocity = Vector2.zero;
        isDashing = false;
        anim.SetBool("isDashing", isDashing);

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // --- Editor Debugging ---
    // 씬 뷰에서 접지 판정 박스를 눈으로 확인하기 위함
    private void OnDrawGizmos()
    {
        if (footPos != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(footPos.position, groundCheckSize);
        }
    }
}