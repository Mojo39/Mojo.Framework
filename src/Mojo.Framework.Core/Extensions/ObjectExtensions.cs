namespace Mojo.Framework.Core.Extensions;

#pragma warning disable SA1402 // File may only contain a single type
public static class ObjectExtensions
#pragma warning restore SA1402 // File may only contain a single type
{
    public static T DeserializeXml<T>(this string toDeserialize)
    {
        var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
        using (var textReader = new System.IO.StringReader(toDeserialize))
        {
            return (T)xmlSerializer.Deserialize(textReader);
        }
    }

    public static string SerializeXml<T>(this T toSerialize)
    {
        var xmlSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
        using (var textWriter = new System.IO.StringWriter())
        {
            xmlSerializer.Serialize(textWriter, toSerialize);
            return textWriter.ToString();
        }
    }
}
