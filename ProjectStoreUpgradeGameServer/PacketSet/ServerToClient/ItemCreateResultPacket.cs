using System;

public class ItemCreateResultPacket : Packet<ItemCreateResultData, ItemCreateResultSerializer>
{
    // constructor - packet data
    public ItemCreateResultPacket(ItemCreateResultData data)
    {
        // allocate serialzier
        serializer = new ItemCreateResultSerializer();

        // allocate data
        dataElement = data;
    }

    // constructor - byte stream
    public ItemCreateResultPacket(byte[] data)
    {
        // allocate serializer
        serializer = new ItemCreateResultSerializer();
        serializer.SetDeserializedData(data);

        // allocate data
        dataElement = new ItemCreateResultData();

        // deserialize data
        serializer.Deserialize(ref dataElement);
    }

    // return data set
    public override ItemCreateResultData GetData()
    {
        return dataElement;
    }

    // return packet data -> packet id
    public override int GetPacketID()
    {
        return (int)ServerToClientPacket.ItemCreateResult;
    }

    // return packet data -> byte stream data section
    public override byte[] GetPacketData()
    {
        serializer.Serialize(dataElement);

        return serializer.GetSerializeData();
    }
}

