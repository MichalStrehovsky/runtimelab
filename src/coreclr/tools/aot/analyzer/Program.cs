// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using ILCompiler;
using ILCompiler.Dataflow;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

internal class Program
{
    static void Main()
    {
        // Run this twice because the first one is a warmup for CoreCLR
        // to load all assemblies and JIT everything.
        //
        // Compiling with NativeAOT would eliminate having to do that :).
        //
        // There's no static state because we don't do such things. Everything is computed anew.
        Main2();
        Main2();
    }

    static void Main2()
    {
        var sw = Stopwatch.StartNew();

        TargetDetails target = new TargetDetails(TargetArchitecture.Unknown, TargetOS.Unknown, TargetAbi.Unknown);
        var context = new CompilerTypeSystemContext(target, SharedGenericsMode.Disabled);
        context.InputFilePaths = new Dictionary<string, string>
        {
            { "System.Private.CoreLib", @"C:\git\runtime\artifacts\bin\coreclr\windows.x64.Release\aotsdk\System.Private.CoreLib.dll" }
        };
        context.ReferenceFilePaths = new Dictionary<string, string>();

        var logger = new Logger(Console.Out, isVerbose: false);

        var ilprovider = new AnalyzerILProvider();

        // Get a feature switch manager to eliminate unreachable blocks of IL
        var featureSwitchManager = new FeatureSwitchManager(ilprovider, switchValues: Array.Empty<KeyValuePair<string, bool>>());

        context.SetSystemModule(context.GetModuleForSimpleName("System.Private.CoreLib"));

        var annotations = new FlowAnnotations(logger, ilprovider);

        CountdownEvent countdown = new CountdownEvent(1);

        // Go over all methods and all types and methods in the assembly. We could instead go over all reachable
        // things, but that's more work than a 2 hour prototype (still not much more work).
        foreach (var type in context.SystemModule.GetAllTypes())
        {
            foreach (var method in type.GetMethods())
            {
                countdown.AddCount();

                ThreadPool.QueueUserWorkItem((state) =>
                {
                    MethodIL methodil = featureSwitchManager.GetMethodIL(method);

                    if (methodil != null)
                        ILCompiler.Dataflow.ReflectionMethodBodyScanner.ScanAndProcessReturnValue(null, annotations, logger, methodil);

                    countdown.Signal(1);
                });
            }
        }

        countdown.Signal(1);

        countdown.Wait();

        Console.WriteLine(sw.ElapsedMilliseconds);
    }
}
