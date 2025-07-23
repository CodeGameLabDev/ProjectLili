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

    [Header("Backgrounds")]
    public List<Sprite> backgroundSprites = new List<Sprite>();
    public int antarcticaIndex = 0; // BG index reserved for Penguin level

    #endregion

    #region Inspector – Prefab Templates
    [Header("Templates")]
    [Tooltip("Prefab with Image component for displaying the animal.")]
    public GameObject animalTemplatePrefab;

    [Tooltip("Prefab with Image component for each food.")]
    public GameObject foodTemplatePrefab;
    #endregion

    #region Inspector – Scene References
    [Header("Scene References")]
    public Image backgroundImage;
    public Transform animalContainer;
    public Transform animalTargetPosition;

    [Tooltip("Transforms that mark where foods start.")]
    public List<Transform> startPositions = new List<Transform>();
    [Tooltip("Transforms that mark where foods move after click (same order as startPositions).")]
    public List<Transform> targetPositions = new List<Transform>();

    [Header("Gameplay Settings")]
    public float moveDuration = 0.5f;
    #endregion

    #region Runtime State
    [TabGroup("Runtime"), ReadOnly] public int currentLevelIndex = 0;
    [ReadOnly] public int currentFoodCount = 0;

    private GameObject currentAnimal;
    private readonly List<GameObject> spawnedFoods = new List<GameObject>();
    private int remainingFood; // decremented when a food reaches its target

    public bool IsCompleted => throw new NotImplementedException();

    public string LevelName => throw new NotImplementedException();

    // Event fired when the current level is completed. Passes the completed level index.
    public event Action<int> OnLevelCompleted;
    public event Action OnGameStart;
    public event Action OnGameComplete;
    #endregion

    #region Unity Life-cycle
    private void Start()
    {
        //StartGame();
    }
    #endregion

    #region Level Management
    public void StartGame()
    {
        Debug.Log($"[FeedAnimalModule] === SetupLevel for index {currentLevelIndex} ===");

        if (!ValidateData()) return;

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
        Debug.Log($"[FeedAnimalModule] Level {currentLevelIndex} complete!");
        OnLevelCompleted?.Invoke(currentLevelIndex);
        // Automatic progression removed – call NextLevel() externally when desired.
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
} 