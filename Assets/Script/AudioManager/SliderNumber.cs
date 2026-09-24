using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SliderNumber : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI slidertext;

    void Update()
    {
        slidertext.text = (slider.value * 100f).ToString("0") + "%";
    }
}
