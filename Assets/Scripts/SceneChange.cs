using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChange : MonoBehaviour
{
    public static SceneChange Instance { get; private set; }

    private GameObject sceneChangeCanvas;

    [Header("Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeTime = 0.5f;

    public bool IsChanging { get; private set; }

    private void Awake()
    {
        Canvas canvas = fadeImage != null ? fadeImage.GetComponentInParent<Canvas>(true) : null;
        sceneChangeCanvas = canvas != null ? canvas.gameObject : null;

        if (Instance != null && Instance != this)
        {
            if (fadeImage != null)
            {
                Instance.ApplySceneFadeColor(fadeImage.color);
            }

            if (sceneChangeCanvas != null)
            {
                Destroy(sceneChangeCanvas);
            }

            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (sceneChangeCanvas != null)
        {
            transform.SetParent(sceneChangeCanvas.transform, false);
            DontDestroyOnLoad(sceneChangeCanvas);
        }
        else
        {
            Debug.LogWarning("SceneChangeCanvas was not found. SceneChange will be kept instead.", this);
            DontDestroyOnLoad(gameObject);
        }

        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0;
            fadeImage.color = color;
        }
    }

    private void ApplySceneFadeColor(Color sceneColor)
    {
        if (fadeImage == null)
        {
            return;
        }

        sceneColor.a = fadeImage.color.a;
        fadeImage.color = sceneColor;
    }

    public void LoadScene(string sceneName)
    {
        if (IsChanging) return;

        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        IsChanging = true;

        //　関数をここで呼ぶ↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓

        // フェードアウト
        yield return StartCoroutine(Fade(0f, 1f));

        SceneManager.LoadScene(sceneName);

        yield return null;

        // フェードイン
        yield return StartCoroutine(Fade(1f, 0f));

        IsChanging = false;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {

        float time = 0f;

        while (time < fadeTime)
        {
            //↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓ここにシーンチェンジの処理を記述する↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓
            //やるときは消して
            //フェードになってるから画像使いたいときはalphaの部分を外のクラスから参照できるようにしてそのクラスで画像の色とか位置を変えるのがいいとおもうぞ

            time += Time.deltaTime;

            float alpha = Mathf.Lerp(
                startAlpha,
                endAlpha,
                time / fadeTime
            );

            Color color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;

            //↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑

            yield return null;
        }

        // 変数はここで変更↓↓↓↓↓↓↓↓↓↓↓↓↓
        Color finalColor = fadeImage.color;
        finalColor.a = endAlpha;
        fadeImage.color = finalColor;
    }
}
