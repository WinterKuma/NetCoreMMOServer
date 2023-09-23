using TMPro;
using UnityEngine;

public class UIInventory : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _number;
    private Entity _playerEntity;

    public void SetPlayerEntity(Entity playerEntity)
    {
        _playerEntity = playerEntity;
        UpdateInventory();
    }

    public void UpdateInventory()
    {
        if (_playerEntity?.EntityData is PlayerEntity playerData)
        {
            int blockCount = playerData.Inventory.GetItemCount(ItemCode.Block);
            _number.text = blockCount.ToString();
        }
    }

    public void Update()
    {
        UpdateInventory();
    }
}
