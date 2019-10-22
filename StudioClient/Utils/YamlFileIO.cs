using System.IO;
using YamlDotNet.Serialization;

namespace StudioClient.Utils
{
    class YamlFileIO
    {
        private static StreamReader yamlReader;
        private static Deserializer yamlDeserializer;
        private static StreamWriter yamlWriter;
        private static Serializer yamlSerializer;

        /// <summary>
        /// 读取（反序列化）Yaml 文件 -> 实体对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static T Reader<T>(string filePath)
        {
            yamlReader = File.OpenText(filePath);
            yamlDeserializer = new Deserializer();
            T deserializeModel = yamlDeserializer.Deserialize<T>(yamlReader);
            yamlReader.Close();
            return deserializeModel;
        }

        /// <summary>
        /// 写入（序列化）实体对象 -> Yaml 文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="serializeModel"></param>
        public static void Writer<T>(string filePath, T serializeModel)
        {
            yamlWriter = File.CreateText(filePath);
            yamlSerializer = new Serializer();
            yamlSerializer.Serialize(yamlWriter, serializeModel);
            yamlWriter.Close();
        }
    }
}
