// CharSelectManager.cs
using UnityEngine;
using System.Collections.Generic;

public class CharSelectManager : MonoBehaviour
{
    public static CharSelectManager Instance { get; private set; }

    [Header("Class SOs")]
    public CharacterClass[] allClasses;

    private List<CharacterClass> _selectedClasses
        = new List<CharacterClass>();

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
        BuildClassCards();
        RefreshPartySlots();
    }

    
    private void BuildClassCards()
    {
        foreach (var so in allClasses)
        {
            var go   = Instantiate(
                UIManager.Instance.classCardPrefab,
                UIManager.Instance.classCardsParent);
            var card = go.GetComponent<ClassCard>();
            if (card != null) card.Setup(so);
        }
    }

    
    public void AddToParty(CharacterClass so)
    {
        if (_selectedClasses.Count >= 4)
        {
            Debug.Log("Party is full! Max 4 characters.");
            return;
        }
        _selectedClasses.Add(so);
        RefreshPartySlots();
        UpdateBeginButton();
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
        foreach (Transform child in UIManager.Instance.partySlotsParent)
            Destroy(child.gameObject);

        for (int i = 0; i < 4; i++)
        {
            var go   = Instantiate(
                UIManager.Instance.partySlotPrefab,
                UIManager.Instance.partySlotsParent);
            var slot = go.GetComponent<PartySlot>();

            if (i < _selectedClasses.Count)
                slot.Setup(_selectedClasses[i], i, this);
            else
                slot.SetEmpty();
        }
    }

    private void UpdateBeginButton()
    {
        if (UIManager.Instance.beginAdventureButton != null)
            UIManager.Instance.beginAdventureButton.interactable
                = _selectedClasses.Count >= 1;
    }

    
    public void BeginAdventure()
    {
        GameManager.Instance.party.Clear();

        for (int i = 0; i < _selectedClasses.Count; i++)
        {
            var data = new PlayerRunTimeData();
            data.Initialize(
                _selectedClasses[i],
                $"{_selectedClasses[i].className} {i + 1}",
                (ulong)i);
            GameManager.Instance.party.Add(data);
        }

        GameManager.Instance.StartDungeon();
    }
}