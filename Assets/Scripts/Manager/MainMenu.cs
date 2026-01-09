using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using System.IO;      

public class MainMenu : MonoBehaviour
{
    [Header("UI 연결")]
    public Button continueButton; // '이어 하기' 버튼 컴포넌트

    void Start()
    {
        // 1. 저장된 파일이 있는지 경로 확인
        string savePath = Path.Combine(Application.persistentDataPath, "save.json");
        
        // 2. 파일 존재 여부에 따라 버튼 상태 결정
        if (File.Exists(savePath))
        {
            // 파일이 있으면 -> 버튼 활성화 (클릭 가능, 글자 색 선명함)
            continueButton.interactable = true; 
        }
        else
        {
            // 파일이 없으면 -> 버튼 비활성화 (클릭 불가, 글자 색 흐릿해짐)
            continueButton.interactable = false;
        }
    }

    // --- 버튼 클릭 시 실행될 함수들 ---

    public void OnClickNewGame()
    {
        // GameManager에게 '새 게임(데이터 초기화 + 1스테이지 이동)' 명령
        if (GameManager.Instance != null)
        {
            GameManager.Instance.NewGame();
        }
        else
        {
            // 혹시 GameManager가 없다면 임시로 1번 씬 로드
            SceneManager.LoadScene(1);
        }
    }

    public void OnClickContinue()
    {
        // GameManager에게 '저장된 데이터 로드 + 해당 씬 이동' 명령
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadGame();
        }
    }

    public void OnClickQuit()
    {
        Debug.Log("게임 종료!");
        Application.Quit(); // 실제 빌드된 게임(exe, apk)에서만 창이 꺼집니다.
    }
}