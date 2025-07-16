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
    // public string Url => "ws://104.225.143.227:9944";

    [SerializeField]
    AccountComponent _account;

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

    [SerializeField]
    CameraController _cameraController;

    [SerializeField]
    EnergySlider _energySlider;

    [SerializeField] GameObject _callIndicatorPrefab;


    SubstrateClientExt _client;

    ulong _updateNumber = 0;

    Dictionary<Vector2, AsteroidView> _asteroids = new Dictionary<Vector2, AsteroidView>();
    Dictionary<string, ShipEntity> _shipsInSpace = new Dictionary<string, ShipEntity>();

    UIManager _uiManager;

    GameCallsService _gameCallsService;
    bool _isGameStarted = false;

    async void Start()
    {
        for (int i = 0; i < 50; i++)
        {
            for (int j = 0; j < 50; j++)
            {
                var callIndicator = Instantiate(_callIndicatorPrefab);
                callIndicator.transform.position = new Vector3(i, 0, j);
            }
        }
        _uiManager = FindAnyObjectByType<UIManager>();
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

        var accountId = _account.GetAccount().ToAccountId32();
        Debug.Log($"[NetworkManager] [UpdateState] Account ID: {accountId.ToAddress()}");
        await LoadResources(accountId);

        //ActiveShips
        var myShip = await _client.TemplateStorage.ActiveShips(accountId, null, CancellationToken.None);
        if (myShip != null)
        {
            Debug.Log($"Storage 'ActiveShips' = {myShip.Energy}");
            _uiManager.SetEnergy(myShip.Energy.ConvertTo<int>());
        }
        else
        {
            _uiManager.SetEnergy(0);
            Debug.Log("Storage 'ActiveShips' is not set yet.");
        }

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
                        var tuple2 = templateEvent.Value2 as BaseTuple<EnumAsteroidKind, Coord>;
                        Debug.Log($"[NetworkManager] [UpdateState] AsteroidSpawned {(tuple2?.Value[0] as EnumAsteroidKind).ToSafeString()}");

                        var key = tuple2?.Value[0] as EnumAsteroidKind;
                        var coord = tuple2?.Value[1] as Coord;
                        var vect = coord.ToVector2();

                        if (!_asteroids.ContainsKey(vect))
                        {

                            var asteroid = Instantiate(_asteroidPrefab, coord.ToVector3(), Quaternion.identity);
                            asteroid.name = key.ToString();
                            var asteroidView = asteroid.GetComponent<AsteroidView>();
                            asteroidView.SetId((int)key.Value);

                            Debug.Log($"[AsteroidSpawned] Trying to add asteroid at: {coord?.X}, {coord?.Y}");
                            _asteroids.Add(vect, asteroidView);
                        }

                        Debug.Log($"Spawned resource at {tuple2?.Value[0]} with id {tuple2?.Value[1]}");
                        break;
                    case Event.AsteroidRemoved:
                        var tuple3 = templateEvent.Value2 as Coord;
                        var vect2 = tuple3.ToVector2();

                        if (_asteroids.ContainsKey(vect2))
                        {
                            Debug.Log($"[NetworkManager] [AsteroidRemoved] Removing asteroid at {tuple3}");

                            Destroy(_asteroids[vect2].gameObject);
                            _asteroids.Remove(vect2);
                        }
                        else
                        {
                            Debug.Log($"[NetworkManager] [AsteroidRemoved] No asteroid found at {tuple3}");
                        }


                        Debug.Log($"Destroyed resource at {tuple3}");
                        break;

                    case Event.FlightStarted:
                        // tuple4: (AccountId, Coord from, Coord to, U32 endBlock)
                        var tuple4 = templateEvent.Value2 as BaseTuple<AccountId32, Coord, Coord, U32, U32>;

                        var owner = (tuple4.Value[0] as AccountId32).ToAddress();
                        var from = tuple4.Value[1] as Coord;
                        var to = tuple4.Value[2] as Coord;
                        var end = (tuple4.Value[3] as U32).ConvertTo<int>();
                        var flightNftSkin = (tuple4.Value[4] as U32).ConvertTo<int>();

                        Lounche(blockNumber, owner, from, to, end, flightNftSkin);
                        _energySlider.FillInstantly();

                        Debug.Log($"[NetworkManager] [FlightStarted] {owner?.ToString()} from {from?.X},{from?.Y} to {to?.X},{to?.Y}, ends at {end}");

                        break;

                    case Event.EnergyDepleted:
                        var depletedOwner = (templateEvent.Value2 as AccountId32).ToAddress();
                        if (_shipsInSpace.ContainsKey(depletedOwner))
                        {
                            Destroy(_shipsInSpace[depletedOwner].gameObject);
                            _shipsInSpace.Remove(depletedOwner);
                            Debug.Log($"[NetworkManager] Energy depleted for {depletedOwner}");
                        }

                        if (depletedOwner == _account.GetAccount().ToAccountId32().ToAddress())
                        {
                            _isGameStarted = false;
                            ShowStartGamePanel();
                        }

                        break;

                    case Event.GameStarted:
                        _isGameStarted = true;
                        var gameTuple = templateEvent.Value2 as BaseTuple<AccountId32, Coord, U32>;
                        var gameOwner = (gameTuple.Value[0] as AccountId32).ToAddress();
                        var startCoord = gameTuple.Value[1] as Coord;
                        var nftSkin = gameTuple.Value[2] as U32;

                        if (!_shipsInSpace.ContainsKey(gameOwner))
                        {
                            // var ship = Instantiate(_starshipPrefab, startCoord.ToVector3(), Quaternion.identity);
                            // ship.name = gameOwner;
                            // var entity = ship.GetComponent<ShipEntity>();
                            // entity.Appear(startCoord.ToVector3());
                            // _shipsInSpace[gameOwner] = entity;
                            // _cameraController.SetTarget(ship.transform);

                            var ship = Instantiate(_starshipPrefab, startCoord.ToVector3(), Quaternion.identity);
                            ship.name = gameOwner;
                            _cameraController.SetTarget(ship.transform);

                            var shipEntity = ship.GetComponent<ShipEntity>();
                            shipEntity.SetSkin(nftSkin.ConvertTo<int>());
                            // shipEntity.Lounch(startCoord.ToVector3(), startCoord.ToVector3(),
                            //                                          blockNumber.ConvertTo<int>(),
                            //                                          0);
                            _shipsInSpace.Add(gameOwner.ToString(), shipEntity);
                        }

                        Debug.Log($"[NetworkManager] GameStarted for {gameOwner} at {startCoord.X}, {startCoord.Y}");
                        break;

                }
            }
        }
    }

    private async Task LoadResources(AccountId32 accountId)
    {
        var resourceTypes = new List<AsteroidKind>
        {
            AsteroidKind.Gold,
            AsteroidKind.Dot0
        };

        foreach (var item in resourceTypes)
        {
            var asteroidKindEnum = new EnumAsteroidKind();
            asteroidKindEnum.Create(item);
            var energyKey = new BaseTuple<AccountId32, EnumAsteroidKind>(accountId, asteroidKindEnum);

            var result = await _client.TemplateStorage.AccountResources(energyKey, null, CancellationToken.None);
            if (result != null)
            {
                Debug.Log($"Storage 'AccountResources energy' = {result.Value}");
                _uiManager.SetResource(item, result.Value.ConvertTo<int>());
            }
            else
            {
                _uiManager.SetResource(item, 0);
                Debug.Log("Storage 'AccountResources' is not set yet.");
            }
        }
    }

    private async Task LoadNfts(AccountId32 accountId)
    {
        var resourceTypes = new List<AsteroidKind>
        {
            AsteroidKind.Nft0,
            AsteroidKind.Nft1,
            AsteroidKind.Nft2
        };

        foreach (var item in resourceTypes)
        {
            var asteroidKindEnum = new EnumAsteroidKind();
            asteroidKindEnum.Create(item);
            var energyKey = new BaseTuple<AccountId32, EnumAsteroidKind>(accountId, asteroidKindEnum);

            var result = await _client.TemplateStorage.AccountResources(energyKey, null, CancellationToken.None);
            if (result != null)
            {
                Debug.Log($"Storage 'AccountResources nft {item}' = {result.Value}");
                _uiManager.SetNftCount(item, result.Value.ConvertTo<int>());
            }
            else
            {
                Debug.Log($"Storage 'AccountResources nft {item}' = 0");
                _uiManager.SetNftCount(item, 0);
            }
        }

    }

    private void Lounche(U64 blockNumber, string owner, Coord from, Coord to, int end, int nftSkin = 0)
    {
        Debug.Log($"[NetworkManager] [Lounche] {owner} from {from?.X},{from?.Y} to {to?.X},{to?.Y},{blockNumber} ends at {end}");

        if (!_shipsInSpace.ContainsKey(owner.ToString())) return;

        var ship = _shipsInSpace[owner.ToString()];
        ship.Lounch(ship.transform.position,  //from.ToVector3(),
                                                 to.ToVector3(),
                                                 blockNumber.ConvertTo<int>(),
                                                 end);
        ship.SetSkin(nftSkin);

    }

    private async Task Connect()
    {
        Debug.Log("[NetworkManager] [Connect] Connecting to node...");
        await _client.ConnectAsync(true, true, CancellationToken.None);
        Debug.Log("[NetworkManager] [Connect] Connected to node");
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

    public async void LaunchStarship(Vector2 coord)
    {
        if (!_isGameStarted) return;

        Debug.Log("[NetworkManager] [LounchStarship] " + coord);
        await _gameCallsService.CallDoSomethingAsync(coord, (status) => _energySlider.FillOverTime(2.5f));
    }

    public void StartGame()
    {
        Debug.Log("[NetworkManager] [StartGame] Starting game...");
        Vector2 pos = new Vector2(25, 25);
        var nftSkin = _uiManager.GetSelectedNftItems() ?? 0;
        _gameCallsService = new GameCallsService(_client, _account.GetAccount());

        _gameCallsService.StartGameAsync(pos, (uint)nftSkin).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"[NetworkManager] [StartGame] Error starting game: {task.Exception}");
            }
            else
            {
                Debug.Log("[NetworkManager] [StartGame] Game started successfully");
            }
        });
    }

    public void ShowStartGamePanel()
    {
        RefreshNfts();
        _uiManager.ShowStartGamePanel();

    }

    public void RefreshNfts()
    {
        var accountId = _account.GetAccount().ToAccountId32();
        LoadNfts(accountId).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"[NetworkManager] [RefreshNfts] Error loading NFTs: {task.Exception}");
            }
            else
            {
                Debug.Log("[NetworkManager] [RefreshNfts] NFTs loaded successfully");
            }
        });
    }

    internal void OnGridTap(Vector2 coord)
    {
        var address = _account.GetAccount().ToAccountId32().ToAddress();
        if (!_shipsInSpace.ContainsKey(address)) return;
        var playerShip = _shipsInSpace[address];

        // Calculate Manhattan distance: |x1 - x2| + |y1 - y2|
        float dx = Math.Abs(coord.x - playerShip.transform.position.x);
        float dy = Math.Abs(coord.y - playerShip.transform.position.z);
        float distance = dx + dy;
        Debug.Log($"[NetworkManager] [OnGridTap] Distance to target: {distance} playerShip.From: {playerShip.transform.position} coord: {coord}");
        if (distance < 4f)
        {
            _gameCallsService.TryToCollectResourceAsync(coord).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"[NetworkManager] [OnGridTap] Error collecting resource: {task.Exception}");
                }
                else
                {
                    Debug.Log("[NetworkManager] [OnGridTap] Resource collection initiated");
                }
            });
            Debug.Log($"[NetworkManager] [OnGridTap] Already at {coord}, no need to launch again.");
            return;
        }
        else
        {
            LaunchStarship(coord);
        }
    }
}

