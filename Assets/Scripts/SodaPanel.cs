using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComandaSystem;

public class SodaPanel : MonoBehaviour
{
    [SerializeField] Material[] comandaMaterials;
    [SerializeField] GameObject[] comandasMesa1;
    [SerializeField] GameObject[] comandasMesa2;
    [SerializeField] GameObject[] comandasMesa3;
    [SerializeField] GameObject[] comandasMesa4;
    [SerializeField] GameObject[] comandasMesa5;
    [SerializeField] GameObject[] comandasMesa6;

    public static SodaPanel instance;
    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(this);  
    }

    
    void Update()
    {
        
        
    }

    Material GetComandaMaterial(TipoBebida bebida)
    {
        switch (bebida)
        {
            case TipoBebida.SodaBlue:
                return comandaMaterials[0];
                break;
            case TipoBebida.SodaRed:
                return comandaMaterials[1];
                break;
            case TipoBebida.SodaCian:
                return comandaMaterials[2];
                break;
            case TipoBebida.SodaOrange:
                return comandaMaterials[3];
                break;
            case TipoBebida.SodaPruple:
                return comandaMaterials[4];
                break;
            case TipoBebida.SodaGreen:
                return comandaMaterials[5];
                break;

        }
        return null;
    }

    public void SetMesaVisualComanda(Comanda comanda, bool open)
    {
        switch (comanda.numeroDeMesa)
        {
            case 1:
                if (open) SetVisualComanda(comanda.comandaDeBebidas, comandasMesa1);
                else CloseVisualComanda(comandasMesa1);
                break;
            case 2:
                if (open) SetVisualComanda(comanda.comandaDeBebidas, comandasMesa2);
                 else CloseVisualComanda(comandasMesa2);
                break;
            case 3:
                if (open) SetVisualComanda(comanda.comandaDeBebidas, comandasMesa3);
                 else CloseVisualComanda(comandasMesa3);
                break;
            case 4:
                if (open) SetVisualComanda(comanda.comandaDeBebidas, comandasMesa4);
                else CloseVisualComanda(comandasMesa4);
                break;
            case 5:
                if (open) SetVisualComanda(comanda.comandaDeBebidas, comandasMesa5);
                else CloseVisualComanda(comandasMesa5);
                break;
            case 6:
                if (open) SetVisualComanda(comanda.comandaDeBebidas, comandasMesa6);
                else CloseVisualComanda(comandasMesa6);
                break;
        }
    }
    void SetVisualComanda(List<TipoBebida> comandaBebidas, GameObject[] comandaUI)
    {
        for (int i = 0; i < comandaBebidas.Count; i++)
        {
            comandaUI[i].SetActive(true);
            comandaUI[i].GetComponent<MeshRenderer>().material = GetComandaMaterial(comandaBebidas[i]);
        }
    }

    void CloseVisualComanda(GameObject[] comandaUI)
    {
        foreach (GameObject sodaUI in comandaUI)
        {
            sodaUI.SetActive(false);
        }
    }
}
