# Combined reward selector verification

The selector is deterministic by command ID. These cases should be exercised in the plugin test harness before the gateway is enabled.

| Case | Input | Expected result |
|---|---|---|
| Default weighting | All three categories populated; default weights | One category selected, then one entry from that category |
| Retry | Repeat the same command ID, weights and ordered pools | Identical category and entry |
| Different redemption | Different command ID with the same pools | A separately derived selection |
| Empty category | One category has no eligible entries | Empty category is skipped |
| Disabled category | A category weight is zero | Category is skipped |
| No candidates | All pools empty | Clear unavailable-pool failure |
| Invalid weights | Negative, NaN, infinite, or all-zero weights | Validation failure |
| Stable catalogue | Same entries supplied in different order | Selection follows the supplied order; production catalogues must sort by stable content ID |

## Catalogue contract

The selector intentionally accepts already-validated candidates. The game adapter is responsible for building these lists from the current run's REPOLib registries, sorting them by stable content ID, and filtering to supported content. The utility-item list must contain only eligible non-weapon shop utilities; unsupported entries must never enter the list.

The first production integration should log the eligible counts for each category once when the run catalogue is built. Do not write catalogue snapshots to D1 or KV; this is transient game-session data.
