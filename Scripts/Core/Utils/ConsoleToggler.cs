using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Utils
{
    public class ConsoleToggler : MonoBehaviour
    {
        public GameObject Console;
        
    [Header("Output")]
    public UnityEvent OnActivated;

    [Header("Hot Zone (normalized 0..1 screen coords)")]
    [Tooltip("Hot zone center")]
    public Vector2 hotZoneCenter01 = new Vector2(0.9f, 0.1f); // sağ-alt köşe civarı
    [Tooltip("Hot zone size")]
    public Vector2 hotZoneSize01 = new Vector2(0.12f, 0.12f);

    [Header("Pattern")]
    [Tooltip("Required taps. Then a long tap is required")]
    public int requiredTaps = 6;
    [Tooltip("Max duration for tap")]
    public float shortTapMaxDuration = 0.22f;
    [Tooltip("Max delay between taps")]
    public float interTapMaxGap = 1.0f;
    [Tooltip("Long press duration")]
    public float longPressDuration = 0.7f;
    [Tooltip("Tolerance between taps")]
    public float moveTolerancePx = 18f;

    [Header("Editor Test")]
    public bool enableEditorMouseTest = true; 

    // --- state ---
    int _tapCount;
    float _lastTapEndTime;
    bool _pressActive;
    Vector2 _pressStartPos;
    float _pressStartTime;

    void Update()
    {
#if UNITY_EDITOR
        if (enableEditorMouseTest)
        {
            HandleMouse();
        }
#endif
        
#if DEV_BUILD || UNITY_EDITOR
    if(Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ToggleConsole();
        }
#endif
        HandleTouch();
    }

    Rect GetHotZonePixels()
    {
        float w = Screen.width * Mathf.Clamp01(hotZoneSize01.x);
        float h = Screen.height * Mathf.Clamp01(hotZoneSize01.y);
        float cx = Screen.width * Mathf.Clamp01(hotZoneCenter01.x);
        float cy = Screen.height * Mathf.Clamp01(hotZoneCenter01.y);

        return new Rect(cx - w * 0.5f, cy - h * 0.5f, w, h);
    }

    void ResetSequence()
    {
        _tapCount = 0;
        _pressActive = false;
        _pressStartTime = 0f;
    }

    bool InHotZone(Vector2 posPixel)
    {
        return GetHotZonePixels().Contains(posPixel);
    }

    void HandleTouch()
    {
        if (Input.touchCount <= 0) return;

        var t = Input.GetTouch(0);
        switch (t.phase)
        {
            case TouchPhase.Began:
                // inter-tap zaman aşımı
                if (_tapCount > 0 && (Time.unscaledTime - _lastTapEndTime) > interTapMaxGap)
                    ResetSequence();

                if (!InHotZone(t.position))
                {
                    // Hot zone dışında başlarsa sıfırla
                    ResetSequence();
                    return;
                }

                _pressActive = true;
                _pressStartPos = t.position;
                _pressStartTime = Time.unscaledTime;
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                if (!_pressActive) return;
                // fazla hareket ederse başarısız say
                if ((t.position - _pressStartPos).magnitude > moveTolerancePx)
                {
                    ResetSequence();
                    return;
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (!_pressActive) return;

                float held = Time.unscaledTime - _pressStartTime;
                bool isShortTap = held <= shortTapMaxDuration;
                bool isLongPress = held >= longPressDuration;

                if (_tapCount < requiredTaps)
                {
                    // kısa tap bekliyoruz
                    if (isShortTap && InHotZone(t.position))
                    {
                        _tapCount++;
                        _lastTapEndTime = Time.unscaledTime;
                        _pressActive = false;
                        // başarılı; sıradaki dokunuşa geç
                    }
                    else
                    {
                        ResetSequence();
                    }
                }
                else
                {
                    // (N+1). dokunuş: uzun basma beklenir
                    if (isLongPress && InHotZone(t.position))
                    {
                        // başarı!
                        ToggleConsole();
                        ResetSequence();
                    }
                    else
                    {
                        ResetSequence();
                    }
                }
                break;
        }
    }

#if UNITY_EDITOR
    void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (_tapCount > 0 && (Time.unscaledTime - _lastTapEndTime) > interTapMaxGap)
                ResetSequence();

            Vector2 pos = Input.mousePosition; 
            if (!InHotZone(pos))
            {
                ResetSequence();
                return;
            }
            _pressActive = true;
            _pressStartPos = pos;
            _pressStartTime = Time.unscaledTime;
        }

        if (Input.GetMouseButton(0) && _pressActive)
        {
            Vector2 pos = Input.mousePosition;
            if ((pos - _pressStartPos).magnitude > moveTolerancePx)
            {
                ResetSequence();
                return;
            }
        }

        if (Input.GetMouseButtonUp(0) && _pressActive)
        {
            Vector2 pos = Input.mousePosition;
            float held = Time.unscaledTime - _pressStartTime;
            bool isShortTap = held <= shortTapMaxDuration;
            bool isLongPress = held >= longPressDuration;

            if (_tapCount < requiredTaps)
            {
                if (isShortTap && InHotZone(pos))
                {
                    _tapCount++;
                    _lastTapEndTime = Time.unscaledTime;
                    _pressActive = false;
                }
                else
                {
                    ResetSequence();
                }
            }
            else
            {
                if (isLongPress && InHotZone(pos))
                {
                    ToggleConsole();
                    ResetSequence();
                }
                else
                {
                    ResetSequence();
                }
            }
        }
    }
#endif
        [Button("Toggle Console")]
        private void ToggleConsole()
        {
            OnActivated?.Invoke();
            if (Console == null) return;
            Console.SetActive(!Console.activeSelf);
        }
        
    // (opsiyonel) Scene view'da hot zone'u görmek için
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var r = GetHotZonePixels();
    }
#endif
    }
}