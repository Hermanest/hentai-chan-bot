using JetBrains.Annotations;

namespace HentaiChanBot.API.Rule34 {
    [PublicAPI]
    public class R34PostData {
        public int id;
        public bool isVideo;
        public string? postUrl;
        //public string? fileUrl;
        public string? sampleUrl;
        public string[]? artists;
        public string[]? characters;
        //public string[]? tags;
    }
}