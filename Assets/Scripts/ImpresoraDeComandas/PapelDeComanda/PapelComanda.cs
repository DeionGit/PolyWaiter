using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ComandaSystem;
public class PapelComanda : MonoBehaviour
{
    [SerializeField] string preNumeroMesa;
    [SerializeField] TextMeshPro numeroMesa;
    [SerializeField] string prebebida;
    [SerializeField] TextMeshPro[] bebidas;


    
    public void SetComandaInfo(Comanda comanda)
    {
        comanda.SetPapelComanda(this);
        numeroMesa.text = preNumeroMesa + comanda.numeroDeMesa;

        for (int i = 0; i < comanda.comandaDeBebidas.Count; i++)
        {
            if (comanda.comandaDeBebidas[i] != default)
            {
                bebidas[i].text = prebebida + comanda.comandaDeBebidas[i].ToString();
            }else
            {
                bebidas[i].text = "";
            }

        }
    }

   

    public void TacharBebidaEntregada(int listPosition)
    {
        bebidas[listPosition].fontStyle = FontStyles.Strikethrough; 
    }
   
    
}
