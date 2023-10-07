using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInvSlot : MonoBehaviour
{
    [field: SerializeField]
    private Color _selectColor;

    [field: SerializeField]
    private Color _diselectColor;


    [field: SerializeField]
    private GameObject _itemSlot;
    [field: SerializeField]
    private Image _slotImage;
    [field: SerializeField]
    private TextMeshProUGUI _itemCount;

    public void SetSelect(bool value)
    {
        if (value)
        {
            _slotImage.color = _selectColor;
        }
        else
        {
            _slotImage.color = _diselectColor;
        }
    }

    public void UpdateSlotItem(Item item)
    {
        _itemSlot.SetActive(item.code != ItemCode.None);
        _itemCount.text = item.count.ToString();
    }
}
