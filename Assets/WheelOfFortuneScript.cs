using System.Collections;
using UnityEngine;
using System.Text.RegularExpressions;
using System;

public class WheelOfFortuneScript : MonoBehaviour {

    public KMAudio audio;
    public KMAudio.KMAudioRef music;
    public KMBombInfo bomb;
    public KMSelectable scoreBtn;
    public KMSelectable stopBtn;
    public KMSelectable module;
    public KMSelectable[] muteBtns;
    public GameObject[] muteXs;
    public GameObject wheel;
    public Renderer buttonRend;
    public Material[] buttonMats;
    public TextMesh[] displays;
    public Light[] lights;

    private int[] lightScores = { 7, 7, 6, 6, 5, 5, 4, 4, 3, 3 };
    private int target = 0;
    private int current = 0;
    private int curLight = -1;
    private bool animating = false;
    private bool focused = false;
    private bool activated = false;
    private float lightSpeed;
    private Coroutine lightAnim;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        stopBtn.OnInteract += delegate () { PressStopButton(stopBtn); return false; };
        scoreBtn.OnInteract += delegate () { PressScoreButton(scoreBtn); return false; };
        foreach (KMSelectable btn in muteBtns)
            btn.OnInteract += delegate () { PressMuteButton(btn); return false; };
        module.OnFocus += Focus;
        module.OnDefocus += Defocus;
        GetComponent<KMBombModule>().OnActivate += OnActivate;
    }

    void Focus()
    {
        focused = true;
        if (lightAnim != null || moduleSolved || muteXs[2].activeSelf)
            return;
        music = audio.PlaySoundAtTransformWithRef("music", transform);
    }

    void Defocus()
    {
        focused = false;
        if (music != null)
        {
            music.StopSound();
            music = null;
        }
    }

    void Start () {
        lightSpeed = 0.031f + UnityEngine.Random.Range(-0.003f, 0f);
        wheel.transform.localEulerAngles = new Vector3(0, UnityEngine.Random.Range(0f, 360f), 0);
        buttonRend.material = buttonMats[0];
        displays[0].gameObject.SetActive(false);
        displays[1].gameObject.SetActive(false);
        displays[2].gameObject.SetActive(false);
        float scalar = transform.lossyScale.x;
        foreach (Light l in lights)
            l.range *= scalar;
        for (int i = 0; i < lights.Length; i++)
            lights[i].enabled = false;
        target = UnityEngine.Random.Range(290, 401);
        displays[1].text = target.ToString();
        Debug.LogFormat("[Wheel of Fortune Arcade #{0}] The target score is {1}", moduleId, target);
    }

    void OnActivate()
    {
        buttonRend.material = buttonMats[1];
        displays[0].gameObject.SetActive(true);
        displays[1].gameObject.SetActive(true);
        displays[2].gameObject.SetActive(true);
        StartCoroutine(RunTheLights());
        activated = true;
    }

    void PressStopButton(KMSelectable pressed)
    {
        if (moduleSolved || !activated) return;
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
        if (!animating)
        {
            animating = true;
            buttonRend.material = buttonMats[0];
            StopAllCoroutines();
            for (int i = 0; i < lights.Length; i++)
                lights[i].enabled = false;
            int rig = UnityEngine.Random.Range(0, 2);
            Debug.LogFormat("<Wheel of Fortune Arcade #{0}> This hit is{1} rigged", moduleId, rig != 0 ? "" : "n't");
            if (curLight == 0 && rig == 0)
            {
                lightAnim = StartCoroutine(EffectsDuringSpin());
                StartCoroutine(SpinTheWheel());
            }
            else
            {
                if (curLight == 0)
                {
                    if (UnityEngine.Random.Range(0, 2) == 0)
                        curLight = 1;
                    else
                        curLight = 47;
                }
                StartCoroutine(FlashLight());
            }
        }
    }

    void PressScoreButton(KMSelectable pressed)
    {
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
        if (displays[2].text.Equals("T"))
        {
            displays[2].text = "C";
            displays[1].text = current.ToString("000");
        }
        else
        {
            displays[2].text = "T";
            displays[1].text = target.ToString();
        }
    }

    void PressMuteButton(KMSelectable pressed)
    {
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
        int index = Array.IndexOf(muteBtns, pressed);
        muteXs[index].SetActive(!muteXs[index].activeSelf);
        if (index == 2 && muteXs[index].activeSelf && music != null)
        {
            music.StopSound();
            music = null;
        }
        else if (index == 2 && !muteXs[index].activeSelf && music == null && lightAnim == null && !moduleSolved)
            music = audio.PlaySoundAtTransformWithRef("music", transform);
    }

    IEnumerator RunTheLights()
    {
        int start = 1;
        while (true)
        {
            for (int i = start; i < lights.Length; i++)
            {
                if (curLight != -1)
                    lights[curLight].enabled = false;
                lights[i].enabled = true;
                curLight = i;
                yield return new WaitForSecondsRealtime(lightSpeed);
            }
            start = 0;
        }
    }

    IEnumerator FlashLight()
    {
        if (!muteXs[0].activeSelf)
            audio.PlaySoundAtTransform("ding", transform);
        for (int i = 0; i < 7; i++)
        {
            if (i == 3 && !muteXs[1].activeSelf)
            {
                if (UnityEngine.Random.Range(0, 2) == 0)
                    audio.PlaySoundAtTransform("ooh", transform);
                else
                    audio.PlaySoundAtTransform("aah", transform);
            }
            if (i != 0)
            {
                lights[curLight].enabled = true;
                yield return new WaitForSecondsRealtime(0.15f);
            }
            lights[curLight].enabled = false;
            yield return new WaitForSecondsRealtime(0.15f);
        }
        if (curLight <= 10)
        {
            current += lightScores[curLight - 1];
            Debug.LogFormat("[Wheel of Fortune Arcade #{0}] Hit the light {3} clockwise from blue for {2}, current score: {1}", moduleId, current, lightScores[curLight - 1], curLight);
        }
        else if (curLight >= 38)
        {
            current += lightScores[47 - curLight];
            Debug.LogFormat("[Wheel of Fortune Arcade #{0}] Hit the light {3} clockwise from blue for {2}, current score: {1}", moduleId, current, lightScores[47 - curLight], curLight);
        }
        else
            Debug.LogFormat("[Wheel of Fortune Arcade #{0}] Hit the light {2} clockwise from blue for 0, current score: {1}", moduleId, current, curLight);
        if (displays[2].text.Equals("C"))
            displays[1].text = current.ToString("000");
        if (current >= target)
        {
            if (music != null)
            {
                music.StopSound();
                music = null;
            }
            moduleSolved = true;
            GetComponent<KMBombModule>().HandlePass();
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            Debug.LogFormat("[Wheel of Fortune Arcade #{0}] Current score is greater than or equal to the target score, module solved!", moduleId);
            yield break;
        }
        curLight = -1;
        StartCoroutine(RunTheLights());
        animating = false;
        buttonRend.material = buttonMats[1];
    }

    IEnumerator SpinTheWheel()
    {
        if (music != null)
        {
            music.StopSound();
            music = null;
        }
        Debug.LogFormat("[Wheel of Fortune Arcade #{0}] Blue light has been hit, let's spin the wheel!", moduleId);
        float speed = 0.01f;
        int times = UnityEngine.Random.Range(650, 701);
        for (int i = 0; i < times; i++)
        {
            wheel.transform.Rotate(0.0f, speed, 0.0f, Space.Self);
            if (i <= (times * .2))
                speed += Time.deltaTime * 5f;
            else  if (i >= (times * .8))
                speed -= Time.deltaTime * 5f;
            yield return new WaitForSecondsRealtime(0.01f);
        }
        float rotation = wheel.transform.localEulerAngles.y;
        Debug.LogFormat("<Wheel of Fortune Arcade #{0}> Wheel rotation: {1}", moduleId, rotation);
        if (rotation >= 0 && rotation < 9f || rotation >= 351f && rotation < 360f)
        {
            current += 90;
            Debug.LogFormat("[Wheel of Fortune Arcade #{0}] The wheel landed on 90, current score: {1}", moduleId, current);
        }
        else
        {
            int[] values = { 300, 50, 40, 25, 80, 35, 45, 70, 30, 60, 500, 30, 50, 60, 55, 40, 90, 25, 30 };
            int ct = 0;
            while (true)
            {
                if (rotation >= (9f + (18 * ct)) && rotation < (27f + (18 * ct)))
                {
                    current += values[ct];
                    Debug.LogFormat("[Wheel of Fortune Arcade #{0}] The wheel landed on {1}, current score: {2}", moduleId, values[ct], current);
                    break;
                }
                ct++;
            }
        }
        if (displays[2].text.Equals("C"))
            displays[1].text = current.ToString("000");
        StopCoroutine(lightAnim);
        lightAnim = null;
        for (int i = 0; i < lights.Length; i++)
            lights[i].enabled = false;
        if (current >= target)
        {
            moduleSolved = true;
            GetComponent<KMBombModule>().HandlePass();
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            Debug.LogFormat("[Wheel of Fortune Arcade #{0}] Current score is greater than or equal to the target score, module solved!", moduleId);
            yield break;
        }
        if (focused && !muteXs[2].activeSelf)
            music = audio.PlaySoundAtTransformWithRef("music", transform);
        curLight = -1;
        StartCoroutine(RunTheLights());
        animating = false;
        buttonRend.material = buttonMats[1];
    }

    IEnumerator EffectsDuringSpin()
    {
        if (!muteXs[0].activeSelf)
            audio.PlaySoundAtTransform("spin", transform);
        yield return new WaitForSecondsRealtime(3.8f);
        if (!muteXs[1].activeSelf)
            audio.PlaySoundAtTransform("chant", transform);
        int prev1 = -1;
        int prev2 = -1;
        int index = 10;
        while (true)
        {
            if (prev1 != -1)
                lights[prev1].enabled = false;
            if (prev2 != -1)
                lights[prev2].enabled = false;
            if (index != 0)
            {
                lights[index].enabled = true;
                lights[48 - index].enabled = true;
                prev1 = index;
                prev2 = 48 - index;
            }
            else
            {
                lights[index].enabled = true;
                prev1 = index;
                prev2 = index;
            }
            index--;
            if (index == -1)
                index = 10;
            yield return new WaitForSecondsRealtime(0.05f);
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} stop [Hits the stop button] | !{0} toggle [Toggles the score display] | !{0} mute <s/sfx/v/voice/m/music> [Presses the specified mute button]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*stop\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (animating)
            {
                yield return "sendtochaterror The stop button can only be hit when it is lit!";
                yield break;
            }
            yield return null;
            stopBtn.OnInteract();
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*toggle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            scoreBtn.OnInteract();
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*mute s|sfx\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            muteBtns[0].OnInteract();
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*mute v|voice\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            muteBtns[1].OnInteract();
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*mute m|music\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            muteBtns[2].OnInteract();
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!moduleSolved)
        {
            if (curLight == 0 && lightAnim == null)
                stopBtn.OnInteract();
            yield return true;
        }
    }
}