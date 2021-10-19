// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    /// helper class for cases where we have XML output
    public class StackWithCount {
        public string Callstack;
        public string Resolvedstack;
        public int Count;
    }
}
