// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    using System.Collections.Generic;

    internal class ThreadParams {
        internal int threadOrdinal;
        internal List<StackWithCount> listOfCallStacks;
        internal string symPath;
        internal bool searchPDBsRecursively;
        internal List<string> dllPaths;
        internal bool searchDLLRecursively;
        internal bool framesOnSingleLine;
        internal bool includeSourceInfo;
        internal bool showInlineFrames;
        internal bool relookupSource;
        internal bool includeOffsets;
        internal int numThreads;
        internal bool cachePDB;
    }
}