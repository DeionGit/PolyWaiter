using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuFunctions : MonoBehaviour
{
    public void StartGame()
    {
        GameManager.instance.StartGame();
    }
    public void QuitApp()
    {
        Application.Quit();
    }
    public void ReStartScene()
    {
        SceneManager.LoadScene(0);
    }
}
