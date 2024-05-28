

using Avalonia.Input;
using Digger.Architecture;

namespace Digger;

public static class Game
{
	private const string mapWithPlayerTerrain = @"
TTT T
TTP T
T T T
TT TT";

	private const string mapWithPlayerTerrainSackGold = @"
PTTGTT TS
TST  TSTT
TTTTTTSTT
T TSTS TT
T TTTG ST
TSTSTT TT";

	private const string mapWithPlayerTerrainSackGoldMonster = @"
PTTGTT TST
TST  T TTM
TTT    TTT
T TSTS TTT
T TTTGMSTS
T TMT M TS
TSTSTTMTTT
S TTST  TG
 TGST MTTT
 T  TMTTTT";

	private const string testMap = @"
TTTTTTTTTTTTTTTTTTTTTTTT
T                      T
T TTTTTTTTTTTTTTTTTTTT T
T T                    T
T T TTTTTTTTTTTTTTTTTTTT
TPT                    T
TTTTTTTTTTTTTTTTTTTTTT T
TM                     T
TTTTTTTTTTTTTTTTTTTTTTTT";

	public static ICreature[,] Map;
	public static int Scores;
	public static bool IsOver;

	public static Key KeyPressed;
	public static int MapWidth => Map.GetLength(0);
	public static int MapHeight => Map.GetLength(1);

	public static void CreateMap()
	{
		Map = CreatureMapCreator.CreateMap(testMap);
	}
}