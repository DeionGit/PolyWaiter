using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComandaSystem;

public class ImpresoraDeComandas : MonoBehaviour
{
    [SerializeField] GameObject papelComandaPrefab;
    [SerializeField] Transform spawnPosComanda;


    public static ImpresoraDeComandas instance;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }


    public void ImprimirPapelComanda(Comanda comanda)
    {
        GameObject papelComanda = Instantiate(papelComandaPrefab, transform.position, transform.rotation);

        PapelComanda papel = papelComanda.GetComponent<PapelComanda>();

    }
}
