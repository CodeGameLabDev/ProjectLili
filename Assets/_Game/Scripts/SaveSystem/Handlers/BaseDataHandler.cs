using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

namespace Lili.SaveSystem.Handlers
{
    public abstract class BaseDataHandler
    {
        protected Dictionary<string, object> LoadedData = new Dictionary<string, object>();
        protected List<string> HandlerKeys = new List<string>();

        private string _fileName;
        private string _filePath;
        private string _debug;

        protected BaseDataHandler(string fileName)
        {
            Initialize(fileName);
        }

        private void Initialize(string fileName)
        {
            SetFileNameAndPath(fileName);
            SetupHandlerKeys();

            LoadedData = ConvertData(RetrieveLocalData());
        }

        private Dictionary<string, object> RetrieveLocalData()
        {
            return LoadDataFromFile();
        }

        private Dictionary<string, object> LoadDataFromFile()
        {
            if (!Directory.Exists(SaveDataManager.SaveDataDirectory))
            {
                Debug.Log("Save Data directory doesn't exist");
                return new Dictionary<string, object>();
            }

            if (!File.Exists(_filePath))
            {
                Debug.Log("Save Data file doesn't exist");
                return new Dictionary<string, object>();
            }

            var reader = new StreamReader(_filePath);

            string content;

            try
            {
                content = reader.ReadToEnd();
                reader.Close();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }

            var sw = new Stopwatch();
            sw.Start();

            try
            {
                var loadedData = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                sw.Stop();
                Debug.Log($"Loading data took {sw.ElapsedMilliseconds} ms");
                return loadedData;
            }
            catch (Exception e)
            {
                sw.Stop();
                Debug.LogException(e);
                throw;
            }
        }
        public abstract Dictionary<string, object> ConvertData(Dictionary<string, object> data);
        protected abstract void SetupHandlerKeys();

        private void SetFileNameAndPath(string fileName)
        {
            _fileName = $"{fileName}_Data.json";
            _filePath = $"{SaveDataManager.SaveDataDirectory}/{_fileName}";
            _debug = $"{_fileName}";
        }
        
        protected void SaveData(bool forceToFetchWithCloud = false)
        {
#if UNITY_EDITOR
            var sw = new Stopwatch();
            sw.Start();
#endif

            SaveDataToLocalFile();
#if UNITY_EDITOR
            sw.Stop();
            Debug.Log($"Saving data took {sw.ElapsedMilliseconds}ms");
#endif
        }

        protected void SaveDataToLocalFile()
        {
            if (!Directory.Exists(SaveDataManager.SaveDataDirectory))
            {
                Debug.Log("SaveData directory not found. Creating directory.");
                Directory.CreateDirectory(SaveDataManager.SaveDataDirectory);
            }

            if (!File.Exists(_filePath))
            {
                Debug.Log($"File [{_fileName}] not found. Creating save file.");
                File.WriteAllText(_filePath, JsonConvert.SerializeObject(new Dictionary<string, object>()));
                Debug.Log($"File [{_fileName}] created!");
            }

            try
            {
                var loadedDataJson = JsonConvert.SerializeObject(LoadedData);
                using var sw = new StreamWriter(_filePath);
                sw.Write(loadedDataJson);
                sw.Close();
                Debug.Log("Local data successfully saved to file.");
            }
            catch (Exception e)
            {
                Debug.Log("Unable to save local data to file.");
                Debug.LogException(e);
            }
        }
    }
}