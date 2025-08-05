using System.Threading.Tasks;
using DotStriker.NetApiExt.Generated;
using UnityEngine;
using Substrate.NetApi.Model.Extrinsics;
using Substrate.NetApi.Model.Types;
using Substrate.NetApi.Model.Types.Primitive;
using DotStriker.NetApiExt.Generated.Model.pallet_dot_striker;
using DotStriker.NetApiExt.Generated.Storage;
using System;
using DotStriker.Integration.Helper;

public class GameCallsService
{
    private readonly SubstrateClientExt _client;
    private readonly Account _account;

    public GameCallsService(SubstrateClientExt client, Account account)
    {
        _client = client;
        _account = account;
    }

    public async Task CallStartFlightAsync(Vector2 coord, Action<string> callback = null)
    {

        Method method = DotStrikerCalls.StartFlight(coord.ToCoord());

        string subscriptionId = await _client.TransactionWatchCalls.TransactionWatchV1SubmitAndWatchAsync(
            (status, info) =>
            {
                callback?.Invoke(status);

                var result = $"[NetworkManager] CallDoSomethingAsync Status: {status}, Events: {info?.ToString()}";
                Debug.Log(result);
            },
            method,
            _account,
            ChargeTransactionPayment.Default(),
            64
        );

        Debug.Log($"[NetworkManager] Submitted: {subscriptionId}");
    }

    public async Task StartGameAsync(Vector2 pos, uint nftSkin)
    {
        Method method = DotStrikerCalls.StartGame(pos.ToCoord(), new U32(nftSkin));

        await _client.TransactionWatchCalls.TransactionWatchV1SubmitAndWatchAsync(
            (status, info) =>
            {
                Debug.Log($"[GameCallsService] StartGame: {status} {info}");
            },
            method,
            _account,
            ChargeTransactionPayment.Default(),
            64
        );
    }

    public async Task TryToCollectResourceAsync(Vector2 pos)
    {
        Method method = DotStrikerCalls.TryToCollectResource(pos.ToCoord());
        await _client.TransactionWatchCalls.TransactionWatchV1SubmitAndWatchAsync(
            (status, info) =>
            {
                Debug.Log($"[GameCallsService] TryToCollectResource: {status} {info}");
            },
            method,
            _account,
            ChargeTransactionPayment.Default(),
            64
        );
    }

    public async Task CallAdminSetMapSizeAsync(uint size, Action<string> callback = null)
    {
        Method method = DotStrikerCalls.AdminSetMapSize(new U32(size));

        string subscriptionId = await _client.TransactionWatchCalls.TransactionWatchV1SubmitAndWatchAsync(
            (status, info) =>
            {
                callback?.Invoke(status);
                Debug.Log($"[GameCallsService] AdminSetMapSize: {status} {info}");
            },
            method,
            _account,
            ChargeTransactionPayment.Default(),
            64
        );

        Debug.Log($"[GameCallsService] Submitted AdminSetMapSize: {subscriptionId}");
    }
}