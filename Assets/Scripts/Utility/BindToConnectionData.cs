using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class BindToConnectionData : MonoBehaviour
{
    public enum ConnectionDataEnum
    {
        IpAddress,
        Port,
    }

    [Tooltip("Which ConnectionData property this input field will read from and write to.")]
    public ConnectionDataEnum BindTo;

    private Unity.Netcode.Transports.UTP.UnityTransport unityTransport;

    private TMP_InputField inputField
    {
        get
        {
            if (_inputField == null)
            {
                _inputField = this.GetComponent<TMP_InputField>();
            }
            return _inputField;
        }
    }
    private TMP_InputField _inputField;

    IEnumerator Start()
    {
        // ConnectionData existing is required for this data binding to function.
        inputField.interactable = false;
        
        // wait until ConnectionData is in a valid state.
        yield return new WaitUntil(() => NetworkManager.Singleton.NetworkConfig.NetworkTransport as Unity.Netcode.Transports.UTP.UnityTransport != null);
        unityTransport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as Unity.Netcode.Transports.UTP.UnityTransport;

        // load values from ConnectionData.
        switch (BindTo)
        {
            case ConnectionDataEnum.IpAddress:
                inputField.text = unityTransport.ConnectionData.Address;
                inputField.onEndEdit.AddListener(NewIpAddress);
                break;
            case ConnectionDataEnum.Port:
                inputField.text = unityTransport.ConnectionData.Port.ToString();
                inputField.onEndEdit.AddListener(NewPort);
                break;
            default:
                Debug.LogException(new System.NotImplementedException(BindTo + " is not implmented"));
                break;
        }

        // allow ConnectionData to be modified.
        inputField.interactable = true;
    }

    public void NewIpAddress(string ipAddress)
    {
        unityTransport.SetConnectionData(ipAddress, unityTransport.ConnectionData.Port);
    }

    public void NewPort(string portAsString)
    {
        if (ushort.TryParse(portAsString, out ushort port))
        {
            unityTransport.SetConnectionData(unityTransport.ConnectionData.Address, port);
        }
        else
        {
            inputField.text = unityTransport.ConnectionData.Port.ToString();
        }
    }
}
