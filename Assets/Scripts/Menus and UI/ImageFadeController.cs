using UnityEngine;
using UnityEngine.UI;

public class ImageFadeController : MonoBehaviour
{
    private float decayTime;
    private float fadeTime;
    private float totalFadeTime;
    private MaskableGraphic[] graphics;
    private float[] maxAlpha;
    private bool timerStarted;
    void Awake()
    {
        graphics = GetComponentsInChildren<MaskableGraphic>();
        maxAlpha = new float[graphics.Length];
        for (int i = 0; i < graphics.Length; i++)
            maxAlpha[i] = graphics[i].color.a;
    }
    public void SetVisible()
    {
        timerStarted = false;
        gameObject.SetActive(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].color = new Color(graphics[i].color.r, graphics[i].color.g, graphics[i].color.b, maxAlpha[i]);
    }
    public void UnlockTimer()
    {
        timerStarted = true;
    }
    public void SetTimer(float decayTime, float fadeTime)
    {
        gameObject.SetActive(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].color = new Color(graphics[i].color.r, graphics[i].color.g, graphics[i].color.b, maxAlpha[i]);

        this.decayTime = decayTime;
        this.fadeTime = fadeTime;
        totalFadeTime = fadeTime;
    }
    public void StartTimer(float decayTime, float fadeTime)
    {
        SetTimer(decayTime, fadeTime);
        UnlockTimer();
    }
    void Update()
    {
        if (!timerStarted)
            return;

        if (decayTime > 0f)
            decayTime -= Time.deltaTime;
        else if (fadeTime > 0f)
        {
            fadeTime -= Time.deltaTime;
            for (int i = 0; i < graphics.Length; i++)
                graphics[i].color = new Color(graphics[i].color.r, graphics[i].color.g, graphics[i].color.b, maxAlpha[i] * (fadeTime / totalFadeTime));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
