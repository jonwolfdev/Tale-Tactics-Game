using Assets.Scripts;
using Assets.Scripts.Hub;
using Assets.Scripts.Models;
using Assets.Scripts.ServerModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneController : MonoBehaviour
{
    public CanvasGroup cgGameCanvas;

    public AudioSource sndWaiting;
    Coroutine crFadeInWaiting;

    bool receivedFirstHmCommand;

    /// <summary>
    /// This is for the global settings audio
    /// </summary>
    public AudioMixer audioMixer;
    /// <summary>
    /// This is for the dynamically created AudioSources
    /// </summary>
    public AudioMixerGroup masterVolume;

    public Canvas cnvWaiting;
    public Canvas cnvGame;
    public GameObject panelImage;
    bool isImage1Showing = true;
    Coroutine crImageFadeIn = default;
    Coroutine crImageFadeOut = default;
    long idImageShowing = 0;
    public Image imgImage1;
    public Image imgImage2;

    public GameObject panelTimer;
    public TMP_Text txtTimer;
    public Coroutine crTimer;
    public AudioSource sndTimerTick;
    public AudioSource sndTimerEnded;

    public GameObject panelText;
    /// <summary>
    /// This is the text to show from incoming HmCommand
    /// </summary>
    public TMP_Text txtText;
    public Image biggerTextBg;
    public Image NormalTextBg;

    public Canvas cnvError;
    public TMP_Text txtError;
    public Button btnCloseErrorCanvas;
    public Button btnReconnect;
    public TMP_Text txtWaitingText;
    
    GameCodeModel gameCodeModel;
    IHtHubConnection hub;

    CurrentGameModel currentGameModel;
    public AudioSource sndEffect;
    readonly Dictionary<long, AudioSource> allBgms = new();
    readonly List<AudioPlayingModel> fadeOutAudios = new();
    AudioPlayingModel currentBgm;

    long idAudioBgmPlaying = 0;

    /// <summary>
    /// This is to not show an error when it first connects because with JS it will be notified with event
    /// </summary>
    bool isFirstConnectedJs = true;

    bool quit;
    bool alreadySentQuit;

    readonly QueuedExecutionManager queuedExecutionManager = new();
    // Start is called before the first frame update
    async void Start()
    {
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Global.CurrentScene = Constants.GameSceneName;

        var gameModel = Global.GetGameConfiguration();
        currentGameModel = Global.CurrentGameModel;
        if (gameModel == default || currentGameModel == default)
        {
            SceneManager.LoadScene(Constants.MainSceneName);
            return;
        }

        gameCodeModel = new GameCodeModel(currentGameModel.GameCode);

        var settings = Global.GetCurrentUserSettings();
        Global.ApplyUserSettings(settings, audioMixer);
        UpdateGameTextFontSize(settings);

        Global.OnUserSettingsChanged += OnUserSettingsChanged;
        Global.OnExitGame += OnExitGame;

        // Load audiosource (fix webgl bug)
        foreach (var audio in currentGameModel.GetAllAudios())
        {
            if (audio.Value.Model.IsBgm)
            {
                var audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.loop = true;
                audioSource.clip = audio.Value.AudioClip;
                audioSource.outputAudioMixerGroup = masterVolume;
                allBgms.Add(audio.Key, audioSource);
            }
        }

        crFadeInWaiting = StartCoroutine(AudioHelper.FadeIn(sndWaiting, Constants.AudioFadeInTime));

        btnCloseErrorCanvas.onClick.AddListener(() =>
        {
            if (!btnReconnect.gameObject.activeInHierarchy)
            {
                cnvError.gameObject.SetActive(false);
            }
        });

        btnReconnect.onClick.AddListener(async () =>
        {
            btnReconnect.gameObject.SetActive(false);
            // only when "Disconnected" comes up, it will automatically disconnect
            txtWaitingText.text = "Trying to reconnect...";
            // reset so it can show it connected again for js
            isFirstConnectedJs = true;
            if (await hub.ConnectAsync())
            {
#if UNITY_EDITOR || PLATFORM_SUPPORTS_MONO
                txtWaitingText.text = "Connected to hub. Waiting for Game Moderator...";
                txtError.text = "Connected, you may now close this message";
                // for webgl it will be called through connection status event
#endif
            }
            else
            {
                txtWaitingText.text = "Error connecting to hub...";
            }
        });

#if UNITY_EDITOR || PLATFORM_SUPPORTS_MONO
        hub = new HtHubConnectionMono(gameCodeModel, queuedExecutionManager);
#elif UNITY_WEBGL
        hub = new HtHubConnectionWebGl(gameCodeModel);
#elif UNITY_ANDROID
        // NOT USED Android support Mono
        hub = new HtHubConnectionJava(gameCodeModel);
#endif
        // These do nothing for mono implementations
        hub.OnConnectionStatusChanged += Hub_OnConnectionStatusChanged;
        hub.OnHmCommand += Hub_OnHmCommand;
        hub.OnHmPredefinedCommand += Hub_OnHmPredefinedCommand;

        txtWaitingText.text = "Connecting to hub...";
        if (await hub.ConnectAsync())
        {
#if UNITY_EDITOR || PLATFORM_SUPPORTS_MONO
            txtWaitingText.text = "Connected to hub. Waiting for Game Moderator...";
            // for webgl it will be called through connection status event
#endif
        }
        else
        {
            txtWaitingText.text = "Error connecting to hub...";
        }

    }

    private void Hub_OnHmPredefinedCommand(object sender, HmCommandPredefinedModel e)
    {
        if (e == null)
        {
            Debug.LogError("e is null");
            return;
        }
        FirstReceviedHmCommand();

        // This is to stop the waiting audio if its the first command
        Handle_OnHmCommand_Audio(new HmCommandModel());

        if (e.StopSoundEffects.HasValue && e.StopSoundEffects.Value)
        {
            //TODO: welp.. can't stop play playoneshots

            if (crTimer != default)
                StopCoroutine(crTimer);

            if (sndTimerTick.isPlaying)
                sndTimerTick.Stop();
            if (sndTimerEnded.isPlaying)
                sndTimerEnded.Stop();

            panelTimer.SetActive(false);
        }

        if (e.ClearScreen.HasValue && e.ClearScreen.Value)
        {
            if (crImageFadeIn != default)
                StopCoroutine(crImageFadeIn);
            if (crImageFadeOut != default)
                StopCoroutine(crImageFadeOut);
            idImageShowing = 0;

            // ! because they are reversed
            if (!isImage1Showing)
            {
                if (imgImage2.color.a > 0)
                {
                    var img2color = imgImage2.color;
                    img2color.a = 0;
                    imgImage2.color = img2color;
                }

                if (imgImage1.color.a > 0)
                    crImageFadeOut = StartCoroutine(AudioHelper.FadeOutImage(imgImage1, Constants.ImageFadeOutTime));
            }
            else
            {
                if (imgImage1.color.a > 0)
                {
                    var img1color = imgImage1.color;
                    img1color.a = 0;
                    imgImage1.color = img1color;
                }

                if (imgImage2.color.a > 0)
                    crImageFadeOut = StartCoroutine(AudioHelper.FadeOutImage(imgImage2, Constants.ImageFadeOutTime));
            }

            txtText.text = string.Empty;
            panelText.SetActive(false);
        }

        if (e.StopBgm.HasValue && e.StopBgm.Value)
        {
            CleanFadeOuts();
            if (currentBgm != null)
            {
                // Only 1 should be playing, so only 1 to stop (if others are playing, they should be fading out)
                StopCoroutine(currentBgm.Coroutine);
                fadeOutAudios.Add(new AudioPlayingModel()
                {
                    Id = currentBgm.Id,
                    AudioSource = currentBgm.AudioSource,
                    Coroutine = StartCoroutine(AudioHelper.FadeOut(currentBgm.AudioSource, Constants.AudioFadeOutTime))
                });
                currentBgm = null;
            }
            idAudioBgmPlaying = 0;
        }

        // this has to be here
        receivedFirstHmCommand = true;
    }

    private void CleanFadeOuts(long id = -1)
    {
        var index = 0;
        while (index < fadeOutAudios.Count)
        {
            if (id > -1 && fadeOutAudios[index].Id == id)
            {
                Debug.Log("Removed audiosource clean up FORCED");
                StopCoroutine(fadeOutAudios[index].Coroutine);
                fadeOutAudios.RemoveAt(index);
            }
            else if (!fadeOutAudios[index].AudioSource.isPlaying)
            {
                Debug.Log("Removed audiosource clean up");
                fadeOutAudios.RemoveAt(index);
            }
            else
            {
                index++;
            }
        }
    }

    private void FirstReceviedHmCommand()
    {
        if (!receivedFirstHmCommand)
        {
            StartCoroutine(AudioHelper.FadeOutCanvasGroup(cgGameCanvas, Constants.ImageFadeOutTime, cnvWaiting));

            Global.CanvasChangedForOptionsBtn(cnvGame);
            cnvGame.gameObject.SetActive(true);
            // panel image will be always active
            panelImage.SetActive(true);

            // fadeout waiting sound
            StopCoroutine(crFadeInWaiting);
            StartCoroutine(AudioHelper.FadeOut(sndWaiting, Constants.AudioFadeOutTime));
            crFadeInWaiting = default;
        }
    }

    private void Hub_OnHmCommand(object sender, HmCommandModel e)
    {
        if (e == null)
        {
            Debug.LogError("e is null");
            return;
        }

        FirstReceviedHmCommand();

        Handle_OnHmCommand_Timer(e);
        Handle_OnHmCommand_Audio(e);
        Handle_OnHmCommand_Image(e);
        Handle_OnHmCommand_Text(e);

        // this has to be here
        receivedFirstHmCommand = true;
    }
    void Handle_OnHmCommand_Audio(HmCommandModel e)
    {
        bool gotBgm = false;
        if (e.AudioIds != default && e.AudioIds.Count > 0)
        {
            foreach (var id in e.AudioIds)
            {
                var audio = currentGameModel.GetAudio(id);
                if (audio != default)
                {
                    
                    if (!audio.Model.IsBgm)
                    {
                        sndEffect.PlayOneShot(audio.AudioClip);
                        Debug.Log("Playing sound effect: " + audio.Model.Name);
                    }
                    else if (!gotBgm)
                    {
                        gotBgm = true;
                        if (idAudioBgmPlaying == id)
                            continue;

                        idAudioBgmPlaying = id;

                        var audioSource = allBgms[id];
                        // if the same id is playing (fadein or normal), this won't happen
                        // see condition above
                        if (audioSource.isPlaying)
                            CleanFadeOuts(id);
                        else
                            CleanFadeOuts();

                        if (currentBgm != default)
                        {
                            StopCoroutine(currentBgm.Coroutine);
                            fadeOutAudios.Add(new AudioPlayingModel()
                            {
                                Id = currentBgm.Id,
                                AudioSource = currentBgm.AudioSource,
                                Coroutine = StartCoroutine(AudioHelper.FadeOut(currentBgm.AudioSource, Constants.AudioFadeOutTime))
                            });
                            currentBgm = null;
                        }

                        currentBgm = new AudioPlayingModel()
                        {
                            Id = id,
                            AudioSource = audioSource,
                            Coroutine = StartCoroutine(AudioHelper.FadeIn(audioSource, Constants.AudioFadeOutTime))
                        };
                    }
                }
            }
        }
    }
    void Handle_OnHmCommand_Image(HmCommandModel e)
    {
        if (e.ImageId.HasValue && e.ImageId.Value > 0)
        {
            var image = currentGameModel.GetImage(e.ImageId.Value);
            if (image != default)
            {
                if(idImageShowing != e.ImageId)
                {
                    idImageShowing = e.ImageId.Value;
                    if (isImage1Showing)
                    {
                        isImage1Showing = false;
                        if (crImageFadeIn != default)
                            StopCoroutine(crImageFadeIn);
                        if (crImageFadeOut != default)
                            StopCoroutine(crImageFadeOut);

                        if (imgImage2.color.a > 0)
                            crImageFadeOut = StartCoroutine(AudioHelper.FadeOutImage(imgImage2, Constants.ImageFadeOutTime));

                        imgImage1.sprite = image.Sprite;
                        crImageFadeIn = StartCoroutine(AudioHelper.FadeInImage(imgImage1, Constants.ImageFadeInTime));
                    }
                    else
                    {
                        isImage1Showing = true;

                        if (crImageFadeIn != default)
                            StopCoroutine(crImageFadeIn);

                        if (crImageFadeOut != default)
                            StopCoroutine(crImageFadeOut);

                        if (imgImage1.color.a > 0)
                            crImageFadeOut = StartCoroutine(AudioHelper.FadeOutImage(imgImage1, Constants.ImageFadeOutTime));

                        imgImage2.sprite = image.Sprite;
                        crImageFadeIn = StartCoroutine(AudioHelper.FadeInImage(imgImage2, Constants.ImageFadeInTime));
                    }
                }
            }
        }
    }
    void Handle_OnHmCommand_Timer(HmCommandModel e)
    {
        if (e.Timer.HasValue && e.Timer.Value > 0)
        {
            panelTimer.SetActive(true);
            if (crTimer != default)
                StopCoroutine(crTimer);

            crTimer = StartCoroutine(StartTimer(e.Timer.Value));
        }
    }

    IEnumerator StartTimer(long timer)
    {
        sndTimerTick.Play();
        txtTimer.text = $"{timer}s";
        while (timer > 0)
        {
            yield return new WaitForSeconds(1);
            timer--;
            txtTimer.text = $"{timer}s";
        }
        sndTimerTick.Stop();

        sndTimerEnded.Play();
        while (timer > -4)
        {
            timer--;
            yield return new WaitForSeconds(1);
        }
        panelTimer.SetActive(false);
    }

    void Handle_OnHmCommand_Text(HmCommandModel e)
    {
        if (!string.IsNullOrEmpty(e.Text))
        {
            panelText.SetActive(true);
            txtText.text = e.Text;
        }
        else
        {
            if (e.ImageId.HasValue)
            {
                txtText.text = string.Empty;
                panelText.SetActive(false);
            }
        }
    }
    private void Hub_OnConnectionStatusChanged(object sender, HubConnectionStatusEventArgs e)
    {
        string ex = string.Empty;
        if (e.Exception != default)
            ex = $"({e.Exception.Message})";

        Debug.Log($"Connection status changed: {e}");

        if (isFirstConnectedJs && e.ConnectedByJs)
        {
            isFirstConnectedJs = false;
            txtWaitingText.text = "Connected to hub. Waiting for Game Moderator...";
            cnvError.gameObject.SetActive(false);
            return;
        }

        if (e.Reconnected)
        {
            cnvError.gameObject.SetActive(false);
            return;
        }

        cnvError.gameObject.SetActive(true);
        btnReconnect.gameObject.SetActive(false);

        if (e.IsReconnecting)
        {
            txtError.text = "Reconnecting...";
        }
        else if (e.Disconnected)
        {
            // Have to call Connect again
            txtError.text = "Disconnected from hub. If problem persists, exit game and enter game again." + ex;
            btnReconnect.gameObject.SetActive(true);
        }
        else if (e.InvokeFailed)
        {
            txtError.text = "Failed to call hub invoke. If problem persists, exit game and enter game again." + ex;
        }
        else if (e.FailedToConnect)
        {
            // Have to call Connect again
            txtError.text = "Not able to connect to hub. If problem persists, exit game and enter game again." + ex;
            btnReconnect.gameObject.SetActive(true);
        }
    }

    async void OnDestroy()
    {
        foreach (var audioSources in allBgms)
        {
            audioSources.Value.Stop();
            audioSources.Value.clip = null;
            Destroy(audioSources.Value);
        }

        if (hub != default)
        {
            hub.OnConnectionStatusChanged -= Hub_OnConnectionStatusChanged;
            hub.OnHmCommand -= Hub_OnHmCommand;
            hub.OnHmPredefinedCommand -= Hub_OnHmPredefinedCommand;

            // unsubscribe because this will call `OnConnectionStatusChanged` with connection closed
            await hub.DisposeAsync();
        }

        Global.OnUserSettingsChanged -= OnUserSettingsChanged;
        Global.OnExitGame -= OnExitGame;
        Debug.Log("MenuController:OnDestroy");
    }

    void OnExitGame(object sender, EventArgs args)
    {
        quit = true;
    }

    void OnUserSettingsChanged(object sender, UserSettingsEventArgs args)
    {
        Global.ApplyUserSettings(args, audioMixer);
        UpdateGameTextFontSize(args);
    }

    void UpdateGameTextFontSize(UserSettingsEventArgs settings)
    {
        if (!settings.BiggerGameText.HasValue)
            return;

        if (settings.BiggerGameText == Constants.BiggerGameTextValue)
        {
            txtText.fontSize = Constants.BigGameTextSize;
            biggerTextBg.gameObject.SetActive(true);
            NormalTextBg.gameObject.SetActive(false);
        }
        else
        {
            txtText.fontSize = Constants.NormalGameTextSize;
            biggerTextBg.gameObject.SetActive(false);
            NormalTextBg.gameObject.SetActive(true);
        }
    }

    // Update is called once per frame
    async void Update()
    {
        if (quit)
        {
            if (!alreadySentQuit)
            {
                alreadySentQuit = true;

                // TODO: if hub is not connected, do not send
                //await hub.PlayerSendLog(new TextLogModel()
                //{
                //    From = "A player",
                //    Message = "Has left the game"
                //});
                hub.OnConnectionStatusChanged -= Hub_OnConnectionStatusChanged;
                hub.OnHmCommand -= Hub_OnHmCommand;
                hub.OnHmPredefinedCommand -= Hub_OnHmPredefinedCommand;
                await hub.StopAsync();
                Global.OkExitGame();
            }
            return;
        }
#if UNITY_EDITOR || PLATFORM_SUPPORTS_MONO
        else
        {
            var actions = queuedExecutionManager.Dequeue();
            if (actions != null)
            {
                foreach (var action in actions)
                {
                    switch (action.Item1)
                    {
                        case QueuedAction.PlayerReceiveHmCommand:
                            Hub_OnHmCommand(null, action.Item2 as HmCommandModel);
                            break;
                        case QueuedAction.PlayerReceiveHmCommandPredefined:
                            Hub_OnHmPredefinedCommand(null, action.Item2 as HmCommandPredefinedModel);
                            break;
                        case QueuedAction.OnConnectionStatusChanged:
                            Hub_OnConnectionStatusChanged(null, action.Item2 as HubConnectionStatusEventArgs);
                            break;
                        default:
                            Debug.LogError("Bug. Unkown queuedaction: " + action.Item1);
                            break;
                    }
                }
            }
        }
#endif
    }

}
