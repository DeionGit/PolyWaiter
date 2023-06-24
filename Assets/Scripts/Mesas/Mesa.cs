using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mesa : MonoBehaviour
{
    bool ocupada = false;

    [SerializeField] Transform entradaMesa1;
    [SerializeField] Transform entradaMesa2;
    
    void Start()
    {
        
    }
    public bool GetMesaOcupada()
    {
        return ocupada;
    }
    public void SetMesaOcupada(bool estaOcupada)
    {
        ocupada = estaOcupada;
    }

}
