using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMyHp : MonoBehaviour
{
    [field: SerializeField]
    private GameObject[] _hearts;
    private Entity _playerEntity;

    public void SetPlayerEntity(Entity playerEntity)
    {
        _playerEntity = playerEntity;
        UpdateHP();
    }

    public void UpdateHP()
    {
        if (_playerEntity?.EntityData is PlayerEntity playerData)
        {
            for (int i = 0; i < _hearts.Length; i++)
            {
                _hearts[i].SetActive(i < playerData.Hp.Value);
            }
        }
    }

    public void Update()
    {
        UpdateHP();
    }
}
