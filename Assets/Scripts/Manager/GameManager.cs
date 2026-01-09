using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Data")]
    public int maxHealth = 5;
    public int currentHealth = 5;
    public Vector3 lastCheckPointPos; 
    
    // ★ 1. 저장 경로를 프로퍼티로 변경 (외부에서 접근 가능, 일관성 유지)
    // Application.persistentDataPath는 윈도우/맥/모바일 어디서든 안전한 저장소를 찾아줌.
    public string SavePath
    {
        get { return Path.Combine(Application.persistentDataPath, "save.json"); }
    }

    // ★ 2. 파일이 존재하는지 확인하는 헬퍼 (MainMenu에서 사용)
    public bool HasSaveFile
    {
        get { return File.Exists(SavePath); }
    }

    [Header("State")]
    public bool isGameOver = false;
    public int currentScore = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 시작 시 체크포인트가 없으면 플레이어 위치를 찾아 임시 저장
        if (lastCheckPointPos == Vector3.zero) 
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) lastCheckPointPos = player.transform.position;
        }
    }

    // ★ 저장하기
    public void SaveGame()
    {
        // 저장하기 직전에 현재 플레이어의 상태를 가져와서 갱신함
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 1. 현재 위치 갱신
            lastCheckPointPos = player.transform.position;

            // 2. 현재 체력 갱신 (PlayerHealth 스크립트에서 가져오기)
            PlayerHealth pHealth = player.GetComponent<PlayerHealth>();
            if (pHealth != null)
            {
                currentHealth = pHealth.currentHealth;
            }
        }
        SaveData data = new SaveData();
        data.playerPosition = lastCheckPointPos;
        data.health = currentHealth;
        // 현재 씬 번호도 저장
        data.currentSceneIndex = SceneManager.GetActiveScene().buildIndex; 

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json); // SavePath 프로퍼티 사용

        Debug.Log("게임 저장 완료: " + SavePath);
        Debug.Log("PlayerPosition: " + data.playerPosition);
        Debug.Log("Health: " + data.health);
        Debug.Log("Scene Index: " + data.currentSceneIndex);

    }

    // ★ 불러오기 (타이틀 -> 게임 진입)
    public void LoadGame()
    {
        if (!HasSaveFile)
        {
            Debug.Log("저장된 파일이 없습니다.");
            return; 
        }
        Debug.Log("저장된 파일을 불러옵니다.");

        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // 1. 데이터 적용
        lastCheckPointPos = data.playerPosition;
        currentHealth = data.health;
        
        // 2. 저장된 씬으로 이동
        int sceneIndex = (data.currentSceneIndex > 0) ? data.currentSceneIndex : 1;

        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeToScene(sceneIndex);
        }
        else
        {
            SceneManager.LoadScene(sceneIndex);
        }

        Debug.Log("로드 완료! 위치: " + lastCheckPointPos);
    }

    // ★ 새로하기
    public void NewGame()
    {
        Debug.Log("새로운 게임 시작!!");
        // 데이터 초기화
        lastCheckPointPos = Vector3.zero; 
        currentHealth = maxHealth;
        isGameOver = false;

        // ★ 페이더가 있으면 페이드 효과 사용, 없으면 그냥 로드 (에러 방지)
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeToScene(1);
        }
        else
        {
            SceneManager.LoadScene(1); 
        }
    }
    public void Retry()
    {
        Debug.Log("게임 다시 시작!!");
        // 1. 체력을 최대치로 회복 (이게 없으면 죽은 체력 그대로 시작됨)
        currentHealth = maxHealth; 
        
        // 2. 게임오버 상태 해제
        isGameOver = false;

        // 3. 시간 다시 흐르게 하기
        Time.timeScale = 1f;

        // 4. 현재 씬을 다시 로드
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void AddScore(int amount)
    {
        currentScore += amount;
        Debug.Log($"현재 점수: {currentScore}");
    }
    
    // ★ 게임 오버 처리 (UI 연동)
    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        Debug.Log("사망! 게임오버 UI 호출");
        
        // 1. 체력바 0으로 갱신
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateHealthUI(0);

        // 2. 자동 부활 삭제 -> UI 창 띄우기
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOver();
        }
    }
}