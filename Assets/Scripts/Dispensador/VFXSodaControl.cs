using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class VFXSodaControl : MonoBehaviour
{
    [SerializeField] MeshRenderer meshRend;


    void Start()
    {
        meshRend = GetComponent<MeshRenderer>();   
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) VFXDispensarSoda();
    }
    void VFXDispensarSoda()
    {
        DOVirtual.Float(0.3f, 0.75f, 3f, y => SetTextureOffset(y)).OnComplete(()=>
        {
            DOVirtual.Float(0.3f, 0.75f, 3f, y => SetTextureOffset(y))
        });
    }

    void SetTextureOffset(float offsetY)
    {
        meshRend.material.SetTextureOffset("_MaskTex", new Vector2(0, offsetY));
    }
}
