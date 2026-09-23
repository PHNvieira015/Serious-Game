using TMPro;
using UnityEngine;

public class Dropdown : MonoBehaviour
{
    [SerializeField] private TMP_Text numberText;

    public void Dropdownsample(int index)
    {
        switch (index)
        {
            case 0:
                numberText.text = "1";
                break;

            case 1:
                numberText.text = "2";
                break;

            case 2:
                numberText.text = "3";
                break;

            case 3:
                numberText.text = "4";
                break;

            case 4:
                numberText.text = "5";
                break;
        }
    }
}
