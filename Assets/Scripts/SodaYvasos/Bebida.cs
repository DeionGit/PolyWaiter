using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bebida : MonoBehaviour
{
    [SerializeField] KeyCode testingKey;


    [SerializeField] float rayToPos = 0.1f;
    [SerializeField] LayerMask LayersToDetect;

    bool colocadoEnBandeja = false;
    Collider vasoCollider;
    Collider bandejaCollider;

    [Header("Liquido del vaso")]
    public Liquid InsideLiquid;
    public GameObject LiquidObject;

    TipoBebida tipoBebida;
    void Start()
    {
        #region Referencias de liquido y colliders
        vasoCollider = GetComponent<Collider>();
        LiquidObject = transform.GetChild(0).gameObject;
        InsideLiquid = GetComponentInChildren<Liquid>();
        #endregion 


    }


    void Update()
    {

    }

    #region Features futuras NO EN LA BETA, APARTAR
    public void ColocarEnbandeja()
    {
        if (HayBandeja())
        {
            Physics.Raycast(transform.position, -transform.up, out RaycastHit hit);
            bandejaCollider = hit.collider;
            Physics.IgnoreCollision(vasoCollider, hit.collider, true);
            colocadoEnBandeja = true;
            transform.position = hit.point;
            transform.parent = hit.transform;
        }
    }

    public void QuitarDeBandeja()
    {
        Physics.IgnoreCollision(vasoCollider, bandejaCollider, false);
        colocadoEnBandeja = false;
    }
    bool HayBandeja()
    {
        if (Physics.Raycast(transform.position, -transform.up, rayToPos, LayersToDetect))
        {
            return true;
        }
        else return false;

    }
    #endregion


    private void OnDrawGizmos()
    {

    }

    public void SetTipoBebida(TipoBebida bebida)
    {
        tipoBebida = bebida;
    }

    TipoBebida GetTipoBebida()
    {
        return tipoBebida;
    }

}
public enum TipoBebida
{
    SodaGreen = 0,
    SodaBlue = 1,
    SodaRed = 2,
    SodaPruple = 4,
    SodaCian = 5,
    SodaOrange = 6
}
