using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    private static string PrefIpAddress = "PrefIpAddress";
    private static string PrefPort = "PrefIpPort";

    [SerializeField]
    private Animator m_menuAnimator;

    [SerializeField]
    private CharacterDataSO[] m_characterDatas;

    [SerializeField]
    private AudioClip m_confirmClip;

    private bool m_pressAnyKeyActive = true;
    private const string k_enterMenuTriggerAnim = "enter_menu";

    [SerializeField]
    private SceneName nextScene = SceneName.CharacterSelection;

    private IEnumerator Start()
    {
        // -- To test with latency on development builds --
        // To set the latency, jitter and packet-loss percentage values for develop builds we need
        // the following code to execute before NetworkManager attempts to connect (changing the
        // values of the parameters as desired).
        //
        // If you'd like to test without the simulated latency, just set all parameters below to zero(0).
        //
        // More information here:
        // https://docs-multiplayer.unity3d.com/netcode/current/tutorials/testing/testing_with_artificial_conditions#debug-builds
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
        NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().
            SetDebugSimulatorParameters(
                packetDelay: 50,
                packetJitter: 5,
                dropRate: 3);
#endif

        ClearAllCharacterData();

        // Wait for the network Scene Manager to start
        yield return new WaitUntil(() => NetworkManager.Singleton.SceneManager != null);
        TryPlayerPrefs();

        // Set the events on the loading manager
        // Doing this because every time the network session ends the loading manager stops
        // detecting the events
        LoadingSceneManager.Instance.Init();
    }

    private void Update()
    {
        if (m_pressAnyKeyActive)
        {
            if (Input.anyKey)
            {
                TriggerMainMenuTransitionAnimation();

                m_pressAnyKeyActive = false;
            }
        }
    }

    public void OnClickHost()
    {
        NetworkManager.Singleton.StartHost();
        AudioManager.Instance.PlaySoundEffect(m_confirmClip);
        LoadingSceneManager.Instance.LoadScene(nextScene);
    }

    public void OnClickJoin()
    {
        AudioManager.Instance.PlaySoundEffect(m_confirmClip);
        StartCoroutine(Join());
    }

    public void OnClickQuit()
    {
        AudioManager.Instance.PlaySoundEffect(m_confirmClip);
        Application.Quit();
    }

    private void ClearAllCharacterData()
    {
        // Clean the all the data of the characters so we can start with a clean slate
        foreach (CharacterDataSO data in m_characterDatas)
        {
            data.EmptyData();
        }
    }

    private void TriggerMainMenuTransitionAnimation()
    {
        m_menuAnimator.SetTrigger(k_enterMenuTriggerAnim);
        AudioManager.Instance.PlaySoundEffect(m_confirmClip);
    }

    // We use a coroutine because the server is the one who makes the load
    // we need to make a fade first before calling the start client
    private IEnumerator Join()
    {
        LoadingFadeEffect.Instance.FadeAll();

        yield return new WaitUntil(() => LoadingFadeEffect.s_canLoad);

        if (NetworkManager.Singleton.StartClient())
        {
            // On successful connection, save connection data for next time the game is played
            var unityTransport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as Unity.Netcode.Transports.UTP.UnityTransport;
            PlayerPrefs.SetString(PrefIpAddress, unityTransport.ConnectionData.Address);
            PlayerPrefs.SetInt(PrefPort, unityTransport.ConnectionData.Port);
        }
    }

    /// <summary> Loads connection data that has been stored in PlayerPrefs. </summary>
    private void TryPlayerPrefs()
    {
        // Return if there is no connection data to load
        if (false == PlayerPrefs.HasKey(PrefIpAddress) && false == PlayerPrefs.HasKey(PrefPort))
        {
            return;
        }

        // Load players last successful connection data
        var unityTransport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as Unity.Netcode.Transports.UTP.UnityTransport;
        string ipAddress = PlayerPrefs.GetString(PrefIpAddress, unityTransport.ConnectionData.Address);
        ushort port = (ushort)PlayerPrefs.GetInt(PrefPort, unityTransport.ConnectionData.Port);
        unityTransport.SetConnectionData(ipAddress, port);
    }
}