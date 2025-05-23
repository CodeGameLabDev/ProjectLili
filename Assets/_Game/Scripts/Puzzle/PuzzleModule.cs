using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;
using System;
public class PuzzleModule : MonoBehaviour
{
    [SerializeField] private List<PuzzlePiece> puzzleParcalari = new List<PuzzlePiece>();
    [SerializeField] private Image arkaplanGolgesi;
    [SerializeField] private List<Transform> targets = new List<Transform>(); // Puzzle parçalarının gideceği hedef noktalar
    public static Action OnPuzzleCompleted;
    public static Action<PuzzlePiece> OnPiecePlaced;
    // Start is called before the first frame update
    void Start()
    {

    }

    private void OnEnable()
    {
        OnPiecePlaced += PiecePlaced;
    }

    private void OnDisable()
    {
        OnPiecePlaced -= PiecePlaced;
    }

 

    int puzzlePieceCount = 0;
    private void PiecePlaced(PuzzlePiece puzzlePiece)
    {
        Debug.Log("Puzzle parçası yerleştirildi");
        puzzlePieceCount++;
        if (puzzlePieceCount == puzzleParcalari.Count)
        {
            Debug.Log("Puzzle tamamlandı");
            OnPuzzleCompleted?.Invoke();
        }
    }   
    


    [Button("OyunuBaslat")]
    public void OyunuBaslat()
    {
        puzzlePieceCount = 0;
        if (puzzleParcalari.Count == 0 || targets.Count == 0)
        {
            Debug.LogWarning("Puzzle parçaları veya hedefler tanımlanmamış!");
            return;
        }
        
        if (targets.Count < puzzleParcalari.Count)
        {
            Debug.LogWarning("Hedef nokta sayısı puzzle parça sayısından az!");
            return;
        }
        
        List<Transform> karistirilmisHedefler = new List<Transform>(targets);
        KaristirListe(karistirilmisHedefler);
        
        for (int i = 0; i < puzzleParcalari.Count; i++)
        {
            PuzzlePiece puzzleParcasi = puzzleParcalari[i];
            Transform hedef = karistirilmisHedefler[i];
            
            ParcayiTasi(puzzleParcasi.transform, hedef.position, 1f);


        }
    }
    
    // Liste karıştırma yardımcı fonksiyonu (Fisher-Yates algoritması)
    private void KaristirListe<T>(List<T> liste)
    {
        System.Random rastgele = new System.Random();
        int n = liste.Count;
        
        for (int i = n - 1; i > 0; i--)
        {
            int j = rastgele.Next(0, i + 1);
            T temp = liste[i];
            liste[i] = liste[j];
            liste[j] = temp;
        }
    }
    
    // UniTask ile animasyonlu geçiş için yardımcı fonksiyon
    private async UniTask ParcayiTasi(Transform parca, Vector3 hedefPozisyon, float sure)
    {
        Vector3 baslangicPozisyon = parca.position;
        float gecenSure = 0f;
        
        while (gecenSure < sure)
        {
            parca.position = Vector3.Lerp(baslangicPozisyon, hedefPozisyon, gecenSure / sure);
            gecenSure += Time.deltaTime;
            await UniTask.Yield();
        }

        parca.GetComponent<PuzzlePiece>().SetLocked(false);
        
        parca.position = hedefPozisyon;
    }

    [Button("ResmiGuncelle")]
    public void ResmiGuncelle(Sprite yeniResim)
    {
        // Arkaplan gölgesi resmini güncelle
        if (arkaplanGolgesi != null)
        {
            arkaplanGolgesi.sprite = yeniResim;
            arkaplanGolgesi.raycastTarget = false;
        }
        
        // Her puzzle parçasına resmi alt obje olarak ekle
        foreach (PuzzlePiece puzzleParcasi in puzzleParcalari)
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
                puzzleParcasi.gameObject.AddComponent<Mask>();
            }
        }
    }
}
