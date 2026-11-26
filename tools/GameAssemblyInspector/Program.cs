using Mono.Cecil;
using System.Text.Json;

if (args.Length < 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: SafeShopInspector <Assembly-CSharp.dll> [output.json]");
    return 2;
}

using var assembly = AssemblyDefinition.ReadAssembly(Path.GetFullPath(args[0]), new ReaderParameters
{
    ReadSymbols = false,
    InMemory = true
});

// Strictly scoped to the three requested non-weapon shop groups.
// Item is included only as the shared base component needed to validate
// the exact approved prefab names at runtime. No inventory registry is read.
var allowedNames = new[] { "Upgrade", "Drone" };
var exactSharedTypes = new HashSet<string>(StringComparer.Ordinal)
{
    "Item", "ItemAttributes", "Cart", "ItemCart"
};
var types = assembly.MainModule.Types
    .SelectMany(Flatten)
    .Where(t => exactSharedTypes.Contains(t.Name) ||
        allowedNames.Any(name => t.Name.Contains(name, StringComparison.OrdinalIgnoreCase)))
    .OrderBy(t => t.FullName, StringComparer.Ordinal)
    .Select(t => new
    {
        type = t.FullName,
        baseType = t.BaseType?.FullName,
        fields = t.Fields.Where(f => f.IsPublic).Select(f => new
        {
            name = f.Name,
            type = f.FieldType.FullName,
            isStatic = f.IsStatic
        }).ToArray(),
        properties = t.Properties.Select(p => new
        {
            name = p.Name,
            type = p.PropertyType.FullName,
            canRead = p.GetMethod != null,
            canWrite = p.SetMethod != null
        }).ToArray(),
        methods = t.Methods.Where(m => m.IsPublic).Select(m => new
        {
            name = m.Name,
            returnType = m.ReturnType.FullName,
            isStatic = m.IsStatic,
            parameters = m.Parameters.Select(p => new { name = p.Name, type = p.ParameterType.FullName }).ToArray()
        }).ToArray()
    }).ToArray();

var result = new
{
    assembly = assembly.Name.FullName,
    matchedTypeCount = types.Length,
    scope = new[] { "upgrades", "drones", "Pocket C.A.R.T.", "C.A.R.T." },
    types
};

var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(args.Length > 1 ? args[1] : "safe-shop-metadata.json", json);
Console.WriteLine(json);
Console.WriteLine($"SAFE_SHOP_METADATA_COMPLETE types={types.Length}");
return 0;

static IEnumerable<TypeDefinition> Flatten(TypeDefinition type)
{
    yield return type;
    foreach (var nested in type.NestedTypes)
        foreach (var child in Flatten(nested))
            yield return child;
}
