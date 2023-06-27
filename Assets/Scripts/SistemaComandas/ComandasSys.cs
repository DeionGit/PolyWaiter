using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComandasSys : MonoBehaviour
{
    [System.Serializable]
    public class Comanda 
    {
        
        public List<TipoBebida> comandaDeBebidas = new List<TipoBebida>();
        
        int numeroMesa = Random.Range(1, 7);
        public Comanda()
        {
            comandaDeBebidas.Capacity = Random.Range(1, 5);

            for (int i = 0; i < comandaDeBebidas.Capacity; i++)
            {
                int randomBebida = Random.Range(0, 7);
                comandaDeBebidas.Add((TipoBebida)randomBebida);
                Debug.Log("Cliente " + i + " beberá una " + comandaDeBebidas[i]);
            }
        }    
    }

    public List<Comanda> ListaDeComandas = new List<Comanda>();

    void Start()
    {
        
    }

    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ListaDeComandas.Add(new Comanda());
            Debug.Log("Hay " + ListaDeComandas.Count + " comandas");
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            foreach (TipoBebida bebida in ListaDeComandas[1].comandaDeBebidas)
            {
                Debug.Log(bebida.ToString());
            }
        }
    }
}
