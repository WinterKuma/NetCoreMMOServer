using MemoryPack;
using NetCoreMMOServer.Contents.Entity;
using NetCoreMMOServer.Framework;
using NetCoreMMOServer.Network.Components.Contents;
using NetCoreMMOServer.Packet;
using NetCoreMMOServer.Utility;
using System.Net.Http.Json;
using System.Numerics;
using System.Text;

namespace NetCoreMMOServer
{
    internal class MMOServer : ZoneServer
    {
        private EntityDataBase?[,,] _groundEntities;

        //Packet Instance
        private SetLinkedEntityPacket _setLinkedEntityPacket;

        private HttpClient _httpClient;

        public MMOServer(int port) : base(port)
        {
            _groundEntities = new EntityDataBase[(int)MathF.Ceiling(ZoneOption.TotalZoneWidth), (int)MathF.Ceiling(ZoneOption.TotalZoneHeight), (int)MathF.Ceiling(ZoneOption.TotalZoneDepth)];

            _httpClient = new HttpClient() { BaseAddress = new Uri("https://localhost:7251") };

            _setLinkedEntityPacket = new();
        }

        public async Task CommandAsync()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
                        Console.WriteLine($"눌린 키: {keyInfo.Key}");
                        if (keyInfo.Key == ConsoleKey.S)
                        {
                            foreach (var zone in ZoneList)
                            {
                                await SaveZoneDB(zone.ZoneCoord);
                            }
                        }
                    }
                }
            });
        }

        private bool CreateEntity(EntityType entityType, out EntityDataBase entity, Vector3 position)
        {
            switch (entityType)
            {
                case EntityType.Player:
                    if (CreateEntity<PlayerEntity>(out entity, position))
                    {

                    }
                    break;
                case EntityType.Block:
                    if (CreateEntity<BlockEntity>(out entity, position))
                    {
                        Vector3Int gPos = new Vector3Int(entity.Position.Value + ZoneOption.TotalZoneHalfSize);
                        _groundEntities[gPos.X, gPos.Y, gPos.Z] = entity;
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            return true;
        }

        private bool ReleaseEntity(EntityDataBase entity)
        {
            switch (entity.EntityType)
            {
                case EntityType.Player:
                    if (!ReleaseEntity<PlayerEntity>(entity))
                    {
                        return false;
                    }
                    break;
                case EntityType.Block:
                    if (ReleaseEntity<BlockEntity>(entity))
                    {
                        return false;
                    }
                    Vector3Int gPos = new Vector3Int(entity.Position.Value + ZoneOption.TotalZoneHalfSize);
                    _groundEntities[gPos.X, gPos.Y, gPos.Z] = null;
                    break;

                default:
                    throw new NotImplementedException();
            }

            return true;
        }

        public Vector3 GetZonePosition(Vector3Int zoneCoord)
        {
            return new Vector3(zoneCoord.X, zoneCoord.Y, zoneCoord.Z) * ZoneOption.ZoneSize - (ZoneOption.ZoneCountXYZ - Vector3.One) * ZoneOption.ZoneSize * 0.5f;
        }

        private async Task LoadZoneDB(Vector3Int zoneCoord)
        {
            Zone zone = GetZone(zoneCoord);

            using (var response = await _httpClient.GetAsync($"Chunks?id={zone.ZoneID}"))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var jsonContent = await response.Content.ReadFromJsonAsync<ZoneDTO>();
                    ReadOnlyMemory<byte> buffer = Encoding.UTF8.GetBytes(jsonContent.ChunkBinary);
                    ZoneChunk zoneChunk = GetZone(zoneCoord).ZoneChunk;
                    MemoryPackSerializer.Deserialize<ZoneChunk>(buffer.Span, ref zoneChunk!);
                    Vector3 zonePosition = GetZonePosition(zoneCoord);
                    for (int i = 0; i < ZoneOption.ZoneWidth; i++)
                    {
                        for (int j = 0; j < ZoneOption.ZoneHeight; j++)
                        {
                            for (int k = 0; k < ZoneOption.ZoneWidth; k++)
                            {
                                if (zoneChunk.chunks[i, j, k] == BlockType.Block)
                                {
                                    Vector3 blockPosition = zonePosition + new Vector3(i, j, k) - ZoneOption.ZoneSize * 0.5f + Vector3.One * 0.5f;
                                    if (!CreateEntity(EntityType.Block, out _, blockPosition))
                                    {
                                        Console.WriteLine($"Error:: Don't Create Entity [{EntityType.Block}]");
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    await CreateZoneDB(zoneCoord);
                }
            }
        }

        private async Task SaveZoneDB(Vector3Int zoneCoord)
        {
            Zone zone = GetZone(zoneCoord);

            using (var response = await _httpClient.PutAsJsonAsync($"Chunks?id={zone.ZoneID}", zone.GetZoneDTO()))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    //var jsonContent = await response.Content.ReadFromJsonAsync<ZoneDTO>();
                }
            }
        }

        private async Task CreateZoneDB(Vector3Int zoneCoord)
        {
            Zone zone = GetZone(zoneCoord);

            ZoneChunk zoneChunk = zone.ZoneChunk;

            if (zoneCoord.Y == 0)
            {
                for (int i = 0; i < ZoneOption.ZoneWidth; i++)
                {
                    for (int j = 0; j < ZoneOption.ZoneHeight; j++)
                    {
                        for (int k = 0; k < ZoneOption.ZoneWidth; k++)
                        {
                            zoneChunk.chunks[i, j, k] = BlockType.Block;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < ZoneOption.ZoneWidth; i++)
                {
                    for (int j = 0; j < ZoneOption.ZoneHeight; j++)
                    {
                        for (int k = 0; k < ZoneOption.ZoneWidth; k++)
                        {
                            zoneChunk.chunks[i, j, k] = BlockType.None;
                        }
                    }
                }
            }

            using (var response = await _httpClient.PostAsJsonAsync($"Chunks", zone.GetZoneDTO()))
            {
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var jsonContent = await response.Content.ReadFromJsonAsync<ZoneDTO>();
                    ReadOnlyMemory<byte> buffer = Encoding.UTF8.GetBytes(jsonContent.ChunkBinary);
                    MemoryPackSerializer.Deserialize<ZoneChunk>(buffer.Span, ref zoneChunk!);
                    Vector3 zonePosition = GetZonePosition(zoneCoord);
                    for (int i = 0; i < ZoneOption.ZoneWidth; i++)
                    {
                        for (int j = 0; j < ZoneOption.ZoneHeight; j++)
                        {
                            for (int k = 0; k < ZoneOption.ZoneWidth; k++)
                            {
                                if (zoneChunk.chunks[i, j, k] == BlockType.Block)
                                {
                                    Vector3 blockPosition = zonePosition + new Vector3(i, j, k) - ZoneOption.ZoneSize * 0.5f + Vector3.One * 0.5f;
                                    if (!CreateEntity(EntityType.Block, out _, blockPosition))
                                    {
                                        Console.WriteLine($"Error:: Don't Create Entity [{EntityType.Block}]");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override void Initialize()
        {
            //_ = CommandAsync();

            foreach (var zone in ZoneList)
            {
                LoadZoneDB(zone.ZoneCoord).ConfigureAwait(false);
            }
        }

        protected override void Release()
        {

        }

        protected override void Update(float dt)
        {

        }

        protected override void ProcessPacket(in IMPacket packet, in User user)
        {
            switch (packet)
            {
                case EntityDataTable entityDataTablePacket:
                    if (!EntityTable.ContainsKey(entityDataTablePacket.EntityInfo))
                    {
                        Console.WriteLine($"Error:: Not Contain EntityInfo {entityDataTablePacket.EntityInfo.EntityID}");
                        break;
                    }
                    EntityTable[entityDataTablePacket.EntityInfo].LoadDataTablePacket(entityDataTablePacket);
                    break;

                case GroundModificationPacket groundModificationPacket:
                    Vector3Int gPos = groundModificationPacket.Position;
                    Vector3Int gGridPos = gPos + new Vector3Int(ZoneOption.TotalZoneHalfSize);
                    EntityDataBase? gBlock = _groundEntities[gGridPos.X, gGridPos.Y, gGridPos.Z];
                    if (groundModificationPacket.IsCreate)
                    {
                        if (gBlock == null)
                        {
                            if (user.LinkedEntity is PlayerEntity player)
                            {
                                if (player.Inventory.GetItemCount(ItemCode.Block) <= 0)
                                {
                                    break;
                                }

                                if (!CreateEntity(EntityType.Block, out var entity, new Vector3(gPos.X, gPos.Y, gPos.Z)))
                                {
                                    Console.WriteLine($"Error:: Don't Create Entity [{EntityType.Block}]");
                                    break;
                                }
                                else
                                {
                                    player.Inventory.RemoveItem(ItemCode.Block, 1);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (gBlock != null)
                        {
                            if (!ReleaseEntity(gBlock))
                            {
                                Console.WriteLine($"Error:: Don't Create Entity [{EntityType.Block}]");
                                break;
                            }
                            else
                            {
                                if (user.LinkedEntity is PlayerEntity player)
                                {
                                    if (player.Inventory.AddItem(ItemCode.Block, 1))
                                    {

                                    }
                                }
                            }
                        }
                    }
                    break;

                default:
                    Console.WriteLine("Error:: Not Found Packet Protocol!!");
                    break;
            }
        }

        protected override void ConnectedUser(User user)
        {
            if (!CreateEntity(EntityType.Player, out var player, Vector3.Zero))
            {
                Console.WriteLine($"Error:: Don't Create Entity [{EntityType.Block}]");
                return;
            }
            user.LinkEntity(player);

            _setLinkedEntityPacket.EntityInfo = user.LinkedEntity!.EntityInfo;
            MemoryPackSerializer.Serialize<IMPacket, PacketBufferWriter>(user.PacketBufferWriter, _setLinkedEntityPacket);
        }

        protected override void DisconnectedUser(User user)
        {
            if (user.LinkedEntity != null)
            {
                ReleaseEntity(user.LinkedEntity);
            }
        }
    }
}
