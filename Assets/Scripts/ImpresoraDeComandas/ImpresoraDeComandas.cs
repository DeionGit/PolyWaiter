using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ComandaSystem;
using DG.Tweening;

public class ImpresoraDeComandas : MonoBehaviour
{
    [SerializeField] GameObject papelComandaPrefab;
    [SerializeField] Transform spawnPosComanda;

    [Header("Tween Variables")]
    [SerializeField] float move;
    [SerializeField] Ease tween;


    public static ImpresoraDeComandas instance;
    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void ImprimirPapelComanda(Comanda comanda)
    {
        GameObject papelComanda = Instantiate(papelComandaPrefab, spawnPosComanda.position,spawnPosComanda.rotation, spawnPosComanda);

        PapelComanda papel = papelComanda.GetComponent<PapelComanda>();

        papel.SetComandaInfo(comanda);

        papelComanda.transform.DOLocalMoveY(move, 0.6f).SetEase(tween).OnComplete(() =>
         {
             papel.MakeItGrabbable(true);

         });
    }
}
