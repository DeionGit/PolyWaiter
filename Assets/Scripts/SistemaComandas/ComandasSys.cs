using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComandaSystem
{
    public class Comanda
    {

        public List<TipoBebida> comandaDeBebidas = new List<TipoBebida>();

        public int numeroDeMesa;
        public Comanda()
        {
            comandaDeBebidas.Capacity = Random.Range(1, 5);

            for (int i = 0; i < comandaDeBebidas.Capacity; i++)
            {
                int randomBebida = Random.Range(0, 6);
                comandaDeBebidas.Add((TipoBebida)randomBebida);
                Debug.Log("Cliente " + i + " beberá una " + comandaDeBebidas[i]);
            }
        }

        public void BebidaEntregada(TipoBebida tipoBebida)
        {
            comandaDeBebidas.Remove(tipoBebida);
        }
        
    }
    public class ComandasSys : MonoBehaviour
    {    

        public List<Comanda> ListaDeComandas = new List<Comanda>();

        static public ComandasSys instance;
        private void Awake()
        {
            #region Singleton
            if (instance == null) instance = this;
            else Destroy(this);
            #endregion

        }
        void Start()
        {

        }
        public void AddComandaToList(Comanda comanda)
        {
            ListaDeComandas.Add(comanda);
        }
        public void FinishComandaToList(Comanda comanda)
        {
            ListaDeComandas.Remove(comanda);
            GameManager.instance.Set1MoreScore();
        }
        public Comanda GetComanda(int numeroMesa)
        {
            foreach (Comanda com in ListaDeComandas)
            {
                if (com.numeroDeMesa == numeroMesa)
                {
                    return com;
                    break;
                }
            }
            return null;
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

}
