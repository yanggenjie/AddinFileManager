using AddinFileManager.UI.Model;
using System.Collections.Generic;

namespace AddinFileManager.Services;

/// <summary>
/// 操作历史记录
/// </summary>
public class OperationHistory
{
    public OperationType Type { get; set; }
    public AddinInfoModel Model { get; set; }
    public bool PreviousState { get; set; }
    public string PreviousFilePath { get; set; }
    public string PreviousFileName { get; set; }
}

public enum OperationType
{
    ToggleEnable,
    Delete
}

/// <summary>
/// 操作历史服务接口
/// </summary>
public interface IOperationHistoryService
{
    /// <summary>
    /// 最大历史记录数
    /// </summary>
    int MaxHistoryCount { get; }

    /// <summary>
    /// 添加操作记录
    /// </summary>
    void AddOperation(OperationHistory operation);

    /// <summary>
    /// 获取最后一条操作记录
    /// </summary>
    OperationHistory GetLastOperation();

    /// <summary>
    /// 移除最后一条操作记录
    /// </summary>
    void RemoveLastOperation();

    /// <summary>
    /// 是否可以撤销
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// 清空历史记录
    /// </summary>
    void Clear();
}

/// <summary>
/// 操作历史服务实现
/// </summary>
public class OperationHistoryService : IOperationHistoryService
{
    private readonly LinkedList<OperationHistory> _history = new();

    public int MaxHistoryCount => 50;

    public void AddOperation(OperationHistory operation)
    {
        _history.AddLast(operation);
        if (_history.Count > MaxHistoryCount)
        {
            _history.RemoveFirst();
        }
    }

    public OperationHistory GetLastOperation()
    {
        return _history.Count > 0 ? _history.Last.Value : null;
    }

    public void RemoveLastOperation()
    {
        if (_history.Count > 0)
        {
            _history.RemoveLast();
        }
    }

    public bool CanUndo => _history.Count > 0;

    public void Clear()
    {
        _history.Clear();
    }
}