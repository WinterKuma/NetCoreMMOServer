using TMPro;
using UnityEngine;

public class UIInventory : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI _number;
    private Entity _playerEntity;

    [field: SerializeField]
    private UIInvSlot[] _invSlots;

    public void SetPlayerEntity(Entity playerEntity)
    {
        _playerEntity = playerEntity;
        UpdateInventory();
    }

    public void UpdateInventory()
    {
        if (_playerEntity?.EntityData is PlayerEntity playerData)
        {
            for(int i = 0; i < _invSlots.Length; i++)
            {
                _invSlots[i].UpdateSlotItem(playerData.Inventory.Items[i].Value.GetItem());
                _invSlots[i].SetSelect(playerData.Inventory.SelectSlotIndex.Value == i);
            }
            //int blockCount = playerData.Inventory.GetItemCount(ItemCode.Block);
            //_number.text = blockCount.ToString();
        }
    }

    public void Update()
    {
        UpdateInventory();
    }
}
