using System.Collections.Generic;
using SpectrumRepoMod.API;

namespace SpectrumRepoMod.Runtime;

/// <summary>
/// Orchestrates the independent category catalogues and external mod registrations.
/// Existing category IDs are deliberately preserved for Control Center compatibility.
/// </summary>
internal static class RepoCatalogBuilder
{
    public static RepoCatalogSnapshot Build(RepoRuntimePolicy? policy = null)
    {
        policy ??= new RepoRuntimePolicy();
        var rows = new List<RepoCatalogRow>();
        RepoValuableCatalogue.AddRows(rows, policy);
        RepoMonsterCatalogue.AddRows(rows, policy);
        RepoUtilityItemCatalogue.AddRows(rows, policy);
        RepoSpawnRegistry.AppendRows(rows, policy);
        return new RepoCatalogSnapshot(rows);
    }
}
