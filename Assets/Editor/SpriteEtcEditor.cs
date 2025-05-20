using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

public class SpriteEtcEditor : Editor
{
    [MenuItem("Assets/Sprite Editor/Fix Image For Etc2/Ceil to 4x4")]
    private static void ExtendImage()
    {
        FixImageForEtc(false);
    }

    [MenuItem("Assets/Sprite Editor/Fix Image For Etc2/Floor to 4x4")]
    private static void CropImage()
    {
        FixImageForEtc(true);
    }

    [MenuItem("Assets/Sprite Editor/Make Etc2")]
    private static void MakeEtc2()
    {
        string mainPath = "";

        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Texture2D), SelectionMode.Assets))
        {
            mainPath = AssetDatabase.GetAssetPath(obj);

            MakeEtc2(mainPath);
        }

        AssetDatabase.Refresh();
    }

    private static void MakeEtc2(string path)
    {
        var destFmt = TextureImporterFormat.ETC2_RGBA8Crunched;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        if (null == texture) return;

        int numChanges = 0;
        string assetPath = AssetDatabase.GetAssetPath(texture);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (importer != null)
        {
            var def = importer.GetDefaultPlatformTextureSettings();
            var changed = false;

            Action<TextureImporterPlatformSettings> maybeChange = (platSettings) =>
            {
                if (platSettings.format != destFmt ||
                    platSettings.compressionQuality != def.compressionQuality ||
                    platSettings.maxTextureSize != def.maxTextureSize ||
                    !platSettings.overridden
                )
                {
                    platSettings.format = destFmt;
                    platSettings.compressionQuality = def.compressionQuality;
                    platSettings.maxTextureSize = def.maxTextureSize;
                    platSettings.overridden = true;

                    changed = true;
                    importer.SetPlatformTextureSettings(platSettings);
                }
            };

            maybeChange(importer.GetPlatformTextureSettings("iPhone"));
            maybeChange(importer.GetPlatformTextureSettings("Android"));

            if (changed)
            {
                importer.SaveAndReimport();
                ++numChanges;
            }

            AssetDatabase.ImportAsset(assetPath);
        }

        Debug.Log(String.Format("Update Platform Specific Image Compression: {0} images updated", numChanges));
    }

    private static void FixImageForEtc(bool roundToFloor)
    {
        string mainPath = "";

        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Texture2D), SelectionMode.Assets))
        {
            mainPath = AssetDatabase.GetAssetPath(obj);

            if (roundToFloor)
            {
                CropImageAtPath(mainPath);
            }
            else
            {
                ExtendImageAtPath(mainPath);
            }
        }

        AssetDatabase.Refresh();
    }

    public static void SetTextureImporterFormat(Texture2D texture, bool isReadable)
    {
        if (null == texture) return;

        string assetPath = AssetDatabase.GetAssetPath(texture);
        var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

        if (tImporter != null)
        {
            // tImporter.textureType = TextureImporterType.Default;

            tImporter.isReadable = isReadable;

            AssetDatabase.ImportAsset(assetPath);
        }
    }

    private static void ExtendImageAtPath(string path)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int widthExtension = 4 - tex.width % 4;
        int heightExtension = 4 - tex.height % 4;

        bool isReadable = tex.isReadable;

        if (widthExtension != 4 || heightExtension != 4)
        {
            if (!isReadable)
            {
                SetTextureImporterFormat(tex, true);
            }

            int widthUpExtension = widthExtension / 2;
            int widthDownExtension = widthExtension - widthUpExtension;

            int heightUpExtension = heightExtension / 2;
            int heightDownExtension = heightExtension - heightUpExtension;

            var newTex = new Texture2D(tex.width + widthExtension, tex.height + heightExtension, tex.format, false);
            // Color[] rpixels = newTex.GetPixels();
            // int pxIndex = 0;

            for (int i = 0; i < newTex.width; i++)
            {
                for (int j = 0; j < newTex.height; j++)
                {
                    try
                    {
                        int tex_i = i - widthDownExtension;
                        int tex_j = j - heightDownExtension;

                        Color pixelColor = new Color(0, 0, 0, 0);

                        if (tex_i >= 0 && tex_j >= 0 &&
                            tex_i < tex.width && tex_j < tex.height)
                        {
                            pixelColor = tex.GetPixel(tex_i, tex_j);
                        }

                        newTex.SetPixel(i, j, pixelColor);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                        throw;
                    }
                }
            }

            // newTex.SetPixels(rpixels, 0);
            newTex.Apply();

            var bytes = newTex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Debug.Log($"{path} has been cropped successfully.");

            if (!isReadable)
            {
                SetTextureImporterFormat(tex, false);
            }
        }
    }

    /// <summary>
    /// Obsolete.
    /// </summary>
    /// <param name="path"></param>
    private static void CropImageAtPath(string path)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int widthCrop = tex.width % 4;
        int heightCrop = tex.height % 4;

        bool isReadable = tex.isReadable;

        if (widthCrop != 0 || heightCrop != 0)
        {
            if (!isReadable)
            {
                SetTextureImporterFormat(tex, true);
            }

            int widthUpCrop = widthCrop / 2;
            int widthDownCrop = widthCrop - widthUpCrop;

            int heightUpCrop = heightCrop / 2;
            int heightDownCrop = heightCrop - heightUpCrop;

            var newTex = new Texture2D(tex.width - widthCrop, tex.height - heightCrop, tex.format, false);
            // Color[] rpixels = newTex.GetPixels();
            // int pxIndex = 0;

            Color x = Color.black;

            for (int i = 0; i < tex.width; i++)
            {
                for (int j = 0; j < tex.height; j++)
                {
                    if (i >= widthDownCrop && i < tex.width - widthUpCrop &&
                        j >= heightDownCrop && j < tex.height - heightUpCrop)
                    {
                        try
                        {
                            var pixelColor = tex.GetPixel(i, j);
                            newTex.SetPixel(i - widthDownCrop, j - heightDownCrop, pixelColor);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.Message);
                            throw;
                        }
                    }
                }
            }

            // newTex.SetPixels(rpixels, 0);
            newTex.Apply();

            var bytes = newTex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Debug.Log($"{path} has been cropped successfully.");

            if (!isReadable)
            {
                SetTextureImporterFormat(tex, false);
            }
        }
    }

    private static bool ImageSelectionValidation()
    {
        bool atLeastOne = false;
        
        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Texture2D), SelectionMode.Assets))
        {
            Texture2D texture = obj as Texture2D;

            if (null == texture)
            {
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(texture);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (importer == null)
            {
                return false;
            }

            atLeastOne = true;
        }

        return atLeastOne;
    }

    private static bool ImageSelectionValidationByExtension(params string[] extensions)
    {
        string mainPath = "";

        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Texture2D), SelectionMode.Assets))
        {
            mainPath = AssetDatabase.GetAssetPath(obj);
            string ext = Path.GetExtension(mainPath);
            bool isExtAllowed = false;

            foreach (var e in extensions)
            {
                if (e == ext)
                {
                    isExtAllowed = true;
                    break;
                }
            }

            if (!isExtAllowed)
            {
                return false;
            }
        }

        if (string.IsNullOrEmpty(mainPath)) // not even one texture found
        {
            return false;
        }

        return true;
    }

    [MenuItem("Assets/Sprite Editor/Fix Image For Etc2/Ceil to 4x4", true)]
    private static bool CeilImageValidation()
    {
        return ImageSelectionValidation();
    }

    [MenuItem("Assets/Sprite Editor/Fix Image For Etc2/Floor to 4x4", true)]
    private static bool FloorImageValidation()
    {
        return ImageSelectionValidation();
    }

    [MenuItem("Assets/Sprite Editor/Make Etc2", true)]
    private static bool MakeEtc2Validation()
    {
        return ImageSelectionValidation();
    }
}