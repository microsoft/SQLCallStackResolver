// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    internal class ThreadParams {
        internal int threadOrdinal, numThreads;
        internal List<StackDetails> listOfCallStacks;
        internal string symPath;
        internal List<string> dllPaths;
        internal bool searchPDBsRecursively, searchDLLRecursively, framesOnSingleLine, includeSourceInfo, showInlineFrames, relookupSource, includeOffsets, cachePDB;
    }
}
