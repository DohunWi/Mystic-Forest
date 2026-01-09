using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("상태창 UI")]
    public Image hpBar; 
    public Image mpBar; 
    public Text scoreText; // 점수 텍스트 - 

    [Header("패널 연결")]
    public GameObject pausePanel;
    public GameObject gameOverPanel;

    [Header("보스 UI")]
    public GameObject bossHealthPanel; 
    public Image bossHpBar;
    private bool isPaused = false; // 현재 일시정지 중인가?

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        // ESC 키를 누르면 일시정지 토글 (게임오버 상태가 아닐 때만)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    // --- 체력 & 점수 관리 ---
    // 체력 업데이트 함수 (비율 계산)
    public void UpdateHealthUI(int currentHealth)
    {
        // 1. 최대 체력 가져오기 (비율 계산을 위해 필요)
        float maxHealth = GameManager.Instance.maxHealth;

        // 2. 현재 체력 비율 계산 (0.0 ~ 1.0)
        // ★ 주의: 정수끼리 나누면 소수점이 버려지므로 반드시 (float)로 형변환 해야 함
        float ratio = (float)currentHealth / maxHealth;

        // 3. UI에 적용 (Fill Amount 조절)
        if (hpBar != null)
        {
            hpBar.fillAmount = ratio; 
            // 예: 체력 50/100 -> ratio 0.5 -> 바가 절반만 참
        }
    }
    
    // Boss UI
    public void ShowBossHealth(bool isShow)
    {
        if (bossHealthPanel != null)
        {
            bossHealthPanel.SetActive(isShow);
            // 켜질 때 체력 꽉 채워서 보여주기
            if (isShow && bossHpBar != null) bossHpBar.fillAmount = 1.0f;
        }
    }

    // 보스 체력 업데이트 (BossController가 호출)
    public void UpdateBossHealth(float currentHealth, float maxHealth)
    {
        if (bossHpBar != null)
        {
            // float 형변환 확인하기
            bossHpBar.fillAmount = currentHealth / maxHealth;
        }
    }
    public void UpdateScoreUI(int score)
    {
        if (scoreText != null)
            scoreText.text = "x " + score.ToString();
    }

    // --- 일시정지 기능 ---
    public void PauseGame()
    {
        isPaused = true;
        pausePanel.SetActive(true); // 패널 켜기
        Time.timeScale = 0f; // ★ 시간을 멈춤 (물리, 이동 정지)
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false); // 패널 끄기
        Time.timeScale = 1f; // ★ 시간을 다시 흐르게 함
    }

    // "저장하고 나가기" 버튼용
    public void SaveAndQuit()
    {
        Time.timeScale = 1f; // (중요) 씬 이동 전에 시간은 다시 흐르게 해야 함
        GameManager.Instance.SaveGame(); // 저장
        SceneManager.LoadScene("TitleScene"); // 타이틀로 이동 (씬 이름 확인!)
    }

    // --- 게임오버 기능 ---
    public void ShowGameOver()
    {
        gameOverPanel.SetActive(true);
        // 게임오버 시 시간을 멈출지 말지는 선택 (보통은 멈추거나 슬로우모션)
        Time.timeScale = 0f; 
    }

    public void RetryGame()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Retry(); 
        }
        else
        {
            // 보험
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}