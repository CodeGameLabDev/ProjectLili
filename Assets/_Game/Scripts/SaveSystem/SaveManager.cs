using System;
using System.Collections;
using System.Collections.Generic;
using Lili.SaveSystem.Handlers;
using UnityEngine;

namespace Lili.SaveSystem
{
    public class SaveDataManager : MonoBehaviour
    {
        public static SaveDataManager Instance;
        
        public const string SaveDataFolder = "SaveData";
        public const string AlphabetSaveFileName = "Alphabet";

        public static string SaveDataDirectory;

        public AlphabetDataHandler AlphabetDataHandler { get; private set; }
        
        private List<BaseDataHandler> _dataHandlers = new List<BaseDataHandler>();
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public IEnumerator Initialize()
        {
            SaveDataDirectory = $"{Application.persistentDataPath}/CodeGames/{SaveDataFolder}";
            
            InitializeMembers();

            yield return null;
        }

        private void InitializeMembers()
        {
            AlphabetDataHandler = new AlphabetDataHandler(AlphabetSaveFileName);

            _dataHandlers = new List<BaseDataHandler>()
            {
                AlphabetDataHandler
            };
            
        }
    }
}