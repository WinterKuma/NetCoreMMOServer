using System;
using System.Collections.Generic;

namespace NetCoreMMOServer.Packet
{
    public static partial class PacketExtensions
    {
        private static readonly Dictionary<Type, PacketProtocol> DtoPacketProtocolDictionary = new()
        {
            { typeof(EntityDto), PacketProtocol.EntityDto },
            { typeof(MoveDto), PacketProtocol.MoveDto },
        };
        private static Dictionary<PacketProtocol, Func<MPacket, Dto?>> DeserializeDictionary = new()
        {
            { PacketProtocol.None, (packet) => null },
            { PacketProtocol.EntityDto, Deserialize<EntityDto> },
            { PacketProtocol.MoveDto, Deserialize<MoveDto> },
        };
    }
}
