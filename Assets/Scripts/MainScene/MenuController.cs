using Assets.Scripts;
using Assets.Scripts.Hub;
using Assets.Scripts.Models;
using Assets.Scripts.ServerModels;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public Button btnStartGame;
    public TMP_InputField txtGameCode;
    public TMP_Text txtError;
    public TMP_Text txtMobileNotice;

    public Canvas mainCanvas;
    public Canvas creditsCanvas;
    public Canvas instructionsCanvas;
    public Button btnGoBackMain;
    public Button btnGoBackMainFromInstructions;
    public Button btnGoCredits;
    public Button btnInstructions;
    public Button btnExit;

    IJsCodeHelper jsCodeHelper;

    // Start is called before the first frame update
    void Start()
    {
        if (Constants.IsDebug)
        {
            txtGameCode.text = "4d30a";
        }

        Screen.sleepTimeout = SleepTimeout.SystemSetting;

        Global.CurrentScene = Constants.MainSceneName;
        creditsCanvas.enabled = false;
        creditsCanvas.gameObject.SetActive(false);
        instructionsCanvas.enabled = false;
        instructionsCanvas.gameObject.SetActive(false);

        var model = Global.GetGameConfiguration();
        if (model != default)
        {
            // set "current" one
            txtGameCode.text = model.GameCode;
        }

        btnStartGame.onClick.AddListener(EnterGame_Click);
        btnGoCredits.onClick.AddListener(GoCredits_Click);
        btnGoBackMain.onClick.AddListener(GoBackMain_Click);
        btnGoBackMainFromInstructions.onClick.AddListener(GoBackMainFromInstructions_Click);
        btnInstructions.onClick.AddListener(BtnInstructions_Click);

        var settings = Global.GetCurrentUserSettings();
        Global.ApplyUserSettings(settings);

        Global.OnUserSettingsChanged += OnUserSettingsChanged;

#if UNITY_EDITOR || PLATFORM_SUPPORTS_MONO || UNITY_ANDROID
        jsCodeHelper = new JsCodeHelperMono();

        btnExit.onClick.AddListener(BtnExit_Click);

#if UNITY_ANDROID
        btnExit.enabled = false;
        btnExit.gameObject.SetActive(false);
#else
        txtMobileNotice.enabled = false;
        txtMobileNotice.gameObject.SetActive(false);
#endif
#elif UNITY_WEBGL
        btnExit.enabled = false;
        btnExit.gameObject.SetActive(false);

        jsCodeHelper = new JsCodeHelper((str) =>
        {
            txtGameCode.text = str.Trim();
        });

        // This will call the js function then through JsCodeHelper `Action` it will be return the code
        // . and be set to txtGameCode
        jsCodeHelper.GetCode();
#else
        Debug.LogError("What is this?");
        jsCodeHelper = new JsCodeHelperMono();
#endif
    }

    void OnDestroy()
    {
        jsCodeHelper.Dispose();

        Global.OnUserSettingsChanged -= OnUserSettingsChanged;
        Debug.Log("MenuController:OnDestroy");
    }

    void OnUserSettingsChanged(object sender, UserSettingsEventArgs args)
    {
        Global.ApplyUserSettings(args);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void BtnInstructions_Click()
    {
        instructionsCanvas.enabled = true;
        instructionsCanvas.gameObject.SetActive(true);
    }

    public void BtnExit_Click()
    {
        Application.Quit();
    }

    public void EnterGame_Click()
    {
        btnStartGame.interactable = false;
        btnInstructions.interactable = false;
        StartCoroutine(GetStoryModel(txtGameCode.text.Trim()));
    }

    public void GoCredits_Click()
    {
        creditsCanvas.enabled = true;
        creditsCanvas.gameObject.SetActive(true);
    }

    public void GoBackMain_Click()
    {
        Debug.Log("Credits: Go back main");
        creditsCanvas.enabled = false;
        creditsCanvas.gameObject.SetActive(false);
    }
    public void GoBackMainFromInstructions_Click()
    {
        Debug.Log("Instructions: Go back main");
        instructionsCanvas.enabled = false;
        instructionsCanvas.gameObject.SetActive(false);
    }
    IEnumerator GetStoryModel(string gameCode)
    {
        txtError.text = "Loading game configuration...";

        var lastGame = Global.CurrentGameModel;
        if (lastGame != default)
        {
            if (lastGame.GameCode == gameCode)
            {
                var lastConfig = Global.GetGameConfiguration();
                if (lastConfig == default)
                {
                    throw new Exception("lastConfig is null. This is a bug");
                }

                // Load from cache
                SceneManager.LoadScene(Constants.LoadingSceneName);
                yield break;

                throw new Exception("yieldbreak didn't work. This is a bug");
            }
        }

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
        else if (www.responseCode == 200)
        {
            var json = www.downloadHandler.text;
            Debug.Log("Server returned: " + json);

            try
            {
                var model = JsonConvert.DeserializeObject<ReadGameConfigurationModel>(json);
                if (model == default)
                    throw new InvalidOperationException("Deserialize object returned null");

                Global.SetGameConfiguration(new CurrentGameConfigurationModel(gameCode, model));
            }
            catch (Exception ex)
            {
                txtError.text = "Exception happened: " + ex.Message;
                throw;
            }
            
            txtError.text = "OK. Got game configuration";

            SceneManager.LoadScene(Constants.LoadingSceneName);
        }
        else if (www.result != UnityWebRequest.Result.Success)
        {
            txtError.text = "Web request was not successful: " + www.error;
        }
        else
        {
            txtError.text = "Unkown response code: " + www.responseCode + " Error: " + www.error;
        }

        btnStartGame.interactable = true;
        btnInstructions.interactable = true;
    }
}
