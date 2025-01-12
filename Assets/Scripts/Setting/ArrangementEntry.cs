using System;
using System.Collections.Generic;
using System.Linq;
using Setting;
using Unity.Netcode;

[System.Serializable]
public class ArrangementEntry : INetworkSerializable
{
    public int row;
    public int column;
    public PieceType pieceType;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref row);
        serializer.SerializeValue(ref column);
        serializer.SerializeValue(ref pieceType);
        
    }
}

public class ArrangementEntryArray : INetworkSerializable
{
    public ArrangementEntry[] ArrangementEntry;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        var length = 0;
        if (!serializer.IsReader)
            length = ArrangementEntry.Length;

        serializer.SerializeValue(ref length);

        if (serializer.IsReader)
            ArrangementEntry = new ArrangementEntry[length];

        for (var n = 0; n < length; ++n)
            serializer.SerializeValue(ref ArrangementEntry[n]);
    }
}