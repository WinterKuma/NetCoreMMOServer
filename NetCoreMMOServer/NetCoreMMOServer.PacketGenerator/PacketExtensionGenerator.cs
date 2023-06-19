using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace NetCoreMMOServer.Packet
{
    [Generator]
    public class PacketExtensionGenerator : IIncrementalGenerator
    {
        public const string PacketableAttributeSource =
            @"using System;

namespace NetCoreMMOServer.Packet
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PacketableAttribute : Attribute
    {
    }
}
";
        public const string PacketEnumGeneratorSource =
            @"namespace NetCoreMMOServer.Packet
{
    public enum PacketProtocol : ushort
    {
        None, ";

        public const string PacketExtensionsCodeGeneratorSource =
            @"using System;
using MemoryPack;
using NetCoreMMOServer.Utility;

namespace NetCoreMMOServer.Packet
{
    public static partial class PacketExtensions
    {
        /*private static ConcurrentPool<PacketBufferWriter> _packetBufferWriterPool = new();

        public static PacketProtocol GetProtocol(this Dto dto, Type type)
        {
            if (DtoPacketProtocolDictionary.TryGetValue(type, out PacketProtocol packetProtocol))
            {
                return packetProtocol;
            }
            return PacketProtocol.None;
        }

        public static ReadOnlyMemory<byte> AsMemory(this MPacket packet)
        {
            PacketBufferWriter writer = _packetBufferWriterPool.Get();
            writer.Clear();
            MemoryPackSerializer.Serialize(writer, packet);

            ReadOnlyMemory<byte> result = writer.GetFilledMemory();

            _packetBufferWriterPool.Return(writer);
            return result;
        }

        public static void ToMPacket<T>(this T dto, ref MPacket packet) where T : Dto
        {
            PacketBufferWriter writer = _packetBufferWriterPool.Get();
            writer.Clear();
            MemoryPackSerializer.Serialize(writer, dto);

            packet.PacketProtocol = dto.GetProtocol(typeof(T));
            packet.Dto = writer.GetFilledMemory();

            _packetBufferWriterPool.Return(writer);
        }

        public static MPacket ToMPacket<T>(this T dto) where T : Dto
        {
            PacketBufferWriter writer = _packetBufferWriterPool.Get();
            writer.Clear();
            MemoryPackSerializer.Serialize(writer, dto);

            MPacket packet = new()
            {
                PacketProtocol = dto.GetProtocol(typeof(T)),
                Dto = writer.GetFilledMemory()
            };

            _packetBufferWriterPool.Return(writer);
            return packet;
        }

        public static T? Deserialize<T>(this MPacket packet)
        {
            return MemoryPackSerializer.Deserialize<T>(packet.Dto.Span);
        }

        public static int Deserialize<T>(this MPacket packet, ref T? dto)
        {
            return MemoryPackSerializer.Deserialize<T>(packet.Dto.Span, ref dto);
        }

        public static Dto? Deserialize(this MPacket packet)
        {
            return DeserializeDictionary[packet.PacketProtocol].Invoke(packet);
        }*/
    }
}
";
        public const string PacketExtensionsDictionaryGeneratorSource =
            @"using System;
using System.Collections.Generic;

namespace NetCoreMMOServer.Packet
{
    public static partial class PacketExtensions
    {/*";

        private const string PacketableAttributeFullName = "NetCoreMMOServer.Packet.PacketableAttribute";
        private const string DtoPacketProtocolDictionaryName = @"        private static readonly Dictionary<Type, PacketProtocol> DtoPacketProtocolDictionary = new()";
        private const string DeserializeDictionaryName = @"        private static Dictionary<PacketProtocol, Func<MPacket, Dto?>> DeserializeDictionary = new()";
        private const string PacketProtocolName = "PacketProtocol";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput((ctx) =>
            {
                ctx.AddSource("PacketableAttribute.g.cs", SourceText.From(PacketableAttributeSource, Encoding.UTF8));
                ctx.AddSource("PacketExtensionsFunc.g.cs", SourceText.From(PacketExtensionsCodeGeneratorSource, Encoding.UTF8));
            });

            IncrementalValuesProvider<ClassDeclarationSyntax> classesDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null)!;

            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses = context.CompilationProvider.Combine(classesDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            ClassDeclarationSyntax? structDeclarationSyntax = context.Node as ClassDeclarationSyntax;

            if (structDeclarationSyntax is null)
            {
                return null;
            }

            foreach (AttributeListSyntax attributeListSyntax in structDeclarationSyntax.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }

                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName.Equals(PacketableAttributeFullName))
                    {
                        return structDeclarationSyntax;
                    }
                }
            }

            return null;
        }

        private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
            {
                GenerateTempExtension(context);
                return;
            }

            IEnumerable<ClassDeclarationSyntax> distinctStructs = classes.Distinct();

            INamedTypeSymbol? packetableAttribute = compilation.GetTypeByMetadataName(PacketableAttributeFullName);
            if (packetableAttribute is null)
            {
                GenerateTempExtension(context);
                return;
            }

            List<string> packetableClassNameList = new();

            foreach (ClassDeclarationSyntax classDeclarationSyntax in distinctStructs)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                SemanticModel semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
                {
                    continue;
                }

                packetableClassNameList.Add(classSymbol.Name);
            }

            if (packetableClassNameList.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine(PacketEnumGeneratorSource);

                foreach (var className in packetableClassNameList)
                {
                    sb.Append(@"        ").Append(className).AppendLine(", ");
                }
                sb.AppendLine(@"    }");
                sb.AppendLine(@"}");

                context.AddSource("PacketProtocolEnum.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));

                sb.Clear();
                sb.AppendLine(PacketExtensionsDictionaryGeneratorSource);

                sb.AppendLine(DtoPacketProtocolDictionaryName);
                sb.AppendLine(@"        {");
                foreach (var className in packetableClassNameList)
                {
                    sb.Append(@"            ").Append("{ typeof(").Append(className).Append("), ").Append(PacketProtocolName).Append(".").Append(className).AppendLine(" }, ");
                }
                sb.AppendLine(@"        };");

                sb.AppendLine(DeserializeDictionaryName);
                sb.AppendLine(@"        {");
                sb.Append(@"            ").Append("{ ").Append(PacketProtocolName).AppendLine(".None, (packet) => null }, ");
                foreach (var className in packetableClassNameList)
                {
                    sb.Append(@"            ").Append("{ ").Append(PacketProtocolName).Append(".").Append(className).Append(", Deserialize<").Append(className).AppendLine("> }, ");
                }
                sb.AppendLine(@"        };");
                sb.AppendLine(@"    }");
                sb.AppendLine(@"}");

                context.AddSource("PacketExtensionsDictionary.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            }
            else
            {
                GenerateTempExtension(context);
            }
        }

        public static void GenerateTempExtension(SourceProductionContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine(PacketEnumGeneratorSource);
            sb.AppendLine(@"    }");
            sb.AppendLine(@"}");
            context.AddSource("PacketProtocolEnum.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));

            sb.Clear();
            sb.AppendLine(PacketExtensionsDictionaryGeneratorSource);
            sb.AppendLine(DtoPacketProtocolDictionaryName);
            sb.AppendLine(@"        {");
            sb.AppendLine(@"        };");
            sb.AppendLine(DeserializeDictionaryName);
            sb.AppendLine(@"        {");
            sb.Append("{ ").Append(PacketProtocolName).Append(".None, (packet) => null }, ");
            sb.AppendLine(@"        };*/");
            sb.AppendLine(@"    }");
            sb.AppendLine(@"}");
            context.AddSource("PacketExtensionsDictionary.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        }
    }
}