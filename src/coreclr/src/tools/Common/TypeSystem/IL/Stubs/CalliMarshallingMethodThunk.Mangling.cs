// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

namespace Internal.IL.Stubs
{
    public partial class CalliMarshallingMethodThunk : IPrefixMangledSignature
    {
        MethodSignature IPrefixMangledSignature.BaseSignature
        {
            get
            {
                return _targetSignature;
            }
        }

        string IPrefixMangledSignature.Prefix
        {
            get
            {
                string callconv = "CallConvDefault";

                DefType callconvType = _targetSignature.GetCallConvType();
                if (callconvType != null)
                    callconv = callconvType.Name;

                return $"Calli_{callconv}";
            }
        }
    }
}
