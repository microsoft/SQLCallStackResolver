﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License - see LICENSE file in this repo.
namespace Microsoft.SqlServer.Utils.Misc.SQLCallStackResolver {
    /// <summary>
    /// Helper class for cases where we have XML frames with embedded module information.
    /// </summary>
    public class StackDetails {
        private string _annotation;
        private readonly string _stackKey;
        private string _callStack;
        private readonly bool _framesOnSingleLine;
        private string _resolvedStack;

        public StackDetails(string callStack, bool framesOnSingleLine, string annotation = null, string stackKey = null) {
            this._annotation = annotation;
            this._stackKey = stackKey;
            this._framesOnSingleLine = framesOnSingleLine;
            this._callStack = framesOnSingleLine ? Regex.Replace(callStack, @"\s{2,}", " ") : callStack;
            _stackKey = stackKey;
        }

        public string Callstack {
            get { return this._callStack; }
            set { this._callStack = value; }
        }

        public string[] CallstackFrames {
            get {
                // sometimes we see call stacks which are arranged horizontally (this typically is seen when copy-pasting directly
                // from the SSMS XEvent window (copying the callstack field without opening it in its own viewer)
                // in that case, space is a valid delimiter, and we need to support that as an option
                var delims = this._framesOnSingleLine ? new char[3] { ' ', '\t', '\n' } : new char[1] { '\n' };
                return this._callStack.Replace("\r", string.Empty).Split(delims);
            }
        }
        public string Resolvedstack {
            get {
                var sbOut = new StringBuilder();
                if (!(string.IsNullOrEmpty(this._annotation) && string.IsNullOrEmpty(this._stackKey))) {
                    if (!string.IsNullOrEmpty(this._annotation)) sbOut.AppendLine(this._annotation);
                    if (!string.IsNullOrEmpty(this._stackKey)) sbOut.AppendLine(this._stackKey);
                    sbOut.AppendLine();
                }
                sbOut.Append(this._resolvedStack);
                return sbOut.ToString();
            }
            set { this._resolvedStack = value; }
        }

        public void UpdateAnnotation(string extra) {
            this._annotation += extra;
        }
    }
}
