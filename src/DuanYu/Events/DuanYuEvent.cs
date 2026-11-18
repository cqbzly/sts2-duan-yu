using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using DuanYu.Assets;
using DuanYu.Character;
using DuanYu.Runtime;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Runs;

namespace DuanYu.Events;

public abstract class DuanYuEvent : CustomEventModel
{
    private bool _visitedBefore;

    protected DuanYuEvent() : base() { }
    public override bool IsShared => false;
    public override string? CustomInitialPortraitPath => DuanYuAssets.EventPortrait(Id.Entry);
    public override LocString InitialDescription => PageDescription(CanParticipate ? "INITIAL" : "SPECTATOR");

    protected bool CanParticipate => Owner != null && Owner.Character is DuanYuCharacter
        && !Owner.Creature.IsDead && !_visitedBefore;
    protected Player Participant => Owner ?? throw new InvalidOperationException("The event has not started.");

    public override bool IsAllowed(IRunState runState) =>
        runState.CurrentActIndex < 2
        && !runState.Players.Any(player => DuanYuState.HasVisitedEvent(player, Id.Entry))
        && runState.Players.Any(player => player.Character is DuanYuCharacter && !player.Creature.IsDead);

    protected override Task BeforeEventStarted(bool isPreFinished)
    {
        _visitedBefore = Owner != null && DuanYuState.HasVisitedEvent(Owner, Id.Entry);
        if (Owner != null && Owner.Character is DuanYuCharacter)
            DuanYuState.RecordEvent(Owner, Id.Entry);
        return Task.CompletedTask;
    }

    protected EventOption Choice(string key, Func<Task> action, bool enabled = true, bool cancelable = false) =>
        new(this, enabled ? action : null, $"{Id.Entry}.pages.INITIAL.options.{key}", !cancelable);

    protected Task Leave()
    {
        if (!IsFinished) SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}
