using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ComandaSystem;
using Autohand;
public class PapelComanda : MonoBehaviour
{
    [SerializeField] string preNumeroMesa;
    [SerializeField] TextMeshPro numeroMesa;
    [SerializeField] string prebebida;
    [SerializeField] TextMeshPro[] bebidas;

    Grabbable grabbable;
    Rigidbody rb;
    Collider col;

    private void Awake()
    {
        grabbable = GetComponent<Grabbable>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public void SetComandaInfo(Comanda comanda)
    {
        comanda.SetPapelComanda(this);
        numeroMesa.text = preNumeroMesa + comanda.numeroDeMesa;

        for (int i = 0; i < bebidas.Length; i++)
        {
            if (i < comanda.comandaDeBebidas.Count)
            {
                if (comanda.comandaDeBebidas[i] != default)
                {
                    bebidas[i].text = prebebida + comanda.comandaDeBebidas[i].ToString();
                }
                
            }
            else
            {
                bebidas[i].text = "";
            }
        }
    }

    public void MakeItGrabbable(bool active)
    {
        grabbable.enabled = active;
        rb.isKinematic = !active;
        col.enabled = active;
    }

    public void TacharBebidaEntregada(int listPosition)
    {
        bebidas[listPosition].fontStyle = FontStyles.Strikethrough; 
    }
   
    
}
