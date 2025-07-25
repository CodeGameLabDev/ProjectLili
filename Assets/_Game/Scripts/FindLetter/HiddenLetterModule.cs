using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Sirenix.OdinInspector;
using DG.Tweening;

namespace HiddenLetterGame
{
    /// <summary>
    /// FindObject mantığının harf versiyonu. Sahnede gizlenmiş harfleri bulup yukarıdaki slotlara yerleştirir.
    /// </summary>
    public class HiddenLetterModule : MonoBehaviour, IGameLevel
    {
        // ---- IGameLevel implementation ----
        public event System.Action OnGameStart;
        public event System.Action OnGameComplete;

        private bool isCompleted = false;
        public bool IsCompleted => isCompleted;
        public string LevelName => targetLetters;

        // Flag to prevent double-start
        private bool hasStartedLevel = false;

        [Header("Kelime Ayarları")]
        [Tooltip("Hedef kelime. Harfler sıralı olarak slotlara yerleşir.")]
        public string targetLetters;

        [Header("Letter Holder Settings")]
        [Tooltip("Sehirmedeki saklı (tıklanabilir) harfler için prefab")] public GameObject letterHolderClickablePrefab;
        [Tooltip("Ekrandaki sabit UI harfleri için prefab")] public GameObject letterHolderUIPrefab;
        // Çalışma sırasında instantiate edilen holder'ları takip etmek için
        private readonly List<GameObject> spawnedLetterHolders = new List<GameObject>();

        [Header("Progress Bar")]
        public Image progressBar;

        [Header("Animasyon Ayarları")]
        public float moveToSlotDuration = 0.6f;
        public AnimationCurve moveEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Shake Ayarları (Bulunmayan harfler için isteğe bağlı)")]
        public bool enableShake = true;
        public float shakeIntensity = 0.1f;
        public float shakeSpeed = 5f;
        public float idleTimeBeforeShake = 3f;

        [Header("Letter Move Animation")]
        public float letterMoveDuration = 0.8f;
        public Ease letterMoveEase = Ease.OutQuart;
        [Range(0.1f,1f)] public float letterEndScale = 0.4f;
        public float shadowHighlightScale = 1.3f;
        public float shadowHighlightDuration = 0.3f;

        // Overlay canvas for floating letters (similar to WordBaloon)
        [HideInInspector] public Canvas overlayCanvas;

        [Header("Debug")]
        public bool showDebugMessages = true;
        [ReadOnly] public int placedCount = 0;

        [Header("Level System")]
        [Tooltip("Resources/FindLetters klasöründeki prefab isimleri")] public string[] levelPrefabNames =
            {"Level_1", "Level_2", "Level_3", "Level_4", "Level_5"};
        [SerializeField] private Transform levelParent;

        // Internal state
        private HiddenLetterGame.HiddenLetterAssetHolder assetHolder;
        private Dictionary<GameObject, HiddenLetterGame.HiddenLetter> letterMap;
        private Dictionary<GameObject, Vector3> originalPositions;
        private Dictionary<GameObject, Transform> originalParents;
        private Dictionary<GameObject, Coroutine> shakeCoroutines;
        private float lastInteractionTime;
        private int currentLevelIndex = 0;
        private const string LEVEL_PROGRESS_KEY = "HiddenLetterLevelIndex";

        [Header("Auto Slot Generation")]
        public bool autoGenerateSlots = true;
        public Transform autoSlotParent;
        public float autoSlotSpacing = 120f;
        public float autoSlotSize = 100f;

        [Header("Letter Database")]
        public LetterPathDatabase letterDatabase;

        // Runtime slot list (either from assetHolder or generated)
        private List<RectTransform> currentSlots = new List<RectTransform>();
        // For matching found letters to their correct shadow slots
        private List<char> slotLetterChars = new List<char>();
        private List<bool> slotFilled = new List<bool>();

        // ------- Helper methods copied/adapted from WordBaloon --------
        private float GetLetterHolderSize()
        {
            // If prefab has RectTransform use its max size, else fallback to autoSlotSize
            if (letterHolderUIPrefab == null) return autoSlotSize;
            var rect = letterHolderUIPrefab.GetComponent<RectTransform>();
            if (rect != null)
            {
                return Mathf.Max(rect.sizeDelta.x, rect.sizeDelta.y);
            }
            return autoSlotSize;
        }

        private void SetupLetterComponent(Transform letterTransform, Sprite sprite, string type)
        {
            if (letterTransform == null || sprite == null) return;

            var rect = letterTransform.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = letterTransform.gameObject.AddComponent<RectTransform>();
            }
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;

            var image = letterTransform.GetComponent<Image>();
            if (image == null)
            {
                image = letterTransform.gameObject.AddComponent<Image>();
            }
            image.sprite = sprite;
            image.SetNativeSize();

            // Prefer runtime holder size if available
            float holderSize = 0f;
            var parentRt = letterTransform.parent as RectTransform;
            if (parentRt != null)
            {
                holderSize = Mathf.Max(parentRt.sizeDelta.x, parentRt.sizeDelta.y);
            }
            if (holderSize <= 0f)
            {
                holderSize = GetLetterHolderSize();
            }
            if (holderSize > 0)
            {
                Vector2 spriteSize = image.sprite.rect.size;
                float scale = Mathf.Min(holderSize / spriteSize.x, holderSize / spriteSize.y);
                rect.sizeDelta = spriteSize * scale;
            }

            if (type == "Shadow")
            {
                image.color = new Color(1,1,1,0.3f);
                letterTransform.gameObject.SetActive(true);
            }
            else // Sprite
            {
                // Ensure full opacity
                Color col = image.color;
                image.color = new Color(col.r, col.g, col.b, 1f);
                // Start hidden; will be enabled when placed
                letterTransform.gameObject.SetActive(false);
            }
        }

        private void SetupSpineComponent(Transform spineTransform, GameObject spinePrefab)
        {
            if (spineTransform == null || spinePrefab == null) return;

            var rect = spineTransform.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = spineTransform.gameObject.AddComponent<RectTransform>();
            }
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            // anchoredPosition preserved; also keep original localScale (do NOT force to 1)

            GameObject spineInstance = Instantiate(spinePrefab, spineTransform);
            spineInstance.transform.localPosition = Vector3.zero;
            spineInstance.transform.localRotation = Quaternion.identity;
            spineInstance.transform.localScale = Vector3.one;

            // Prefer runtime holder size from parent
            float holderSize = 0f;
            var parentRt2 = spineTransform.parent as RectTransform;
            if (parentRt2 != null)
                holderSize = Mathf.Max(parentRt2.sizeDelta.x, parentRt2.sizeDelta.y);
            if (holderSize <= 0f)
                holderSize = GetLetterHolderSize();
            var spineRect = spineInstance.GetComponent<RectTransform>();
            if (spineRect != null && holderSize > 0)
            {
                Vector2 spineSize = spineRect.sizeDelta;
                float scale = Mathf.Min(holderSize / spineSize.x, holderSize / spineSize.y);
                spineRect.sizeDelta = spineSize * scale;
            }

            spineTransform.gameObject.SetActive(false);
        }

        void Awake()
        {
            LoadProgress();

            // Ensure overlay canvas exists (ScreenSpaceOverlay)
            overlayCanvas = FindObjectsOfType<Canvas>().FirstOrDefault(c => c.renderMode == RenderMode.ScreenSpaceOverlay);
            if (overlayCanvas == null)
            {
                GameObject canvasGO = new GameObject("HiddenLetterOverlayCanvas");
                overlayCanvas = canvasGO.AddComponent<Canvas>();
                overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            overlayCanvas.sortingOrder = 1000;
        }

        // MonoBehaviour Start kept empty; GameManager will call StartGame()
        void Start() {}

        // ---------------- IGameLevel METHODS ----------------
        public void StartGame()
        {
            if (hasStartedLevel) return;

            DetermineTargetLetters();

            placedCount = 0;
            isCompleted = false;

            LoadCurrentLevel();

            OnGameStart?.Invoke();
            hasStartedLevel = true;
        }

        public void CompleteGame()
        {
            if (isCompleted) return;
            isCompleted = true;
            OnGameComplete?.Invoke();
        }

        // ---------------- Level Loading -------------
        void LoadCurrentLevel()
        {
            if (currentLevelIndex >= levelPrefabNames.Length)
            {
                Debug.Log("Tüm level'lar tamamlandı, başa dönülüyor.");
                currentLevelIndex = 0;
            }

            string levelName = levelPrefabNames[currentLevelIndex];
            string resourcePath = $"FindLetters/{levelName}";

            GameObject levelPrefab = Resources.Load<GameObject>(resourcePath);
            if (levelPrefab == null)
            {
                Debug.LogError($"Level prefab'ı bulunamadı: {resourcePath}");
                return;
            }

            // clear previous
            if (levelParent == null)
            {
                Debug.LogWarning("levelParent atanmamış, yeni empty GameObject oluşturulacak");
                levelParent = new GameObject("HiddenLetterLevelParent").transform;
            }
            foreach (Transform child in levelParent) Destroy(child.gameObject);

            GameObject holderInstance = Instantiate(levelPrefab, levelParent);
            assetHolder = holderInstance.GetComponent<HiddenLetterGame.HiddenLetterAssetHolder>();
            if (assetHolder == null)
            {
                Debug.LogError("Level prefab'ında HiddenLetterAssetHolder bulunamadı!");
                return;
            }

            // Subscribe to game won event to notify GameManager
            assetHolder.OnGameWon.RemoveAllListeners();
            assetHolder.OnGameWon.AddListener(() => CompleteGame());

            placedCount = 0;
            assetHolder.ResetProgress();

            // Generate hidden letters inside the level based on target word
            if (assetHolder != null)
            {
                assetHolder.GenerateHiddenLetters(targetLetters, letterHolderClickablePrefab, letterDatabase);
            }

            SpawnLetterHolders();
            InitializeLetters();
            UpdateProgressBar();
            lastInteractionTime = Time.time;
            Debug.Log($"HiddenLetter level yüklendi: {levelName} (index {currentLevelIndex})");
        }

        [Button("Sonraki Level'e Geç")]
        public void NextLevel()
        {
            currentLevelIndex++;
            SaveProgress();
            LoadCurrentLevel();
        }

        void SaveProgress() { PlayerPrefs.SetInt(LEVEL_PROGRESS_KEY, currentLevelIndex); }
        void LoadProgress() { currentLevelIndex = PlayerPrefs.GetInt(LEVEL_PROGRESS_KEY, 0); }

        void Update()
        {
            if (!enableShake) return;

            if (Time.time - lastInteractionTime > idleTimeBeforeShake)
            {
                StartShakingUnfoundLetters();
                lastInteractionTime = Time.time; // sürekli tetiklenmesin
            }
        }

        [Button("Oyunu Başlat", ButtonSizes.Large)]
        [GUIColor(0.4f, 0.8f, 1f)]
        public void OyunuBaslat()
        {
            placedCount = 0;
            assetHolder.ResetProgress();
            InitializeLetters();
            SpawnLetterHolders();
            UpdateProgressBar();
        }

        void InitializeLetters()
        {
            letterMap = new Dictionary<GameObject, HiddenLetterGame.HiddenLetter>();
            originalPositions = new Dictionary<GameObject, Vector3>();
            originalParents = new Dictionary<GameObject, Transform>();
            shakeCoroutines = new Dictionary<GameObject, Coroutine>();

            foreach (var hidden in assetHolder.hiddenLetters)
            {
                if (hidden.obj == null) continue;

                letterMap[hidden.obj] = hidden;
                originalPositions[hidden.obj] = hidden.obj.transform.position;
                originalParents[hidden.obj] = hidden.obj.transform.parent;
                hidden.isFound = false;

                AddClickListener(hidden.obj);
            }
        }

        void AddClickListener(GameObject obj)
        {
            // Önce Button varsa kullan
            var btn = obj.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnLetterClicked(obj));
            }
            else
            {
                // Yoksa ClickableUIObject kullan
                var clickable = obj.GetComponent<ClickableUIObject>();
                if (clickable == null)
                {
                    clickable = obj.AddComponent<ClickableUIObject>();
                }
                clickable.OnClick = () => OnLetterClicked(obj);
            }
        }

        void OnLetterClicked(GameObject clickedObj)
        {
            if (!letterMap.ContainsKey(clickedObj)) return;

            HiddenLetterGame.HiddenLetter hidden = letterMap[clickedObj];
            if (hidden.isFound) return;

            // Kelime uzunluğunu aştıysa ya da slot kalmadıysa ignor
            if (currentSlots == null || placedCount >= currentSlots.Count)
            {
                if (showDebugMessages) Debug.Log("Tüm slotlar dolu, ek harf yoksayılıyor.");
                return;
            }

            // Harfin kelimedeki beklenen harf olup olmadığı kontrolü yapılabilir
            // (isteğe bağlı) Şimdilik her harfi sırayla kabul ediyoruz.

            lastInteractionTime = Time.time;
            StartCoroutine(MoveLetterToSlot(clickedObj));
        }

        IEnumerator MoveLetterToSlot(GameObject obj)
        {
            // Mark as found
            HiddenLetterGame.HiddenLetter hidden = letterMap[obj];
            hidden.isFound = true;

            // Stop shake if running
            if (shakeCoroutines.ContainsKey(obj) && shakeCoroutines[obj] != null)
            {
                StopCoroutine(shakeCoroutines[obj]);
                shakeCoroutines[obj] = null;
                obj.transform.position = originalPositions[obj];
            }

            // Determine target slot index based on letter char and fill status
            char letterChar = hidden.letter;

            int slotIndex = -1;
            for (int i = 0; i < slotLetterChars.Count; i++)
            {
                if (!slotFilled[i] && slotLetterChars[i] == letterChar)
                {
                    slotIndex = i;
                    break;
                }
            }

            // If no exact-case slot found, ignore click (optional feedback)
            if (slotIndex == -1)
            {
                if (showDebugMessages) Debug.Log($"Letter {letterChar} için uygun slot yok veya dolu.");
                hidden.isFound = false; // revert state so can click again
                yield break;
            }

            if (currentSlots == null || currentSlots.Count == 0 || slotIndex == -1 || slotIndex >= currentSlots.Count)
            {
                Debug.LogError("Slot list yetersiz veya boş!");
                yield break;
            }

            RectTransform targetSlot = currentSlots[slotIndex];

            // Move object under overlay canvas for screen-space animation
            Canvas overlay = GetOverlayCanvas();
            RectTransform objRect = obj.GetComponent<RectTransform>();
            if (objRect == null) objRect = obj.AddComponent<RectTransform>();

            // Capture current screen position BEFORE reparenting
            Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(null, obj.transform.position);

            obj.transform.SetParent(overlay.transform, false);

            // Restore the same screen position so animation starts from current location
            objRect.position = startScreen;

            // Convert target slot position to screen point
            Vector2 targetScreen = RectTransformUtility.WorldToScreenPoint(null, targetSlot.transform.position);

            // Tween position & scale
            Sequence seq = DOTween.Sequence();
            seq.Join(objRect.DOMove(targetScreen, letterMoveDuration).SetEase(letterMoveEase));
            Vector3 endScale = Vector3.one * Mathf.Clamp01(letterEndScale);
            seq.Join(objRect.DOScale(endScale, letterMoveDuration).SetEase(Ease.InOutQuad));

            // On complete – attach to slot & finalize
            seq.OnComplete(() =>
            {
                obj.transform.SetParent(targetSlot, false);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localScale = Vector3.one;

                // Ensure letter holder size matches the slot (prevents oversized sprites)
                var objRt = obj.GetComponent<RectTransform>();
                if (objRt != null)
                {
                    objRt.sizeDelta = targetSlot.sizeDelta;
                }

                // Adjust inner child graphics to fit new holder size
                AdjustChildGraphicsSize(objRt);

                // Toggle via FindLetterUIHolder component for clarity and robustness
                if (targetSlot.childCount > 0)
                {
                    Transform letterHolderTf = targetSlot.GetChild(0);
                    var uiHolder = letterHolderTf.GetComponent<FindLetterUIHolder>();
                    if (uiHolder != null)
                    {
                        uiHolder.ShowSpriteHideShadow();
                    }
                }

                // Highlight effect
                HighlightShadow(targetSlot.gameObject);

                // Hide sprite & shadow of moving holder, show spine only (optional)
                var ui = obj.GetComponent<FindLetterUIHolder>();
                if (ui != null)
                {
                    if (ui.spriteObject != null) ui.spriteObject.SetActive(false);
                    if (ui.shadowObject != null) ui.shadowObject.SetActive(false);
                    if (ui.spineObject != null) ui.spineObject.SetActive(true);
                }

                // After transferring, we can deactivate holder to avoid overlap if desired:
                obj.SetActive(false);

                placedCount++;
                slotFilled[slotIndex] = true;
                UpdateProgressBar();
                assetHolder.OnLetterFoundCallback();

                if (assetHolder.GetProgressPercentage() >= 100f)
                {
                    if (showDebugMessages) Debug.Log("🎉 Tüm harfler bulundu!");
                }
            });

            yield return null;
        }

        // Highlight animation identical to WordBaloon
        void HighlightShadow(GameObject shadow)
        {
            Transform spriteLetter = shadow.transform.Find("SpriteLetter");
            Transform shadowLetter = shadow.transform.Find("ShadowLetter");
            if (shadowLetter != null) shadowLetter.gameObject.SetActive(false);
            if (spriteLetter != null) spriteLetter.gameObject.SetActive(true);

            shadow.transform.DOScale(shadowHighlightScale, shadowHighlightDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    shadow.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBounce);
                });
        }

        void UpdateProgressBar()
        {
            if (progressBar == null) return;
            if (assetHolder != null)
                progressBar.fillAmount = placedCount / (float)Mathf.Max(1, assetHolder.hiddenLetters.Count);
        }

        void StartShakingUnfoundLetters()
        {
            List<HiddenLetterGame.HiddenLetter> unfound = new List<HiddenLetterGame.HiddenLetter>();
            if (assetHolder == null) return;
            foreach (var h in assetHolder.hiddenLetters)
            {
                if (!h.isFound && h.obj != null && h.obj.activeInHierarchy)
                {
                    unfound.Add(h);
                }
            }
            if (unfound.Count == 0) return;

            HiddenLetterGame.HiddenLetter selected = unfound[Random.Range(0, unfound.Count)];

            if (!shakeCoroutines.ContainsKey(selected.obj) || shakeCoroutines[selected.obj] == null)
            {
                shakeCoroutines[selected.obj] = StartCoroutine(ShakeObject(selected.obj));
            }
        }

        IEnumerator ShakeObject(GameObject obj)
        {
            Vector3 originalPos = originalPositions[obj];
            while (true)
            {
                float shakeX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
                float shakeY = Mathf.Cos(Time.time * shakeSpeed * 1.1f) * shakeIntensity;
                obj.transform.position = originalPos + new Vector3(shakeX, shakeY, 0);
                yield return null;
            }
        }

        // --- Letter Holder Spawn ---
        void SpawnLetterHolders()
        {
            // Öncekileri temizle
            foreach (var go in spawnedLetterHolders)
            {
                if (go != null) Destroy(go);
            }
            spawnedLetterHolders.Clear();

            currentSlots.Clear();
            slotLetterChars.Clear();
            slotFilled.Clear();

            if (letterHolderUIPrefab == null)
            {
                Debug.LogWarning("letterHolderUIPrefab atanmamış!");
                return;
            }

            // 1) Use existing children of autoSlotParent (if assigned) as slots.
            List<RectTransform> slotSource = null;

            if (autoSlotParent != null && autoSlotParent.childCount > 0)
            {
                slotSource = new List<RectTransform>();
                foreach (Transform child in autoSlotParent)
                {
                    var rt = child as RectTransform ?? child.gameObject.AddComponent<RectTransform>();
                    slotSource.Add(rt);
                }
            }

            // 2) If no slots found and auto generate is enabled, create them dynamically under autoSlotParent
            if ((slotSource == null || slotSource.Count == 0) && autoGenerateSlots)
            {
                // create autoSlotParent if it doesn't exist
                if (autoSlotParent == null)
                {
                    autoSlotParent = new GameObject("AutoSlotParent").AddComponent<RectTransform>();
                    autoSlotParent.SetParent(levelParent ?? transform, false);
                    var rect = autoSlotParent.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 1f);
                    rect.anchorMax = new Vector2(0.5f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(0, -50f);
                }

                string trimmedWord = string.IsNullOrEmpty(targetLetters) ? "" : targetLetters.Replace(" ", "");
                float startX = -((trimmedWord.Length - 1) * autoSlotSpacing) / 2f;

                for (int i = 0; i < trimmedWord.Length; i++)
                {
                    GameObject slotGO = new GameObject($"AutoSlot_{i}", typeof(RectTransform));
                    RectTransform slotRect = slotGO.GetComponent<RectTransform>();
                    slotRect.SetParent(autoSlotParent, false);
                    slotRect.sizeDelta = new Vector2(autoSlotSize, autoSlotSize);
                    slotRect.anchoredPosition = new Vector2(startX + i * autoSlotSpacing, 0);
                    slotSource ??= new List<RectTransform>();
                    slotSource.Add(slotRect);
                }
            }

            if (slotSource == null)
            {
                Debug.LogError("Slot kaynağı bulunamadı. hiddenLetterPositions boş ve autoGenerateSlots kapalı.");
                return;
            }

            for (int idx = 0; idx < slotSource.Count; idx++)
            {
                RectTransform slot = slotSource[idx];
                if (slot == null) continue;

                GameObject holder = Instantiate(letterHolderUIPrefab, slot);
                holder.transform.localPosition = Vector3.zero;

                // Keep holder size in sync with slot (useful when autoSlotSize is tweaked)
                var holderRt = holder.GetComponent<RectTransform>();
                if (holderRt != null)
                {
                    holderRt.sizeDelta = slot.sizeDelta;
                }

                spawnedLetterHolders.Add(holder);

                // Child isimleri hem *Letter hem *Holder varyasyonlarını desteklesin
                string[] spriteNames = {"SpriteLetter", "SpriteHolder"};
                string[] shadowNames = {"ShadowLetter", "ShadowHolder"};
                string[] spineNames  = {"SpineLetter",  "SpineHolder"};

                Transform spriteTf = spriteNames.Select(n => holder.transform.Find(n)).FirstOrDefault(t => t != null);
                Transform shadowTf = shadowNames.Select(n => holder.transform.Find(n)).FirstOrDefault(t => t != null);
                Transform spineTf  = spineNames.Select(n => holder.transform.Find(n)).FirstOrDefault(t => t != null);

                // Load letter data if possible
                char letterChar = '\0';
                if (!string.IsNullOrEmpty(targetLetters))
                {
                    string trimmedWord = targetLetters.Replace(" ", "");
                    if (idx < trimmedWord.Length) letterChar = trimmedWord[idx];
                }

                slotLetterChars.Add(letterChar);
                slotFilled.Add(false);

                if (letterDatabase != null && letterChar != '\0')
                {
                    var letterData = letterDatabase.LoadLetterData(letterChar.ToString());
                    if (letterData != null)
                    {
                        // setup components
                        SetupLetterComponent(spriteTf, letterData.letterSprite, "Sprite");
                        SetupLetterComponent(shadowTf, letterData.letterShadowSprite, "Shadow");
                        if (letterData.prefab != null)
                            SetupSpineComponent(spineTf, letterData.prefab);
                    }
                }

                // Varsayılan görünürlük: shadow açık, sprite kapalı
                if (shadowTf != null) shadowTf.gameObject.SetActive(true);
                if (spriteTf != null) spriteTf.gameObject.SetActive(false);

                currentSlots.Add(slot);
            }

            // if we used assetHolder list directly, also populate currentSlots
            if (currentSlots.Count == 0 && slotSource != null)
            {
                currentSlots.AddRange(slotSource);
            }
        }

        public Canvas GetOverlayCanvas() => overlayCanvas;

        // Helper to rescale inner Sprite/Spine to holder size
        void AdjustChildGraphicsSize(RectTransform holderRt)
        {
            float holderSize = Mathf.Max(holderRt.sizeDelta.x, holderRt.sizeDelta.y);
            if (holderSize <= 0f) return;

            string[] childNames = {"SpriteLetter", "ShadowLetter"};
            foreach (string n in childNames)
            {
                var tf = holderRt.Find(n) as RectTransform;
                if (tf == null) continue;
                var img = tf.GetComponent<Image>();
                if (img == null || img.sprite == null) continue;
                Vector2 spriteSize = img.sprite.rect.size;
                float scale = Mathf.Min(holderSize / spriteSize.x, holderSize / spriteSize.y);
                tf.sizeDelta = spriteSize * scale;
            }

            // SpineLetter scaling
            var spineTf = holderRt.Find("SpineLetter") as RectTransform;
            if (spineTf != null && spineTf.childCount > 0)
            {
                var childRt = spineTf.GetChild(0).GetComponent<RectTransform>();
                if (childRt != null)
                {
                    Vector2 spineSize = childRt.sizeDelta;
                    float scale = Mathf.Min(holderSize / spineSize.x, holderSize / spineSize.y);
                    childRt.sizeDelta = spineSize * scale;
                }
            }
        }

        // Determine letters from ModuleData similar to WordGameManager / FindLetterLevel
        private void DetermineTargetLetters()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            AlfabeModuleData alfabe = gm.GetAlfabeModuleData();
            NumberModuleData number = gm.GetNumberModuleData();

            if (alfabe != null)
            {
                // If LevelName explicitly set, use it directly
                if (!string.IsNullOrEmpty(alfabe.LevelName))
                {
                    targetLetters = alfabe.LevelName;
                }
                else
                {
                    if (gm.currentIndex == 0)
                        targetLetters = alfabe.UpperCaseLetter.letter.ToString();
                    else if (gm.currentIndex == 1)
                        targetLetters = alfabe.LowerCaseLetter.letter.ToString();
                    else
                        targetLetters = alfabe.Word;
                }
            }
            else if (number != null)
            {
                targetLetters = number.NumberData.letter.ToString();
            }

            // Final fallback: use IGameData.LevelName
            if (string.IsNullOrEmpty(targetLetters) && GameManager.Instance.GameData != null)
            {
                targetLetters = GameManager.Instance.GameData.LevelName;
            }

            // Duplicate single letter to AaAaAa pattern
            if (!string.IsNullOrEmpty(targetLetters) && targetLetters.Length == 1)
            {
                char ch = targetLetters[0];
                if (char.IsLetter(ch))
                {
                    char upper = char.ToUpper(ch);
                    char lower = char.ToLower(ch);
                    targetLetters = "" + upper + lower + upper + lower + upper + lower;
                }
            }
        }
    }
}
