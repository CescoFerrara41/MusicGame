using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class TurnQueue : Node
{
	private Queue<BattleStep> steps = new();

	public async Task RunQueue(BattleController battle)
	{
		while (steps.Count > 0)
		{
			BattleStep step = steps.Dequeue();
			await step.Execute(battle);
		}
	}

	public void AddStep(BattleStep step)
	{
		steps.Enqueue(step);
	}

	public void Clear()
	{
		steps.Clear();
	}
}
