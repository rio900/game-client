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
using Substrate.NetApi.Model.Types.Base;
using DotStriker.NetApiExt.Generated.Model.pallet_template;
using DotStriker.NetApiExt.Generated.Model.sp_core.crypto;
using DotStriker.Integration.Helper;

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

    [SerializeField]
    GameObject _asteroidPrefab;

    [SerializeField]
    GameObject _starshipPrefab;

    SubstrateClientExt _client;
    Account _ownerAccount;

    int _updateNumber = 0;

    Dictionary<long, Coord> _asteroids = new Dictionary<long, Coord>();
    Dictionary<string, ShipEntity> _shipsInSpace = new Dictionary<string, ShipEntity>();

    List<GameObject> _asteroidObjects = new List<GameObject>();

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
        var blockNumber = block.Block.Header.Number;
        _blockNumberText.text = "Block number: " + blockNumber;

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

                    case Event.AsteroidSpawned:
                        var tuple2 = templateEvent.Value2 as BaseTuple<U64, Coord>;

                        var key = (tuple2?.Value[0] as U64).ConvertTo<long>();
                        if (!_asteroids.ContainsKey(key))
                            _asteroids.Add(key, tuple2?.Value[1] as Coord);

                        Debug.Log($"Spawned resource at {tuple2?.Value[0]} with id {tuple2?.Value[1]}");
                        break;
                    case Event.AsteroidRemoved:
                        var tuple3 = templateEvent.Value2 as U64;
                        var id = tuple3.ConvertTo<long>();
                        if (_asteroids.ContainsKey(id))
                            _asteroids.Remove(id);

                        Debug.Log($"Destroyed resource at {tuple3}");
                        break;

                    case Event.FlightStarted:
                        // tuple4: (AccountId, Coord from, Coord to, U32 endBlock)
                        var tuple4 = templateEvent.Value2 as BaseTuple<AccountId32, Coord, Coord, U32>;

                        var owner = (tuple4.Value[0] as AccountId32).ToAddress();
                        var from = tuple4.Value[1] as Coord;
                        var to = tuple4.Value[2] as Coord;
                        var end = (tuple4.Value[3] as U32).ConvertTo<int>();

                        if (_shipsInSpace.ContainsKey(owner.ToString()))
                        {
                            var ship = _shipsInSpace[owner.ToString()];
                            ship.Lounch(from.ToVector3(),
                                                                     to.ToVector3(),
                                                                     blockNumber.ConvertTo<int>(),
                                                                     end);
                        }
                        else
                        {
                            var ship = Instantiate(_starshipPrefab, from.ToVector3(), Quaternion.identity);
                            ship.name = owner.ToString();
                            var shipEntity = ship.GetComponent<ShipEntity>();
                            shipEntity.Lounch(from.ToVector3(), to.ToVector3(),
                                                                     blockNumber.ConvertTo<int>(),
                                                                     end);
                            _shipsInSpace.Add(owner.ToString(), shipEntity);
                        }


                        Debug.Log($"[NetworkManager] [FlightStarted] {owner?.ToString()} from {from?.X},{from?.Y} to {to?.X},{to?.Y}, ends at {end}");

                        break;

                }
            }
        }

        // That’s also terrible
        // I’m really sorry, please forgive me
        // I just want to display the asteroids as soon as possible
        _asteroidObjects.ForEach(x => Destroy(x));
        foreach (var item in _asteroids)
        {
            var id = item.Key;
            var coord = item.Value;

            if (coord != null)
            {
                var asteroid = Instantiate(_asteroidPrefab, new Vector3(coord.X.ConvertTo<float>(), 0, coord.Y.ConvertTo<float>()), Quaternion.identity);
                asteroid.name = id.ToString();
                _asteroidObjects.Add(asteroid);
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

    public async void LounchStarship(Vector2 coord)
    {
        Debug.Log("[NetworkManager] [LounchStarship] " + coord);

        await CallDoSomethingAsync(coord);
    }

    public async Task CallDoSomethingAsync(Vector2 coord)
    {
        var value = new Coord()
        {
            X = new U32((uint)coord.x),
            Y = new U32((uint)coord.y)
        };

        Method method = DotStriker.NetApiExt.Generated.Storage.TemplateCalls.DoSomething(value);

        string subscriptionId = await _client.TransactionWatchCalls.TransactionWatchV1SubmitAndWatchAsync(
            (status, info) =>
            {
                var result = $"[NetworkManager] CallDoSomethingAsync Status: {status}, Events: {info?.ToString()}";
                Debug.Log(result);
            },
            method,
            GetAccount(),
            ChargeTransactionPayment.Default(),
            64
        );

        Debug.Log($"[NetworkManager] Submitted: {subscriptionId}");
    }

    internal void OwnerShipLounch(Vector2 coord)
    {
        throw new NotImplementedException();
    }
}

public static class CalculationHelper
{
    public static Vector3 ToVector3(this Coord coord)
    {
        return new Vector3(coord.X.ConvertTo<float>(), 0, coord.Y.ConvertTo<float>());
    }

}
