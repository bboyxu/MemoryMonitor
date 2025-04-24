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
        private static Dictionary<string, FunctionData> dataRecords = new Dictionary<string, FunctionData>();

        private static Thread mainThread = Thread.CurrentThread;

        /// <summary>
        /// 统计开始.
        /// </summary>
        /// <param name="name">被监控的函数名.</param>
        public static void Begin(string name)
        {
            if (Thread.CurrentThread != mainThread)
            {
                return;
            }

            // 在unity保留的系统内存中，已经申请使用的内存
            long tmpMemory = Profiler.GetTotalAllocatedMemoryLong();
            float tmpTime = Time.realtimeSinceStartup;
            FunctionData tmpData;
            if (dataRecords.ContainsKey(name))
            {
                tmpData = dataRecords[name];
            }
            else
            {
                tmpData = new FunctionData()
                {
                    Name = name,
                };

                dataRecords.Add(name, tmpData);
            }

            tmpData.StartMemory = tmpMemory;
            tmpData.StartTime = tmpTime;
        }

        /// <summary>
        /// 统计结束.
        /// </summary>
        /// <param name="name">被监控的函数名.</param>
        public static void End(string name)
        {
            if (Thread.CurrentThread != mainThread)
            {
                return;
            }

            // 在unity保留的系统内存中，已经申请使用的内存
            long tmpMemory = Profiler.GetTotalAllocatedMemoryLong();
            float tmpTime = Time.realtimeSinceStartup;
            FunctionData tmpData = dataRecords[name];

            // 过滤因为GC而统计不正确的数据
            if (tmpMemory - tmpData.StartMemory >= 0)
            {
                tmpData.OnceMemory = tmpMemory - tmpData.StartMemory;
                tmpData.OnceTime = tmpTime - tmpData.StartTime;
                tmpData.TotalMemory += tmpData.OnceMemory;
                tmpData.TotalTime += tmpData.OnceTime;
                tmpData.Calls += 1;
            }
        }

        /// <summary>
        /// 输出统计结果.
        /// </summary>
        public static void SaveToLocalFile()
        {
            string nowTime = System.DateTime.Now.ToString("[yyyy-MM-dd]-[HH-mm-ss]");
            string fileName = nowTime + ".csv";
            string header = "函数名(Name),单次占用内存(OnceMemory/KB),均次占用内存(KB),单次耗时(S),均次耗时(S),执行次数";

            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.WriteLine(header);
                var ge = dataRecords.GetEnumerator();
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