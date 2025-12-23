# Emergence Implementation Guide

This guide documents the Emergence toolchain, runtime pipeline, and extension points for micro-society NPC simulation in Unity 6000.3.2f1 with full DOTS (Entities only at runtime).

## Overview

Emergence provides:
- A library of ScriptableObject definitions for signals, rules, effects, metrics, norms, institutions, domains, and profiles.
- A UI Toolkit editor tool (Emergence Studio) for designers to create and maintain assets.
- DOTS runtime systems that collect signals, evaluate rules, apply effects, and sample metrics.
- Baking support that compiles definitions into BlobAssets for high performance.
- Schedule curves baked from authoring to shape time-of-day signals.
- Society resource distribution for shared village inventories.
- NPC spawner authoring and runtime instantiation helpers.

The runtime has no GameObject usage. GameObjects are used only for authoring and baking.

## Folder Layout

- `Assets/Scripts/Emergence/Shared/Definitions`: ScriptableObject asset types.
- `Assets/Scripts/Emergence/Authoring`: MonoBehaviour authoring components and blob builders, grouped by theme (Library, Profiles, Society, Npc).
- `Assets/Scripts/Emergence/Runtime`: ECS components, blobs, and systems, grouped by Components, Data, and Systems (Core, Simulation, Spawning).
- `Assets/Scripts/Emergence/Runtime/Presentation`: Runtime presentation helpers (TMP debug HUD).
- `Assets/Scripts/Emergence/Editor`: UI Toolkit editor window and editor utilities, grouped by Studio, Library, Examples, Debug, and Setup.
- `Assets/Scriptable Objects/<Category>`: Default storage for Emergence assets (Signals, RuleSets, Effects, Metrics, Norms, Institutions, Domains, Profiles, Libraries).
- `Assets/Scriptable Objects/Debug`: Default storage for debug message templates.

## Core Asset Types

- `EM_MechanicLibrary`: Registry of all Emergence definitions.
- `EM_SocietyProfile`: Global tuning and scalability settings (volatility, tick rates, queue limits).
- `EM_SignalDefinition`: Signals emitted by gameplay and systems.
- `EM_RuleSetDefinition`: Groups of rules driven by signals.
- `EM_EffectDefinition`: Effects applied by rules.
- `EM_MetricDefinition`: Metrics sampled by the telemetry system.
- `EM_NormDefinition`: Norms used by institutions.
- `EM_InstitutionDefinition`: Institutions that enforce norms.
- `EM_DomainDefinition`: Domain grouping for rules and tuning.

All serialized fields include Tooltips and can be edited from Emergence Studio or the Inspector.

## Emergence Studio (UI Toolkit)

Open: `Tools/Emergence/Studio`

Capabilities:
- Assign or create a `EM_MechanicLibrary`.
- Browse and select categories (Signals, Rule Sets, Effects, Metrics, Norms, Institutions, Domains, Profiles).
- Create new assets in `Assets/Scriptable Objects/<Category>`.
- Inspect and edit the selected asset.

Recommended workflow:
1. Create a `EM_MechanicLibrary`.
2. Create Domains and Signals first.
3. Create Effects and Rule Sets.
4. Create a Society Profile that references Rule Sets and Metrics.
5. Assign the library and profile in the authoring components.

## Authoring and Baking

### EmergenceLibraryAuthoring
Attach to a GameObject in a SubScene and assign:
- `Library`: your `EM_MechanicLibrary`.
- `Default Profile`: optional, used for global tick rates and queue limits.

The baker compiles the library into a `BlobAssetReference<EmergenceLibraryBlob>` and creates a signal queue and metric sample buffer.

### EmergenceSocietyProfileAuthoring
Attach to the society root GameObject and assign:
- `Library`: the same `EM_MechanicLibrary`.
- `Profile`: the `EM_SocietyProfile` for that society.
- `Initial Lod`: starting LOD tier.
- `Debug Name`: optional label used by the debug HUD.

The baker creates:
- `EmergenceSocietyProfileReference`
- `EmergenceSocietyRoot`
- `EmergenceSocietyLod`
- `EmergenceMetricTimer` buffer

### EmergenceSocietyRegionAuthoring
Optional visual aid for designers:
- Draws a wireframe region for LOD and aggregation sizing.

### EmergenceSocietySimulationAuthoring
Attach to the society root GameObject to configure:
- Clock and schedule windows.
- Schedule signal ids and tick interval.
- Schedule curves (sleep/work/leisure) and sampling resolution.
- Need update tick rate.
- Trade tick rate and social modifiers.
- Trade success/fail signal ids.
- Initial society resource pool and distribution settings.

### EmergenceNpcAuthoring
Attach to NPC GameObjects to configure:
- Society membership and LOD tier.
- Initial needs, resources, and need rules.
- Relationship seeds.
- Random seed for decision making.
- Debug name used by the debug HUD (spawners append an index and the HUD shows the ECS id when a name is present).

### EmergenceNpcSpawnerAuthoring
Attach to a GameObject in a SubScene to spawn NPC entities at runtime:
- NPC prefab to instantiate.
- Optional society root override for spawned NPCs.
- Spawn count, radius, height, and deterministic seed.
- Debug name prefix override for spawned NPCs (optional, suffixed with an index in the debug HUD).

## Example Assets

Menu: `Tools/Emergence/Create Example Village Assets`

Creates a minimal set of Signals, Rule Sets, Effects, Metrics, and a Society Profile for a small village test. Assets are stored in `Assets/Scriptable Objects/<Category>` and linked into the library.
The same menu also creates example NPC and spawner prefabs in `Assets/Prefabs/Emergence` if they are missing.

Menu: `Tools/Emergence/Create Example Village Scene`

Creates an example scene plus a SubScene with a configured society root, library authoring, and NPC spawner. This is the fastest way to get a runnable setup.

## Debug HUD (TMP)

Emergence can optionally emit debug events (schedule windows, schedule ticks, trade attempts/results, and resource distribution transfers). These events are displayed by a TMP-based HUD.

Menu: `Tools/Emergence/Debug Templates`

Use this editor window to create or edit `EM_DebugMessageTemplates`, which control the formatting of debug messages. Tokens like `{time}`, `{subject}`, `{resource}`, and `{reason}` can be edited freely.

`Tools/Emergence/Create Example Village Scene` automatically creates a simple Canvas + TMP setup (`EM_DebugUI`) in the main scene and assigns the templates asset.
The project now includes the TextMeshPro package dependency in `Packages/manifest.json`.

When an entity has `EmergenceDebugName`, the log displays the name and its ECS id (example: `Villager 01 (ID 123)`).

## Schedule Curves

`EmergenceSocietySimulationAuthoring` exposes three AnimationCurves for sleep/work/leisure. The baker samples them into a blob, and the clock system uses the curve value to scale schedule tick signals. Rule effects multiply by the signal value, so curves directly shape need changes across the day.

## Example Village Setup (Clock + Needs + Trade + Distribution)

1. (Optional) Run `Tools/Emergence/Create Example Village Scene` to auto-create the scene, SubScene, and debug HUD.
2. If you want manual setup, run `Tools/Emergence/Create Example Village Assets`.
3. In a SubScene, add:
   - `EmergenceLibraryAuthoring` (assign library + profile).
   - `EmergenceSocietyProfileAuthoring` on the society root.
   - `EmergenceSocietySimulationAuthoring` on the society root (set curves + distribution settings).
4. Populate the society resource pool in `EmergenceSocietySimulationAuthoring` (e.g., `Resource.Food`, `Resource.Water`, `Resource.Sleep`).
5. Use `EM_Prefab_NpcExample` or create your own NPC prefab with `EmergenceNpcAuthoring`.
6. Add `EM_Prefab_NpcSpawner` (or a GameObject with `EmergenceNpcSpawnerAuthoring`) and assign:
   - NPC prefab.
   - Society root override (optional).

Manual debug HUD setup (optional):
1. Add a Canvas (Screen Space Overlay) in the main scene.
2. Add two TMP Text elements (time label and log label).
3. Add `EmergenceDebugUiManager` to the Canvas and assign the TMP fields + `EM_DebugMessageTemplates`.

## Runtime Components

Common components and buffers:
- `EmergenceSignalEvent` (buffer): queued signal events.
- `EmergenceSignalEmitter` (tag): marks entities that emit signals.
- `EmergenceSocietyRoot` (tag): identifies society roots.
- `EmergenceSocietyMember`: links an entity to a society root.
- `EmergenceSocietyLod`: LOD tier for the entity or society.
- `EmergenceSocietyClock`: time-of-day tracking for schedules.
- `EmergenceSocietySchedule`: schedule windows and signal ids.
- `EmergenceScheduleCurveBlob`: pre-sampled curves stored in `EmergenceSocietySchedule.Curve`.
- `EmergenceScheduleSignal` (buffer): schedule signal queue for broadcasting.
- `EmergenceNeedRule` (buffer): need decay and trade configuration.
- `EmergenceNeedResolutionState` (buffer): cooldowns for need resolution.
- `EmergenceDebugLog`: debug log settings for runtime event capture.
- `EmergenceDebugEvent` (buffer): debug events emitted by schedule, trade, and distribution systems.
- `EmergenceNeedTickSettings`: tick rate for need decay.
- `EmergenceTradeSettings`: trade rate and social modifiers.
- `EmergenceSocietyResourceDistributionSettings`: resource distribution tuning per society.
- `EmergenceSocietyResourceDistributionState`: next distribution tick timestamp.
- `EmergenceRelationship` (buffer): affinity values between NPCs.
- `EmergenceRandomSeed`: deterministic random seed per NPC.
- `EmergenceNeed` (buffer): entity needs.
- `EmergenceResource` (buffer): entity resources.
- `EmergenceReputation`: entity reputation.
- `EmergenceCohesion`: society cohesion.
- `EmergencePopulation`: population count (optional, updated by custom systems).
- `EmergenceMetricSample` (buffer): sampled metrics stored on the library entity.
- `EmergenceNpcSpawner`: spawn parameters for NPC instantiation.
- `EmergenceNpcSpawnerState`: tracks one-time spawner execution.

## Runtime Systems

### EmergenceDebugLogSystem
- Ensures a debug log singleton exists so runtime systems can emit debug events.

### EmergenceSignalCollectSystem
- Collects signal events from all entities with `EmergenceSignalEmitter`.
- Appends events to the central signal queue.
- Enforces `MaxSignalQueue` from the global settings.

### EmergenceRuleEvaluateSystem
- Processes signals based on LOD tick gating.
- Evaluates rules grouped by signal id.
- Applies effects to event targets or society roots.
- Respects:
  - Signal minimum LOD
  - Rule set mask from profile
  - Rule set enable flag

### EmergenceMetricsSystem
- Samples metrics per society profile.
- Uses `EmergenceMetricTimer` to respect individual sample intervals.
- Writes samples to `EmergenceMetricSample` buffer on the library entity.

### EmergenceSocietyClockSystem
- Advances the society clock based on day length.
- Emits schedule signals for sleep/work/leisure windows.
- Scales schedule tick signal values using baked curves.

### EmergenceScheduleBroadcastSystem
- Broadcasts schedule signals from societies to their members.
- Allows schedule-driven rules to target NPCs directly.

### EmergenceNeedDecaySystem
- Applies need decay based on `EmergenceNeedRule` at the configured tick rate.

### EmergenceTradeSystem
- Selects the most urgent need and attempts trade based on increasing probability.
- Uses relationship affinity and social modifiers to accept/refuse trade.
- Emits trade success/fail signals for Emergence rules.

### EmergenceSocietyResourceDistributionSystem
- Distributes resources from a society pool to members based on need urgency.
- Uses the same need rules for resource/need mapping.

### EmergenceNpcSpawnerSystem
- Instantiates NPC prefabs once per spawner entity.
- Supports society root overrides and randomized spawn positions.

## Emitting Signals (Example)

Emit signals from ECS systems by writing to a `DynamicBuffer<EmergenceSignalEvent>` on an entity with `EmergenceSignalEmitter`:

```csharp
using Unity.Collections;
using Unity.Entities;

DynamicBuffer<EmergenceSignalEvent> buffer = SystemAPI.GetBuffer<EmergenceSignalEvent>(entity);
EmergenceSignalEvent signalEvent = new EmergenceSignalEvent
{
    SignalId = new FixedString64Bytes("Need.Hunger"),
    Value = 1f,
    Target = entity,
    LodTier = EmergenceLodTier.Full,
    Time = 0d
};

buffer.Add(signalEvent);
```

The collect system overwrites `Time` with the current timestamp when it queues the event.

## Extending Effects

To add a new effect type:
1. Add a new value in `EmergenceEffectType`.
2. Extend `EmergenceRuleEvaluateSystem.ApplyEffect` with a new handler.
3. Add any required runtime components or buffers.
4. Update any editor validation you want to add.

## Extending Metrics

To add a new metric type:
1. Add a new value in `EmergenceMetricType`.
2. Extend `EmergenceMetricsSystem.SampleMetric`.
3. Optionally add new runtime components to hold data.

## Scalability Guidance

Default target (PC mid, 60 FPS):
- LOD0 Full Sim: 5k NPC @ 20-30 Hz
- LOD1 Simplified: 50k NPC @ 2-5 Hz
- LOD2 Aggregated: 100k+ NPC @ 0.2-1 Hz

Use `EM_SocietyProfile` tick rates to tune update cost. Larger worlds should rely on LOD2 aggregated simulation at region level.

## Known Limitations (Current Implementation)

- Rule cooldowns are defined but not enforced; add a cooldown buffer if needed.
- `SignalRate` metric uses the global signal queue length.
- Population counting requires a custom system to update `EmergencePopulation`.
- Rule evaluation assumes effect targets already have required components.
- Trade partner search is brute-force and intended for small village tests.

## Recommended Next Steps

- Implement rule cooldown tracking for high-frequency signals.
- Add validation passes for duplicate ids and missing references.
- Add custom metrics for your domain (economy, relations, institutions).
- Author bespoke signal emitters for NPC behavior systems.
