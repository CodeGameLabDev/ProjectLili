using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PuzzleModule : MonoBehaviour
{
    [SerializeField] private List<GameObject> puzzleParcalari = new List<GameObject>();
    [SerializeField] private Image arkaplanGolgesi;
    [SerializeField] private Sprite puzzleResmi;
    
    // Start is called before the first frame update
    void Start()
    {
        if (puzzleResmi != null)
        {
            ResmiGuncelle(puzzleResmi);
        }
    }

    public void ResmiGuncelle(Sprite yeniResim)
    {
        // Arkaplan gölgesi resmini güncelle
        if (arkaplanGolgesi != null)
        {
            arkaplanGolgesi.sprite = yeniResim;
            arkaplanGolgesi.raycastTarget = false;
        }
        
        // Her puzzle parçasına resmi alt obje olarak ekle
        foreach (GameObject puzzleParcasi in puzzleParcalari)
        {
            // Puzzle parçasının RectTransform'ını al
            RectTransform parcaRect = puzzleParcasi.GetComponent<RectTransform>();
            if (parcaRect == null) continue;
            
            // Eğer zaten bir resim alt objesi varsa, onu kaldır
            Transform mevcut = puzzleParcasi.transform.Find("PuzzleResmi");
            if (mevcut != null)
            {
                Destroy(mevcut.gameObject);
            }
            
            // Yeni bir Image objesi oluştur ve puzzle parçasının alt objesi olarak ekle
            GameObject resimObjesi = new GameObject("PuzzleResmi");
            
            // Önce parçanın child'ı olarak ayarla
            resimObjesi.transform.SetParent(puzzleParcasi.transform, false);
            
            // Image bileşeni ekle ve resmi ayarla
            Image resimBileseni = resimObjesi.AddComponent<Image>();
            resimBileseni.sprite = yeniResim;
            resimBileseni.raycastTarget = false; // Etkileşimi engellemek için
            
            // RectTransform'ı al
            RectTransform resimRect = resimObjesi.GetComponent<RectTransform>();
            RectTransform arkaplanRect = arkaplanGolgesi.GetComponent<RectTransform>();
            
            // RectTransform değerlerini arkaplan gölgesinin değerleriyle aynı yap
            resimRect.anchorMin = arkaplanRect.anchorMin;
            resimRect.anchorMax = arkaplanRect.anchorMax;
            resimRect.sizeDelta = arkaplanRect.sizeDelta;
            resimRect.pivot = arkaplanRect.pivot;
            resimRect.anchoredPosition = arkaplanRect.anchoredPosition;
            
            // Pozisyon, döndürme ve ölçek değerlerini de ayarla
            resimObjesi.transform.position = arkaplanGolgesi.transform.position;
            resimObjesi.transform.rotation = arkaplanGolgesi.transform.rotation;
            resimObjesi.transform.localScale = arkaplanGolgesi.transform.localScale;
            
            // Mask bileşeninin puzzle parçasında olduğundan emin ol
            if (puzzleParcasi.GetComponent<Mask>() == null)
            {
                puzzleParcasi.AddComponent<Mask>();
            }
        }
    }
    
    public void ResmiAyarla(Sprite resim)
    {
        puzzleResmi = resim;
        ResmiGuncelle(resim);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
