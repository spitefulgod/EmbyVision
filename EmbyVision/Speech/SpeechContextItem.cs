namespace EmbyVision.Speech
{
    public class SpeechContextItem
    {
        public SpeechContextItem() { }
        public SpeechContextItem(string Item)
        {
            this.Item = Item;
            this.Context = Item;
        }
        public SpeechContextItem(string Item, object Context)
        {
            this.Item = Item;
            this.Context = Context;
        }

        public string Item { get; set; }
        public object Context { get; set; }
    }
}
