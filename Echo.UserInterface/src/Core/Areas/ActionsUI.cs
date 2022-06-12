using System;
using Echo.UserInterface.Backend;
using Echo.UserInterface.Core.Common;
using ImGuiNET;

namespace Echo.UserInterface.Core.Areas;

public class ActionsUI : AreaUI
{
	public ActionsUI() : base("Actions") { }

	protected override void Update(in Moment moment)
	{
		if (ImGui.Button("Clear All")) ActionQueue.ClearHistory();

		ImGui.Separator();
		ImGui.BeginChild("History");

		foreach ((string label, (DateTime time, ActionQueue.EventType type)) in ActionQueue.History)
		{
			string text = type switch
			{
				ActionQueue.EventType.EnqueueSucceed => $"Successfully enqueued `{label}`",
				ActionQueue.EventType.EnqueuedDuplicate => $"Attempting to enqueue duplicate `{label}`",
				ActionQueue.EventType.DequeueStarted => $"Started executing `{label}`",
				ActionQueue.EventType.DequeueCompleted => $"Completed executing `{label}`",
				_ => throw new ArgumentOutOfRangeException()
			};

			ImGui.TextUnformatted($"[{time.ToStringDefault()}]: {text}");
		}

		ImGui.EndChild();
	}
}