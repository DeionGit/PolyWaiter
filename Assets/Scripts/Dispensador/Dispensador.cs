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
        public Color FoamColor;
        public Color SodaColor;
        public Color RimColor;
    }

    public List<Surtidor> surtidores;


    [SerializeField] LayerMask layerToDetect;
    void Start()
    {
        
    }

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
        
        if (Physics.Raycast(surtidor.transform.position, surtidor.transform.forward, out RaycastHit hit, layerToDetect))
        {
            // AQUI VAN LAS PARTICULAS!!! 
          if (hit.transform.CompareTag("Liquid"))
          {
                Liquid liquid = hit.transform.GetComponent<Liquid>();

                Material liquidMat = hit.transform.GetComponent<MeshRenderer>().material;

                //AQUI VA UNA FUNCION QUE RECOJA ESTAS VARIABLES Y CREE EL REFRESCO
                
          }
        }
    }

    void Update()
    {
        
    }
}
