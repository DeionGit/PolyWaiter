using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VasoSpawner : MonoBehaviour
{
    [SerializeField] GameObject vasoPref;

    bool canUse = true;
    void Start()
    {
       
    }
    public void SpawnPrefab()
    {
        if (canUse)
        {
            canUse = false;
            Instantiate(vasoPref, transform.position, transform.rotation, transform);

            Invoke("ResetCooldown", 1f);
        }
        
    }

    private void ResetCooldown()
    {
        canUse = true;
    }

}
