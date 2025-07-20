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


    [SerializeField] GridRenderer _gridRenderer;

    SubstrateClientExt _client;

    ulong _updateNumber = 0;

    Dictionary<Vector2, AsteroidView> _asteroids = new Dictionary<Vector2, AsteroidView>();
    Dictionary<string, ShipEntity> _shipsInSpace = new Dictionary<string, ShipEntity>();

    UIManager _uiManager;

    GameCallsService _gameCallsService;
    bool _isGameStarted = false;

    int _mapSize = 0; // Default map size, can be adjusted later

    ulong _lastProcessedBlockNumber = 0;
    async void Start()
    {

        _uiManager = FindAnyObjectByType<UIManager>();
        _client = new SubstrateClientExt(new Uri(Url),
                                        ChargeTransactionPayment.Default());


        _nodeUrlText.text = Url;
        await Connect();

        InvokeRepeating(nameof(UpdateState), 1.0f, 1.0f);
        InvokeRepeating(nameof(UpdatePlayerCount), 3.0f, 3.0f);
    }

    private async void UpdatePlayerCount()
    {
        if (_client == null || !_client.IsConnected)
        {
            Debug.Log("[NetworkManager] [UpdatePlayerCount] Client is null or disposed");
            return;
        }
        var playerCount = await _client.TemplateStorage.PlayersCount(null, CancellationToken.None);
        if (playerCount != null)
            _uiManager.SetPlayerCount(playerCount.ConvertTo<int>());
    }

    private void Update()
    {
        foreach (var ship in _shipsInSpace)
        {
            if (!ship.Value.Idle) continue;

            var position = ship.Value.transform.position;

            foreach (var asteroid in _asteroids)
            {
                var asteroidPosition = asteroid.Key;
                float distance = Vector2.Distance(new Vector2(position.x, position.z), asteroidPosition);
                if (distance < 1f)
                {
                    asteroid.Value.Collect();
                    // var address = _account.GetAccount().ToAccountId32().ToAddress();
                    // if (address == ship.Key)
                    //     _uiManager.CollectScreenEffect();
                }
            }
        }
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

        if (blockNumber <= _lastProcessedBlockNumber)
        {
            Debug.Log($"[UpdateState] Block {blockNumber} already processed, skipping");
            return;
        }
        _lastProcessedBlockNumber = blockNumber;

        if (_mapSize == 0)
        {
            var mapSize = await _client.TemplateStorage.MapSize(null, CancellationToken.None);
            if (mapSize != null)
            {
                _mapSize = mapSize.ConvertTo<int>();
                Debug.Log($"[NetworkManager] [UpdateState] [mapSize] Map size: {_mapSize}");
            }
            else
            {
                _mapSize = 50; // Default map size if not set
            }
            _gridRenderer.Draw(_mapSize);
        }

        Debug.Log($"[NetworkManager] [UpdateState] Starting to get events");
        var events = await _client.SystemStorage.Events(null, CancellationToken.None);
        var toArr = events.Value.ToArray();

        var accountId = _account.GetAccount().ToAccountId32();
        Debug.Log($"[NetworkManager] [UpdateState] Account ID: {accountId.ToAddress()}");

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

                        if (!_shipsInSpace.ContainsKey(owner))
                            CreateNewStarship(owner, from, flightNftSkin);

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
                            CreateNewStarship(gameOwner, startCoord, nftSkin.ConvertTo<int>());
                        }

                        Debug.Log($"[NetworkManager] GameStarted for {gameOwner} at {startCoord.X}, {startCoord.Y}");
                        break;
                    case Event.AsteroidCollected:
                        var tuple = templateEvent.Value2 as BaseTuple<AccountId32, Coord, EnumAsteroidKind, U32>;

                        var address = (tuple.Value[0] as AccountId32).ToAddress();
                        //var pos = tuple.Value[1] as Coord;
                        var resource = tuple.Value[2] as EnumAsteroidKind;
                        // var amount = (tuple.Value[3] as U32).ConvertTo<int>();

                        if (address == _account.GetAccount().ToAccountId32().ToAddress())
                        {
                            await LoadResources(accountId);


                            _uiManager.CollectScreenEffect();

                        }
                        break;

                }
            }
        }
    }

    private void CreateNewStarship(string owner, Coord startCoord, int nftSkin)
    {
        var ship = Instantiate(_starshipPrefab, startCoord.ToVector3(), Quaternion.identity);
        ship.name = owner;

        var accountAddress = _account.GetAccount().ToAccountId32().ToAddress();

        var shipEntity = ship.GetComponent<ShipEntity>();

        if (accountAddress == owner)
        {
            _cameraController.SetTarget(ship.transform);
            shipEntity.LocalPlayer = true;
        }

        shipEntity.SetSkin(nftSkin.ConvertTo<int>());
        // shipEntity.Lounch(startCoord.ToVector3(), startCoord.ToVector3(),
        //                                          blockNumber.ConvertTo<int>(),
        //                                          0);
        _shipsInSpace.Add(owner.ToString(), shipEntity);
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

        var address = _account.GetAccount().ToAccountId32().ToAddress();
        if (!_shipsInSpace.ContainsKey(address))
        {
            Debug.Log("[NetworkManager] [LaunchStarship] No ship found for the player, cannot launch.");
            return;
        }

        var playerShip = _shipsInSpace[address];
        if (!playerShip.Idle) return;
        if (_gameCallsService == null) return;

        _energySlider.FillOverTime(2.5f);

        Debug.Log("[NetworkManager] [LounchStarship] " + coord);
        await _gameCallsService.CallStartFlightAsync(coord, (status) =>
        {

        });
    }

    public void StartGame()
    {
        Debug.Log("[NetworkManager] [StartGame] Starting game...");
        _gameCallsService = new GameCallsService(_client, _account.GetAccount());
        var nftSkin = _uiManager.GetSelectedNftItems() ?? 0;

        Vector2 pos = new Vector2(_mapSize / 2, _mapSize / 2);

        _isGameStarted = true;
        CreateNewStarship(_account.GetAccount().ToAccountId32().ToAddress(), pos.ToCoord(), (int)nftSkin);

        _gameCallsService.StartGameAsync(pos, (uint)nftSkin).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"[StartGame] Error starting game: {task.Exception}");
            }
            else
            {
                Debug.Log("[StartGame] Game started successfully");
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
        if (coord.x < 0 || coord.y < 0 || coord.x >= _mapSize || coord.y >= _mapSize)
        {
            Debug.LogWarning($"[NetworkManager] [OnGridTap] Tap outside map boundaries: {coord}");
            return;
        }

        var address = _account.GetAccount().ToAccountId32().ToAddress();
        if (!_shipsInSpace.ContainsKey(address)) return;
        var playerShip = _shipsInSpace[address];
        if (!playerShip.Idle) return;

        // Calculate Manhattan distance: |x1 - x2| + |y1 - y2|
        float dx = Math.Abs(coord.x - playerShip.transform.position.x);
        float dy = Math.Abs(coord.y - playerShip.transform.position.z);
        float distance = dx + dy;
        Debug.Log($"[NetworkManager] [OnGridTap] Distance to target: {distance} playerShip.From: {playerShip.transform.position} coord: {coord}");
        if (distance < 4f && _asteroids.ContainsKey(coord))
        {
            // We need to implement this: visually hide the resource immediately after it’s clicked, 
            // so the player sees that it’s been collected without any delay. 
            // The actual resource reward will be granted a couple of seconds later, 
            // once it’s confirmed. In the future, 
            // we could add a small timer mechanic — where the resource is “mined” over 
            // a short period after the click. The mining could succeed or fail, depending 
            // on probability or luck.
            var asteroidView = _asteroids[coord];
            asteroidView.Collect();
            // _uiManager.CollectScreenEffect();

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

