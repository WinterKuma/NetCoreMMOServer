using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using System.Xml.Linq;

namespace NetCoreMMOServer.PacketGenerator
{
    [Generator]
    public class EntityRPCExtensionGenerator : IIncrementalGenerator
    {
        public const string ServerRPCAttributeSource = @"using System;

namespace NetCoreMMOServer.Contents.Entity
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerRPCAttribute : Attribute
    {
    }
}";
        public const string ClientRPCAttributeSource = @"using System;

namespace NetCoreMMOServer.Contents.Entity
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ClientRPCAttribute : Attribute
    {
    }
}";

        public const string RPCPacketSource = @"using MemoryPack;
using System.Numerics;

namespace NetCoreMMOServer.Packet
{";

        public const string RPCPacketClassSource = @"    public abstract partial class RPCPacket
    {
        
    }";

        public static List<string> baseNameList = new();

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput((ctx) =>
            {
                ctx.AddSource("ServerRPCAttribute.g.cs", SourceText.From(ServerRPCAttributeSource, Encoding.UTF8));
                ctx.AddSource("ClientRPCAttribute.g.cs", SourceText.From(ClientRPCAttributeSource, Encoding.UTF8));
            });

            IncrementalValuesProvider<ClassDeclarationSyntax> classesDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                predicate: static (s, _) => true,
                transform: static (ctx, _) => GetNetEntityForGeneration(ctx))
                .Where(static m => m is not null)!;

            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses = context.CompilationProvider.Combine(classesDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => ExecuteNetEntity(source.Item1, source.Item2, spc));
        }

        private static ClassDeclarationSyntax? GetNetEntityForGeneration(GeneratorSyntaxContext context)
        {
            ClassDeclarationSyntax? classDeclarationSyntax = context.Node as ClassDeclarationSyntax;

            if (classDeclarationSyntax is null)
            {
                return null;
            }

            INamedTypeSymbol? netEntityBase = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);

            if (netEntityBase is null)
            {
                return null;
            }

            if (netEntityBase.BaseType is null)
            {
                return null;
            }

            if (netEntityBase.BaseType.ToDisplayString().Equals("NetCoreMMOServer.Framework.NetEntity"))
            {
                return classDeclarationSyntax;
            }

            return null;
        }

        private static void ExecuteNetEntity(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
            {
                return;
            }

            IEnumerable<ClassDeclarationSyntax> distinctStructs = classes.Distinct();

            INamedTypeSymbol? netEntityBase = compilation.GetTypeByMetadataName("NetCoreMMOServer.Framework.NetEntity");
            if (netEntityBase is null)
            {
                return;
            }

            List<string> netEntityBaseClassNameList = new();
            Dictionary<string, List<string>> serverRPCDictionary = new();
            Dictionary<string, List<string>> clientRPCDictionary = new();

            Dictionary<string, string> methodToPacketStringDictionary = new();
            Dictionary<string, ParameterListSyntax> rpcPackDataDictionary = new();

            foreach (ClassDeclarationSyntax classDeclarationSyntax in distinctStructs)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                SemanticModel semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
                {
                    continue;
                }

                serverRPCDictionary[classSymbol.Name] = new List<string>();
                clientRPCDictionary[classSymbol.Name] = new List<string>();

                foreach (MemberDeclarationSyntax memberDeclarationSyntax in classDeclarationSyntax.Members)
                {
                    SemanticModel memberSemanticModel = compilation.GetSemanticModel(memberDeclarationSyntax.SyntaxTree);
                    foreach (AttributeListSyntax attributeListSyntax in memberDeclarationSyntax.AttributeLists)
                    {
                        foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                        {
                            if (semanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                            {
                                continue;
                            }

                            INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                            string fullName = attributeContainingTypeSymbol.ToDisplayString();

                            var member = memberSemanticModel.GetDeclaredSymbol(memberDeclarationSyntax);
                            if (fullName.Equals("NetCoreMMOServer.Contents.Entity.ServerRPCAttribute"))
                            {
                                if (member is null)
                                {
                                    continue;
                                }
                                string rpcPacketName = member.Name;
                                serverRPCDictionary[classSymbol.Name].Add(rpcPacketName);
                                if (memberDeclarationSyntax is MethodDeclarationSyntax methodDeclarationSyntax)
                                {
                                    string packetName = $"RPC{rpcPacketName}Packet";
                                    methodToPacketStringDictionary.Add(rpcPacketName, packetName);
                                    rpcPackDataDictionary.Add(packetName, methodDeclarationSyntax.ParameterList);
                                }
                            }
                            if (fullName.Equals("NetCoreMMOServer.Contents.Entity.ClientRPCAttribute"))
                            {
                                if (member is null)
                                {
                                    continue;
                                }
                                string rpcPacketName = member.Name;
                                clientRPCDictionary[classSymbol.Name].Add(rpcPacketName);
                                if (memberDeclarationSyntax is MethodDeclarationSyntax methodDeclarationSyntax)
                                {
                                    string packetName = $"RPC{rpcPacketName}Packet";
                                    methodToPacketStringDictionary.Add(rpcPacketName, packetName);
                                    rpcPackDataDictionary.Add(packetName, methodDeclarationSyntax.ParameterList);
                                }
                            }
                        }
                    }
                }

                netEntityBaseClassNameList.Add(classSymbol.Name);
            }

            if (netEntityBaseClassNameList.Count > 0)
            {
                var sb = new StringBuilder();

                int index = 0;
                sb.AppendLine(RPCPacketSource);
                foreach (var rpcPacket in rpcPackDataDictionary.Keys)
                {
                    ++index;
                    sb.AppendLine($"    [MemoryPackUnion({index}, typeof({rpcPacket}))]");
                }
                sb.AppendLine(RPCPacketClassSource);

                foreach (var rpcPacketKVP in rpcPackDataDictionary)
                {
                    sb.AppendLine();
                    sb.AppendLine($"    [MemoryPackable]");
                    sb.AppendLine($"    public partial class {rpcPacketKVP.Key} : RPCPacket");
                    sb.AppendLine(@"    {");
                    foreach (var parameter in rpcPacketKVP.Value.Parameters)
                    {
                        sb.AppendLine($"        public {parameter.Type} {parameter.Identifier} {{ get; set; }} = default;");
                    }
                    sb.AppendLine(@"    }"); 
                }
                sb.AppendLine("}");

                using (FileStream fstream = new FileStream("./NetCoreMMOServer.Packet/RPCPacket.g.cs", FileMode.Create))
                {
                    using (StreamWriter writter = new StreamWriter(fstream, Encoding.UTF8))
                    {
                        writter.Write(sb.ToString());
                    }
                }

                //context.AddSource($"RPCPacket.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));

                foreach (var className in netEntityBaseClassNameList)
                {
                    sb.Clear();
                    
                    sb.AppendLine("using NetCoreMMOServer.Packet;");
                    sb.AppendLine("using System.Numerics;");
                    sb.AppendLine("");
                    sb.AppendLine("namespace NetCoreMMOServer.Contents.Entity.Remote");
                    sb.AppendLine("{");
                    sb.AppendLine($"    public abstract class Remote_{className}_Base : {className}");
                    sb.AppendLine("    {");
                    // Add Caching Packet Field
                    foreach (var clientRPC in clientRPCDictionary[className])
                    {
                        string packetName = $"_{clientRPC}Packet";
                        sb.AppendLine($"        private {methodToPacketStringDictionary[clientRPC]} {packetName};");
                    }
                    sb.AppendLine("");
                    sb.AppendLine($"        public Remote_{className}_Base()");
                    sb.AppendLine("        {");
                    // Add Init Caching Packet
                    foreach (var clientRPC in clientRPCDictionary[className])
                    {
                        string packetName = $"_{clientRPC}Packet";
                        sb.AppendLine($"            {packetName} = new {methodToPacketStringDictionary[clientRPC]}();");
                    }
                    sb.AppendLine("        }");
                    sb.AppendLine("");
                    sb.AppendLine("        public void UpdateDataTablePacket()");
                    sb.AppendLine("        {");
                    sb.AppendLine("            this.UpdateDataTablePacket_Client();");
                    sb.AppendLine("        }");
                    sb.AppendLine("");
                    sb.AppendLine("        public void LoadDataTablePacket(EntityDataTable loadDataTable)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            this.LoadDataTablePacket_Client(loadDataTable);");
                    sb.AppendLine("        }");
                    sb.AppendLine("");
                    foreach (var clientRPC in clientRPCDictionary[className])
                    {
                        string packetName = $"_{clientRPC}Packet";
                        sb.Append($"        public override void {clientRPC}(");

                        bool hasParameter = false;
                        foreach (var parameter in rpcPackDataDictionary[methodToPacketStringDictionary[clientRPC]].Parameters)
                        {
                            if (hasParameter)
                            {
                                sb.Append(", ");
                            }
                            else
                            {
                                hasParameter = true;
                            }
                            sb.Append($"{parameter.Type} {parameter.Identifier}");
                        }
                        sb.AppendLine(")");
                        sb.AppendLine("        {");
                        foreach (var parameter in rpcPackDataDictionary[methodToPacketStringDictionary[clientRPC]].Parameters)
                        {
                            sb.AppendLine($"            {packetName}.{parameter.Identifier} = {parameter.Identifier};");
                        }
                        sb.AppendLine($"            SendRPCPacket({packetName});");
                        sb.AppendLine("        }");
                        sb.AppendLine("");
                    }
                    //sb.AppendLine("");
                    sb.AppendLine("        public override void ReceiveRPC(in RPCPacket rpcPacket)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            switch (rpcPacket)");
                    sb.AppendLine("            {");
                    foreach (var serverRPC in serverRPCDictionary[className])
                    {
                        string packetName = $"_{serverRPC}Packet";
                        string varName = serverRPC[0].ToString().ToLower() + serverRPC.Substring(1);
                        sb.AppendLine($"                case {methodToPacketStringDictionary[serverRPC]} {varName}:");
                        sb.Append($"                    {serverRPC}(");
                        bool hasParameter = false;
                        foreach (var parameter in rpcPackDataDictionary[methodToPacketStringDictionary[serverRPC]].Parameters)
                        {
                            if (hasParameter)
                            {
                                sb.Append(", ");
                            }
                            else
                            {
                                hasParameter = true;
                            }
                            sb.Append($"{varName}.{parameter.Identifier}");
                        }
                        sb.AppendLine(");");
                        sb.AppendLine($"                    break;");
                        sb.AppendLine("");
                    }
                    sb.AppendLine("                default:");
                    sb.AppendLine("                    Console.WriteLine($\"Error:: Not Found RPC Packet ({rpcPacket})\");");
                    sb.AppendLine("                    break;");
                    sb.AppendLine("            }");
                    sb.AppendLine("        }");
                    sb.AppendLine("    }");
                    sb.AppendLine("}");

                    //context.AddSource($"Remote_{className}_Base.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));

                    using (FileStream fstream = new FileStream($"./NetCoreMMOServer.Contents/Client/Entity_Bases/Remote_{className}_Base.g.cs", FileMode.Create))
                    {
                        using (StreamWriter writter = new StreamWriter(fstream, Encoding.UTF8))
                        {
                            writter.Write(sb.ToString());
                        }
                    }

                    sb.Clear();

                    sb.AppendLine("using NetCoreMMOServer.Packet;");
                    sb.AppendLine("using System.Numerics;");
                    sb.AppendLine("");
                    sb.AppendLine("namespace NetCoreMMOServer.Contents.Entity.Master");
                    sb.AppendLine("{");
                    sb.AppendLine($"    public abstract class Master_{className}_Base : {className}");
                    sb.AppendLine("    {");
                    // Add Caching Packet Field
                    foreach (var serverRPC in serverRPCDictionary[className])
                    {
                        string packetName = $"_{serverRPC}Packet";
                        sb.AppendLine($"        private {methodToPacketStringDictionary[serverRPC]} {packetName};");
                    }
                    sb.AppendLine($"        public Master_{className}_Base()");
                    sb.AppendLine("        {");
                    // Add Init Caching Packet
                    foreach (var serverRPC in serverRPCDictionary[className])
                    {
                        string packetName = $"_{serverRPC}Packet";
                        sb.AppendLine($"            {packetName} = new {methodToPacketStringDictionary[serverRPC]}();");
                    }
                    sb.AppendLine("        }");
                    sb.AppendLine("");
                    sb.AppendLine("        public void UpdateDataTablePacket()");
                    sb.AppendLine("        {");
                    sb.AppendLine("            this.UpdateDataTablePacket_Server();");
                    sb.AppendLine("        }");
                    sb.AppendLine("");
                    sb.AppendLine("        public void LoadDataTablePacket(EntityDataTable loadDataTable)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            this.LoadDataTablePacket_Server(loadDataTable);");
                    sb.AppendLine("        }");
                    sb.AppendLine("");
                    //sb.AppendLine("        public override void Update(float dt)");
                    //sb.AppendLine("        {");
                    //sb.AppendLine("            RigidBodyComponent.RigidBody.Velocity = new Vector3(Velocity.Value.X, RigidBodyComponent.RigidBody.Velocity.Y, Velocity.Value.Z);");
                    //sb.AppendLine("            if (IsJump.IsDirty && IsJump.Value)");
                    //sb.AppendLine("            {");
                    //sb.AppendLine("                RigidBodyComponent.RigidBody.Velocity += new Vector3(0.0f, 6.0f, 0.0f);");
                    //sb.AppendLine("            }");
                    //sb.AppendLine("        }");
                    //sb.AppendLine("");
                    foreach (var serverRPC in serverRPCDictionary[className])
                    {
                        string packetName = $"_{serverRPC}Packet";
                        sb.Append($"        public override void {serverRPC}(");

                        bool hasParameter = false;
                        foreach (var parameter in rpcPackDataDictionary[methodToPacketStringDictionary[serverRPC]].Parameters)
                        {
                            if (hasParameter)
                            {
                                sb.Append(", ");
                            }
                            else
                            {
                                hasParameter = true;
                            }
                            sb.Append($"{parameter.Type} {parameter.Identifier}");
                        }
                        sb.AppendLine(")");
                        sb.AppendLine("        {");
                        foreach (var parameter in rpcPackDataDictionary[methodToPacketStringDictionary[serverRPC]].Parameters)
                        {
                            sb.AppendLine($"            {packetName}.{parameter.Identifier} = {parameter.Identifier};");
                        }
                        sb.AppendLine($"            SendRPCPacket({packetName});");
                        sb.AppendLine("        }");
                        sb.AppendLine("");
                    }
                    sb.AppendLine("        public override void ReceiveRPC(in RPCPacket rpcPacket)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            switch (rpcPacket)");
                    sb.AppendLine("            {");
                    foreach (var clientRPC in clientRPCDictionary[className])
                    {
                        string packetName = $"_{clientRPC}Packet";
                        string varName = clientRPC[0].ToString().ToLower() + clientRPC.Substring(1);
                        sb.AppendLine($"                case {methodToPacketStringDictionary[clientRPC]} {varName}:");
                        sb.Append($"                    {clientRPC}(");
                        bool hasParameter = false;
                        foreach (var parameter in rpcPackDataDictionary[methodToPacketStringDictionary[clientRPC]].Parameters)
                        {
                            if (hasParameter)
                            {
                                sb.Append(", ");
                            }
                            else
                            {
                                hasParameter = true;
                            }
                            sb.Append($"{varName}.{parameter.Identifier}");
                        }
                        sb.AppendLine(");");
                        sb.AppendLine($"                    break;");
                        sb.AppendLine("");
                    }
                    sb.AppendLine("                default:");
                    sb.AppendLine("                    Console.WriteLine($\"Error:: Not Found RPC Packet ({rpcPacket})\");");
                    sb.AppendLine("                    break;");
                    sb.AppendLine("            }");
                    sb.AppendLine("        }");
                    sb.AppendLine("    }");
                    sb.AppendLine("}");

                    //context.AddSource($"Master_{className}_Base.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));

                    using (FileStream fstream = new FileStream($"./NetCoreMMOServer.Contents/Server/Entity_Bases/Master_{className}_Base.g.cs", FileMode.Create))
                    {
                        using (StreamWriter writter = new StreamWriter(fstream, Encoding.UTF8))
                        {
                            writter.Write(sb.ToString());
                        }
                    }
                }
            }
        }
    }
}
