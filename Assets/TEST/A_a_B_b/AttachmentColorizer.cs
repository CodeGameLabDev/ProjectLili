using Spine;
using Spine.Unity;
using UnityEngine;

public class AttachmentColorizer : MonoBehaviour
{
    [Header("Test - Genel Renk")]
    public Color globalTestColor = Color.red;
    public bool useGlobalColor = false;
    
    [Header("Attachment Colors")]
    public Color aLowerColor = Color.white;
    public Color eyeWhiteLeftColor = Color.white;
    public Color eyeWhiteRightColor = Color.white;
    public Color eyeBlackLeftColor = Color.white; // Değişmesi için beyaz dışında bir renk yapacağız
    public Color eyeBlackRightColor = Color.white;
    public Color eyeLightLeftColor = Color.white;
    public Color eyeLightRightColor = Color.white;
    public Color eyelidMidLeftColor = Color.red; // Test için kırmızı
    public Color eyelidMidRightColor = Color.blue; // Test için mavi
    public Color eyeFrameLeftColor = Color.green; // Test için yeşil
    public Color eyeFrameRightColor = Color.yellow; // Test için sarı
    
    private SkeletonGraphic _skeletonGraphic;

    private void Awake()
    {
        _skeletonGraphic = GetComponent<SkeletonGraphic>();
    }

    private void Start()
    {
        Debug.Log("AttachmentColorizer Start() çağrıldı!");
        ApplyRandomColorsToAttachments();
    }
    
    private void Update()
    {
        // SPACE tuşuna basıldığında renkleri uygula - test için
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("SPACE tuşuna basıldı - renkleri uyguluyorum!");
            ApplyColorsDirectly();
        }
    }

    public void ApplyRandomColorsToAttachments()
    {
        Debug.Log("ApplyRandomColorsToAttachments başladı!");
        
        if (_skeletonGraphic == null) 
        {
            Debug.LogError("SkeletonGraphic component bulunamadı!");
            return;
        }

        var skeleton = _skeletonGraphic.Skeleton;
        if (skeleton == null)
        {
            Debug.LogError("Skeleton null!");
            return;
        }
        
        var skin = skeleton.Skin;
        if (skin == null)
        {
            Debug.LogWarning("Skin not found!");
            return;
        }
        
        Debug.Log($"Skin bulundu: {skin.Name}, Slot sayısı: {skeleton.Slots.Count}");

        // Tüm slot'ları dolaş ve direkt slot rengini değiştir
        var slots = skeleton.Slots;
        for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
        {
            var slot = slots.Items[slotIndex];
            
            // Slot adına göre renk belirle
            Color targetColor = GetColorForSlot(slot.Data.Name, "");
            
            // Slot rengini direkt değiştir - bu daha etkili!
            slot.SetColor(targetColor);
            
            Debug.Log($"Slot '{slot.Data.Name}' rengini değiştirdi -> Color: #{ColorUtility.ToHtmlStringRGB(targetColor)}");
        }

        // Skeleton'u güncelle ve force rebuild
        skeleton.SetSlotsToSetupPose();
        _skeletonGraphic.Update();
        _skeletonGraphic.LateUpdate();
        _skeletonGraphic.SetMaterialDirty();
    }
    
    private Color GetColorForSlot(string slotName, string attachmentName)
    {
        // Slot adına göre uygun rengi döndür
        switch (slotName.ToLower())
        {
            case "a_lower":
                return aLowerColor;
            case "eye_white_l_a_lower":
                return eyeWhiteLeftColor;
            case "eye_white_r_a_lower":
                return eyeWhiteRightColor;
            case "eyeblack5":
            case "eyeblack_r_a_lower":
                return slotName.Contains("_r_") ? eyeBlackRightColor : eyeBlackLeftColor;
            case "eyelight5":
            case "eyelight_l_a_lower":
                return slotName.Contains("_r_") ? eyeLightRightColor : eyeLightLeftColor;
            case "eyelid_mid_l_a_lower":
                return eyelidMidLeftColor;
            case "eyelid_mid_r_a_lower":
                return eyelidMidRightColor;
            case "eye_frame_l_a_lower":
                return eyeFrameLeftColor;
            case "eye_frame_r_a_lower":
                return eyeFrameRightColor;
            default:
                                 return Color.white; // Default renk
         }
     }
     
     private void ApplyColorsDirectly()
     {
         if (_skeletonGraphic == null) return;
         
         if (useGlobalColor)
         {
             // Genel renk testi - tüm objeyi boyar
             _skeletonGraphic.color = globalTestColor;
             return;
         }
         
         var skeleton = _skeletonGraphic.Skeleton;
         if (skeleton == null) return;
         
         // ZORLA GÖRSEL GÜNCELLEME İÇİN DAHA AGRESİF YAKLAŞIM
         
         // Her slot için renk uygula
         var slots = skeleton.Slots;
         for (int i = 0; i < slots.Count; i++)
         {
             var slot = slots.Items[i];
             Color targetColor = GetColorForSlot(slot.Data.Name, "");
             
             Debug.Log($"DEBUG: Slot '{slot.Data.Name}' için GetColorForSlot döndürüyor: {targetColor}");
             
             // Eğer beyaz değilse (özelleştirilmişse) rengi uygula
             if (targetColor != Color.white)
             {
                 // Önce slot'u sıfırla
                 slot.SetColor(Color.white);
                 
                 // Sonra hedef rengi uygula
                 slot.SetColor(targetColor);
                 
                 // Alpha değerini de zorla ayarla
                 slot.A = targetColor.a;
                 slot.R = targetColor.r;
                 slot.G = targetColor.g;
                 slot.B = targetColor.b;
                 
                 Debug.Log($"UYGULANDI: Slot '{slot.Data.Name}' -> Renk: {targetColor}");
             }
         }
         
         // ZORLA YENİDEN RENDER ET
         skeleton.SetSlotsToSetupPose();
         
         // SkeletonGraphic'i tamamen yeniden oluştur
         _skeletonGraphic.SetMaterialDirty();
         _skeletonGraphic.SetVerticesDirty();
         _skeletonGraphic.enabled = false;
         _skeletonGraphic.enabled = true;
         
         // Canvas'ı da zorla güncelle
         var canvas = _skeletonGraphic.GetComponentInParent<Canvas>();
         if (canvas != null)
         {
             canvas.enabled = false;
             canvas.enabled = true;
         }
         
         Debug.Log("ZORLA YENİDEN RENDER YAPILDI!");
     }
     
     [System.Serializable]
     public class ColorTest
     {
         public string slotName;
         public Color color = Color.red;
     }
     
     [Header("Manuel Test")]
     public ColorTest[] testColors = new ColorTest[]
     {
         new ColorTest { slotName = "eyelid_mid_l_a_lower", color = Color.red },
         new ColorTest { slotName = "eyelid_mid_r_a_lower", color = Color.blue },
         new ColorTest { slotName = "eye_frame_l_a_lower", color = Color.green },
         new ColorTest { slotName = "eye_frame_r_a_lower", color = Color.yellow }
     };
     
     [ContextMenu("TEST - Manuel Renk Uygula")]
     public void TestManualColors()
     {
         if (_skeletonGraphic == null || _skeletonGraphic.Skeleton == null) return;
         
         var skeleton = _skeletonGraphic.Skeleton;
         var slots = skeleton.Slots;
         
         Debug.Log("=== MANUEL RENK TESTI BAŞLADI ===");
         
         for (int i = 0; i < testColors.Length; i++)
         {
             var test = testColors[i];
             
             // Slot'u bul
             for (int j = 0; j < slots.Count; j++)
             {
                 var slot = slots.Items[j];
                 if (slot.Data.Name.ToLower() == test.slotName.ToLower())
                 {
                     Debug.Log($"Slot '{slot.Data.Name}' bulundu! Renk uygulanıyor: {test.color}");
                     
                     // Rengi uygula - her yöntemi dene
                     slot.SetColor(test.color);
                     slot.R = test.color.r;
                     slot.G = test.color.g;
                     slot.B = test.color.b;
                     slot.A = test.color.a;
                     
                     break;
                 }
             }
         }
         
         // Güncelle
         skeleton.SetSlotsToSetupPose();
         _skeletonGraphic.SetMaterialDirty();
         _skeletonGraphic.SetVerticesDirty();
         
         // Component'i yeniden aktifleştir
         StartCoroutine(RefreshComponent());
         
         Debug.Log("=== MANUEL RENK TESTI BİTTİ ===");
     }
     
     [ContextMenu("TEST - Material Renk Değiştir")]
     public void TestMaterialColor()
     {
         if (_skeletonGraphic == null) return;
         
         Debug.Log("=== MATERIAL RENK TESTI BAŞLADI ===");
         
         // SkeletonGraphic'in color property'sini değiştir
         _skeletonGraphic.color = Color.red;
         Debug.Log("SkeletonGraphic.color = Red");
         
         // Material'i klonla ve rengini değiştir
         var material = _skeletonGraphic.material;
         if (material != null)
         {
             var newMaterial = new Material(material);
             newMaterial.color = Color.red;
             _skeletonGraphic.material = newMaterial;
             Debug.Log("Material rengi değiştirildi");
         }
         
         // Renderer component'ini kontrol et
         var renderer = _skeletonGraphic.GetComponent<CanvasRenderer>();
         if (renderer != null)
         {
             renderer.SetColor(Color.red);
             Debug.Log("CanvasRenderer rengi değiştirildi");
         }
         
         Debug.Log("=== MATERIAL RENK TESTI BİTTİ ===");
     }
     
     [ContextMenu("TEST - Tüm Sistemleri Dene")]
     public void TestAllSystems()
     {
         Debug.Log("=== TÜM SİSTEMLER TESTİ ===");
         
         // 1. Global renk
         _skeletonGraphic.color = Color.blue;
         Debug.Log("1. Global color = Blue");
         
         // 2. Slot renkleri
         var skeleton = _skeletonGraphic.Skeleton;
         if (skeleton != null)
         {
             var slots = skeleton.Slots;
             for (int i = 0; i < slots.Count; i++)
             {
                 var slot = slots.Items[i];
                 slot.SetColor(Color.green);
                 Debug.Log($"2. Slot '{slot.Data.Name}' = Green");
             }
         }
         
         // 3. Material
         var material = _skeletonGraphic.material;
         if (material != null)
         {
             material.color = Color.yellow;
             Debug.Log("3. Material = Yellow");
         }
         
         // 4. Force update
         _skeletonGraphic.SetMaterialDirty();
         _skeletonGraphic.SetVerticesDirty();
         
         Debug.Log("=== TEST BİTTİ ===");
     }
     
     private System.Collections.IEnumerator RefreshComponent()
     {
         _skeletonGraphic.enabled = false;
         yield return null;
         _skeletonGraphic.enabled = true;
         yield return null;
         _skeletonGraphic.SetMaterialDirty();
         _skeletonGraphic.SetVerticesDirty();
     }
 }
