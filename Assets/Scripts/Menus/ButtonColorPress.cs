using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ButtonColorPress : MonoBehaviour
{
    Image buttonImage;
    [SerializeField] float vColor = 0.2f;
    bool changeColor = true;
    private void Awake()
    {
        buttonImage = GetComponent<Image>();
    }
    void Start()
    {
        
    }
    public void MakeColorPressed()
    {
        changeColor = false;
        Color.RGBToHSV(buttonImage.color, out float h, out float s, out float v);
        Color pressedButtonColr = Color.HSVToRGB(h, s, vColor);
        buttonImage.DOColor(pressedButtonColr, 0.5f).SetEase(Ease.InSine).OnComplete(() =>
        {
            buttonImage.DOColor(Color.HSVToRGB(h, s, v), 0.5f).OnComplete(() =>
            {
                changeColor = true;
            });
        });
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G) && changeColor)
        {
            MakeColorPressed();
        }
    }
}
