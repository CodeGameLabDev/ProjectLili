using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Cysharp.Threading.Tasks;
using System;

public class PuzzleModule : MonoBehaviour
{
    [Header("Puzzle System")]
    [SerializeField] private string[] puzzleImageNames = new string[] { 
        "Puzzle_1", "Puzzle_2", "Puzzle_3", "Puzzle_4", "Puzzle_5", "Puzzle_6", "Puzzle_7", "Puzzle_8", "Puzzle_9", "Puzzle_10"
    };
    private int currentPuzzleIndex = 0;
    private bool hasStarted = false;

    [Header("Puzzle Components")]
    [SerializeField] private List<PuzzlePiece> puzzleParcalari = new List<PuzzlePiece>();
    [SerializeField] private Image arkaplanGolgesi;
    [SerializeField] private List<Transform> targets = new List<Transform>(); // Puzzle parçalarının gideceği hedef noktalar
    
    [Header("Debug Info")]
    [ReadOnly] public string currentPuzzleName;
    [ReadOnly] public int debugCurrentPuzzleIndex;
    
    public static Action OnPuzzleCompleted;
    public static Action<PuzzlePiece> OnPiecePlaced;

    private const string PUZZLE_PROGRESS_KEY = "CurrentPuzzleIndex";
    private int puzzlePieceCount = 0;

    void Start()
    {
        Debug.Log($"[PuzzleModule] Start - currentPuzzleIndex (before LoadProgress): {currentPuzzleIndex}");
        LoadProgress();
        Debug.Log($"[PuzzleModule] Start - currentPuzzleIndex (after LoadProgress): {currentPuzzleIndex}");
        UpdateCurrentPuzzleDisplay();
        hasStarted = true;
    }

    private void OnEnable()
    {
        OnPiecePlaced += PiecePlaced;
        OnPuzzleCompleted += OnPuzzleCompletedHandler;
    }

    private void OnDisable()
    {
        OnPiecePlaced -= PiecePlaced;
        OnPuzzleCompleted -= OnPuzzleCompletedHandler;
    }

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

    private void OnPuzzleCompletedHandler()
    {
        Debug.Log($"Puzzle tamamlandı: {puzzleImageNames[currentPuzzleIndex]}");
        
        // Bir sonraki puzzle'a geç
        currentPuzzleIndex++;
        SaveProgress();
        UpdateCurrentPuzzleDisplay();
        
        if (currentPuzzleIndex < puzzleImageNames.Length)
        {
            Debug.Log($"Bir sonraki puzzle hazır: {puzzleImageNames[currentPuzzleIndex]}");
        }
        else
        {
            Debug.Log("Tüm puzzle'lar tamamlandı! Tebrikler!");
        }
    }

    [Button("Oyunu Başlat")]
    public void OyunuBaslat()
    {
        Debug.Log($"[PuzzleModule] OyunuBaslat called - hasStarted: {hasStarted}, currentPuzzleIndex: {currentPuzzleIndex}");
        
        // Eğer Start() henüz çalışmamışsa
        if (!hasStarted)
        {
            Debug.Log("[PuzzleModule] Start() henüz çalışmamış, LoadProgress() manuel olarak çağrılıyor...");
            LoadProgress();
            UpdateCurrentPuzzleDisplay();
            hasStarted = true;
        }

        // Mevcut puzzle resmini yükle
        LoadCurrentPuzzleImage();
        
        // Puzzle parçalarını karıştır ve başlat
        StartPuzzleGame();
    }

    private void LoadCurrentPuzzleImage()
    {
        if (currentPuzzleIndex >= puzzleImageNames.Length)
        {
            Debug.Log("Tüm puzzle'lar tamamlandı! Başa dönüyor...");
            currentPuzzleIndex = 0;
            SaveProgress();
        }

        string puzzleName = puzzleImageNames[currentPuzzleIndex];
        string resourcePath = $"Puzzle/{puzzleName}";

        Debug.Log($"[PuzzleModule] Yüklenecek puzzle: {puzzleName}, Resource Path: {resourcePath}");

        Sprite puzzleSprite = Resources.Load<Sprite>(resourcePath);
        
        if (puzzleSprite == null)
        {
            Debug.LogError($"Puzzle resmi bulunamadı: {resourcePath}. Resources/Puzzle/ klasöründe {puzzleName}.png/jpg dosyası olduğundan emin olun.");
            return;
        }

        // Resmi güncelle
        ResmiGuncelle(puzzleSprite);
        
        Debug.Log($"Puzzle yüklendi: {puzzleName} (İndeks: {currentPuzzleIndex})");
    }

    private void StartPuzzleGame()
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

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(PUZZLE_PROGRESS_KEY, currentPuzzleIndex);
        PlayerPrefs.Save();
        Debug.Log($"Puzzle ilerlemesi kaydedildi: {currentPuzzleIndex}");
    }

    private void LoadProgress()
    {
        currentPuzzleIndex = PlayerPrefs.GetInt(PUZZLE_PROGRESS_KEY, 0);
        Debug.Log($"Puzzle ilerlemesi yüklendi: {currentPuzzleIndex}");
    }

    private void UpdateCurrentPuzzleDisplay()
    {
        if (currentPuzzleIndex < puzzleImageNames.Length)
        {
            currentPuzzleName = puzzleImageNames[currentPuzzleIndex];
        }
        else
        {
            currentPuzzleName = "Tamamlandı";
        }
        debugCurrentPuzzleIndex = currentPuzzleIndex;
        Debug.Log($"[PuzzleModule] UpdateCurrentPuzzleDisplay - currentPuzzleName: {currentPuzzleName}, currentPuzzleIndex: {currentPuzzleIndex}");
    }

    [Button("Reset Progress")]
    public void ResetProgress()
    {
        currentPuzzleIndex = 0;
        SaveProgress();
        UpdateCurrentPuzzleDisplay();
        Debug.Log("Puzzle ilerlemesi sıfırlandı!");
    }

    [Button("Skip Current Puzzle")]
    public void SkipCurrentPuzzle()
    {
        OnPuzzleCompleted?.Invoke();
    }

    public int GetCurrentPuzzleIndex()
    {
        return currentPuzzleIndex;
    }

    public string GetCurrentPuzzleName()
    {
        return currentPuzzleIndex < puzzleImageNames.Length ? puzzleImageNames[currentPuzzleIndex] : "Tamamlandı";
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

    private void ResmiGuncelle(Sprite yeniResim)
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
