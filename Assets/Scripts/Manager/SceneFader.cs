using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance;

    [Header("설정")]
    public GameObject fadePanel;
    public CanvasGroup canvasGroup;
    public float fadeDuration = 1.0f; // 페이드 시간 (1초)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바껴도 파괴되지 않음
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        //  게임 시작하자마자 패널을 강제로 킴.
        if (fadePanel != null)
        {
            fadePanel.SetActive(true);
        }

        // 게임 시작 시(또는 씬 로드 직후) 검은 화면 -> 투명하게 (Fade In)
        StartCoroutine(FadeIn());
    }

    // 외부에서 이 함수를 호출해서 씬을 바꿈.
    public void FadeToScene(int sceneIndex)
    {
        StartCoroutine(FadeOutAndLoad(sceneIndex));
    }

    // 화면이 밝아짐 (검은색 Alpha 1 -> 0)
    IEnumerator FadeIn()
    {
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // 1에서 0으로 줄어듦
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false; // 마우스 클릭 허용
    }

    // 화면이 어두워지고 -> 씬 로드 -> 다시 밝아짐
    IEnumerator FadeOutAndLoad(int sceneIndex)
    {
        canvasGroup.blocksRaycasts = true; // 이동 중 클릭 방지

        // 1. Fade Out (투명 -> 검은색)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            // 0에서 1로 늘어남
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // 2. 씬 로드 (검은 화면일 때 몰래 바꿈)
        yield return SceneManager.LoadSceneAsync(sceneIndex);

        // 3. Fade In (검은색 -> 투명)
        // 새 씬이 로드되면 Start()가 다시 호출되면서 FadeIn이 실행될 수도 있지만,
        // 안전하게 여기서 명시적으로 실행하거나 or Start()에 맡겨도 됨.
        StartCoroutine(FadeIn()); 
    }
}