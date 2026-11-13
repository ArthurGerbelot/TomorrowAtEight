// ARCH-005 | 2026-11-11 | Simulation systems for the provisional test scenes. All require
// GameSessionReady, so nothing runs for a partly initialized or torn-down session.
// Burst and jobs are intentionally not used until a web baseline is verified.
// Decision: .agents/ARCHITECTURE.md#arch-005---unity-entities-simulation-and-web-presentation
// Journal: .agents/days/2026-11-12.md
using TomorrowAtEight.Bootstrap;
using TomorrowAtEight.Simulation;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TomorrowAtEight.Definitions
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct SpinSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSessionReady>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (transform, spin) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Spin>>())
            {
                quaternion step = quaternion.AxisAngle(spin.ValueRO.Axis, spin.ValueRO.RadiansPerSecond * deltaTime);
                transform.ValueRW.Rotation = math.normalize(math.mul(step, transform.ValueRO.Rotation));
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct DriftSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSessionReady>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (transform, drift) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Drift>>())
                transform.ValueRW.Position += drift.ValueRO.Velocity * deltaTime;
        }
    }

    /// <summary>Deferred spawning: structural changes play back at EndSimulation (ARCH-001.b).</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct EmitterSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSessionReady>();
            state.RequireForUpdate<Emitter>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            EntityCommandBuffer commands = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (emitterRef, transform) in SystemAPI.Query<RefRW<Emitter>, RefRO<LocalTransform>>())
            {
                Emitter emitter = emitterRef.ValueRO;
                if (emitter.Prefab == Entity.Null || emitter.Interval <= 0f)
                    continue;

                emitter.Timer -= deltaTime;
                int burst = 0;
                while (emitter.Timer <= 0f && burst < 4)
                {
                    burst++;
                    emitter.Timer += emitter.Interval;

                    float angle = emitter.Rng.NextFloat(0f, math.PI * 2f);
                    float speed = emitter.Rng.NextFloat(1.0f, 2.4f);
                    var velocity = new float3(math.cos(angle) * speed, emitter.Rng.NextFloat(0.2f, 0.9f), math.sin(angle) * speed);
                    float scale = emitter.Rng.NextFloat(0.5f, 0.95f);

                    Entity spawned = EntityFactory.SpawnDeferred(commands, emitter.Prefab,
                        SpawnArgs.At(transform.ValueRO.Position, quaternion.identity, scale));
                    commands.SetComponent(spawned, new Drift { Velocity = velocity });
                    commands.SetComponent(spawned, new Lifetime { Remaining = emitter.SpawnLifetime });
                }
                if (emitter.Timer <= 0f)
                    emitter.Timer = emitter.Interval;

                emitterRef.ValueRW = emitter;
            }
        }
    }

    /// <summary>Deferred despawning; a composed entity's children leave with their root.</summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct LifetimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSessionReady>();
            state.RequireForUpdate<Lifetime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            EntityCommandBuffer commands = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (lifetime, entity) in SystemAPI.Query<RefRW<Lifetime>>().WithEntityAccess())
            {
                lifetime.ValueRW.Remaining -= deltaTime;
                if (lifetime.ValueRO.Remaining <= 0f)
                    EntityFactory.DespawnDeferred(commands, entity);
            }
        }
    }

    /// <summary>
    /// Input adapter: reads the Input System on the main thread and submits a command.
    /// Managed because devices, the camera, and the event system are managed Unity APIs.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    public partial class PawnInputSystem : SystemBase
    {
        const float GroundHalfExtent = 6.5f;

        protected override void OnCreate()
        {
            RequireForUpdate<GameSessionReady>();
            RequireForUpdate<MoveCommand>();
        }

        protected override void OnUpdate()
        {
            MoveCommand command = SystemAPI.GetSingleton<MoveCommand>();

            float2 direction = float2.zero;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) direction.y += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) direction.y -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) direction.x += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) direction.x -= 1f;
            }
            if (math.lengthsq(direction) > 0f)
            {
                direction = math.normalize(direction);
                command.HasTarget = false;
            }
            command.Direction = direction;

            Pointer pointer = Pointer.current;
            Camera camera = GameApp.Current == null ? null : GameApp.Current.Camera;
            if (pointer != null && camera != null && pointer.press.wasPressedThisFrame)
            {
                bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                Vector2 screenPoint = pointer.position.ReadValue();
                Ray ray = camera.ScreenPointToRay(new Vector3(screenPoint.x, screenPoint.y, 0f));
                if (!overUi && ray.direction.y < -0.0001f)
                {
                    // Intersect the pointer ray with the ground plane y = 0.
                    float distance = -ray.origin.y / ray.direction.y;
                    Vector3 hit = ray.origin + ray.direction * distance;
                    command.Target = new float3(
                        math.clamp(hit.x, -GroundHalfExtent, GroundHalfExtent),
                        0f,
                        math.clamp(hit.z, -GroundHalfExtent, GroundHalfExtent));
                    command.HasTarget = true;
                }
            }

            SystemAPI.SetSingleton(command);
        }
    }

    /// <summary>Fixed-step movement driven only by the command singleton.</summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct PawnMoveSystem : ISystem
    {
        const float GroundHalfExtent = 6.5f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameSessionReady>();
            state.RequireForUpdate<MoveCommand>();
        }

        public void OnUpdate(ref SystemState state)
        {
            MoveCommand command = SystemAPI.GetSingleton<MoveCommand>();
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, pawn) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<Pawn>>())
            {
                float3 position = transform.ValueRO.Position;
                float maxStep = pawn.ValueRO.Speed * deltaTime;
                float3 step = float3.zero;

                if (math.lengthsq(command.Direction) > 0f)
                {
                    step = new float3(command.Direction.x, 0f, command.Direction.y) * maxStep;
                }
                else if (command.HasTarget)
                {
                    float3 toTarget = command.Target - position;
                    toTarget.y = 0f;
                    float distance = math.length(toTarget);
                    if (distance > 0.02f)
                        step = toTarget / distance * math.min(maxStep, distance);
                }

                if (math.lengthsq(step) > 1e-10f)
                {
                    position += step;
                    position.x = math.clamp(position.x, -GroundHalfExtent, GroundHalfExtent);
                    position.z = math.clamp(position.z, -GroundHalfExtent, GroundHalfExtent);
                    transform.ValueRW.Position = position;
                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(math.normalize(step), math.up());
                }
            }

            foreach (RefRW<LocalTransform> transform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<TargetMarker>())
            {
                // Park the marker out of view while no target is commanded.
                transform.ValueRW.Position = command.HasTarget
                    ? new float3(command.Target.x, 0.4f, command.Target.z)
                    : new float3(0f, -100f, 0f);
            }
        }
    }
}
