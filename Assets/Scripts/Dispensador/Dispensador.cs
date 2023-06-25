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

        public Color FoamColor;
        public Color SodaColor;
    }

    public List<Surtidor> surtidores;


    [SerializeField] LayerMask layerToDetect;
    void Start()
    {
        
    }

    public Surtidor GetSurtidor(string sodaName)
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
        Surtidor surtidor = GetSurtidor(SodaName);
        
        if (Physics.Raycast(surtidor.transform.position, surtidor.transform.forward, out RaycastHit hit, layerToDetect))
        {
         
         
         
        }
    }
    void Update()
    {
        
    }
}
