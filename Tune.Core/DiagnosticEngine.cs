﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tune.Core.Services;

namespace Tune.Core
{
    public class DiagnosticEngine
    {
        public delegate void LogHandler(string message);
        public event LogHandler Log;
        private NativeTarget nativeTarget;
        private readonly IAssemblyPathsRepository _assemblyRepository;

        public DiagnosticEngine(IAssemblyPathsRepository assemblyRepository)
        {
            this.nativeTarget = new NativeTarget(Process.GetCurrentProcess().Id);
            _assemblyRepository = assemblyRepository;
        }

        public DiagnosticAssembly Compile(string script, DiagnosticAssemblyMode mode, DiagnosticAssembyPlatform platform)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(script);

            UpdateLog("Script parsed.");

            string assemblyName = $"assemblyName_{DateTime.Now.Ticks}";

            CSharpCompilation compilation = CSharpCompilation
                .Create(assemblyName)
                .WithOptions(CreateCompilationOptions(mode, platform))
                .AddSyntaxTrees(new[] { syntaxTree })
                .AddReferences(GetMetadataReferences(script.GetLines().GetScriptReferences(), platform));

            var result = new DiagnosticAssembly(this, assemblyName, compilation);
            return result;
        }

        private CSharpCompilationOptions CreateCompilationOptions(DiagnosticAssemblyMode mode, DiagnosticAssembyPlatform platform)
        {
            OptimizationLevel compilationLevel = mode == DiagnosticAssemblyMode.Release //cbMode.SelectedItem.ToString() == "Release"
             ? OptimizationLevel.Release
             : OptimizationLevel.Debug;
            Platform compilationPlatform = platform == DiagnosticAssembyPlatform.x64 ? Platform.X64 : Platform.X86;

            return new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: compilationLevel,
                    allowUnsafe: true,
                    platform: compilationPlatform);
        }

        public void UpdateLog(string message)
        {
            this.Log?.Invoke(message);
        }

        public Symbol ResolveNativeSymbol(ulong address)
        {
            return this.nativeTarget.ResolveSymbol(address);
        }

        public string ResolveSymbol(ulong address)
        {
            using (DataTarget target =
                DataTarget.AttachToProcess(Process.GetCurrentProcess().Id, 5000, AttachFlag.Passive))
            {
                foreach (ClrInfo version in target.ClrVersions)
                {
                    ClrRuntime runtime = target.ClrVersions.Single().CreateRuntime();
                    string methodSignature = runtime.GetMethodByAddress(address)
                        ?.GetFullSignature();
                    if (!string.IsNullOrWhiteSpace(methodSignature))
                    {
                        return methodSignature;
                    }
                }
            }

            Symbol symbol = this.nativeTarget.ResolveSymbol((ulong)address);
            if (!string.IsNullOrWhiteSpace(symbol.MethodName))
            {
                return symbol.ToString();
            }
            return null;
        }

        private MetadataReference[] GetMetadataReferences(IEnumerable<string> referencesNames, DiagnosticAssembyPlatform platform)
        {
            List<MetadataReference> references = new List<MetadataReference>();
            references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

            foreach (var referenceName in referencesNames)
            {
                var assemblyPath = _assemblyRepository.GetAssemblyPathBy(referenceName, platform);

                if (!string.IsNullOrEmpty(assemblyPath))
                {
                    references.Add(MetadataReference.CreateFromFile(assemblyPath));
                }
            }

            return references.ToArray();
        }
    }
}
