/********************************************************************
 * * 使本项目源码前请仔细阅读以下协议内容，如果你同意以下协议才能使用本项目所有的功能,
 * * 否则如果你违反了以下协议，有可能陷入法律纠纷和赔偿，作者保留追究法律责任的权利。
 * *
 * * 1) 本代码为商业源代码，只允许已授权内部人员查看使用
 * * 2) 任何人员无权将代码泄露或者授权给其他未被授权人员使用
 * * 3) 任何修改请保留原始作者信息，不得擅自删除及修改
 * *
 * * Copyright (C) 2015-2022 Nimbus Corporation All rights reserved.
 * * 作者： Han Xu
 * * 请保留以上版权信息，否则作者将保留追究法律责任。
 * * 创建时间：2025/4/23 17:13:44
 ********************************************************************/

namespace MemoryMonitor.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// 内存监视工具.
    /// </summary>
    public class MemoryMonitorTools
    {
        private static string dllRoot = $"{UnityEngine.Application.dataPath}/../Library/ScriptAssemblies";

        private static List<string> assemblyFilters = new List<string>()
        {
           "MemoryMonitor.Test.dll",
           "Nimbus.",
        };

        /// <summary>
        /// 输出结果.
        /// </summary>
        [MenuItem("MemoryMonitor/输出结果")]
        private static void SaveToLocalFile()
        {
            MemoryProfiler.SaveToLocalFile();
        }

        /// <summary>
        /// 主动注入代码.
        /// </summary>
        [MenuItem("MemoryMonitor/注入代码")]
        private static void InjectHooks()
        {
            try
            {
                Debug.Log("InjectHooks running...");

                EditorApplication.LockReloadAssemblies();
                DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver();

                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in assemblies)
                {
                    if (!assembly.IsDynamic)
                    {
                        assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(assembly.Location));
                    }
                }

                assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(EditorApplication.applicationPath) + "/Data/Managed");

                ReaderParameters readerParameters = new ReaderParameters();
                readerParameters.AssemblyResolver = assemblyResolver;

                WriterParameters writerParameters = new WriterParameters();

                string[] dllFiles = Directory.GetFiles(dllRoot);
                foreach (string dllFullName in dllFiles)
                {
                    if (Path.GetExtension(dllFullName) != ".dll")
                    {
                        continue;
                    }

                    bool isMatched = false;
                    foreach (var filter in assemblyFilters)
                    {
                        if (dllFullName.Contains(filter))
                        {
                            isMatched = true;
                            break;
                        }
                    }

                    if (!isMatched)
                    {
                        continue;
                    }

                    string assemblyPath = dllFullName;
                    readerParameters.ReadSymbols = true;
                    writerParameters.WriteSymbols = true;

                    AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);
                    if (ProcessAssembly(assemblyDefinition))
                    {
                        assemblyDefinition.Write(assemblyPath, writerParameters);
                        Debug.Log($"inject completed: {Path.GetFileName(assemblyPath)}");
                    }
                    else
                    {
                        Debug.Log(Path.GetFileName(assemblyPath) + " didn't need to be processed");
                    }
                }

                Debug.Log("InjectHooks completed.");
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }

            EditorApplication.UnlockReloadAssemblies();
        }

        private static bool ProcessAssembly(AssemblyDefinition assemblyDefinition)
        {
            bool wasProcessed = false;

            foreach (ModuleDefinition moduleDefinition in assemblyDefinition.Modules)
            {
                foreach (TypeDefinition typeDefinition in moduleDefinition.Types)
                {
                    if (typeDefinition.Name == typeof(MemoryProfiler).Name
                        || typeDefinition.IsAbstract
                        || typeDefinition.IsInterface
                        || IsDelegateType(typeDefinition))
                    {
                        continue;
                    }

                    foreach (MethodDefinition methodDefinition in typeDefinition.Methods)
                    {
                        if (methodDefinition.Name == ".ctor"
                            || methodDefinition.Name == ".cctor"
                            || methodDefinition.IsGetter
                            || methodDefinition.IsSetter)
                        {
                            continue;
                        }

                        // 如果注入代码失败，可以打开下面的输出看看卡在了那个方法上。
                        ////Debug.Log(methodDefinition.Name + " ===== " + methodDefinition.Body + "======= " + typeDefinition.Name + "======= " + typeDefinition.BaseType.GenericParameters + " ===== " + moduleDefinition.Name);
                        MethodReference logMethodReference = moduleDefinition.ImportReference(typeof(MemoryProfiler).GetMethod("Begin", new Type[] { typeof(string) }));
                        MethodReference logMethodReference1 = moduleDefinition.ImportReference(typeof(MemoryProfiler).GetMethod("End", new Type[] { typeof(string) }));

                        // 如果注入方法失败可以试试先跳过
                        ////if (methodDefinition.Body == null)
                        ////{
                        ////    Debug.Log(methodDefinition.Name);
                        ////    continue;
                        ////}

                        ILProcessor ilProcessor = methodDefinition.Body.GetILProcessor();
                        Instruction first = methodDefinition.Body.Instructions[0];
                        ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, typeDefinition.FullName + "." + methodDefinition.Name));
                        ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Call, logMethodReference));

                        // 解决方法中直接 return 后无法统计的bug 
                        ////https://lostechies.com/gabrielschenker/2009/11/26/writing-a-profiler-for-silverlight-applications-part-1/

                        Instruction last = methodDefinition.Body.Instructions[methodDefinition.Body.Instructions.Count - 1];
                        Instruction lastInstruction = Instruction.Create(OpCodes.Ldstr, typeDefinition.FullName + "." + methodDefinition.Name);
                        ilProcessor.InsertBefore(last, lastInstruction);
                        ilProcessor.InsertBefore(last, Instruction.Create(OpCodes.Call, logMethodReference1));

                        var jumpInstructions = methodDefinition.Body.Instructions.Cast<Instruction>().Where(i => i.Operand == last);
                        foreach (var jump in jumpInstructions)
                        {
                            jump.Operand = lastInstruction;
                        }

                        wasProcessed = true;
                    }
                }
            }

            return wasProcessed;
        }

        private static bool IsDerivedFrom(TypeDefinition type, string typeName)
        {
            if (type.BaseType == null)
            {
                return false;
            }

            if (type.BaseType.Name == typeName)
            {
                return true;
            }

            var baseTypeDef = type.BaseType.Resolve();
            return baseTypeDef != null && IsDerivedFrom(baseTypeDef, typeName);
        }

        private static bool IsDelegateType(TypeDefinition type)
        {
            if (type == null)
            {
                return false;
            }

            // 检查基类链
            TypeDefinition currentType = type;
            while (currentType != null)
            {
                if (currentType.FullName == "System.MulticastDelegate" ||
                    currentType.FullName == "System.Delegate")
                {
                    // 检查是否存在 Invoke 方法
                    return type.Methods.Any(m => m.Name == "Invoke");
                }

                // 移动到基类
                currentType = currentType.BaseType?.Resolve();
            }

            return false;
        }
    }
}