#nullable enable

namespace Ayla
{
    public enum ExecutionTiming
    {
        TimeUpdate,
        Initialization,
        EarlyUpdate,
        PreUpdate,
        Update,
        FixedUpdate,
        PreLateUpdate,
        PostLateUpdate
    }
}
