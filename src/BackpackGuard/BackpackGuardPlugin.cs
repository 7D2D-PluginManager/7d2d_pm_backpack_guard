using PluginManager.Api;
using PluginManager.Api.Capabilities.Implementations.Events.GameEvents;
using PluginManager.Api.Capabilities.Implementations.Translations;
using PluginManager.Api.Capabilities.Implementations.Utils;
using PluginManager.Api.Contracts;
using PluginManager.Api.Hooks;
using PluginManager.Config;
using PluginManager.Localization;

namespace BackpackGuard;

public class BackpackGuardPlugin : BasePlugin
{
    public override string ModuleName => "BackpackGuard";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "kotfoxtrot";

    public override string ModuleDescription =>
        "Protects dropped player backpacks from being looted by others for a limited time";

    private IGameUtil _gameUtil;
    private IPlayerUtil _playerUtil;
    private IContainerUtil _containerUtil;
    private IPlayerLocalization _localization;
    private PluginConfig _config;

    protected override void OnLoad()
    {
        _gameUtil = Capabilities.Get<IGameUtil>();
        _playerUtil = Capabilities.Get<IPlayerUtil>();
        _containerUtil = Capabilities.Get<IContainerUtil>();
        _localization = GetPlayerLocalization();
        _config = ReadPluginConfig();

        RegisterEventHandler<TileEntityAccessAttemptEvent>(OnAccessAttempt, HookMode.Pre);
    }

    private HookResult OnAccessAttempt(TileEntityAccessAttemptEvent evt)
    {
        if (evt.TileEntity == null || evt.TileEntity.Type != TileEntityType.Loot)
            return HookResult.Continue;

        var backpack = _containerUtil.GetDroppedBackpack(evt.TileEntity.Id);
        if (backpack == null || !backpack.Found)
            return HookResult.Continue;

        var lockerEntityId = evt.EntityId;
        if (lockerEntityId == backpack.OwnerEntityId)
            return HookResult.Continue;

        if (_config.ExemptFriends && IsFriend(lockerEntityId, backpack.OwnerId))
            return HookResult.Continue;

        if (IsExpired(backpack.DroppedWorldMinutes))
            return HookResult.Continue;

        NotifyProtected(lockerEntityId);
        return HookResult.Stop;
    }

    private bool IsExpired(int droppedWorldMinutes)
    {
        var dayNightLength = _gameUtil.GetDayNightLength();
        if (dayNightLength <= 0) dayNightLength = 60;

        var thresholdGameMinutes = _config.ProtectionMinutes * 1440.0 / dayNightLength;
        var ageGameMinutes = _gameUtil.WorldTimeToTotalMinutes(_gameUtil.GetWorldTime()) - droppedWorldMinutes;

        return ageGameMinutes >= thresholdGameMinutes;
    }

    private bool IsFriend(int lockerEntityId, string ownerId)
    {
        if (string.IsNullOrEmpty(ownerId))
            return false;

        var client = _playerUtil.GetClientInfoByEntityId(lockerEntityId);
        if (client == null || string.IsNullOrEmpty(client.CrossplatformId))
            return false;

        return _playerUtil.AreFriends(ownerId, client.CrossplatformId);
    }

    private void NotifyProtected(int lockerEntityId)
    {
        var client = _playerUtil.GetClientInfoByEntityId(lockerEntityId);
        var playerId = client?.CrossplatformId;

        var tag = _localization.Translate(playerId, "Tag");
        var text = _localization.Translate(playerId, "Protected");
        _playerUtil.PrintToChat(lockerEntityId, $"{tag}{text}");
    }

    private PluginConfig ReadPluginConfig()
    {
        return new JsonConfigReader().Read<PluginConfig>(ConfigPath);
    }

    private IPlayerLocalization GetPlayerLocalization()
    {
        var playerLanguageStore = Capabilities.Get<IPlayerLanguageStore>();
        return new JsonPlayerLocalizationFactory(playerLanguageStore)
            .Create(LangDirectory, Id);
    }
}
