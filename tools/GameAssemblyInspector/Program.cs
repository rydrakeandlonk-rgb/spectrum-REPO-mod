using Mono.Cecil;
using System.Text.Json;

if (args.Length < 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: GameAssemblyInspector <Assembly-CSharp.dll> [output.json]");
    return 2;
}

var assemblyPath = Path.GetFullPath(args[0]);
var outputPath = args.Length > 1 ? args[1] : "game-assembly-metadata.json";
using var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters
{
    ReadSymbols = false,
    InMemory = true
});

var needles = new[]
{
    "EnemySetup", "EnemyDirector", "LevelValuables",
    "ValuableDirector", "PrefabRef", "LevelGenerator"
};

// Inspect only the known, safe runtime entry points needed for the
// non-weapon monster and valuable catalogue. Do not dump unrelated methods.
var inspectedMethods = new HashSet<string>(StringComparer.Ordinal)
{
    "EnemyDirector::GetEnemy",
    "LevelGenerator::EnemySetup",
    "LevelGenerator::EnemySpawn",
    "LevelGenerator::EnemySpawnObject",
    "ValuableDirector::SpawnValuable",
    "PrefabRef\u00601::get_Prefab",
    "PrefabRef\u00601::IsValid"
};

var matches = assembly.MainModule.Types
    .SelectMany(Flatten)
    .Where(t => needles.Any(n => t.Name.Contains(n, StringComparison.OrdinalIgnoreCase)))
    .OrderBy(t => t.FullName, StringComparer.Ordinal)
    .Select(t => new
    {
        type = t.FullName,
        baseType = t.BaseType?.FullName,
        fields = t.Fields.Select(f => new
        {
            name = f.Name,
            type = f.FieldType.FullName,
            isStatic = f.IsStatic,
            isPublic = f.IsPublic
        }).ToArray(),
        properties = t.Properties.Select(p => new
        {
            name = p.Name,
            type = p.PropertyType.FullName,
            getter = p.GetMethod?.Attributes.ToString(),
            setter = p.SetMethod?.Attributes.ToString()
        }).ToArray(),
        methods = t.Methods
            .Where(m => m.IsPublic || m.Name.Contains("Spawn", StringComparison.OrdinalIgnoreCase)
                || m.Name.Contains("Setup", StringComparison.OrdinalIgnoreCase)
                || m.Name.Contains("Enemy", StringComparison.OrdinalIgnoreCase))
            .Select(m => new
            {
                name = m.Name,
                returnType = m.ReturnType.FullName,
                isStatic = m.IsStatic,
                parameters = m.Parameters.Select(p => new { name = p.Name, type = p.ParameterType.FullName }).ToArray(),
                inspectedBody = inspectedMethods.Contains($"{t.Name}::{m.Name}") && m.HasBody
                    ? m.Body.Instructions.Select(i => new
                    {
                        opcode = i.OpCode.Name,
                        operand = i.Operand switch
                        {
                            MethodReference method => $"{method.DeclaringType.FullName}::{method.Name}",
                            FieldReference field => $"{field.DeclaringType.FullName}::{field.Name}",
                            TypeReference type => type.FullName,
                            string value => value,
                            null => null,
                            _ => i.Operand.ToString()
                        }
                    }).ToArray()
                    : null
            }).ToArray()
    }).ToArray();

var document = new
{
    assembly = assembly.Name.FullName,
    inspectedAtUtc = DateTime.UtcNow.ToString("O"),
    matchedTypeCount = matches.Length,
    types = matches
};

var json = JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(outputPath, json);
Console.WriteLine(json);
Console.WriteLine($"METADATA_INSPECTION_COMPLETE types={matches.Length} output={outputPath}");
return 0;

static IEnumerable<TypeDefinition> Flatten(TypeDefinition type)
{
    yield return type;
    foreach (var nested in type.NestedTypes)
        foreach (var child in Flatten(nested))
            yield return child;
}
