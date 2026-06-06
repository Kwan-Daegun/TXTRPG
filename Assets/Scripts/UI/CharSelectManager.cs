// CharSelectManager.cs
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
public class CharSelectManager : MonoBehaviour
{
    public static CharSelectManager Instance { get; private set; }

    [Header("Class SOs")]
    public CharacterClass[] allClasses;

    private List<CharacterClass> _selectedClasses
        = new List<CharacterClass>();

    private Dictionary<ulong, string> _submissions
        = new Dictionary<ulong, string>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(InitAfterFrame());

    }
    private System.Collections.IEnumerator InitAfterFrame()
    {
        yield return null; // Wait one frame to ensure CharSelectUIManager is ready
        BuildClassCards();
        RefreshPartySlots();

    }


    private void BuildClassCards()
    {
        if (CharSelectUIManager.Instance == null)
        {
            Debug.LogError("CharSelectUIManager not found!");
            return;
        }
        if (CharSelectUIManager.Instance.classCardPrefab == null)
        {
            Debug.LogError("ClassCard prefab not assigned!");
            return;
        }
        if (CharSelectUIManager.Instance.classCardsParent == null)
        {
            Debug.LogError("ClassCards parent not found!");
            return;
        }

        foreach (var so in allClasses)
        {
            var go = Instantiate(
                CharSelectUIManager.Instance.classCardPrefab,
                CharSelectUIManager.Instance.classCardsParent);
            var card = go.GetComponent<ClassCard>();
            if (card != null) card.Setup(so);
        }
    }


    public void AddToParty(CharacterClass so)
    {
        int quota = GetMyQuota();
        if (_selectedClasses.Count >= quota)
        {
            Debug.Log($"You can only pick {quota} character(s)!");
            return;
        }

        if (_selectedClasses.Contains(so))
        {
            Debug.Log("Character already in party!");
            return;
        }

        _selectedClasses.Add(so);
        RefreshPartySlots();
        UpdateBeginButton();
    }

    private int GetMyQuota()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
            return 4;

        int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
        if (playerCount <= 1) return 4;
        if (playerCount == 2) return 2;
        if (playerCount == 3)
            return NetworkManager.Singleton.IsHost ? 2 : 1;
        return 1;
    }

    public void RemoveFromParty(int index)
    {
        if (index < 0 || index >= _selectedClasses.Count) return;
        _selectedClasses.RemoveAt(index);
        RefreshPartySlots();
        UpdateBeginButton();
    }


    private void RefreshPartySlots()
    {
        foreach (Transform child in CharSelectUIManager.Instance.partySlotsParent)
            Destroy(child.gameObject);

        for (int i = 0; i < 4; i++)
        {
            var go = Instantiate(
                CharSelectUIManager.Instance.partySlotPrefab,
                CharSelectUIManager.Instance.partySlotsParent);
            var slot = go.GetComponent<PartySlot>();

            if (i < _selectedClasses.Count)
                slot.Setup(_selectedClasses[i], i, this);
            else
                slot.SetEmpty();
        }
    }

    private void UpdateBeginButton()
    {
        if (CharSelectUIManager.Instance.beginAdventureButton != null)
            CharSelectUIManager.Instance.beginAdventureButton.interactable
                = _selectedClasses.Count >= 1;
    }


    // CharSelectManager.cs
    public void BeginAdventure()
    {
        if (GameManager.Instance == null) return;
        if (_selectedClasses.Count == 0) return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
        {
            GameManager.Instance.party.Clear();
            foreach (var so in _selectedClasses)
            {
                var data = new PlayerRunTimeData();
                data.Initialize(so, so.className, 0);
                GameManager.Instance.party.Add(data);
            }
            GameManager.Instance.StartDungeon();
            return;
        }

        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        string classNames = string.Join(",",
            _selectedClasses.ConvertAll(c => c.className));

        SubmitSelectionServerRpc(classNames);

        if (NetworkManager.Singleton.IsHost)
        {
            _submissions.Clear();
            _submissions[localClientId] = classNames;
            TryStartWhenAllSubmitted();
        }
        else
        {
            GameManager.Instance.party.Clear();
            foreach (var so in _selectedClasses)
            {
                var data = new PlayerRunTimeData();
                data.Initialize(so, so.className, localClientId);
                GameManager.Instance.party.Add(data);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitSelectionServerRpc(string classNames, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        _submissions[clientId] = classNames;
        TryStartWhenAllSubmitted();
    }

    private void TryStartWhenAllSubmitted()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        int expected = NetworkManager.Singleton.ConnectedClients.Count;
        if (_submissions.Count < expected) return;

        BuildPartyAndStart();
    }

    private void BuildPartyAndStart()
    {
        GameManager.Instance.party.Clear();

        var clientIds = new List<ulong>(_submissions.Keys);
        clientIds.Sort();

        foreach (var clientId in clientIds)
        {
            string classNames = _submissions[clientId];
            if (string.IsNullOrWhiteSpace(classNames)) continue;

            string[] names = classNames.Split(',');
            foreach (string className in names)
            {
                CharacterClass so = FindClassByName(className);
                if (so == null) continue;

                var data = new PlayerRunTimeData();
                data.Initialize(so, so.className, clientId);
                GameManager.Instance.party.Add(data);
            }
        }

        NetworkGameSync.Instance.ChangeGameStateServerRpc(
            GameState.EntranceHall);
    }

    private CharacterClass FindClassByName(string name)
    {
        foreach (var so in allClasses)
            if (so.className == name) return so;
        return null;
    }
}
