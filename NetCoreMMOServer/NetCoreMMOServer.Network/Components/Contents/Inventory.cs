using NetCoreMMOServer.Packet;
using System.Runtime.InteropServices;

namespace NetCoreMMOServer.Network.Components.Contents
{
    public enum ItemCode : short
    {
        None = 0,

        Block = 1,
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Item
    {
        [FieldOffset(0)]
        public int buffer;

        [FieldOffset(0)]
        public ItemCode code;
        [FieldOffset(2)]
        public short count;
    }

    public class Inventory : Component
    {
        private readonly List<SyncData<int>> items;

        private readonly short MaxItemCount = 64;

        public Inventory(int size)
        {
            items = new List<SyncData<int>>(size);
            for(int i = 0;i < size; i++)
            {
                items.Add(new SyncData<int>(0));
            }
        }

        public List<SyncData<int>> Items => items;

        public bool AddItem(ItemCode code, int count)
        {
            Item itemBuffer = new Item();
            foreach (SyncData<int> item in items)
            {
                itemBuffer.buffer = item.Value;
                if(itemBuffer.code == code)
                {
                    if (itemBuffer.count + count > MaxItemCount)
                    {
                        count -= MaxItemCount - itemBuffer.count;
                        itemBuffer.count = MaxItemCount;
                        item.Value = itemBuffer.buffer;
                    }
                    else
                    {
                        itemBuffer.count += (short)count;
                        item.Value = itemBuffer.buffer;
                        return true;
                    }
                }
                else if(itemBuffer.code == ItemCode.None)
                {
                    itemBuffer.code = code;
                    if(count > MaxItemCount)
                    {
                        itemBuffer.count = MaxItemCount;
                        count -= MaxItemCount;
                        item.Value = itemBuffer.buffer;
                    }
                    else
                    {
                        itemBuffer.count = (short)count;
                        item.Value = itemBuffer.buffer;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool RemoveItem(ItemCode code, int count)
        {
            Item itemBuffer = new Item();
            int inventoryItemCount = 0;

            foreach(SyncData<int> item in items)
            {
                itemBuffer.buffer = item.Value;
                if(itemBuffer.code == code)
                {
                    inventoryItemCount += itemBuffer.count;
                }
            }

            if(inventoryItemCount >= count)
            {
                foreach (SyncData<int> item in items)
                {
                    itemBuffer.buffer = item.Value;
                    if (itemBuffer.code == code)
                    {
                        if(itemBuffer.count <= count)
                        {
                            count -= itemBuffer.count;
                            itemBuffer.count = 0;
                            itemBuffer.code = ItemCode.None;
                            item.Value = itemBuffer.buffer;
                        }
                        else
                        {
                            itemBuffer.count -= (short)count;
                            item.Value = itemBuffer.buffer;
                            break;
                        }
                    }
                }
                return true;
            }

            return false;
        }

        public int GetItemCount(ItemCode code)
        {
            int itemCount = 0;
            Item itemBuffer = new Item();

            foreach (SyncData<int> item in items)
            {
                itemBuffer.buffer = item.Value;
                if (itemBuffer.code == code)
                {
                    itemCount += itemBuffer.count;
                }
            }

            return itemCount;
        }
    }

    public static class ItemExtension
    {
        public static Item GetItem(this in int buffer)
        {
            Item item = new Item();
            item.buffer = buffer;
            return item;
        }
    }
}
