using UnityEngine;
using System.Collections.Generic;

public class ResetObjects : MonoBehaviour
{
    [Header("Sıfırlanacak Objeler")]
    public List<GameObject> objeler = new List<GameObject>();
    
    void OnDisable()
    {
        // Listedeki tüm objeleri aktif et
        foreach (GameObject obj in objeler)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        
        Debug.Log($"ResetObjects: {objeler.Count} adet obje aktif edildi.");
    }
} 