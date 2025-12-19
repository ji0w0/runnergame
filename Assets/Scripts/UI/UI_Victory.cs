using System;
using UnityEngine;
using UnityEngine.UIElements;

public class UI_Victory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] UIDocument document;

    [Header("Assets")]
    [SerializeField] VisualTreeAsset uxml;
    [SerializeField] StyleSheet uss;

    public event Action OnContinue;

    VisualElement _overlay;
    Label _hamburgerValue;
    Label _moneyValue;
    Label _trashValue;
    Label _totalValue;
    Button _continueButton;

    void Awake()
    {
        if (document == null)
            document = GetComponent<UIDocument>();

        if (document == null)
        {
            Debug.LogError("[UI_Victory] UIDocument is missing.", this);
            enabled = false;
            return;
        }

        if (uxml == null)
        {
            Debug.LogError("[UI_Victory] UXML(VisualTreeAsset) is not assigned.", this);
            enabled = false;
            return;
        }

        var root = document.rootVisualElement;
        root.Clear();

        if (uss != null)
            root.styleSheets.Add(uss);

        var tree = uxml.CloneTree();
        root.Add(tree);

        Cache(tree);
        Hook();
        Hide();
    }

    void Cache(VisualElement tree)
    {
        _overlay = tree.Q<VisualElement>("Overlay");
        _hamburgerValue = tree.Q<Label>("HamburgerValue");
        _moneyValue = tree.Q<Label>("MoneyValue");
        _trashValue = tree.Q<Label>("TrashValue");
        _totalValue = tree.Q<Label>("TotalValue");
        _continueButton = tree.Q<Button>("ContinueButton");

        if (_overlay == null) Debug.LogError("[UI_Victory] Overlay not found in UXML.", this);
        if (_continueButton == null) Debug.LogError("[UI_Victory] ContinueButton not found in UXML.", this);
    }

    void Hook()
    {
        if (_continueButton != null)
            _continueButton.clicked += () => OnContinue?.Invoke();
    }

    public void Show(int hamburger, int money, int trash)
    {
        int total = hamburger + money + trash;

        if (_hamburgerValue != null) _hamburgerValue.text = hamburger.ToString();
        if (_moneyValue != null) _moneyValue.text = money.ToString();
        if (_trashValue != null) _trashValue.text = trash.ToString();
        if (_totalValue != null) _totalValue.text = total.ToString();

        SetVisible(true);
    }

    public void Hide() => SetVisible(false);

    void SetVisible(bool visible)
    {
        if (_overlay == null) return;
        _overlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
