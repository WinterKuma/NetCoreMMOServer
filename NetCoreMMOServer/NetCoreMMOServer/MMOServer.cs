﻿using MemoryPack;
using NetCoreMMOServer.Contents.Entity;
using NetCoreMMOServer.Contents.Entity.Master;
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
        private NetEntity?[,,] _groundEntities;

        //Packet Instance
        private SetLinkedEntityPacket _setLinkedEntityPacket;

        private HttpClient _httpClient;

        public MMOServer(int port) : base(port)
        {
            _groundEntities = new NetEntity[(int)MathF.Ceiling(ZoneOption.TotalZoneWidth), (int)MathF.Ceiling(ZoneOption.TotalZoneHeight), (int)MathF.Ceiling(ZoneOption.TotalZoneDepth)];

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
                        //Console.WriteLine($"눌린 키: {keyInfo.Key}");
                        if (keyInfo.Key == ConsoleKey.S)
                        {
                            Console.WriteLine($"Zone Chunk 수동 저장...");
                            foreach (var zone in ZoneList)
                            {
                                await SaveZoneDB(zone.ZoneCoord);
                            }
                        }
                        if (keyInfo.Key == ConsoleKey.E)
                        {
                            Console.WriteLine($"Server Off");
                            Stop();
                        }
                    }
                }
            });
        }

        private bool CreateEntity(EntityType entityType, out NetEntity entity, Vector3 position)
        {
            switch (entityType)
            {
                case EntityType.Player:
                    if (CreateEntity<Master_PlayerEntity>(out entity, position))
                    {

                    }
                    break;
                case EntityType.Block:
                    if (CreateEntity<BlockEntity>(out entity, position))
                    {
                        Vector3Int gPos = new Vector3Int(entity.Transform.Position + ZoneOption.TotalZoneHalfSize);
                        _groundEntities[gPos.X, gPos.Y, gPos.Z] = entity;
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }

            return true;
        }

        private new bool ReleaseEntity(NetEntity entity)
        {
            switch (entity.EntityType)
            {
                case EntityType.Player:
                    if (!base.ReleaseEntity(entity))
                    {
                        return false;
                    }
                    break;
                case EntityType.Block:
                    if (!base.ReleaseEntity(entity))
                    {
                        return false;
                    }
                    Vector3Int gPos = new Vector3Int(entity.Transform.Position + ZoneOption.TotalZoneHalfSize);
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
            _ = CommandAsync();

            // WebServer (X) Test Setting
            //for (int x = (int)MathF.Ceiling(-ZoneOption.TotalZoneHalfWidth); x < ZoneOption.TotalZoneHalfWidth; x++)
            //{
            //    for (int z = (int)MathF.Ceiling(-ZoneOption.TotalZoneHalfDepth); z < ZoneOption.TotalZoneHalfDepth; z++)
            //    {
            //        if (!CreateEntity(EntityType.Block, out NetEntity entity, new Vector3(x, -2f, z)))
            //        {
            //            Console.WriteLine($"Error:: Don't Create Entity [{EntityType.Block}]");
            //            continue;
            //        }
            //    }
            //}

            // WebServer DB Load
            foreach (var zone in ZoneList)
            {
                LoadZoneDB(zone.ZoneCoord).Wait();
            }
        }

        protected override void Release()
        {
            Console.WriteLine($"Zone Chunk 종료 자동 저장...");
            foreach (var zone in ZoneList)
            {
                SaveZoneDB(zone.ZoneCoord).Wait();
            }
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
                    EntityTable[entityDataTablePacket.EntityInfo].LoadDataTablePacket_Server(entityDataTablePacket);
                    break;

                case GroundModificationPacket groundModificationPacket:
                    Vector3Int gPos = groundModificationPacket.Position;
                    Vector3Int gGridPos = gPos + new Vector3Int(ZoneOption.TotalZoneHalfSize);
                    NetEntity? gBlock = _groundEntities[gGridPos.X, gGridPos.Y, gGridPos.Z];
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
                                    SyncData<int> selectSlot = player.Inventory.SelectSlotItem;
                                    Item item = selectSlot.Value.GetItem();
                                    if (item.code == ItemCode.Block && item.count > 0)
                                    {
                                        item.count--;
                                        if (item.count == 0)
                                        {
                                            item.code = ItemCode.None;
                                        }
                                        selectSlot.Value = item.buffer;
                                    }
                                    else
                                    {
                                        if (!player.Inventory.RemoveItem(ItemCode.Block, 1))
                                        {
                                            Console.WriteLine($"Error:: Don't Have Item {ItemCode.Block}, Player : {player}");
                                        }
                                    }
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
                                Console.WriteLine($"Error:: Don't Release Entity [{EntityType.Block}]");
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

                case RPCPacketProtocol rpcPacket:
                    user.LinkedEntity.ReceiveRPC(rpcPacket.RPCPacket);
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

        protected override void SetZone(NetEntity entity)
        {
            Vector3 pos = entity.Transform.Position;
            if (pos.X > ZoneOption.TotalZoneHalfWidth ||
                pos.Y > ZoneOption.TotalZoneHalfHeight ||
                pos.Z > ZoneOption.TotalZoneHalfDepth ||
                pos.X < -ZoneOption.TotalZoneHalfWidth ||
                pos.Y < -ZoneOption.TotalZoneHalfHeight ||
                pos.Z < -ZoneOption.TotalZoneHalfDepth)
            {
                entity.Transform.Position = Vector3.Zero;
                pos = entity.Transform.Position;

                if (entity is PlayerEntity player)
                {
                    player.GetDamage(2);
                }
            }

            int x = Math.Clamp((int)((pos.X + ZoneOption.TotalZoneHalfWidth) * ZoneOption.InverseZoneWidth), 0, ZoneOption.ZoneCountX - 1);
            int y = Math.Clamp((int)((pos.Y + ZoneOption.TotalZoneHalfHeight) * ZoneOption.InverseZoneHeight), 0, ZoneOption.ZoneCountY - 1);
            int z = Math.Clamp((int)((pos.Z + ZoneOption.TotalZoneHalfDepth) * ZoneOption.InverseZoneDepth), 0, ZoneOption.ZoneCountZ - 1);
            entity.MoveZone(GetZone(x, y, z));
        }
    }
}
