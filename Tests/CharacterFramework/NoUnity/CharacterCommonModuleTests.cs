using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ProjectMS.CharacterSystem;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x;
        public float y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0f, 0f); } }
        public static Vector2 up { get { return new Vector2(0f, 1f); } }
        public static Vector2 down { get { return new Vector2(0f, -1f); } }
        public float sqrMagnitude { get { return x * x + y * y; } }
        public Vector2 normalized { get { float magnitude = (float)Math.Sqrt(sqrMagnitude); return magnitude > 0f ? new Vector2(x / magnitude, y / magnitude) : zero; } }
        public static Vector2 operator +(Vector2 left, Vector2 right) { return new Vector2(left.x + right.x, left.y + right.y); }
        public static Vector2 operator *(Vector2 value, float scalar) { return new Vector2(value.x * scalar, value.y * scalar); }
    }

    public static class Mathf
    {
        public static float Clamp(float value, float min, float max) { return Math.Max(min, Math.Min(max, value)); }
        public static float Max(float left, float right) { return Math.Max(left, right); }
        public static float Abs(float value) { return Math.Abs(value); }
        public static float MoveTowards(float current, float target, float maximumDelta) { return Math.Abs(target - current) <= maximumDelta ? target : current + Math.Sign(target - current) * maximumDelta; }
    }

    public enum ForceMode2D { Impulse }
    public struct Bounds { public Vector2 center; public Vector2 min; public Vector2 size; }
    public class Collider2D { public Bounds bounds; }
    public class Rigidbody2D
    {
        public Vector2 position;
        public Vector2 linearVelocity;
        public float gravityScale = 1f;
        public bool freezeRotation;
        public void AddForce(Vector2 force, ForceMode2D mode) { linearVelocity += force; }
    }
    public struct RaycastHit2D { public Collider2D collider; }
    public struct LayerMask { }
    public static class Physics2D
    {
        public static Vector2 gravity = new Vector2(0f, -9.81f);
        public static RaycastHit2D NextBoxCastHit;
        public static RaycastHit2D BoxCast(Vector2 origin, Vector2 size, float angle, Vector2 direction, float distance, LayerMask layer) { return NextBoxCastHit; }
    }
}

namespace ProjectMS.CharacterSystem
{
    public struct CharacterInputSnapshot
    {
        public float MoveDirection;
        public bool JumpPressed;
        public bool JumpHeld;
    }

    public sealed class CharacterDefinition
    {
        public float MoveSpeed { get; set; }
        public float GroundAcceleration { get; set; }
        public float AirAcceleration { get; set; }
        public float JumpForce { get; set; }
        public float MaxSpeed { get; set; }
        public float GroundCheckDistance { get; set; }
        public UnityEngine.LayerMask GroundLayer { get; set; }
        public float CoyoteTime { get; set; }
        public float JumpBufferTime { get; set; }
        public float FallGravityMultiplier { get; set; }
        public float LowJumpMultiplier { get; set; }
        public float DefaultDashPower { get; set; }
        public float DefaultDashDuration { get; set; }
        public bool AutoHop { get; set; }
        public float AutoHopInterval { get; set; }
        public float AutoHopForce { get; set; }
        public float AutoHopMoveThreshold { get; set; }

        public float GetCooldown(CharacterActionType action)
        {
            return action == CharacterActionType.BasicAttack ? 3f : 0f;
        }
    }
}

internal static class CharacterCommonModuleTests
{
    private static int failures;

    private sealed class FakeActionStateStore : ICharacterActionStateStore
    {
        private readonly Dictionary<CharacterActionType, bool> enabled =
            new Dictionary<CharacterActionType, bool>();
        private readonly Dictionary<CharacterActionType, int> charges =
            new Dictionary<CharacterActionType, int>();
        private readonly Dictionary<CharacterActionType, float> cooldownOverrides =
            new Dictionary<CharacterActionType, float>();
        private readonly Dictionary<CharacterActionType, bool> autoCooldown =
            new Dictionary<CharacterActionType, bool>();
        private readonly Dictionary<CharacterActionType, float> cooldownRemaining =
            new Dictionary<CharacterActionType, float>();

        public float LastStartedSeconds { get; private set; }

        public bool GetEnabled(CharacterActionType action)
        {
            bool value;
            return enabled.TryGetValue(action, out value) && value;
        }

        public void SetEnabled(CharacterActionType action, bool value)
        {
            enabled[action] = value;
        }

        public int GetCharges(CharacterActionType action)
        {
            int value;
            return charges.TryGetValue(action, out value) ? value : 0;
        }

        public void SetCharges(CharacterActionType action, int value)
        {
            charges[action] = value;
        }

        public float GetCooldownDurationOverride(CharacterActionType action)
        {
            float value;
            return cooldownOverrides.TryGetValue(action, out value) ? value : 0f;
        }

        public void SetCooldownDurationOverride(CharacterActionType action, float seconds)
        {
            cooldownOverrides[action] = seconds;
        }

        public bool GetAutoCooldown(CharacterActionType action)
        {
            bool value;
            return autoCooldown.TryGetValue(action, out value) && value;
        }

        public void SetAutoCooldown(CharacterActionType action, bool value)
        {
            autoCooldown[action] = value;
        }

        public bool IsCooldownRunning(CharacterActionType action)
        {
            float value;
            return cooldownRemaining.TryGetValue(action, out value) && value > 0f;
        }

        public void StartCooldown(CharacterActionType action, float seconds)
        {
            LastStartedSeconds = seconds;
            cooldownRemaining[action] = seconds;
        }

        public void ClearCooldown(CharacterActionType action)
        {
            cooldownRemaining.Remove(action);
        }

        public float GetCooldownRemaining(CharacterActionType action)
        {
            float value;
            return cooldownRemaining.TryGetValue(action, out value) ? value : 0f;
        }
    }

    private static void Equal<T>(T expected, T actual, string name)
    {
        if (!Equals(expected, actual))
        {
            failures++;
            Console.Error.WriteLine("FAIL {0}: expected={1}, actual={2}", name, expected, actual);
        }
    }

    private static void UnexpectedDamageCallback()
    {
        throw new InvalidOperationException("unexpected damage pipeline callback");
    }

    private static void TestStatusHandling()
    {
        Assembly assembly = typeof(CharacterCommonModuleTests).Assembly;
        Type handlerType = assembly.GetType("ProjectMS.CharacterSystem.CharacterStatusHandler", false);
        Equal(true, handlerType != null, "status handler exists");
        if (handlerType == null)
            return;

        Type storeType = assembly.GetType("ProjectMS.CharacterSystem.ICharacterStatusStateStore", false);
        Equal(true, storeType != null, "status state store exists");
        if (storeType == null)
            return;

        object store = CreateStatusStore(storeType);
        object handler = Activator.CreateInstance(
            handlerType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new object[] { store },
            null);

        MethodInfo applySlow = handlerType.GetMethod("ApplySlow");
        MethodInfo tick = handlerType.GetMethod("Tick");
        PropertyInfo multiplier = handlerType.GetProperty("MovementSpeedMultiplier");
        Equal(true, applySlow != null, "status handler applies slow");
        Equal(true, tick != null, "status handler ticks slow expiry");
        Equal(true, multiplier != null, "status handler exposes movement multiplier");
        if (applySlow == null || tick == null || multiplier == null)
            return;

        Equal(1f, (float)multiplier.GetValue(handler, null), "normal movement multiplier");

        applySlow.Invoke(handler, new object[] { 0.4f, 0.5f });
        Equal(0.4f, GetStoreField<float>(store, "SlowRatio"), "first slow applied");
        Equal(0.5f, GetStoreField<float>(store, "LastDuration"), "first slow duration");

        applySlow.Invoke(handler, new object[] { 0.2f, 3f });
        Equal(0.4f, GetStoreField<float>(store, "SlowRatio"), "weaker slow ignored");
        Equal(0.5f, GetStoreField<float>(store, "LastDuration"), "weaker slow does not refresh timer");

        applySlow.Invoke(handler, new object[] { 0.4f, 1f });
        Equal(1f, GetStoreField<float>(store, "LastDuration"), "equal slow refreshes timer");

        applySlow.Invoke(handler, new object[] { 0.7f, 0.25f });
        Equal(0.7f, GetStoreField<float>(store, "SlowRatio"), "stronger slow replaces current");
        Equal(0.3f, (float)multiplier.GetValue(handler, null), "slow changes movement multiplier");

        // A network-backed TickTimer stays configured when it expires.  The
        // status contract must therefore present the expired timer as running
        // long enough for Tick() to clear the replicated slow exactly once.
        Equal(true, GetStoreField<bool>(store, "IsSlowRunning"),
            "configured slow remains observable before expiry clear");
        SetStoreField(store, "IsSlowExpired", true);
        tick.Invoke(handler, new object[0]);
        Equal(0f, GetStoreField<float>(store, "SlowRatio"), "expired slow clears ratio");
        Equal(1, GetStoreField<int>(store, "ClearCount"), "expired slow clears exactly once");
        tick.Invoke(handler, new object[0]);
        Equal(1, GetStoreField<int>(store, "ClearCount"), "cleared slow is not cleared again");

        object clampedStore = CreateStatusStore(storeType);
        object clampedHandler = Activator.CreateInstance(handlerType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { clampedStore }, null);
        applySlow.Invoke(clampedHandler, new object[] { 4f, -1f });
        Equal(0.99f, GetStoreField<float>(clampedStore, "SlowRatio"), "slow ratio clamps to maximum");
        Equal(0f, GetStoreField<float>(clampedStore, "LastDuration"), "slow duration clamps to zero");

        object minimumStore = CreateStatusStore(storeType);
        object minimumHandler = Activator.CreateInstance(handlerType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { minimumStore }, null);
        applySlow.Invoke(minimumHandler, new object[] { -4f, 1f });
        Equal(0f, GetStoreField<float>(minimumStore, "SlowRatio"), "slow ratio clamps to minimum");
    }

    private static void TestMovementBoundary()
    {
        CharacterDefinition definition = new CharacterDefinition
        {
            MoveSpeed = 10f,
            GroundAcceleration = 100f,
            AirAcceleration = 100f,
            JumpForce = 8f,
            MaxSpeed = 100f,
            CoyoteTime = 0.1f,
            JumpBufferTime = 0.1f,
            FallGravityMultiplier = 2f,
            LowJumpMultiplier = 1f,
            DefaultDashPower = 14f,
            DefaultDashDuration = 1f,
            AutoHop = true,
            AutoHopInterval = 0.01f,
            AutoHopForce = 5f,
            AutoHopMoveThreshold = 0.05f
        };

        UnityEngine.Rigidbody2D ordinaryBody = new UnityEngine.Rigidbody2D
        {
            linearVelocity = new UnityEngine.Vector2(6f, -3f),
            gravityScale = 2f
        };
        UnityEngine.Collider2D collider = new UnityEngine.Collider2D();
        CharacterMovementHandler ordinaryHandler = new CharacterMovementHandler(ordinaryBody, collider, definition);
        ordinaryHandler.SetMovementEnabled(false);
        Equal(0f, ordinaryBody.linearVelocity.x, "disabled normal movement stops ordinary horizontal velocity immediately");
        Equal(-3f, ordinaryBody.linearVelocity.y, "movement lock preserves vertical velocity");
        Equal(2f, ordinaryBody.gravityScale, "movement lock preserves gravity");
        ordinaryHandler.Tick(new CharacterInputSnapshot(), 0.01f);
        Equal(true, ordinaryBody.linearVelocity.y < -3f, "movement lock preserves falling gravity");

        UnityEngine.Rigidbody2D jumpBody = new UnityEngine.Rigidbody2D();
        UnityEngine.Physics2D.NextBoxCastHit = new UnityEngine.RaycastHit2D { collider = collider };
        CharacterMovementHandler jumpHandler = new CharacterMovementHandler(jumpBody, collider, definition);
        jumpHandler.SetMovementEnabled(false);
        jumpHandler.Tick(new CharacterInputSnapshot { MoveDirection = 1f, JumpPressed = true, JumpHeld = true }, 0.01f);
        Equal(0f, jumpBody.linearVelocity.y, "movement lock blocks manual jump");
        Equal(0f, jumpBody.linearVelocity.x, "movement lock blocks horizontal input");
        Equal(0f, jumpHandler.MoveInput, "movement lock clears movement input state");

        UnityEngine.Rigidbody2D autoHopBody = new UnityEngine.Rigidbody2D();
        CharacterMovementHandler autoHopHandler = new CharacterMovementHandler(autoHopBody, collider, definition);
        autoHopHandler.SetMovementEnabled(false);
        autoHopHandler.Tick(new CharacterInputSnapshot { MoveDirection = 1f }, 0.01f);
        Equal(0f, autoHopBody.linearVelocity.y, "movement lock blocks auto-hop");

        UnityEngine.Rigidbody2D dashBody = new UnityEngine.Rigidbody2D
        {
            linearVelocity = new UnityEngine.Vector2(6f, -3f),
            gravityScale = 2f
        };
        CharacterMovementHandler dashHandler = new CharacterMovementHandler(dashBody, collider, definition);
        dashHandler.StartDefaultDash();
        dashHandler.SetMovementEnabled(false);
        Equal(false, dashHandler.IsDashing, "movement lock cancels active dash");
        Equal(0f, dashBody.linearVelocity.x, "movement lock zeroes active dash horizontal velocity");
        Equal(-3f, dashBody.linearVelocity.y, "active dash cancellation preserves vertical velocity");
        Equal(2f, dashBody.gravityScale, "active dash cancellation restores original gravity");

        dashHandler.StartDefaultDash();
        Equal(false, dashHandler.IsDashing, "movement lock blocks default dash start");
        dashHandler.StartDash(UnityEngine.Vector2.up, 20f, 1f);
        Equal(false, dashHandler.IsDashing, "movement lock blocks custom dash start");
        Equal(2f, dashBody.gravityScale, "blocked dash start preserves gravity");

        UnityEngine.Rigidbody2D enabledBody = new UnityEngine.Rigidbody2D();
        CharacterMovementHandler enabledHandler = new CharacterMovementHandler(enabledBody, collider, definition);
        enabledHandler.SetMovementEnabled(false);
        enabledHandler.SetMovementEnabled(true);
        enabledHandler.Tick(new CharacterInputSnapshot { MoveDirection = 1f, JumpPressed = true, JumpHeld = true }, 0.01f);
        Equal(1f, enabledBody.linearVelocity.x, "re-enabled movement restores horizontal input");
        Equal(8f, enabledBody.linearVelocity.y, "re-enabled movement restores jump");
        enabledHandler.StartDefaultDash();
        Equal(true, enabledHandler.IsDashing, "re-enabled movement restores dash");
    }

    private static void TestTimers()
    {
        CharacterTimerHandler timers = new CharacterTimerHandler();
        int calls = 0;
        CharacterTimerHandle handle = timers.Schedule(0.5f, () => calls++);
        timers.Tick(0.25f);
        Equal(0, calls, "timer not early");
        timers.Tick(0.25f);
        Equal(1, calls, "timer executes once");
        timers.Tick(1f);
        Equal(1, calls, "timer does not repeat");

        CharacterTimerHandle cancelled = timers.Schedule(1f, () => calls++);
        Equal(true, timers.Cancel(cancelled), "active timer cancelled");
        Equal(false, timers.Cancel(cancelled), "cancelled timer cannot be cancelled twice");
        timers.Tick(2f);
        Equal(1, calls, "cancelled timer does not execute");

        int nestedCalls = 0;
        timers.Schedule(0f, () =>
        {
            nestedCalls++;
            timers.Schedule(0f, () => nestedCalls++);
        });
        timers.Tick(0f);
        Equal(1, nestedCalls, "timer scheduled by callback waits for next tick");
        timers.Tick(0f);
        Equal(2, nestedCalls, "timer scheduled by callback executes on next tick");

        int callbackCancelledCalls = 0;
        CharacterTimerHandle cancelledByCallback = default(CharacterTimerHandle);
        timers.Schedule(0f, () => timers.Cancel(cancelledByCallback));
        cancelledByCallback = timers.Schedule(0f, () => callbackCancelledCalls++);
        timers.Tick(0f);
        Equal(0, callbackCancelledCalls, "callback cancellation prevents pending expiry invocation");

        int clearedCalls = 0;
        timers.Schedule(0f, () => clearedCalls++);
        timers.Schedule(1f, () => clearedCalls++);
        timers.CancelAll();
        timers.Tick(2f);
        Equal(0, clearedCalls, "cancel all prevents every pending timer invocation");
        timers.Schedule(-1f, () => clearedCalls++);
        timers.Tick(0f);
        Equal(1, clearedCalls, "handler can schedule again after cancel all");
    }

    private static void TestDamagePipeline()
    {
        float requested = -1f;
        float notified = -1f;
        int modifierCalls = 0;
        int requestCalls = 0;
        int notifyCalls = 0;
        List<string> order = new List<string>();
        CharacterDamagePipeline pipeline = new CharacterDamagePipeline(
            (damage, source) =>
            {
                modifierCalls++;
                order.Add("modify:" + source);
                return source == CharacterDamageSource.Projectile ? damage * 2f : damage + 1f;
            },
            damage =>
            {
                requestCalls++;
                requested = damage;
                order.Add("request");
            },
            damage =>
            {
                notifyCalls++;
                notified = damage;
                order.Add("notify");
            });

        pipeline.Apply(4f, CharacterDamageSource.Direct);
        Equal(5f, requested, "direct modifier applies final request damage");
        Equal(5f, notified, "direct damage dealt receives final damage");
        Equal(1, modifierCalls, "direct modifier invoked once");
        Equal(1, requestCalls, "direct request invoked once");
        Equal(1, notifyCalls, "direct notification invoked once");
        Equal("modify:Direct,request,notify", string.Join(",", order.ToArray()),
            "direct request completes before notification");

        requested = -1f;
        notified = -1f;
        order.Clear();
        pipeline.Apply(10f, CharacterDamageSource.Projectile);
        Equal(20f, requested, "projectile modifier applies final request damage");
        Equal(20f, notified, "projectile damage dealt receives final damage");
        Equal(2, modifierCalls, "projectile modifier invoked once");
        Equal(2, requestCalls, "projectile request invoked once");
        Equal(2, notifyCalls, "projectile notification invoked once");
        Equal("modify:Projectile,request,notify", string.Join(",", order.ToArray()),
            "projectile request completes before notification");

        int stoppedRequests = 0;
        int stoppedNotifications = 0;
        CharacterDamagePipeline stopped = new CharacterDamagePipeline(
            (damage, source) => 0f,
            damage => stoppedRequests++,
            damage => stoppedNotifications++);
        stopped.Apply(10f, CharacterDamageSource.Projectile);
        Equal(0, stoppedRequests, "zero modified damage does not request target damage");
        Equal(0, stoppedNotifications, "zero modified damage does not notify dealt damage");

        int negativeModifierCalls = 0;
        int negativeRequests = 0;
        int negativeNotifications = 0;
        CharacterDamagePipeline negativeStopped = new CharacterDamagePipeline(
            (damage, source) => { negativeModifierCalls++; return -damage; },
            damage => negativeRequests++,
            damage => negativeNotifications++);
        negativeStopped.Apply(6f, CharacterDamageSource.Direct);
        Equal(1, negativeModifierCalls, "negative direct modifier runs once");
        Equal(0, negativeRequests, "negative modified direct damage does not request target damage");
        Equal(0, negativeNotifications, "negative modified direct damage does not notify dealt damage");

        int invalidModifierCalls = 0;
        CharacterDamagePipeline invalid = new CharacterDamagePipeline(
            (damage, source) => { invalidModifierCalls++; return damage; },
            damage => UnexpectedDamageCallback(),
            damage => UnexpectedDamageCallback());
        invalid.Apply(0f, CharacterDamageSource.Direct);
        invalid.Apply(-1f, CharacterDamageSource.Projectile);
        Equal(0, invalidModifierCalls, "zero and negative damage do not enter the pipeline");

        int missingTargetNotifications = 0;
        CharacterDamagePipeline missingTarget = new CharacterDamagePipeline(
            (damage, source) => damage,
            null,
            damage => missingTargetNotifications++);
        missingTarget.Apply(5f, CharacterDamageSource.Direct);
        Equal(0, missingTargetNotifications, "missing target request does not notify dealt damage");
    }

    private static void TestOwnedEntityDurabilityRules()
    {
        Equal(false, OwnedEntityDurabilityRules.UsesHealth(OwnedEntityLifetimeMode.Manual),
            "manual owned entity does not use health");
        Equal(true, OwnedEntityDurabilityRules.UsesHealth(OwnedEntityLifetimeMode.Health),
            "health mode uses health");
        Equal(false, OwnedEntityDurabilityRules.UsesDuration(OwnedEntityLifetimeMode.Health),
            "health mode does not use duration");
        Equal(true, OwnedEntityDurabilityRules.UsesDuration(OwnedEntityLifetimeMode.Duration),
            "duration mode uses duration");
        Equal(true, OwnedEntityDurabilityRules.UsesHealth(OwnedEntityLifetimeMode.HealthOrDuration),
            "combined mode uses health");
        Equal(true, OwnedEntityDurabilityRules.UsesDuration(OwnedEntityLifetimeMode.HealthOrDuration),
            "combined mode uses duration");

        Equal(false, OwnedEntityDurabilityRules.CanReceiveDamage(
                OwnedEntityLifetimeMode.Duration, OwnedEntityDamageRelation.Enemy, false, false, 10f),
            "duration-only entity rejects damage");
        Equal(false, OwnedEntityDurabilityRules.CanReceiveDamage(
                OwnedEntityLifetimeMode.Health, OwnedEntityDamageRelation.Self, false, false, 10f),
            "self damage blocked by default");
        Equal(false, OwnedEntityDurabilityRules.CanReceiveDamage(
                OwnedEntityLifetimeMode.Health, OwnedEntityDamageRelation.Friendly, false, false, 10f),
            "friendly damage blocked by default");
        Equal(true, OwnedEntityDurabilityRules.CanReceiveDamage(
                OwnedEntityLifetimeMode.Health, OwnedEntityDamageRelation.Enemy, false, false, 10f),
            "enemy damage allowed by default");
        Equal(true, OwnedEntityDurabilityRules.CanReceiveDamage(
                OwnedEntityLifetimeMode.Health, OwnedEntityDamageRelation.Self, true, false, 10f),
            "self damage can be enabled");
        Equal(true, OwnedEntityDurabilityRules.CanReceiveDamage(
                OwnedEntityLifetimeMode.Health, OwnedEntityDamageRelation.Friendly, false, true, 10f),
            "friendly damage can be enabled");
        Equal(false, OwnedEntityDurabilityRules.CanReceiveDamage(
                OwnedEntityLifetimeMode.Health, OwnedEntityDamageRelation.Enemy, false, false, 0f),
            "zero damage rejected");
        Equal(false, OwnedEntityDurabilityRules.CanReceiveDamage(
                OwnedEntityLifetimeMode.Health, OwnedEntityDamageRelation.Enemy, false, false, float.NaN),
            "NaN damage rejected");
        Equal(false, OwnedEntityDurabilityRules.CanReceiveDamage(
                OwnedEntityLifetimeMode.Health, OwnedEntityDamageRelation.Enemy, false, false, float.PositiveInfinity),
            "infinite damage rejected");

        Equal(OwnedEntityDestroyReason.HealthDepleted,
            OwnedEntityDurabilityRules.ResolveDestructionReason(true, true),
            "health depletion wins same-tick lifetime tie");
        Equal(OwnedEntityDestroyReason.LifetimeExpired,
            OwnedEntityDurabilityRules.ResolveDestructionReason(false, true),
            "lifetime expiry selected without health depletion");
        Equal(OwnedEntityDestroyReason.None,
            OwnedEntityDurabilityRules.ResolveDestructionReason(false, false),
            "no completed condition returns no destruction");
    }

    private static object CreateStatusStore(Type storeType)
    {
        AssemblyName name = new AssemblyName("CharacterStatusStoreTestAssembly" + Guid.NewGuid().ToString("N"));
        TypeBuilder builder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run)
            .DefineDynamicModule(name.Name).DefineType("FakeStatusStore", TypeAttributes.Public);
        builder.AddInterfaceImplementation(storeType);
        FieldBuilder slowRatio = DefineStatusField(builder, "SlowRatio", typeof(float));
        FieldBuilder slowRunning = DefineStatusField(builder, "IsSlowRunning", typeof(bool));
        FieldBuilder slowExpired = DefineStatusField(builder, "IsSlowExpired", typeof(bool));
        FieldBuilder lastDuration = DefineStatusField(builder, "LastDuration", typeof(float));
        FieldBuilder clearCount = DefineStatusField(builder, "ClearCount", typeof(int));
        DefineStatusProperty(builder, storeType, "SlowRatio", typeof(float), true, slowRatio);
        DefineStatusProperty(builder, storeType, "IsSlowRunning", typeof(bool), false, slowRunning);
        DefineStatusProperty(builder, storeType, "IsSlowExpired", typeof(bool), false, slowExpired);
        DefineStartSlow(builder, storeType, lastDuration, slowRunning, slowExpired);
        DefineClearSlow(builder, storeType, slowRatio, slowRunning, slowExpired, clearCount);
        return Activator.CreateInstance(builder.CreateType());
    }

    private static FieldBuilder DefineStatusField(TypeBuilder builder, string name, Type type)
    {
        return builder.DefineField(name, type, FieldAttributes.Public);
    }

    private static void DefineStatusProperty(TypeBuilder builder, Type interfaceType, string name, Type type, bool writable, FieldInfo field)
    {
        PropertyBuilder property = builder.DefineProperty(name, PropertyAttributes.None, type, null);
        MethodBuilder getter = builder.DefineMethod("get_" + name,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            type, Type.EmptyTypes);
        ILGenerator getIl = getter.GetILGenerator();
        getIl.Emit(OpCodes.Ldarg_0);
        getIl.Emit(OpCodes.Ldfld, field);
        getIl.Emit(OpCodes.Ret);
        property.SetGetMethod(getter);
        builder.DefineMethodOverride(getter, interfaceType.GetProperty(name).GetGetMethod());
        if (!writable)
            return;

        MethodBuilder setter = builder.DefineMethod("set_" + name,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            null, new Type[] { type });
        ILGenerator setIl = setter.GetILGenerator();
        setIl.Emit(OpCodes.Ldarg_0);
        setIl.Emit(OpCodes.Ldarg_1);
        setIl.Emit(OpCodes.Stfld, field);
        setIl.Emit(OpCodes.Ret);
        property.SetSetMethod(setter);
        builder.DefineMethodOverride(setter, interfaceType.GetProperty(name).GetSetMethod());
    }

    private static void DefineStartSlow(TypeBuilder builder, Type interfaceType, FieldInfo lastDuration, FieldInfo slowRunning, FieldInfo slowExpired)
    {
        MethodBuilder method = builder.DefineMethod("StartSlow", MethodAttributes.Public | MethodAttributes.Virtual,
            null, new Type[] { typeof(float) });
        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Stfld, lastDuration);
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldc_I4_1); il.Emit(OpCodes.Stfld, slowRunning);
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Stfld, slowExpired);
        il.Emit(OpCodes.Ret);
        builder.DefineMethodOverride(method, interfaceType.GetMethod("StartSlow"));
    }

    private static void DefineClearSlow(TypeBuilder builder, Type interfaceType, FieldInfo slowRatio, FieldInfo slowRunning, FieldInfo slowExpired, FieldInfo clearCount)
    {
        MethodBuilder method = builder.DefineMethod("ClearSlow", MethodAttributes.Public | MethodAttributes.Virtual,
            null, Type.EmptyTypes);
        ILGenerator il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldc_R4, 0f); il.Emit(OpCodes.Stfld, slowRatio);
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Stfld, slowRunning);
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Stfld, slowExpired);
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Ldfld, clearCount);
        il.Emit(OpCodes.Ldc_I4_1); il.Emit(OpCodes.Add); il.Emit(OpCodes.Stfld, clearCount);
        il.Emit(OpCodes.Ret);
        builder.DefineMethodOverride(method, interfaceType.GetMethod("ClearSlow"));
    }

    private static T GetStoreField<T>(object store, string name)
    {
        return (T)store.GetType().GetField(name).GetValue(store);
    }

    private static void SetStoreField(object store, string name, object value)
    {
        store.GetType().GetField(name).SetValue(store, value);
    }

    public static int Run()
    {
        Equal(0, (int)CharacterDamageSource.Direct, "direct damage enum");
        Equal(1, (int)CharacterDamageSource.Projectile, "projectile damage enum");
        Equal(2, (int)CharacterDamageSource.Area, "area damage enum");
        Equal(0, (int)ProjectileDespawnReason.HitCharacter, "hit character reason");
        Equal(3, (int)ProjectileDespawnReason.Manual, "manual reason");
        Equal(4, (int)ProjectileDespawnReason.HitOwnedEntity, "owned entity hit reason");

        CharacterTimerHandle invalid = default(CharacterTimerHandle);
        Equal(false, invalid.IsValid, "default timer handle invalid");
        Equal(true, new CharacterTimerHandle(7).IsValid, "positive timer handle valid");

        Type actionStateHandler = typeof(CharacterCommonModuleTests).Assembly.GetType(
            "ProjectMS.CharacterSystem.CharacterActionStateHandler",
            false);
        Equal(true, actionStateHandler != null, "action state handler exists");

        FakeActionStateStore store = new FakeActionStateStore();
        CharacterActionStateHandler handler = new CharacterActionStateHandler(
            store,
            action => action == CharacterActionType.Dash ? -4f : 5f);
        handler.Initialize();
        Equal(true, handler.CanUse(CharacterActionType.BasicAttack), "default action usable");
        Equal(-1, handler.GetCharges(CharacterActionType.BasicAttack), "default unlimited charges");
        Equal(false, handler.CanUse(CharacterActionType.None), "none action blocked");

        handler.SetCharges(CharacterActionType.BasicAttack, 2);
        handler.ConsumeCharge(CharacterActionType.BasicAttack);
        Equal(1, handler.GetCharges(CharacterActionType.BasicAttack), "charge consumed");
        handler.ConsumeCharge(CharacterActionType.BasicAttack);
        handler.ConsumeCharge(CharacterActionType.BasicAttack);
        Equal(0, handler.GetCharges(CharacterActionType.BasicAttack), "empty charge does not underflow");
        handler.SetCharges(CharacterActionType.BasicAttack, -3);
        Equal(-1, handler.GetCharges(CharacterActionType.BasicAttack), "charges clamp to unlimited sentinel");

        handler.SetEnabled(CharacterActionType.SkillQ, false);
        Equal(false, handler.CanUse(CharacterActionType.SkillQ), "disabled action blocked");

        handler.SetCooldownDuration(CharacterActionType.Ultimate, 12f);
        handler.StartCooldown(CharacterActionType.Ultimate);
        Equal(12f, store.LastStartedSeconds, "override cooldown duration");
        Equal(false, handler.CanUse(CharacterActionType.Ultimate), "running cooldown blocks action");
        handler.ClearCooldown(CharacterActionType.Ultimate);
        Equal(true, handler.CanUse(CharacterActionType.Ultimate), "cleared cooldown unblocks action");
        Equal(0f, handler.GetCooldownDuration(CharacterActionType.Dash), "negative default cooldown clamps to zero");

        handler.SetAutoCooldown(CharacterActionType.Ultimate, false);
        Equal(false, handler.ShouldStartCooldownAutomatically(CharacterActionType.Ultimate), "deferred cooldown");

        System.Reflection.PropertyInfo stateProperty = typeof(CharacterCooldownHandler).GetProperty(
            "State",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Equal(true, stateProperty != null, "cooldown adapter has shared action state");

        CharacterCooldownHandler cooldowns = new CharacterCooldownHandler(new CharacterDefinition());
        cooldowns.State.SetEnabled(CharacterActionType.BasicAttack, false);
        Equal(false, cooldowns.CanUse(CharacterActionType.BasicAttack), "adapter reads shared enabled state");
        cooldowns.State.SetEnabled(CharacterActionType.BasicAttack, true);
        cooldowns.State.SetCooldownDuration(CharacterActionType.BasicAttack, 7f);
        cooldowns.Start(CharacterActionType.BasicAttack);
        Equal(7f, cooldowns.GetRemaining(CharacterActionType.BasicAttack), "adapter starts shared override cooldown");
        cooldowns.Tick(2f);
        Equal(5f, cooldowns.GetRemaining(CharacterActionType.BasicAttack), "adapter ticks shared cooldown");
        cooldowns.ResetAll();
        Equal(0f, cooldowns.GetRemaining(CharacterActionType.BasicAttack), "adapter reset clears shared cooldown");

        System.Reflection.ConstructorInfo sharedStateConstructor =
            typeof(CharacterCooldownHandler).GetConstructor(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                new Type[] { typeof(CharacterActionStateHandler) },
                null);
        Equal(true, sharedStateConstructor != null, "cooldown adapter accepts shared action state");

        if (sharedStateConstructor != null)
        {
            FakeActionStateStore sharedStore = new FakeActionStateStore();
            CharacterActionStateHandler sharedState = new CharacterActionStateHandler(
                sharedStore,
                action => 4f);
            sharedState.Initialize();
            CharacterCooldownHandler sharedCooldowns = (CharacterCooldownHandler)sharedStateConstructor.Invoke(
                new object[] { sharedState });

            sharedState.SetCooldownDuration(CharacterActionType.SkillQ, 11f);
            sharedCooldowns.Start(CharacterActionType.SkillQ);
            Equal(11f, sharedState.GetCooldownRemaining(CharacterActionType.SkillQ),
                "legacy start updates supplied shared state");
            sharedCooldowns.ResetAll();
            Equal(0f, sharedState.GetCooldownRemaining(CharacterActionType.SkillQ),
                "legacy reset clears supplied shared state");
            sharedState.SetEnabled(CharacterActionType.SkillQ, false);
            Equal(false, sharedCooldowns.CanUse(CharacterActionType.SkillQ),
                "legacy can use observes supplied shared state");
        }
        TestStatusHandling();
        TestMovementBoundary();
        TestTimers();
        TestDamagePipeline();
        TestOwnedEntityDurabilityRules();
        return failures;
    }
}
