using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotStriker.NetApiExt;
using DotStriker.NetApiExt.Generated;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using Substrate.NetApi;
using Substrate.NetApi.Model.Extrinsics;
using Substrate.NetApi.Model.Types;
using DotStriker.Integration.Client;
using DotStriker.NetApiExt.Generated.Model.solochain_template_runtime;
using DotStriker.NetApiExt.Generated.Model.pallet_template.pallet;


public class NetworkManager : MonoBehaviour
{
    public string Url => "ws://127.0.0.1:9944";
    SubstrateClientExt _client;
    Account _ownerAccount;

    async void Start()
    {

        _client = new SubstrateClientExt(new Uri(Url),
                                        ChargeTransactionPayment.Default());

        await Connect();
        Debug.Log("Connected to node");
        InvokeRepeating(nameof(UpdateState), 1.0f, 1.0f);

    }

    private async void UpdateState()
    {
        if (_client == null || !_client.IsConnected)
        {
            Debug.Log("Client is null or disposed");
            return;
        }

        Debug.Log($"UpdateState()");

        var events = await _client.SystemStorage.Events(null, CancellationToken.None);

        var toArr = events.Value;

        foreach (var record in toArr)
        {
            var runtimeEvent = record.Event;
            Debug.Log($"[UpdateState] Enevent: {runtimeEvent}");
        }
    }

    private async Task Connect()
    {
        Debug.Log("Connecting to node...");
        await _client.ConnectAsync(true, true, CancellationToken.None);
        Debug.Log("Connected to node");
    }

    internal Account GetAccount()
    {
        if (_ownerAccount == null)
        {
            _ownerAccount = BaseClient.Alice;
        }

        return _ownerAccount;
    }

    void Update()
    {

    }
}
