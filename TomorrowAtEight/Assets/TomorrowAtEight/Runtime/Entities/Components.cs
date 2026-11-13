// ARCH-002, ARCH-005 | 2026-11-11 | Common unmanaged components shared by every session.
// Decision: .agents/ARCHITECTURE.md#arch-005---unity-entities-simulation-and-web-presentation
// Journal: .agents/days/2026-11-12.md
using Unity.Entities;
using Unity.Mathematics;

namespace TomorrowAtEight.Simulation
{
    /// <summary>
    /// Published by the session bootstrap only after successful initialization.
    /// Gameplay systems require it to update; it is removed before teardown.
    /// </summary>
    public struct GameSessionReady : IComponentData
    {
    }

    /// <summary>
    /// Marks every entity and prefab created for the current content session, so
    /// teardown can release them all without disposing the world.
    /// </summary>
    public struct SessionOwned : IComponentData
    {
    }

    /// <summary>
    /// Compact index of the entity's view template in the session's model table.
    /// Managed content keys stay out of component data (ARCH-001.a).
    /// </summary>
    public struct ViewModel : IComponentData
    {
        public int ModelIndex;
    }

    /// <summary>Initial placement for a spawned entity.</summary>
    public struct SpawnArgs
    {
        public float3 Position;
        public quaternion Rotation;
        public float Scale;

        public static SpawnArgs At(float3 position)
        {
            return new SpawnArgs { Position = position, Rotation = quaternion.identity, Scale = 1f };
        }

        public static SpawnArgs At(float3 position, quaternion rotation, float scale)
        {
            return new SpawnArgs { Position = position, Rotation = rotation, Scale = scale };
        }
    }
}
