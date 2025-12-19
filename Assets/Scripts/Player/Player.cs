using UnityEngine;

[RequireComponent(typeof(PlayerController))]
//[RequireComponent(typeof(PlayerInventory))]
public class Player : MonoBehaviour
{
    public PlayerController Controller { get; private set; }
    public PlayerInventory Inventory { get; private set; }

    void Awake()
    {
        Controller = GetComponent<PlayerController>();
        Inventory = GetComponent<PlayerInventory>();
    }
}
