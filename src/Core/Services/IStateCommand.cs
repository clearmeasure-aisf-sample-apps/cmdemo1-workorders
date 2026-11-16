using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using Mediator;

namespace ClearMeasure.Bootcamp.Core.Services;

public interface IStateCommand : IRequest<StateCommandResult>, IRemotableRequest
{
    bool IsValid();
    string TransitionVerbPresentTense { get; }
    /// <summary>Workflow role used by the UI to style and order command buttons.</summary>
    CommandRole Role { get; }
    bool Matches(string commandName);
    // ReSharper disable once UnusedMemberInSuper.Global -- called via interface dispatch by state-machine infrastructure
    WorkOrderStatus GetBeginStatus();
    // ReSharper disable once UnusedMemberInSuper.Global -- called via interface dispatch by state-machine infrastructure
    void Execute(StateCommandContext context);
}