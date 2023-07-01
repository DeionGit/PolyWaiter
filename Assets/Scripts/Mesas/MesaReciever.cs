using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MesaReciever : MonoBehaviour
{
    [SerializeField] Mesa mesa;


    private void OnTriggerEnter(Collider other)
    {
        if (mesa.GetMesaOcupada())
        {
            if (other.CompareTag("Glass"))
            {
                Bebida bebida = other.GetComponent<Bebida>();
                if (!mesa.BebidaCorrectaEntregada(bebida.GetTipoBebida())) JefeCabreado();
                Destroy(other.gameObject);
               
            }
        }
    }

    void JefeCabreado()
    {
        if (GameManager.instance.HasPerdido())
        {
            GameManager.instance.StopGame();
            mesa.CerrarComandaDeLaMesa();
        }
    }
}
