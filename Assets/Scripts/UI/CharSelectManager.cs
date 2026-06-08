// CharSelectManager.cs
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class CharSelectManager : MonoBehaviour
{
    public static CharSelectManager Instance { get; private set; }

    [Header("Class SOs")]
    public CharacterClass[] allClasses;

    // My own selections
    private List<CharacterClass> _mySelectedClasses = new List<CharacterClass>();

    // All players' selections: clientId -> comma-separated class names
    private Dictionary<ulong, string> _allPlayerSelections = new Dictionary<ulong, string>();

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
        yield return null;
        BuildClassCards();
        RefreshPartySlots();
    }

    private void BuildClassCards()
    {
        if (CharSelectUIManager.Instance == null) return;
        if (CharSelectUIManager.Instance.classCardPrefab == null) return;
        if (CharSelectUIManager.Instance.classCardsParent == null) return;

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
        if (_mySelectedClasses.Count >= quota)
        {
            Debug.Log($"You can only pick {quota} character(s)!");
            return;
        }
        if (_mySelectedClasses.Contains(so))
        {
            Debug.Log("Character already in party!");
            return;
        }

        _mySelectedClasses.Add(so);

        ulong myId = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.LocalClientId
            : 0;
        _allPlayerSelections[myId] = string.Join(",",
            _mySelectedClasses.ConvertAll(c => c.className));

        RefreshPartySlots();
        UpdateBeginButton();
        BroadcastMySlots();
    }

    public void RemoveFromParty(int slotIndex)
    {
        // slotIndex here is global across all players' slots
        // We only allow removing our own — find which of our picks it maps to
        ulong myId = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.LocalClientId
            : 0;

        // Rebuild the flat slot list to find whose slot this is
        var flatSlots = BuildFlatSlotList();
        if (slotIndex < 0 || slotIndex >= flatSlots.Count) return;

        var (ownerId, classIndex) = flatSlots[slotIndex];
        if (ownerId != myId) return; // can't remove other players' picks

        _mySelectedClasses.RemoveAt(classIndex);

        _allPlayerSelections[myId] = _mySelectedClasses.Count > 0
            ? string.Join(",", _mySelectedClasses.ConvertAll(c => c.className))
            : "";

        RefreshPartySlots();
        UpdateBeginButton();
        BroadcastMySlots();
    }

    // Called by NetworkGameSync when another player's selections change
    public void ApplyRemoteSlots(ulong senderClientId, string classNames)
    {
        _allPlayerSelections[senderClientId] = classNames;
        RefreshPartySlots();
    }

    private void BroadcastMySlots()
    {
        if (NetworkGameSync.Instance == null) return;
        ulong myId = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.LocalClientId
            : 0;
        string classNames = _mySelectedClasses.Count > 0
            ? string.Join(",", _mySelectedClasses.ConvertAll(c => c.className))
            : "";
        NetworkGameSync.Instance.SyncPartySlotsServerRpc(myId, classNames);
    }

    // Returns a flat list of (ownerClientId, indexWithinThatOwner'sList)
    private List<(ulong, int)> BuildFlatSlotList()
    {
        var result = new List<(ulong, int)>();
        ulong myId = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.LocalClientId
            : 0;

        // My slots first
        for (int i = 0; i < _mySelectedClasses.Count; i++)
            result.Add((myId, i));

        // Then other players
        foreach (var kvp in _allPlayerSelections)
        {
            if (kvp.Key == myId) continue;
            if (string.IsNullOrWhiteSpace(kvp.Value)) continue;
            string[] names = kvp.Value.Split(',');
            for (int i = 0; i < names.Length; i++)
                if (!string.IsNullOrWhiteSpace(names[i]))
                    result.Add((kvp.Key, i));
        }
        return result;
    }

    private void RefreshPartySlots()
    {
        if (CharSelectUIManager.Instance == null) return;

        foreach (Transform child in CharSelectUIManager.Instance.partySlotsParent)
            Destroy(child.gameObject);

        ulong myId = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.LocalClientId
            : 0;

        // Build combined slot display: my picks + other players' picks
        var allClasses = new List<(CharacterClass so, bool isMine, int myIndex)>();

        for (int i = 0; i < _mySelectedClasses.Count; i++)
            allClasses.Add((_mySelectedClasses[i], true, i));

        foreach (var kvp in _allPlayerSelections)
        {
            if (kvp.Key == myId) continue;
            if (string.IsNullOrWhiteSpace(kvp.Value)) continue;
            foreach (string name in kvp.Value.Split(','))
            {
                var so = FindClassByName(name);
                if (so != null) allClasses.Add((so, false, -1));
            }
        }

        // Show up to 4 slots total
        for (int i = 0; i < 4; i++)
        {
            var go = Instantiate(
                CharSelectUIManager.Instance.partySlotPrefab,
                CharSelectUIManager.Instance.partySlotsParent);
            var slot = go.GetComponent<PartySlot>();

            if (i < allClasses.Count)
            {
                var (so, isMine, myIdx) = allClasses[i];
                if (isMine)
                    slot.Setup(so, myIdx, this);   // removable
                else
                    slot.SetupReadOnly(so);         // not removable
            }
            else
            {
                slot.SetEmpty();
            }
        }
    }

    private void UpdateBeginButton()
    {
        if (CharSelectUIManager.Instance.beginAdventureButton == null) return;
        bool isHost = NetworkManager.Singleton == null || NetworkManager.Singleton.IsHost;
        CharSelectUIManager.Instance.beginAdventureButton.gameObject.SetActive(isHost);
        if (isHost)
            CharSelectUIManager.Instance.beginAdventureButton.interactable
                = _mySelectedClasses.Count >= 1;
    }

    private int GetMyQuota()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
            return 4;
        if (!NetworkManager.Singleton.IsServer)
            return 2; // clients default to 2; host controls the real count
        int playerCount = NetworkManager.Singleton.ConnectedClients.Count;
        if (playerCount <= 1) return 4;
        if (playerCount == 2) return 2;
        if (playerCount == 3)
            return NetworkManager.Singleton.IsHost ? 2 : 1;
        return 1;
    }

    public void BeginAdventure()
    {
        if (GameManager.Instance == null) return;
        if (_mySelectedClasses.Count == 0) return;

        // Offline / solo
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
        {
            GameManager.Instance.party.Clear();
            foreach (var so in _mySelectedClasses)
            {
                var data = new PlayerRunTimeData();
                data.Initialize(so, so.className, 0);
                GameManager.Instance.party.Add(data);
            }
            GameManager.Instance.StartDungeon();
            return;
        }

        // Multiplayer — host builds the party from all collected selections
        if (NetworkManager.Singleton.IsHost)
        {
            // Register host's own picks
            ulong hostId = NetworkManager.Singleton.LocalClientId;
            _allPlayerSelections[hostId] = string.Join(",",
                _mySelectedClasses.ConvertAll(c => c.className));

            BuildPartyAndStart();
        }
    }

    // Called by NetworkGameSync.SubmitSelectionServerRpc (client submissions)
    public void ReceiveSubmission(ulong clientId, string classNames)
    {
        _allPlayerSelections[clientId] = classNames;
        // No auto-start here; host triggers via BeginAdventure button
    }

    private void BuildPartyAndStart()
{
    GameManager.Instance.party.Clear();

    var clientIds = new List<ulong>(_allPlayerSelections.Keys);
    clientIds.Sort();

    // Build payload while constructing the party
    var rosterParts = new List<string>();

    foreach (var clientId in clientIds)
    {
        string classNames = _allPlayerSelections[clientId];
        if (string.IsNullOrWhiteSpace(classNames)) continue;

        foreach (string className in classNames.Split(','))
        {
            CharacterClass so = FindClassByName(className);
            if (so == null) continue;

            var data = new PlayerRunTimeData();
            data.Initialize(so, so.className, clientId);
            GameManager.Instance.party.Add(data);

            // clientId,className,playerName
            rosterParts.Add($"{clientId},{so.className},{so.className}");
        }
    }

    // Send full party roster to all clients BEFORE changing state
    // so their party list is ready when DungeonScene loads
    string rosterPayload = string.Join("|", rosterParts);
    NetworkGameSync.Instance.SyncPartyRosterClientRpc(rosterPayload);

    NetworkGameSync.Instance.ChangeGameStateServerRpc(GameState.EntranceHall);
}
    private CharacterClass FindClassByName(string name)
    {
        foreach (var so in allClasses)
            if (so.className == name) return so;
        return null;
    }
}