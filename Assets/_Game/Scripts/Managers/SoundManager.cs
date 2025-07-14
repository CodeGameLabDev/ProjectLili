using UnityEngine;
using Cysharp.Threading.Tasks;

/// <summary>
/// Ses yönetimi için Singleton SoundManager sınıfı
/// </summary>
public class SoundManager : Singleton<SoundManager>
{
    [Header("Sound Settings")]
    [SerializeField] private AudioSource audioSourceSingle;
    [SerializeField] private float defaultVolume = 1f;
    
    protected override void InitSingleton()
    {
        base.InitSingleton();
        
        // AudioSource yoksa oluştur
        if (audioSourceSingle == null)
        {
            audioSourceSingle = GetComponent<AudioSource>();
            if (audioSourceSingle == null)
            {
                audioSourceSingle = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    
    /// <summary>
    /// Tek seferlik ses çalar
    /// </summary>
    /// <param name="audioClip">Çalınacak ses dosyası</param>
    /// <param name="loop">Sesin döngüde çalıp çalmayacağı</param>
    /// <param name="volume">Ses seviyesi (0-1 arası)</param>
    /// <param name="delay">Ses çalmadan önce beklenecek süre (saniye)</param>
    public static async void PlaySingle(AudioClip audioClip, bool loop = false, float volume = -1f, float delay = 0f)
    {
        if (audioClip == null)
        {
            Debug.LogWarning("[SoundManager] AudioClip null, ses çalınamıyor!");
            return;
        }
        
        // Instance kontrolü
        if (Instance == null)
        {
            Debug.LogError("[SoundManager] SoundManager instance bulunamadı!");
            return;
        }
        
        // Delay varsa bekle
        if (delay > 0f)
        {
            await UniTask.Delay((int)(delay * 1000)); // milisaniye cinsinden
        }
        
        // Volume default değeri kontrolü
        if (volume < 0f)
        {
            volume = Instance.defaultVolume;
        }
        
        // Volume sınırlarını kontrol et
        volume = Mathf.Clamp01(volume);
        
        // AudioSource ayarları
        Instance.audioSourceSingle.clip = audioClip;
        Instance.audioSourceSingle.loop = loop;
        Instance.audioSourceSingle.volume = volume;
        
        // Sesi çal
        Instance.audioSourceSingle.Play();
        
        Debug.Log($"[SoundManager] Ses çalınıyor: {audioClip.name}, Loop: {loop}, Volume: {volume}, Delay: {delay}s");
    }
    
    /// <summary>
    /// Çalan sesi durdurur
    /// </summary>
    public static void StopSingle()
    {
        if (Instance != null && Instance.audioSourceSingle != null)
        {
            Instance.audioSourceSingle.Stop();
        }
    }
    
    /// <summary>
    /// Sesi duraklatır
    /// </summary>
    public static void Pause()
    {
        if (Instance != null && Instance.audioSourceSingle != null)
        {
            Instance.audioSourceSingle.Pause();
        }
    }
    
    /// <summary>
    /// Duraklatılan sesi devam ettirir
    /// </summary>
    public static void Resume()
    {
        if (Instance != null && Instance.audioSourceSingle != null)
        {
            Instance.audioSourceSingle.UnPause();
        }
    }
    
    /// <summary>
    /// Ses seviyesini ayarlar
    /// </summary>
    /// <param name="volume">Yeni ses seviyesi (0-1 arası)</param>
    public static void SetVolume(float volume)
    {
        if (Instance != null && Instance.audioSourceSingle != null)
        {
            Instance.audioSourceSingle.volume = Mathf.Clamp01(volume);
        }
    }
    
    /// <summary>
    /// Şu anda ses çalıp çalmadığını kontrol eder
    /// </summary>
    /// <returns>Ses çalıyorsa true</returns>
    public static bool IsPlaying()
    {
        return Instance != null && Instance.audioSourceSingle != null && Instance.audioSourceSingle.isPlaying;
    }
} 