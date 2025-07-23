using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using DG.Tweening;

/// <summary>
/// Single-script version of the "Feed Animal (By Count)" mini-game.
/// Handles level progression, animal and food spawning, click animations and completion logic.
/// </summary>
public class FeedAnimalModule : MonoBehaviour, IGameLevel
{
    #region Inspector – Data Lists
    [Header("Data – Sprites")]
    [Tooltip("Animal sprites in ABC order (31 items). The index determines level order.")]
    public List<Sprite> animalSprites = new List<Sprite>();

    [Tooltip("Food sprites in ABC order (31 items). Must match animalSprites 1-to-1.")]
    public List<Sprite> foodSprites = new List<Sprite>();

    [Header("Alphabet")]
    [Tooltip("Alphabet list mapping letters to index (must have 31 entries). Example: A,B,C,Ç,D,E,F,G,Ğ ...")] 
    public List<string> alphabet = new List<string>();

    [Tooltip("Letter to load (case-insensitive). Press 'Load Letter Level' button to apply.")]
    public string selectedLetter = "A";

    [Header("Backgrounds")]
    public List<Sprite> backgroundSprites = new List<Sprite>();
    public int antarcticaIndex = 0; // BG index reserved for Penguin level

    #endregion

    #region Inspector – Prefab Templates
    [Header("Templates (used only when prefab loading is OFF)")]
    [ShowIf("@!usePrefabLoading")]
    [Tooltip("Prefab with Image component for displaying the animal.")]
    public GameObject animalTemplatePrefab;

    [ShowIf("@!usePrefabLoading")]
    [Tooltip("Prefab with Image component for each food.")]
    public GameObject foodTemplatePrefab;
    #endregion

    #region Inspector – Scene References
    [Header("Scene References (used only when prefab loading is OFF)")]
    [ShowIf("@!usePrefabLoading")] public Image backgroundImage;
    [ShowIf("@!usePrefabLoading")] public Transform animalContainer;
    [ShowIf("@!usePrefabLoading")] public Transform animalTargetPosition;

    [Tooltip("Transforms that mark where foods start.")]
    public List<Transform> startPositions = new List<Transform>();
    [Tooltip("Transforms that mark where foods move after click (same order as startPositions).")]
    public List<Transform> targetPositions = new List<Transform>();

    [Header("Gameplay Settings")]
    public float moveDuration = 0.5f;
    #endregion

    #region Level Prefabs
    [Header("Level Prefabs (first 9 levels)")]
    [Tooltip("Drag prefabs named Level_1 ... Level_9 here in order.")]
    public List<GameObject> levelPrefabs = new List<GameObject>();
    #endregion

    #region Runtime State
    [TabGroup("Runtime"), ReadOnly] public int currentLevelIndex = 0;
    [ReadOnly] public int currentFoodCount = 0;

    private GameObject currentAnimal;
    private readonly List<GameObject> spawnedFoods = new List<GameObject>();
    private int remainingFood; // decremented when a food reaches its target
    private GameObject activeLevelInstance;

    // IGameLevel implementation state
    private bool levelCompleted = false;
    public bool IsCompleted => levelCompleted;

    public string LevelName => (alphabet != null && currentLevelIndex < alphabet.Count) ? alphabet[currentLevelIndex] : $"Level_{currentLevelIndex+1}";

    // Event fired when the current level is completed. Passes the completed level index.
    public event Action<int> OnLevelCompleted;
    public event Action OnGameStart;
    public event Action OnGameComplete;

    [Header("Prefab Loading Mode")] 
    [Tooltip("If true, levels are loaded as prefabs from Resources/FeedAnimalByCount/Level_X (X starts at 1).")] 
    public bool usePrefabLoading = true;

    [Tooltip("Folder inside Resources that contains Level prefabs.")]
    public string resourcesFolder = "FeedAnimalByCount";

    [Tooltip("Maximum number of levels to attempt when prefab loading mode is on.")]
    public int maxPrefabLevels = 9;

    private IGameLevel currentGameLevel; 
    #endregion

    #region Unity Life-cycle
    private void Start()
    {
        StartGame();
    }
    #endregion

    #region Level Management
    public void StartGame()
    {
        levelCompleted = false;
        OnGameStart?.Invoke();

        if (usePrefabLoading)
        {
            LoadPrefabLevel();
            return;
        }

        Debug.Log($"[FeedAnimalModule] === SetupLevel for index {currentLevelIndex} (Letter={LevelName}) ===");

        if (!ValidateData()) return;

        // Destroy old level instance
        if (activeLevelInstance != null && activeLevelInstance.scene.IsValid())
        {
            Destroy(activeLevelInstance);
            activeLevelInstance = null;
        }

        // If we have a prefab for this level index, instantiate it and refresh position holders
        if (currentLevelIndex < levelPrefabs.Count && levelPrefabs[currentLevelIndex] != null)
        {
            activeLevelInstance = Instantiate(levelPrefabs[currentLevelIndex], transform);
            Debug.Log($"[FeedAnimalModule] Instantiated level prefab {activeLevelInstance.name}");
            ExtractPositionsFromPrefab(activeLevelInstance);
        }

        // ---- Select sprites & counts ----
        Sprite animalSprite = animalSprites[currentLevelIndex];
        Sprite foodSprite = foodSprites[currentLevelIndex];
        Sprite bgSprite = ChooseBackground(animalSprite);

        currentFoodCount = currentLevelIndex + 1; // Level0 = 1 food, Level1 = 2 foods, etc.
        Debug.Log($"[FeedAnimalModule] animalSprite={animalSprite.name}, foodSprite={foodSprite.name}, bgSprite={bgSprite?.name}, foodCount={currentFoodCount}");

        // ---- Apply background ----
        if (backgroundImage != null && bgSprite != null)
            backgroundImage.sprite = bgSprite;

        // ---- Clear previous objects ----
        ClearCurrentObjects();

        // ---- Spawn new animal ----
        SpawnAnimal(animalSprite);

        // ---- Spawn foods ----
        SpawnFoods(foodSprite);

        // ---- Final init ----
        remainingFood = spawnedFoods.Count;
        Debug.Log($"[FeedAnimalModule] Level ready. remainingFood={remainingFood}");
    }

    private bool ValidateData()
    {
        if (animalSprites == null || animalSprites.Count == 0)
        {
            Debug.LogError("[FeedAnimalModule] animalSprites list is empty!");
            return false;
        }
        if (foodSprites == null || foodSprites.Count != animalSprites.Count)
        {
            Debug.LogError("[FeedAnimalModule] foodSprites length must match animalSprites.");
            return false;
        }
        if (startPositions.Count == 0 || targetPositions.Count == 0)
        {
            Debug.LogError("[FeedAnimalModule] startPositions / targetPositions not assigned.");
            return false;
        }
        if (currentLevelIndex >= animalSprites.Count)
        {
            Debug.Log("[FeedAnimalModule] All levels completed!");
            return false;
        }
        return true;
    }

    private Sprite ChooseBackground(Sprite animalSprite)
    {
        if (backgroundSprites == null || backgroundSprites.Count == 0) return null;

        bool penguin = animalSprite != null && animalSprite.name.ToLower().Contains("pengu");
        if (penguin && antarcticaIndex >= 0 && antarcticaIndex < backgroundSprites.Count)
        {
            return backgroundSprites[antarcticaIndex];
        }
        // pick random non-antarctica bg
        List<int> pool = new List<int>();
        for (int i = 0; i < backgroundSprites.Count; i++)
        {
            if (i == antarcticaIndex) continue;
            pool.Add(i);
        }
        if (pool.Count == 0) pool.Add(antarcticaIndex);
        int chosen = pool[UnityEngine.Random.Range(0, pool.Count)];
        return backgroundSprites[chosen];
    }

    private void ExtractPositionsFromPrefab(GameObject instance)
    {
        // Expect child objects named "StartPositions" and "TargetPositions" each with transforms children.
        startPositions.Clear();
        targetPositions.Clear();
        var startRoot = instance.transform.Find("StartPositions");
        var targetRoot = instance.transform.Find("TargetPositions");
        if (startRoot != null)
        {
            foreach (Transform child in startRoot)
                startPositions.Add(child);
        }
        if (targetRoot != null)
        {
            foreach (Transform child in targetRoot)
                targetPositions.Add(child);
        }
        Debug.Log($"[FeedAnimalModule] Extracted {startPositions.Count} start and {targetPositions.Count} target positions from prefab.");
    }

    private void ClearCurrentObjects()
    {
        // Remove foods
        for (int i = spawnedFoods.Count - 1; i >= 0; i--)
        {
            if (spawnedFoods[i] != null && spawnedFoods[i].scene.IsValid())
                Destroy(spawnedFoods[i]);
        }
        spawnedFoods.Clear();

        // Remove animal
        if (currentAnimal != null && currentAnimal.scene.IsValid())
            Destroy(currentAnimal);
        currentAnimal = null;

        // Remove level instance
        if (activeLevelInstance != null && activeLevelInstance.scene.IsValid())
            Destroy(activeLevelInstance);
        activeLevelInstance = null;
    }

    private void SpawnAnimal(Sprite sprite)
    {
        if (animalTemplatePrefab == null)
        {
            Debug.LogWarning("[FeedAnimalModule] animalTemplatePrefab not assigned – skipping animal spawn.");
            return;
        }
        Transform parent = animalContainer != null ? animalContainer : transform;
        currentAnimal = Instantiate(animalTemplatePrefab, parent);
        var img = currentAnimal.GetComponent<Image>();
        if (img != null && sprite != null) img.sprite = sprite;

        if (animalTargetPosition != null)
            currentAnimal.transform.position = animalTargetPosition.position;

        Debug.Log("[FeedAnimalModule] Animal spawned");
    }

    private void SpawnFoods(Sprite sprite)
    {
        int spawnCount = Mathf.Min(currentFoodCount, startPositions.Count, targetPositions.Count);
        Debug.Log($"[FeedAnimalModule] Spawning {spawnCount} food objects");

        for (int i = 0; i < spawnCount; i++)
        {
            Transform start = startPositions[i];
            Transform target = targetPositions[i];
            if (start == null || target == null)
            {
                Debug.LogWarning($"[FeedAnimalModule] Start/Target position missing at index {i}");
                continue;
            }

            GameObject foodObj = foodTemplatePrefab != null
                ? Instantiate(foodTemplatePrefab, start.position, Quaternion.identity, start.parent)
                : new GameObject($"Food_{i}", typeof(RectTransform), typeof(Image));

            // ensure Image & sprite
            var img = foodObj.GetComponent<Image>();
            if (img == null) img = foodObj.AddComponent<Image>();
            if (sprite != null) img.sprite = sprite;

            // add click handler
            FoodItem item = foodObj.AddComponent<FoodItem>();
            item.Init(this, target, moveDuration);

            spawnedFoods.Add(foodObj);
            Debug.Log($"[FeedAnimalModule] Food {i} spawned at {start.name}");
        }
    }

    private void OnFoodArrived(FoodItem item)
    {
        remainingFood--;
        Debug.Log($"[FeedAnimalModule] Food arrived. remainingFood={remainingFood}");
        if (remainingFood <= 0)
        {
            CompleteGame();

        }
    }

    public void CompleteGame()
    {
        if (levelCompleted) return;
        levelCompleted = true;
        Debug.Log($"[FeedAnimalModule] Level {currentLevelIndex} complete!");
        OnGameComplete?.Invoke();
        OnLevelCompleted?.Invoke(currentLevelIndex);
        // automatic progression remains manual
    }

    // Optional: call this method from outside to proceed to the next level manually.
    [Button("Next Level")] // Inspector button
    public void NextLevel()
    {
        if (currentLevelIndex + 1 >= animalSprites.Count)
        {
            Debug.Log("[FeedAnimalModule] Reached last level – no further levels to load.");
            return;
        }
        currentLevelIndex++;
        StartGame();
    }

    

 
    #endregion

    #region Nested FoodItem Component
    private class FoodItem : MonoBehaviour, IPointerClickHandler
    {
        private FeedAnimalModule parent;
        private Transform target;
        private float duration;
        private bool moved;

        public void Init(FeedAnimalModule p, Transform tgt, float dur)
        {
            parent = p;
            target = tgt;
            duration = dur;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (moved) return;
            moved = true;

            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            transform.DOMove(target.position, duration).SetEase(Ease.OutQuad)
                .OnComplete(() => parent?.OnFoodArrived(this));
        }
    }
    #endregion

    // ---------------- Prefab Loading Helpers ----------------
    private void LoadPrefabLevel()
    {
        // Clean current objects (foods, animal, previous prefab)
        ClearCurrentObjects();

        if (currentLevelIndex >= maxPrefabLevels)
        {
            Debug.Log("[FeedAnimalModule] Completed all prefab levels.");
            return;
        }

        string prefabPath = $"{resourcesFolder}/Level_{currentLevelIndex + 1}";
        GameObject prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[FeedAnimalModule] Prefab not found at Resources/{prefabPath}");
            return;
        }

        activeLevelInstance = Instantiate(prefab, transform);
        Debug.Log($"[FeedAnimalModule] Instantiated prefab {prefab.name} for level {currentLevelIndex}");

        currentGameLevel = activeLevelInstance.GetComponent<IGameLevel>();
        if (currentGameLevel != null)
        {
            currentGameLevel.OnGameComplete += PrefabLevelCompleted;
            currentGameLevel.StartGame();
        }
        else
        {
            Debug.LogWarning("[FeedAnimalModule] Prefab does not have a component implementing IGameLevel");
        }
    }

    private void PrefabLevelCompleted()
    {
        if (levelCompleted) return;
        levelCompleted = true;
        Debug.Log($"[FeedAnimalModule] Prefab level {currentLevelIndex} complete!");
        OnGameComplete?.Invoke();
        OnLevelCompleted?.Invoke(currentLevelIndex);
    }
    // ---------------------------------------------------------
} 