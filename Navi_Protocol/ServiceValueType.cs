using System.Text.Json.Serialization;

namespace Navi_Protocol;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServiceValueType
{
    String,
    Boolean,
    Integer,
    Number,
    Object,
    Array
}