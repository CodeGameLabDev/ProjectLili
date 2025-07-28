using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using UnityEngine.UI;

/// <summary>
/// Represents a single "Feed Animal" level. Holds references to the animal GameObject,
/// its target position on screen (usually right-hand side) and the list of food objects
/// that should be present according to the target number.
/// When every food item has been clicked/eaten, <see cref="IGameLevel.OnGameComplete"/> is invoked.
/// </summary>
public class FeedAnimalLevel : MonoBehaviour, IGameLevel
{
    [Header("Level Components")]
    [Tooltip("Container where the animal prefab will be instantiated.")]
    public Transform animalContainer;

    [Tooltip("Animal GameObject instance at runtime (auto-instantiated).")]
    public GameObject animal;

    public Transform animalTargetPosition;

    // Runtime list of spawned food objects (auto-managed; not assigned in Inspector)
    [ShowInInspector, ReadOnly]
    private List<GameObject> foodObjects = new List<GameObject>();

    [Header("Position Holders")] 
    [Tooltip("Empty transforms that define where each food object initially spawns.")]
    public List<Transform> startPositions = new List<Transform>();
    [Tooltip("Empty transforms that define where each food object moves after being clicked.")]
    public List<Transform> targetPositions = new List<Transform>();

    [Tooltip("Duration of the tween when food moves from start to target.")]
    public float moveDuration = 0.5f;

    [Header("Level Info")]
    public string levelName = "FeedAnimal_Level";

    [Header("Spawn Settings")]
    [Tooltip("How many food objects should be spawned for this level.")]
    [MinValue(1)]
    public int foodCount = 1;

    [Header("Food Template")]
    [Tooltip("Prefab that will be instantiated for each food. If null, an empty GameObject with Image will be created.")]
    public GameObject foodTemplatePrefab;

    [Tooltip("Sprite that will be assigned to each food's Image component.")]
    public Sprite foodSprite;

    [Header("UI Background")]
    [Tooltip("Image component whose sprite will be replaced by the chosen background.")]
    public Image backgroundImage;

    /// <summary>
    /// Configures the level visuals (background & food prefab) before <see cref="StartGame"/> is invoked.
    /// </summary>
    /// <param name="bgSprite">Background sprite to apply.</param>
    /// <param name="foodPrefab">Prefab to instantiate for each start position.</param>
    public void ConfigureLevel(Sprite bgSprite, GameObject foodTemplatePrefab, Sprite foodSprite, GameObject animalTemplatePrefab, Sprite animalSprite, int desiredFoodCount)
    {
        Debug.Log($"[FeedAnimalLevel] ConfigureLevel called. bgSprite={bgSprite?.name}, foodSprite={foodSprite?.name}, animalSprite={animalSprite?.name}, desiredFoodCount={desiredFoodCount}");

        // 1. Set background
        if (backgroundImage != null && bgSprite != null)
        {
            backgroundImage.sprite = bgSprite;
            Debug.Log($"[FeedAnimalLevel] Background sprite set to {bgSprite.name}");
        }

        // ---- Animal instantiation ----
        // Destroy previous animal if any
        if (animal != null)
        {
            Destroy(animal);
            animal = null;
        }

        if (animalTemplatePrefab != null)
        {
            Transform parent = animalContainer != null ? animalContainer : transform;
            animal = Instantiate(animalTemplatePrefab, parent);

            // Assign sprite
            if (animalSprite != null)
            {
                var img = animal.GetComponent<Image>();
                if (img != null) img.sprite = animalSprite;
                Debug.Log($"[FeedAnimalLevel] Animal sprite set to {animalSprite.name}");
            }

            // Optionally move to target position immediately
            if (animalTargetPosition != null)
            {
                animal.transform.position = animalTargetPosition.position;
            }
        }

        // Update food count
        foodCount = Mathf.Max(1, desiredFoodCount);
        Debug.Log($"[FeedAnimalLevel] foodCount set to {foodCount}");

        // 2. Clear any pre-existing food objects (in case of replay)
        if (foodObjects != null)
        {
            for (int i = foodObjects.Count - 1; i >= 0; i--)
            {
                if (foodObjects[i] != null && foodObjects[i].scene.IsValid())
                {
                    Destroy(foodObjects[i]);
                }
            }
            foodObjects.Clear();
        }

        // 3. Instantiate food items at their start positions according to 'foodCount'
        int spawnCount = Mathf.Min(foodCount, startPositions.Count, targetPositions.Count);
        Debug.Log($"[FeedAnimalLevel] spawnCount calculated as {spawnCount}");
        for (int i = 0; i < spawnCount; i++)
        {
            Transform start = startPositions[i];
            GameObject instance;
            if (foodTemplatePrefab != null)
            {
                instance = Instantiate(foodTemplatePrefab, start.position, Quaternion.identity, start.parent ?? transform);
            }
            else
            {
                instance = new GameObject($"Food_{i}");
                instance.transform.SetParent(start.parent ?? transform);
                instance.transform.position = start.position;
            }

            // Assign sprite if the instance has an Image component
            if (foodSprite != null)
            {
                var img = instance.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = foodSprite;
                }
            }

            Debug.Log($"[FeedAnimalLevel] Spawned food index {i} at {start.name}. Instance name={instance.name}");
            foodObjects.Add(instance);
        }
    }

    private int remainingFood;
    private bool isCompleted = false;

    // --------------- IGameLevel IMPLEMENTATION -----------------
    public event Action OnGameStart;
    public event Action OnGameComplete;

    public bool IsCompleted => isCompleted;
    public string LevelName => levelName;

    public void StartGame()
    {
        // ---- Dynamic food count from NumberModuleData (if any) ----
        try
        {
            NumberModuleData numData = GameManager.Instance != null ? GameManager.Instance.GetNumberModuleData() : null;
            if (numData != null)
            {
                int parsed;
                if (int.TryParse(numData.LevelName, out parsed))
                {
                    foodCount = Mathf.Max(1, parsed);
                    Debug.Log($"[FeedAnimalLevel] foodCount overridden from NumberModuleData.LevelName = {parsed}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[FeedAnimalLevel] Unable to parse LevelName from NumberModuleData: {ex.Message}");
        }

        // (Re)generate food objects each time level starts to ensure correct count
        GenerateFoodObjects();

        remainingFood = foodObjects.Count;
        isCompleted = false;
        OnGameStart?.Invoke();

        Debug.Log($"[FeedAnimalLevel] StartGame -> remainingFood={remainingFood}");

        // Move animal to its designated position (if provided)
        if (animal != null && animalTargetPosition != null)
        {
            animal.transform.position = animalTargetPosition.position;
        }

        if (!ValidatePositions()) return;

        Debug.Log("[FeedAnimalLevel] ValidatePositions passed");

        // Place food objects at their corresponding start positions and set them up
        for (int i = 0; i < foodObjects.Count; i++)
        {
            GameObject food = foodObjects[i];
            Transform start = startPositions[i];
            Transform target = targetPositions[i];

            food.transform.position = start.position;

            var foodItem = food.GetComponent<FoodItem>();
            if (foodItem == null)
            {
                foodItem = food.AddComponent<FoodItem>();
            }
            foodItem.Initialize(this, target, moveDuration);
        }
        Debug.Log("[FeedAnimalLevel] All food items initialized");
    }

    public void CompleteGame()
    {
        if (isCompleted) return;
        isCompleted = true;
        OnGameComplete?.Invoke();
    }

    // Deprecated: old interaction method kept for compatibility

    public void FoodMoved(FoodItem item)
    {
        remainingFood--;
        if (remainingFood <= 0)
        {
            StartCoroutine(AnimateFoodsToAnimalAndComplete());
        }
    }

    private IEnumerator AnimateFoodsToAnimalAndComplete()
    {
        if (animal == null)
        {
            CompleteGame();
            yield break;
        }

        Vector3 dst = animal.transform.position;
        float extra = moveDuration;

        int finished = 0;
        foreach (var food in foodObjects)
        {
            if (food == null) { finished++; continue; }

            food.transform.DOMove(dst, extra).SetEase(Ease.InQuad)
                .OnComplete(() => finished++);
        }

        while (finished < foodObjects.Count)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);
        CompleteGame();
    }

    private bool ValidatePositions()
    {
        if (foodObjects.Count > startPositions.Count || foodObjects.Count > targetPositions.Count)
        {
            Debug.LogError($"FeedAnimalLevel: Not enough start/target position holders. Foods: {foodObjects.Count}, Starts: {startPositions.Count}, Targets: {targetPositions.Count}");
            return false;
        }
        return true;
    }

    private void GenerateFoodObjects()
    {
        // clear any lingering children in container lists
        foreach (var obj in foodObjects)
        {
            if (obj != null && obj.scene.IsValid()) Destroy(obj);
        }
        foodObjects.Clear();

        int spawnCount = Mathf.Min(foodCount, startPositions.Count, targetPositions.Count);
        for (int i = 0; i < spawnCount; i++)
        {
            Transform start = startPositions[i];
            GameObject instance;
            if (foodTemplatePrefab != null)
            {
                // Instantiate as child of start position so it inherits local anchors easily
                instance = Instantiate(foodTemplatePrefab, start);
                instance.transform.localPosition = Vector3.zero;
            }
            else
            {
                instance = new GameObject($"Food_{i}", typeof(RectTransform), typeof(Image));
                instance.transform.SetParent(start);
                instance.transform.localPosition = Vector3.zero;
            }

            // Try to assign sprite on Image component found on self or first child
            var img = instance.GetComponentInChildren<Image>();
            if (img == null) img = instance.AddComponent<Image>();
            if (foodSprite != null) img.sprite = foodSprite;

            foodObjects.Add(instance);
        }
    }
} 