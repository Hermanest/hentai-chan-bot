using AngleSharp.Dom;

namespace HentaiChanBot.Utils; 

public static class NodeMapExtensions {
    public static bool HasAttributeValue(this IElement element, string name, string value) {
        return element.GetAttribute(name) == value;
    }
}