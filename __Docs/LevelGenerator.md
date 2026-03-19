# Procedural Track Generation System

## Overview

The procedural track generation system dynamically assembles racing circuits from predefined track segments (chunks) as the player progresses. This approach enables infinite, non-repeating tracks while maintaining optimal performance through efficient resource management.

## Architecture

### Core Components

**RoadChunk**  
Modular track segments that serve as the fundamental building blocks. Each chunk contains:

- Entry anchor for connection to the previous segment
- One or more exit anchors for subsequent segments
- Track geometry (straights, curves, intersections)
- Spawn areas for environmental objects

**TrackSeed**  
Manages pseudorandom generation using configurable seeds. The same seed produces identical track layouts, enabling reproducible testing and sharing of specific circuits. Random seed mode generates unique tracks for each session.

**RoadManager**  
Central orchestrator responsible for:

- Initial chunk placement at session start
- Continuous player position monitoring
- Dynamic chunk spawning ahead of the player
- Chunk despawning and cleanup behind the player
- Precise spatial alignment between connected segments

**TrackPooler**  
Implements object pooling pattern to optimize memory allocation:

- Pre-instantiates chunk instances during initialization
- Recycles inactive chunks instead of destroying them
- Maintains separate pools for road chunks, environment chunks, and props
- Reduces GC pressure and instantiation overhead

## Generation Workflow

### Initialization Phase

1. Load track configuration from SOTrack ScriptableObject
2. Initialize random number generator with specified seed
3. Pre-populate object pools based on configuration
4. Position driver at starting point
5. Place initial chunk at origin

### Runtime Generation

The system operates continuously during gameplay:

**Forward Spawning**  
When player distance to the last chunk falls below threshold (default: 50m):

- Select random chunk index using seeded RNG
- Retrieve chunk from object pool
- Calculate alignment based on previous chunk's exit anchor
- Add chunk to active segments list

**Backward Culling**  
When player distance from first chunk exceeds threshold (default: 30m):

- Remove chunk from active segments list
- Deactivate chunk and return to pool
- Update distance tracking

### Chunk Alignment Algorithm

Precise positioning ensures seamless connections:

1. Identify target anchor (exit point of previous chunk)
2. Align new chunk's rotation to match target
3. Calculate local offset from chunk pivot to entry anchor
4. Position chunk such that entry anchor coincides with target anchor

## Configuration

**SOTrack ScriptableObject** serves as the central configuration asset:

- Road/Env/Prop chunks
- Pool sizes

## Technical Considerations

### Initialization Order

Driver positioning must precede initial chunk placement to ensure correct spatial calculations. The recent refactor addressed a bug where the first chunk was placed at world origin before driver repositioning.

### Distance Thresholds

Current thresholds (50m forward, 30m backward) are tuned for optimal balance between visual continuity and memory efficiency. These values should be adjusted based on vehicle speed and track chunk dimensions.

### Data Structure Selection

The system uses LinkedList for active chunk management due to O(1) insertion and removal at both ends, matching the append-at-tail, remove-from-head access pattern.

## Performance Characteristics

**Memory Management**  
Object pooling eliminates runtime allocations, preventing GC spikes during gameplay.

**Rendering Optimization**  
Automatic culling of distant chunks reduces draw calls and maintains consistent frame rates.

**Deterministic Generation**  
Seed-based generation enables reproducible track layouts without storing entire track data.

## Extension Points

The current implementation provides foundation for:

- Biome-specific track sections with thematic variations
- Multi-path track generation with branching routes
- Dynamic obstacle and power-up placement
- Environmental population system for ambient objects

## Known Issues

- Driver must be positioned before first chunk instantiation to avoid incorrect placement
- No validation for chunk connection compatibility (assumes all chunks have compatible anchors)
