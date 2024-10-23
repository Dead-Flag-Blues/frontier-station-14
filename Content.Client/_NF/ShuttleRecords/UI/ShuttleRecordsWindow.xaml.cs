﻿using System.Linq;
using Content.Shared._NF.ShuttleRecords;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._NF.ShuttleRecords.UI;

[GenerateTypedNameReferences]
public sealed partial class ShuttleRecordsWindow : DefaultWindow
{
    [Dependency] private readonly ILocalizationManager _loc = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public Action<ShuttleRecord>? OnCopyDeed;
    public ShuttleRecord? SelectedShuttleRecord;
    private ShuttleRecordsConsoleInterfaceState? _lastStateUpdate;
    private string _searchText = "";

    public ShuttleRecordsWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        SearchText.OnTextChanged += args => OnSearchTextChanged(args.Text);
        ClearSearchButton.OnPressed += ClearSearchText;
    }

    private void OnSearchTextChanged(string text)
    {
        _searchText = text;
        if (_lastStateUpdate != null)
        {
            UpdateState(_lastStateUpdate, false);
        }
    }

    private void ClearSearchText(BaseButton.ButtonEventArgs args)
    {
        _searchText = "";
        SearchText.Text = "";
        if (_lastStateUpdate != null)
        {
            UpdateState(_lastStateUpdate, false);
        }
    }

    public void UpdateState(ShuttleRecordsConsoleInterfaceState state, bool rememberState = true)
    {
        if (rememberState)
        {
            _lastStateUpdate = state;
        }
        ShuttleRecordList.RemoveAllChildren();
        var viewStateList = BuildShuttleRecordListItemViewStateList(state.Records);
        foreach (var pair in viewStateList)
        {
            var listItem = new ShuttleRecordListItem(pair.ViewState);
            listItem.OnPressed += _ => OnShuttleRecordListItemPressed(pair.ShuttleRecord);
            ShuttleRecordList.AddChild(listItem);
        }

        TransactionCostLabel.Text = _loc.GetString(
            messageId: "shuttle-records-transaction-cost",
            arg: ("cost", state.TransactionCost)
        );
    }

    public record ShuttleRecordViewStatePair(
        ShuttleRecord ShuttleRecord,
        ShuttleRecordListItem.ViewState ViewState
    );

    private List<ShuttleRecordViewStatePair> BuildShuttleRecordListItemViewStateList(
        List<ShuttleRecord> shuttleRecords)
    {
        return shuttleRecords
            .Where(shuttleRecord => string.IsNullOrEmpty(_searchText) ||
                                    shuttleRecord.Name.Contains(_searchText, StringComparison.CurrentCultureIgnoreCase) ||
                                    (shuttleRecord.Suffix != null && shuttleRecord.Suffix.Contains(_searchText, StringComparison.CurrentCultureIgnoreCase)) ||
                                     shuttleRecord.OwnerName.Contains(_searchText, StringComparison.CurrentCultureIgnoreCase))
            .Select(shuttleRecord =>
                new ShuttleRecordViewStatePair(
                    shuttleRecord,
                    new ShuttleRecordListItem.ViewState(
                        shuttleRecord.Name + " " + shuttleRecord.Suffix,
                        disabled: shuttleRecord == SelectedShuttleRecord,
                        toolTip: ""
                    )
                ))
            .ToList();
    }

    private void OnShuttleRecordListItemPressed(ShuttleRecord shuttleRecord)
    {
        SelectedShuttleRecord = shuttleRecord;
        ShuttleRecordDetailsContainer.RemoveAllChildren();
        var shuttleStatus = ShuttleExists(netEntity: shuttleRecord.EntityUid)
            ? _loc.GetString(messageId: "shuttle-records-shuttle-status-active")
            : _loc.GetString(messageId: "shuttle-records-shuttle-status-inactive");
        var viewState = new ShuttleRecordDetailsControl.ViewState(
            shuttleName: _loc.GetString(messageId: "shuttle-records-shuttle-name-label",
                arg: ("name", shuttleRecord.Name + " " + shuttleRecord.Suffix)),
            shuttleOwnerName: _loc.GetString(messageId: "shuttle-records-shuttle-owner-label",
                arg: ("owner", shuttleRecord.OwnerName)),
            activity: _loc.GetString(messageId: "shuttle-records-shuttle-status", arg: ("status", shuttleStatus)),
            toolTip: ""
        );
        var control = new ShuttleRecordDetailsControl(state: viewState);
        control.CopyDeedButton.OnPressed += _ =>
        {
            // Disables the button to prevent multiple presses.
            // Resets when you select a different shuttle record.
            control.CopyDeedButton.Disabled = true;
            OnCopyDeed?.Invoke(shuttleRecord);
        };
        ShuttleRecordDetailsContainer.AddChild(child: control);
    }

    private bool ShuttleExists(NetEntity netEntity)
    {
        return _entityManager.TryGetEntity(netEntity, out _);
    }
}
