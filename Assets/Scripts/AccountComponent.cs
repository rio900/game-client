using System.Collections;
using System.Collections.Generic;
using DotStriker.Integration.Client;
using DotStriker.Integration.Helper;
using Substrate.NetApi.Model.Types;
using TMPro;
using UnityEngine;

public enum AccountType
{
    Alice,
    Bob,
    DeviceId
}

public class AccountComponent : MonoBehaviour
{
    public const int AccountSeed = 123456789;

    [SerializeField]
    TMP_Text _accountMainScreenText;
    [SerializeField]
    TMP_InputField _accountMenuScreenText;

    Account _ownerAccount;

    private string _deviceId;
    // Start is called before the first frame update
    void Start()
    {
        _deviceId = SystemInfo.deviceUniqueIdentifier;
        SelectDeviceIdAcc();
    }

    public void SelectAlice()
    {
        SelectAccount(AccountType.Alice);
    }

    public void SelectBob()
    {
        SelectAccount(AccountType.Bob);
    }

    public void SelectDeviceIdAcc()
    {
        SelectAccount(AccountType.DeviceId);
    }

    public void SelectAccount(AccountType accountType)
    {
        if (accountType == AccountType.DeviceId)
        {
            _ownerAccount = BaseClient.RandomAccount(AccountSeed, _deviceId, KeyType.Sr25519);
        }
        else if (accountType == AccountType.Bob)
        {
            _ownerAccount = BaseClient.RandomAccount(AccountSeed, "Bob", KeyType.Sr25519);
        }
        else
        {
            _ownerAccount = BaseClient.Alice;
        }

        _accountMainScreenText.text = $"{_ownerAccount.ToAccountId32().ToAddress()}";
        _accountMenuScreenText.text = $"{_ownerAccount.ToAccountId32().ToAddress()}";

    }

    internal Account GetAccount()
    {
        if (_ownerAccount == null)
        {
            return BaseClient.Alice;
        }
        return _ownerAccount;
    }
}
