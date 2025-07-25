using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

namespace HiddenLetterGame
{
    [System.Serializable]
    public class HiddenLetter
    {
        [Tooltip("Sahnede gizli olan harfin GameObject'i")] public GameObject obj;
        [Tooltip("Bu obje hangi harfi temsil ediyor?")] public char letter;
        [HideInInspector] public bool isFound = false;
    }

    /// <summary>
    /// HiddenLetter module'ündeki asset ve ilerleme bilgisini tutar.
    /// </summary>
    public class HiddenLetterAssetHolder : MonoBehaviour
    {
[Header("Hidden Letter Positions")]
        public List<RectTransform> hiddenLetterPositions;
        [Header("Hidden Letter Assets")]
        public List<HiddenLetter> hiddenLetters = new List<HiddenLetter>();

        [Header("Generated Parent (optional)")]
        public Transform generatedLettersParent;

        [Header("Events")]
        public UnityEvent OnGameWon;
        public UnityEvent OnLetterFound;

        private int foundCount = 0;

        public void OnLetterFoundCallback()
        {
            foundCount++;
            OnLetterFound?.Invoke();

            if (foundCount >= hiddenLetters.Count)
            {
                OnGameWon?.Invoke();
            }
        }

        public void ResetProgress()
        {
            foundCount = 0;
            foreach (var l in hiddenLetters)
            {
                l.isFound = false;
            }
        }

        public float GetProgressPercentage()
        {
            if (hiddenLetters.Count == 0) return 0f;
            return (float)foundCount / hiddenLetters.Count * 100f;
        }

        /// <summary>
        /// Target kelimeye göre gizli harf objelerini üretir ve hiddenLetters listesine doldurur.
        /// </summary>
        public void GenerateHiddenLetters(string targetLetters, GameObject letterHolderPrefab, LetterPathDatabase letterDatabase)
        {
            // Temizle
            foreach (var h in hiddenLetters)
            {
                if (h.obj != null) DestroyImmediate(h.obj);
            }
            hiddenLetters.Clear();

            if (letterHolderPrefab == null || letterDatabase == null)
            {
                Debug.LogError("GenerateHiddenLetters: letterHolderPrefab veya letterDatabase null!");
                return;
            }

            if (generatedLettersParent == null)
            {
                generatedLettersParent = this.transform;
            }

            string trimmed = string.IsNullOrEmpty(targetLetters) ? "" : targetLetters.Replace(" ", "");
            if (trimmed.Length == 0)
            {
                Debug.LogWarning("GenerateHiddenLetters: targetLetters boş!");
                return;
            }

            // Pozisyonları karıştır
            List<RectTransform> availablePositions = new List<RectTransform>(hiddenLetterPositions);
            System.Random rnd = new System.Random();
            availablePositions = availablePositions.OrderBy(x => rnd.Next()).ToList();

            int posIndex = 0;
            foreach (char c in trimmed)
            {
                if (posIndex >= availablePositions.Count)
                {
                    Debug.LogWarning("Pozisyon sayısı harf sayısından az! Fazla harfler yerleştirilemiyor.");
                    break;
                }

                RectTransform slot = availablePositions[posIndex++];
                if (slot == null)
                {
                    Debug.LogWarning("Null slot atlandı");
                    continue;
                }

                // Instantiate AS child of the slot so layout/positioning stays consistent
                GameObject holderInstance = Instantiate(letterHolderPrefab, slot);

                // Ensure it is centered inside the slot
                holderInstance.transform.localPosition = Vector3.zero;
                holderInstance.transform.localScale = Vector3.one;

                // Ensure same sorting/canvas layer if needed (designer may override manually)

                SetupHolderGraphics(holderInstance.transform, c, letterDatabase);

                HiddenLetter newHidden = new HiddenLetter { obj = holderInstance, letter = c, isFound = false };
                hiddenLetters.Add(newHidden);
            }
        }

        // Helper: set sprite/spine/shadow like WordBaloon
        void SetupHolderGraphics(Transform holder, char letterChar, LetterPathDatabase db)
        {
            if (db == null) return;
            string id = letterChar.ToString();
            var data = db.LoadLetterData(id);
            if (data == null) return;

            Transform spriteTf = holder.Find("SpriteLetter") ?? holder.Find("SpriteHolder");
            Transform shadowTf = holder.Find("ShadowLetter") ?? holder.Find("ShadowHolder");
            Transform spineTf = holder.Find("SpineLetter") ?? holder.Find("SpineHolder");

            SetupLetterComponent(spriteTf, data.letterSprite, "Sprite");
            SetupLetterComponent(shadowTf, data.letterShadowSprite, "Shadow");
            if (data.prefab != null) SetupSpineComponent(spineTf, data.prefab);

            // Gizli harf olduğu için Sprite görünsün, Shadow gizli olsun
            if (spriteTf != null) spriteTf.gameObject.SetActive(true);
            if (shadowTf != null) shadowTf.gameObject.SetActive(false);
        }

        // Reuse simplified helpers
        void SetupLetterComponent(Transform tf, Sprite sprite, string type)
        {
            if (tf == null || sprite == null) return;
            var rect = tf.GetComponent<RectTransform>() ?? tf.gameObject.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f,0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;

            var img = tf.GetComponent<UnityEngine.UI.Image>() ?? tf.gameObject.AddComponent<UnityEngine.UI.Image>();
            img.sprite = sprite;
            img.SetNativeSize();

            if (type == "Shadow")
            {
                img.color = new Color(1,1,1,0.3f);
            }
            else
            {
                Color col = img.color;
                img.color = new Color(col.r, col.g, col.b, 1f);
            }
        }

        void SetupSpineComponent(Transform tf, GameObject prefab)
        {
            if (tf == null || prefab == null) return;
            var rect = tf.GetComponent<RectTransform>() ?? tf.gameObject.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f,0.5f);
            // Keep prefab-defined anchoredPosition & localScale

            GameObject inst = Instantiate(prefab, tf);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
        }
    }
} 