using UnityEngine;

public class BGMStarter : MonoBehaviour
{
    [Header("Music Settings")]
    [SerializeField] private AudioClip backgroundMusic; // 이 맵에서 틀 음악 파일

    private void Start()
    {
        // 게임 시작 시 SoundManager에게 음악 재생을 요청
        if (SoundManager.Instance != null && backgroundMusic != null)
        {
            SoundManager.Instance.PlayBGM(backgroundMusic);
        }
    }
}