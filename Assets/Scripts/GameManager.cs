using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 정적 변수로 어디서든 접근 가능하게 함
    public static GameManager Instance { get; private set; }

    [Header("Global State")]
    public int currentScore = 0;
    public int currentHealth = 5;
    public bool isGameOver = false;

    private void Awake()
    {
        // 싱글톤 패턴 핵심 로직
        if (Instance != null && Instance != this)
        {
            // 이미 다른 GameManager가 있으면 나 자신을 파괴 (중복 방지)
            Destroy(gameObject); 
            return;
        }

        Instance = this;
        
        // 씬이 바뀌어도 파괴되지 않게 설정
        DontDestroyOnLoad(gameObject); 
    }

    // 예시 기능: 점수 추가
    public void AddScore(int amount)
    {
        currentScore += amount;
        Debug.Log($"현재 점수: {currentScore}");
    }
    
    // 예시 기능: 게임 오버 처리
    public void GameOver()
    {
        isGameOver = true;
        Debug.Log("게임 오버!");
        // 여기에 UI 표시 로직이나 씬 재시작 로직 연결
    }
}