using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.Unicode;

namespace GeneralUpdate.Tool.Avalonia;

public class IOHelper
{
    public static IOHelper Instance = new Lazy<IOHelper>(() => new IOHelper()).Value;

    private JsonSerializerOptions jsonSerializerSettings;

    public IOHelper(Action<string> logAction = null)
    {
        jsonSerializerSettings = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.CjkUnifiedIdeographs),
            WriteIndented = true,
            // TypeInfoResolver = SourceGenerationContext.Default
        };
    }

    #region Json

    public T ReadContentFromLocal<T>(string filePath)
    {
        try
        {
            T Config;

            var content = System.IO.File.ReadAllText(filePath);

            Config = System.Text.Json.JsonSerializer.Deserialize<T>(content);

            return Config;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public T ReadContentFromLocalSourceGeneration<T>(string filePath, JsonTypeInfo<T> jsonTypeInfo)
    {
        try
        {
            T Config;

            var content = System.IO.File.ReadAllText(filePath);

            Config = JsonSerializer.Deserialize(content, jsonTypeInfo);

            return Config;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    /// <summary></summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="filePath">全路径，包括后缀</param>
    public void WriteContentTolocal<T>(T obj, string filePath)
    {
        try
        {
            string newpath = System.IO.Path.GetDirectoryName(filePath);

            if (!Directory.Exists(newpath))
            {
                Directory.CreateDirectory(newpath);
            }

            var json = JsonSerializer.Serialize(obj, jsonSerializerSettings);//SourceGenerationContext.Default.AllConfigModel

            System.IO.File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    /// <summary></summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="filePath">全路径，包括后缀</param>
    public void WriteContentTolocalSourceGeneration<T>(T obj, string filePath, JsonTypeInfo<T> jsonTypeInfo)
    {
        try
        {
            string newpath = System.IO.Path.GetDirectoryName(filePath);

            if (!Directory.Exists(newpath))
            {
                Directory.CreateDirectory(newpath);
            }

            var json = JsonSerializer.Serialize(obj, jsonTypeInfo);//SourceGenerationContext.Default.AllConfigModel

            System.IO.File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    #endregion Json
}