// ARCH-005 | 2026-11-11 | Unmanaged gameplay state for the provisional test scenes.
// Decision: .agents/ARCHITECTURE.md#arch-005---unity-entities-simulation-and-web-presentation
// Journal: .agents/days/2026-11-12.md
using Unity.Entities;
using Unity.Mathematics;

namespace TomorrowAtEight.Definitions
{
    /// <summary>Constant rotation around an axis.</summary>
    public struct Spin : IComponentData
    {
        public float3 Axis;
        public float RadiansPerSecond;
    }

    /// <summary>Constant linear velocity.</summary>
    public struct Drift : IComponentData
    {
        public float3 Velocity;
    }

    /// <summary>Seconds until the entity is despawned through a command buffer.</summary>
    public struct Lifetime : IComponentData
    {
        public float Remaining;
    }

    /// <summary>Spawns a previously resolved prefab at a fixed interval.</summary>
    public struct Emitter : IComponentData
    {
        public Entity Prefab;
        public float Interval;
        public float Timer;
        public float SpawnLifetime;
        public Random Rng;
    }

    /// <summary>Entity steered by the <see cref="MoveCommand"/> singleton.</summary>
    public struct Pawn : IComponentData
    {
        public float Speed;
    }

    /// <summary>Follows the commanded target position.</summary>
    public struct TargetMarker : IComponentData
    {
    }

    /// <summary>Singleton written by the input adapter and read by fixed-step simulation.</summary>
    public struct MoveCommand : IComponentData
    {
        public float2 Direction;
        public float3 Target;
        public bool HasTarget;
    }
}
