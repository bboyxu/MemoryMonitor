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
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;
    using UnityEngine;
    using UnityEngine.Profiling;

    /// <summary>
    /// 内存统计的钩子函数工具类.
    /// </summary>
    public class HookUtils
    {
        private static Dictionary<string, FunctionData> dataRecord = new Dictionary<string, FunctionData>();

        private static Thread mainThread = Thread.CurrentThread;

        /// <summary>
        /// 统计开始.
        /// </summary>
        /// <param name="name">被监控的函数名.</param>
        public static void Begin(string name)
        {
            if (Thread.CurrentThread == mainThread)
            {
                long tmpMen = Profiler.GetTotalAllocatedMemoryLong();
                float tmpTime = Time.realtimeSinceStartup;
                if (dataRecord.ContainsKey(name))
                {
                    FunctionData tmp = dataRecord[name];
                    tmp.StartMemory = tmpMen;
                    tmp.StartTime = tmpTime;
                    dataRecord[name] = tmp;
                }
                else
                {
                    FunctionData tmp = new()
                    {
                        Name = name,
                        OnceMemory = 0L,
                        OnceTime = 0f,
                        Calls = 0,
                        TotalMemory = 0L,
                        TotalTime = 0f,
                        StartMemory = tmpMen,
                        StartTime = tmpTime,
                    };

                    dataRecord.Add(name, tmp);
                }
            }
        }

        /// <summary>
        /// 统计结束.
        /// </summary>
        /// <param name="name">被监控的函数名.</param>
        public static void End(string name)
        {
            if (Thread.CurrentThread == mainThread)
            {
                long tmpMem = Profiler.GetTotalAllocatedMemoryLong();
                float tmpTime = Time.realtimeSinceStartup;
                FunctionData tmp = dataRecord[name];

                // 过滤因为GC而统计不正确的数据
                if (tmpMem - tmp.StartMemory >= 0)
                {
                    tmp.OnceMemory = tmpMem - tmp.StartMemory;
                    tmp.OnceTime = tmpTime - tmp.StartTime;
                    tmp.TotalMemory += tmp.OnceMemory;
                    tmp.TotalTime += tmp.OnceTime;
                    tmp.Calls += 1;
                    tmp.StartMemory = 0L;
                    tmp.StartTime = 0f;
                    dataRecord[name] = tmp;
                }
            }
        }

        /// <summary>
        /// 输出统计结果.
        /// </summary>
        public static void ToMessage()
        {
            string nowTime = System.DateTime.Now.ToString("[yyyy-MM-dd]-[HH-mm-ss]");
            string fileName = nowTime + ".csv";
            string header = "funName,funMem/k,funAverageMem/k,funTime/s,funAverageTime/s,funCalls";

            using (StreamWriter sw = new (fileName))
            {
                sw.WriteLine(header);
                var ge = dataRecord.GetEnumerator();
                while (ge.MoveNext())
                {
                    FunctionData tmp = ge.Current.Value;

                    // 过滤调用次数0的函数
                    if (tmp.Calls <= 0)
                    {
                        continue;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("{0},", tmp.Name);
                    sb.AppendFormat("{0:f4},", tmp.OnceMemory / 1024.0);
                    sb.AppendFormat("{0:f4},", tmp.TotalMemory / (tmp.Calls * 1024.0));
                    sb.AppendFormat("{0},", tmp.OnceTime);
                    sb.AppendFormat("{0},", tmp.TotalTime / tmp.Calls);
                    sb.AppendFormat("{0}", tmp.Calls);
                    sw.WriteLine(sb);
                }
            }

            Debug.Log("文件输出完成");
        }
    }
}