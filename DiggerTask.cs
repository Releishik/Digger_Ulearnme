using Avalonia.Input;
using Digger.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace Digger;

public abstract class AbstractCreature : ICreature
{
	protected int drawPriority = -1;
	protected string imageFileName = "";
	public abstract CreatureCommand Act(int x, int y);

	public virtual bool DeadInConflict(ICreature conflictedObject) => false;

	public virtual int GetDrawingPriority() => drawPriority;

	public virtual string GetImageFileName() => imageFileName;
}

public class Player : AbstractCreature
{
	public static int X { get; private set; } = 0;
	public static int Y { get; private set; } = 0;

	public static bool IsBeginMove { get; private set; }

	private readonly int[][] deltas = {
		new[] { -1, 0 },
		new[] { 0, -1 },
		new[] { 1, 0 },
		new[] { 0, 1 },
	};
	public Player()
	{
		drawPriority = 0;
		imageFileName = "Digger.png";
		IsBeginMove = false;
	}
	public override CreatureCommand Act(int x, int y)
	{
		X = x;
		Y = y;
		//left - 23, up - 24, right - 25, down - 26
		Key pressedKey = Game.KeyPressed;
		int dx = 0, dy = 0;

		if (pressedKey >= Key.Left && pressedKey <= Key.Down)
		{
			IsBeginMove |= true;
			int keyIndex = (int)pressedKey - 23;
			dx = deltas[keyIndex][0];
			dy = deltas[keyIndex][1];
		}

		int nx = x + dx;
		int ny = y + dy;
		//проверка на край карты
		dx = nx < 0 || nx >= Game.MapWidth ? 0 : dx;
		dy = ny < 0 || ny >= Game.MapHeight ? 0 : dy;
		//проверка на препятствие (мешок)
		ICreature creatureInNextPosition = Game.Map[x + dx, y + dy];
		if (creatureInNextPosition is Sack) { dx = 0; dy = 0; }

		return new CreatureCommand()
		{
			DeltaX = dx,
			DeltaY = dy,
			TransformTo = null
		};
	}
	public override bool DeadInConflict(ICreature conflictedObject)
	{
		if (conflictedObject is Monster || (conflictedObject is Sack && ((Sack)conflictedObject).IsFalling))
		{
			Game.IsOver = true;
			IsBeginMove = false;
			return true;
		}
		return false;
	}
}

public class Terrain : AbstractCreature
{
	public Terrain()
	{
		drawPriority = 1000;
		imageFileName = "Terrain.png";
	}
	public override CreatureCommand Act(int x, int y)
	{
		return new CreatureCommand();
	}

	public override bool DeadInConflict(ICreature conflictedObject) => conflictedObject is Player;
}

public class Sack : AbstractCreature
{
	enum States
	{
		Idle,
		Fall,
		Crush
	}

	private States state = States.Idle;
	private uint fallDistance = 0;

	public bool IsFalling { get => state == States.Fall; }

	public Sack()
	{
		drawPriority = 900;
		imageFileName = "Sack.png";
	}
	public override CreatureCommand Act(int x, int y)
	{
		if (!Player.IsBeginMove) return new CreatureCommand();
		CreatureCommand command = new CreatureCommand();
		int dy = y + 1;
		dy = dy < Game.MapHeight ? dy : Game.MapHeight - 1;
		var nextDownCreature = Game.Map[x, dy];

		switch (state)
		{
			case States.Idle:
				if (nextDownCreature == null)
				{
					state = States.Fall;
					fallDistance++;
					command.DeltaY = 1;
				}
				break;
			case States.Fall:
				if (nextDownCreature is Player || nextDownCreature is Monster || nextDownCreature==null)
				{
					command.DeltaY = 1;
					fallDistance++;
					break;
				}
				if (nextDownCreature!=null || dy==Game.MapHeight-1)
				{
					if (fallDistance<=1)
					{
						state = States.Idle;
						fallDistance = 0;
						break;
					}
					command.TransformTo = new Gold();
					state = States.Crush;
				}
				break;
			default:
				break;
		}

		return command;
	}
}

public class Gold : AbstractCreature
{
	public Gold()
	{
		drawPriority = 800;
		imageFileName = "Gold.png";
	}
	public override CreatureCommand Act(int x, int y)
	{
		return new CreatureCommand();
	}

	public override bool DeadInConflict(ICreature conflictedObject)
	{
		if (conflictedObject is Player)
		{
			Game.Scores += 10;
			return true;
		}
		return conflictedObject is Monster;
	}
}

public class Monster : AbstractCreature
{
	public Monster()
	{
		drawPriority = -1;
		imageFileName = "Monster.png";
	}
	public override CreatureCommand Act(int x, int y)
	{
		if (!Player.IsBeginMove) return new CreatureCommand();
		var nextPosition = FindNextPointToMove(x, y);
		return new CreatureCommand() { DeltaX=nextPosition.Item1-x, DeltaY=nextPosition.Item2-y };
	}
	public override bool DeadInConflict(ICreature conflictedObject)
	{
		return conflictedObject is Sack && ((Sack)conflictedObject).IsFalling || conflictedObject is Monster;
	}

	private Tuple<int, int> GetPlayer()
	{
		for (int y = 0; y < Game.MapHeight; y++)
		{
			for (int x = 0; x < Game.MapWidth; x++)
			{
				if (Game.Map[x, y] is Player) return Tuple.Create(x, y);
			}
		}
		return Tuple.Create(0, 0);
	}

	private double GetDistanse(Tuple<int, int> a, Tuple<int, int> b)
	{
		int x = a.Item1 - b.Item1;
		int y = a.Item2 - b.Item2;
		return Math.Sqrt(x*x+y*y);
	}

	private bool PositionIsValid(Tuple<int, int> p)
	{
		if ((p.Item1 >= 0 && p.Item1 < Game.MapWidth) && (p.Item2 >= 0 && p.Item2 < Game.MapHeight))
		{
			var item = Game.Map[p.Item1, p.Item2];
			return item is not Sack && item is not Terrain && item is not Monster;
		}
		return false;
	}

	private List<Tuple<int, int>> GetNeighbours(Tuple<int, int> p)
	{
		int x = p.Item1;
		int y = p.Item2;
		List<Tuple<int, int>> neighbours = new List<Tuple<int, int>>();
		var l = Tuple.Create(x - 1, y);
		var r = Tuple.Create(x + 1, y);
		var t = Tuple.Create(x, y - 1);
		var b = Tuple.Create(x, y + 1);
		if (PositionIsValid(l)) neighbours.Add(l);
		if (PositionIsValid(r)) neighbours.Add(r);
		if (PositionIsValid(t)) neighbours.Add(t);
		if (PositionIsValid(b)) neighbours.Add(b);
		return neighbours;
	}

	private Dictionary<Tuple<int, int>, Tuple<int, int>> FindPathTo(Tuple<int,int> from, Tuple<int, int> to)
	{
		var front = new Queue<Tuple<int, int>>();
		var visited = new Dictionary<Tuple<int,int>, Tuple<int, int>>();
		front.Enqueue(from);
		visited[from] = null;
		while (front.Count > 0)
		{
			var current = front.Dequeue();
			if (current == to) break;
			var neighbours = GetNeighbours(current);
			foreach (var n in neighbours)
			{
				if (!visited.ContainsKey(n))
				{
					front.Enqueue(n);
					visited[n] = current;
				}
			}

		}
		return visited;
	}

	private Tuple<int, int> RestorePath(Dictionary<Tuple<int, int>, Tuple<int, int>> field, Tuple<int, int> from, Tuple<int, int> to)
	{
		if (!field.ContainsKey(to))
		{
			double minDist = double.MaxValue;
			var minCurrent = Tuple.Create(int.MaxValue, int.MaxValue);
			foreach (var kvp in field)
			{
				double cDist = GetDistanse(to, kvp.Key);
				if (cDist < minDist)
				{
					minDist = cDist;
					minCurrent = kvp.Key;
				}
			}
			to = minCurrent;
		}
		var current = to;
		var prev = current;
		while (current != from)
		{
			prev = current;
			current = field[current];
		}
		return prev;
	}

	private Tuple<int,int> FindNextPointToMove(int x, int y)
	{
		if (!Game.IsOver)
		{
			var player = GetPlayer();
			var position = Tuple.Create(x, y);
			return RestorePath(FindPathTo(position, player),position,player);
		}
		return new Tuple<int,int>(0, 0);
	}
}
//Напишите здесь классы Player, Terrain и другие.