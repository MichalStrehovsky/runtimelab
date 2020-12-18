// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    class NodeFactory
    {
    }
}

namespace ILCompiler.DependencyAnalysisFramework
{
    class DependencyNodeCore<T>
    {
        public class DependencyList : List<object>
        {

        }
    }
}

namespace ILCompiler
{
    partial class CompilerTypeSystemContext
    {
        public CompilerTypeSystemContext(TargetDetails details, SharedGenericsMode genericsMode)
            : base(details)
        {
            _genericsMode = genericsMode;
        }
    }
}
