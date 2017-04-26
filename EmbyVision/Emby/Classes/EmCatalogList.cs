using System.Collections.Generic;

namespace EmbyVision.Emby.Classes
{
    public class EmCatalogList
    {
        public List<EmMediaItem> Items { get; set; }
        public int TotalRecordCount { get; set; }

        public void Append(EmCatalogList Source)
        {
            if (Source != null && Source.Items != null)
                foreach (EmMediaItem Item in Source.Items)
                {
                    if (Items == null)
                        Items = new List<EmMediaItem>();
                    Items.Add(Item);
                    TotalRecordCount++;
                }
        }
    }
}
