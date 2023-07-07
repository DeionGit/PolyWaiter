using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class GameManager : MonoBehaviour
{
    Mesa[] mesasLocal;

    bool serviceOpen = false;

    [Header("Time To Spawn tables")]
    [SerializeField] float comandaSpawnTime = 12f;

    [SerializeField] TextMeshPro textScore;
    [SerializeField] string preScore;

    [SerializeField] TextMeshPro textFails;
    [SerializeField] string preFail;

    [SerializeField] GameObject LoseText;
    [SerializeField] GameObject tipText;
    int score = 0;
    int fails = 0;
    public static GameManager instance;
    private void Awake()
    {
        #region Singleton
        if (instance == null) instance = this;
        else Destroy(gameObject);
        #endregion

        mesasLocal = FindObjectsOfType<Mesa>();
        textFails.text = preFail + fails;
    }

    
    public void StartGame()
    {

        if(!serviceOpen) StartCoroutine(OcuparMesaAleatorio());

    }
    IEnumerator OcuparMesaAleatorio()
    {
        serviceOpen = true;
        int randomComanda = Random.Range(0, 7);
        Debug.Log("LO INTENTA");
        yield return new WaitForSeconds(comandaSpawnTime);
        
        if (randomComanda >= 5)
        {
            for (int i = 0; i < mesasLocal.Length; i++)
            {
                if (!mesasLocal[i].GetMesaOcupada())
                {
                    mesasLocal[i].CrearComandaParaLaMesa();
                    MoreDificult();
                    break;
                }
            }
        }

        StartCoroutine(OcuparMesaAleatorio());
    }
    public void StopGame()
    {
        StopAllCoroutines();
        LoseText.SetActive(true);
        tipText.SetActive(true);
    }
    public void MoreDificult()
    {
        if (comandaSpawnTime > 6.5f) comandaSpawnTime -= 0.07f;
    }
    public void Set1MoreScore()
    {
        score++;
        textScore.text = preScore + score;
    }

    public bool HasPerdido()
    {
        fails++;
        textFails.text = preFail + fails;
        if (fails == 3)
        {
            return true;
        }
        else return false;
    }
}
