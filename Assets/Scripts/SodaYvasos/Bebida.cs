using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bebida : MonoBehaviour
{
    [SerializeField] KeyCode testingKey;


    [SerializeField] float rayToPos = 0.1f;
    [SerializeField] LayerMask LayersToIgnore;

    bool colocadoEnBandeja = false;
    Collider vasoCollider;
    Collider bandejaCollider;

    [Header("Liquido del vaso")]
    public Liquid InsideLiquid;
    public GameObject LiquidObject;
    void Start()
    {
        #region Referencias de liquido y colliders
        vasoCollider = GetComponent<Collider>();
        LiquidObject = transform.GetChild(0).gameObject;
        InsideLiquid = GetComponentInChildren<Liquid>();
        #endregion 


    }

    // Update is called once per frame
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
        if (Physics.Raycast(transform.position, -transform.up, rayToPos, LayersToIgnore))
        {
            return true;
        }
        else return false;

    }
    #endregion


    private void OnDrawGizmos()
    {
        Vector3 to = new Vector3(0, rayToPos, 0);

    }

    

}
public enum TipoBebida
{
    Cerveza,
    SodaAzul,
    SodaVerde,
    SodaRoja,
    SodaMorada
}
