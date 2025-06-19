using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class FindObject{
    public GameObject obj;
    public GameObject uiobj;
    [HideInInspector] public bool isFound = false;
}

// Asset'leri ve event'leri yöneten class
public class FindObjectAssetHolder : MonoBehaviour
{
    [Header("Find Object Assets")]
    public List<FindObject> findObjects;
    
    [Header("Events")]
    public UnityEvent OnGameWon;
    public UnityEvent OnObjectFound;
    
    private int foundObjectsCount = 0;
    
    public void OnObjectFoundCallback()
    {
        foundObjectsCount++;
        OnObjectFound?.Invoke();
        
        Debug.Log($"[FindObjectAssetHolder] Obje bulundu! {foundObjectsCount}/{findObjects.Count}");
        
        // Tüm objeler bulundu mu kontrol et
        if (foundObjectsCount >= findObjects.Count)
        {
            Debug.Log("[FindObjectAssetHolder] Tüm objeler bulundu! GameWon tetikleniyor...");
            GameWon();
        }
    }
    
    void GameWon()
    {
        Debug.Log("[FindObjectAssetHolder] GameWon event'i tetiklendi!");
        OnGameWon?.Invoke();
    }
    
    public float GetProgressPercentage()
    {
        if (findObjects.Count == 0) return 0f;
        return ((float)foundObjectsCount / findObjects.Count) * 100f;
    }
    
    public void ResetProgress()
    {
        foundObjectsCount = 0;
        foreach (var findObj in findObjects)
        {
            findObj.isFound = false;
        }
        Debug.Log("[FindObjectAssetHolder] Progress sıfırlandı!");
    }
    
    public int GetFoundObjectsCount()
    {
        return foundObjectsCount;
    }
    
    public int GetTotalObjectsCount()
    {
        return findObjects.Count;
    }
} 