- exports the spawn populations of spawn handler to config
**- Auto resets to default list every Protocol version change (most updates)** (config is moved to .old)

- settings:

"TargetDensity" - max amount to spawn (factor which also depends on Spawn.max_rate and world size)

"SpawnRate" - how much spawn at once per spawn tick

"ClusterSizeMin" & "ClusterSizeMax" - range between spawn at once

"EnforcePopulationLimits" - remove spawns if more than max

"ScaleWithServerPopulation" - scale with active players online

"AlignToNormal": false,

"Prefabs" - prefab pool to spawn

"Prefab" - asset name to spawn

"Weight" - count how often added to prefab pool