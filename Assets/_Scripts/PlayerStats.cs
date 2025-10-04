using TMPro;
using UnityEngine;

public class PlayerStats : MonoBehaviour,IDamagable
{
    // Start is called before the first frame update

    float dmgreduction;
    //public Transform playerTransform;
    public float playerHp;
    public float maxPlayerHp;
    public float swietosc;
    public bool isDead = false;
    sliderScript swietoscSlid;
    sliderScript hpSlid;
    TextMeshProUGUI hpText;
    

    public void PlayerDamaged(int damage)
    {
        if (dmgreduction > 0)
        {
            damage -= (int)(damage * dmgreduction);
        }
        playerHp -= damage;
    }

    public void DamageReduction(float reductionProcentage)
    {
        dmgreduction = reductionProcentage;
    }

    public void Damaged(float damage)
    {
        playerHp -= damage;
    }

    public void Save(ref PlayerSaveData saveData)
    {
        saveData.position = transform.position;
        saveData.playerHp = playerHp;
    }

    public void Load(PlayerSaveData saveData)
    {
        transform.position = saveData.position;
        playerHp = saveData.playerHp;   
    }
}
[System.Serializable]

public struct PlayerSaveData
{
    public Vector3 position;
    public Quaternion rotation;
    public float playerHp;
}