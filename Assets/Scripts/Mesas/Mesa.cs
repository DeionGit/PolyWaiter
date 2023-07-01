using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComandaSystem;

public class Mesa : MonoBehaviour
{

    Comanda comandaMesa;
    [SerializeField] GameObject recieverObject;
    [SerializeField] int numeroMesa;

    [SerializeField] GameObject visualNPCs;
    private void Awake()
    {
       

    }
    void Start()
    {
        
    }
    public void CrearComandaParaLaMesa()
    {
        comandaMesa = new Comanda();
        comandaMesa.numeroDeMesa = numeroMesa;
        ComandasSys.instance.AddComandaToList(comandaMesa);

        SetMesaVisual(true);
        SodaPanel.instance.SetMesaVisualComanda(comandaMesa, true);
    }
    public void CerrarComandaDeLaMesa()
    {
        ComandasSys.instance.FinishComandaToList(comandaMesa);
        SodaPanel.instance.SetMesaVisualComanda(comandaMesa, false);

        GameManager.instance.MoreDificult();

        comandaMesa = null;
        SetMesaVisual(false);

        Debug.Log("Mesa Cerrada");
    }
    public bool BebidaCorrectaEntregada(TipoBebida tipoBebida)
    {
        for (int i = 0; i < comandaMesa.comandaDeBebidas.Count; i++)
        {
            if (tipoBebida == comandaMesa.comandaDeBebidas[i])
            {
                Debug.Log("BEBIDA ENTREGADA");
                comandaMesa.BebidaEntregada(comandaMesa.comandaDeBebidas[i]);
                if (ComandaTerminada()) CerrarComandaDeLaMesa();
                return true;
                break;
            }
        }
        return false;
    }
    bool ComandaTerminada()
    {
        int num = 0;
        foreach (TipoBebida bebida in comandaMesa.comandaDeBebidas)
        {
            if (bebida == default) num++;

            
        }
        if (num == comandaMesa.comandaDeBebidas.Count)
        {
            return true;
        }
        else return false;

    }
    public bool GetMesaOcupada()
    {
        if (ComandasSys.instance.GetComanda(numeroMesa)!= null)
        {
            return true;
        }else
        {
            return false;
        }

    }

    void SetMesaVisual(bool open)
    {
        if (open)
        {
            recieverObject.SetActive(true);
            visualNPCs.SetActive(true);
        }
        else
        {
            recieverObject.SetActive(false);
            visualNPCs.SetActive(false);
        }

    }
    
}
