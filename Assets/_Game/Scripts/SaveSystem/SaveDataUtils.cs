
namespace Lili.SaveSystem
{
    public static class SaveDataUtils
    {
        #region Data Conversion

        public static T ConvertObject<T>(object originalObject)
        {
            var convertedObject = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(originalObject));
            return convertedObject;
        }

        #endregion
    }
}