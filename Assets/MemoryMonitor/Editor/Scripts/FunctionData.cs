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
 * * 创建时间：2025/4/23 17:00:30
 ********************************************************************/

namespace MemoryMonitor.Editor
{
    /// <summary>
    /// 函数数据.
    /// </summary>
    public class FunctionData
    {
        /// <summary>
        /// Gets or sets 函数名称.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets 执行一次函数占用的内存.
        /// </summary>
        public long OnceMemory { get; set; }

        /// <summary>
        /// Gets or sets 执行一次函数的耗时.
        /// </summary>
        public float OnceTime { get; set; }

        /// <summary>
        /// Gets or sets 函数被调用的次数.
        /// </summary>
        public int Calls { get; set; }

        /// <summary>
        /// Gets or sets 函数多次执行后累计占用的内存.
        /// </summary>
        public float TotalMemory { get; set; }

        /// <summary>
        /// Gets or sets 函数多次执行后累计的耗时.
        /// </summary>
        public float TotalTime { get; set; }

        /// <summary>
        /// Gets or sets 第一次开始执行时的内存.
        /// </summary>
        public long StartMemory { get; set; }

        /// <summary>
        /// Gets or sets 第一次开始执行时的时间.
        /// </summary>
        public float StartTime { get; set; }
    }
}