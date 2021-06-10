// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    /// helper class for cases where we have XML output
    class StackWithCount {
        internal string Callstack;
        internal string Resolvedstack;
        internal int Count;
    }
}
