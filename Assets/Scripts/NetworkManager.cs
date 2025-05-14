using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotStriker.NetApiExt.Generated;
using UnityEngine;
using Substrate.NetApi.Model.Extrinsics;
using Substrate.NetApi.Model.Types;
using DotStriker.Integration.Client;
using DotStriker.NetApiExt.Generated.Model.solochain_template_runtime;
using DotStriker.NetApiExt.Generated.Model.pallet_template.pallet;
using Event = DotStriker.NetApiExt.Generated.Model.pallet_template.pallet.Event;
using System.Linq;
using Substrate.NetApi.Model.Types.Primitive;
using TMPro;
using Unity.VisualScripting;

public class NetworkManager : MonoBehaviour
{
    public string Url => "ws://127.0.0.1:9944";


    [SerializeField]
    TMP_Text _nodeUrlText;

    [SerializeField]
    TMP_Text _blockNumberText;

    [SerializeField]
    TMP_Text _updateNumberText;

    [SerializeField]
    TMP_Text _numberInsideTestEventText;

    [SerializeField]
    GameObject _debugInfoPanel;

    SubstrateClientExt _client;
    Account _ownerAccount;

    int _updateNumber = 0;

    async void Start()
    {

        _client = new SubstrateClientExt(new Uri(Url),
                                        ChargeTransactionPayment.Default());

        _nodeUrlText.text = Url;
        await Connect();

        InvokeRepeating(nameof(UpdateState), 1.0f, 1.0f);
    }

    private async void UpdateState()
    {
        _updateNumber++;
        _updateNumberText.text = "Update number: " + _updateNumber.ToString();

        if (_client == null || !_client.IsConnected)
        {
            Debug.Log("[NetworkManager] [UpdateState] Client is null or disposed");
            return;
        }

        var block = await _client.Chain.GetBlockAsync(CancellationToken.None);
        _blockNumberText.text = "Block number: " + block.Block.Header.Number.ToString();

        Debug.Log($"[NetworkManager] [UpdateState] Starting to get events");
        var events = await _client.SystemStorage.Events(null, CancellationToken.None);
        var toArr = events.Value.ToArray();

        foreach (var record in toArr)
        {
            var runtimeEvent = record.Event;
            Debug.Log($"[NetworkManager] [UpdateState] Enevent: {runtimeEvent}");

            if (runtimeEvent.Value == RuntimeEvent.Template)
            {
                var templateEvent = runtimeEvent.Value2 as EnumEvent;

                switch (templateEvent.Value)
                {
                    case Event.TestEvent:
                        var tuple1 = templateEvent.Value2 as U32;
                        _numberInsideTestEventText.text = "Test event:" + tuple1.ConvertTo<int>().ToString();

                        Debug.Log($"[NetworkManager] [UpdateState] TestEvent {tuple1}");
                        break;
                }
            }
        }
    }

    private async Task Connect()
    {
        Debug.Log("[NetworkManager] [Connect] Connecting to node...");
        await _client.ConnectAsync(true, true, CancellationToken.None);
        Debug.Log("[NetworkManager] [Connect] Connected to node");
    }

    internal Account GetAccount()
    {
        if (_ownerAccount == null)
        {
            _ownerAccount = BaseClient.Alice;
        }

        return _ownerAccount;
    }

    public void OnDebugClick()
    {
        if (_debugInfoPanel.activeInHierarchy == false)
        {
            _debugInfoPanel.SetActive(true);
        }
        else
        {
            _debugInfoPanel.SetActive(false);
        }
    }
}
