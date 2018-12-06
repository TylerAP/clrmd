namespace Microsoft.Diagnostics.Runtime {
    public enum CorDebugVersion : int
    {
        Invalid = 0,
        CorDebugVersion_1_0,
        CorDebugVersion_1_1,
        CorDebugVersion_2_0,
        CorDebugVersion_4_0,
        CorDebugVersion_4_5,
        CorDebugLatestVersion = CorDebugVersion_4_5
    }
}