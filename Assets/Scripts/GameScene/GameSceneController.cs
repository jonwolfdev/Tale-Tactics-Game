using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var gameModel = Global.GetGameConfiguration();
        var game = Global.CurrentGameModel;
        if (gameModel == default || game == default)
        {
            SceneManager.LoadScene(Constants.MainSceneName);
            return;
        }

        StartCoroutine(JustWait());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator JustWait()
    {
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene(Constants.MainSceneName);
    }
}