using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dispensador : MonoBehaviour
{
    [System.Serializable]
    public class Surtidor
    {
        public string sodaName;
        public Transform transform;

        public TipoBebida bebida;
        [ColorUsageAttribute(true, true)]
        public Color FoamColor;
        [ColorUsageAttribute(true, true)]
        public Color SodaColor;
    
        public Color RimColor;

        public ParticleSystem particulasDispensador;

        bool canUse = true;

        public IEnumerator Dispensando()
        {
            canUse = false;
            yield return new WaitForSeconds(3f);
            canUse = true;

        }
        
        public bool surtidorDisponible()
        {
            return canUse;
        }
    }

    public List<Surtidor> surtidores;

   
    public Surtidor GetSurtidorByName(string sodaName)
    {
        foreach (Surtidor surtidor in surtidores)
        {
            if (surtidor.sodaName == sodaName)
            {
                return surtidor;
                break;
            }
        }
        Debug.Log("Dispensador no encontro soda con nombre " + sodaName);
        return null;
    }

    public void DispensarSoda(string SodaName)
    {
        Surtidor surtidor = GetSurtidorByName(SodaName);
        if (surtidor.surtidorDisponible())
        {
            StartCoroutine(surtidor.Dispensando());
            if (Physics.Raycast(surtidor.transform.position, surtidor.transform.forward, out RaycastHit hit))
            {
                surtidor.particulasDispensador.Play();

                if (hit.transform.CompareTag("Glass"))
                {
                    hit.transform.GetChild(0).gameObject.SetActive(true);
                    Liquid liquid = hit.transform.GetComponentInChildren<Liquid>();

                    liquid.CrearBebida(surtidor.bebida, surtidor.FoamColor, surtidor.SodaColor, surtidor.RimColor);

                }
                else Debug.Log(hit.transform.tag);
            }
        }
        
    }

}
