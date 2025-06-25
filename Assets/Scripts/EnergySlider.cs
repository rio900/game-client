using UnityEngine;
using UnityEngine.UI;

public class EnergySlider : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private float _fillDuration = 1f;
    private float _startTime;
    private bool _isFilling;

    void Start()
    {
        if (slider == null)
            slider = GetComponent<Slider>();

        slider.value = 0f;
    }

    void Update()
    {
        if (_isFilling)
        {
            float elapsed = Time.time - _startTime;
            float t = Mathf.Clamp01(elapsed / _fillDuration);
            slider.value = t;

            if (t >= 1f)
            {
                slider.value = 0f;
                _isFilling = false;
            }
        }
    }


    public void FillOverTime(float duration)
    {
        if (_isFilling) return;

        _fillDuration = duration;
        _startTime = Time.time;
        _isFilling = true;
        slider.value = 0f;
    }

    public void FillInstantly()
    {
        _isFilling = false;
        slider.value = 0f;
    }
}