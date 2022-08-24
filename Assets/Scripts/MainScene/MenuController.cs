using Assets.Scripts;
using Assets.Scripts.Models;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public Button btnStartGame;
    public TMP_InputField txtGameCode;
    public TMP_Text txtError;

    // Start is called before the first frame update
    void Start()
    {
        btnStartGame.onClick.AddListener(EnterGame_Click);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EnterGame_Click()
    {
        StartCoroutine(GetStoryModel(txtGameCode.text.Trim()));
    }

    IEnumerator GetStoryModel(string gameCode)
    {
        txtError.text = "Loading game configuration...";
        using var www = UnityWebRequest.Get(Constants.GetGameConfigurationUrl(gameCode));
        yield return www.SendWebRequest();

        if (www.responseCode == 404)
        {
            txtError.text = $"Game code does not exist. Verify the code is valid (case sensitive) [{gameCode}]";
        }
        else if (www.responseCode >= 500 && www.responseCode <= 599)
        {
            txtError.text = "Server error. Returned response code: " + www.responseCode;
            Debug.LogError("Server error: " + www.downloadHandler.text);
        }
        else if(www.responseCode == 200)
        {
            var json = www.downloadHandler.text;
            Debug.Log("Server returned: " + json);
            var model = JsonConvert.DeserializeObject<ReadGameConfiguration>(json);
            txtError.text = "OK. Got game configuration";
        }
        else
        {
            txtError.text = "Unkown response code: " + www.responseCode;
        }
    }
}
