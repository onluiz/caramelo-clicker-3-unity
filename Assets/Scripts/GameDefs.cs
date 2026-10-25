using System.Collections.Generic;

public enum UpgradeKind { ClickMultiplier, ClickPercent, GlobalMultiplier, Building }

public class BuildingDef
{
    public string Name;
    public string Description;
    public double BaseCost;
    public double BaseCps;
    public int Icon;
}

public class UpgradeDef
{
    public string Id;
    public string Name;
    public string Description;
    public double Cost;
    public UpgradeKind Kind;
    public int Building;
    public int RequiredOwned;
    public int Icon;
}

public static class GameDefs
{
    public const double CostGrowth = 1.15;

    public static readonly BuildingDef[] Buildings =
    {
        new BuildingDef { Name = "Ossinho de Borracha", Description = "Um brinquedinho que sempre rende uns petiscos.", BaseCost = 15, BaseCps = 0.1, Icon = 0 },
        new BuildingDef { Name = "Tigela Cheia", Description = "Sempre sobra um petisco no fundo.", BaseCost = 100, BaseCps = 1, Icon = 1 },
        new BuildingDef { Name = "Passeador", Description = "Passeia com o Caramelo e acha petiscos pelo caminho.", BaseCost = 1100, BaseCps = 8, Icon = 2 },
        new BuildingDef { Name = "Pet Shop", Description = "Vende petiscos para toda a vizinhança.", BaseCost = 12000, BaseCps = 47, Icon = 3 },
        new BuildingDef { Name = "Padaria de Biscoitos", Description = "Biscoitinhos quentinhos o dia inteiro.", BaseCost = 130000, BaseCps = 260, Icon = 4 },
        new BuildingDef { Name = "Fábrica de Ração", Description = "Produção industrial de petiscos.", BaseCost = 1400000, BaseCps = 1400, Icon = 5 },
        new BuildingDef { Name = "Churrascaria Canina", Description = "Rodízio liberado para cachorro.", BaseCost = 20000000, BaseCps = 7800, Icon = 6 },
        new BuildingDef { Name = "Parque dos Vira-latas", Description = "Uma matilha inteira cavando tesouros.", BaseCost = 330000000, BaseCps = 44000, Icon = 7 },
        new BuildingDef { Name = "Portal do Céu dos Cachorros", Description = "Petiscos infinitos vindos do além.", BaseCost = 5100000000, BaseCps = 260000, Icon = 8 },
    };

    public const int PawIcon = 9;
    public const int StarIcon = 7;

    // Static initializers run in textual order: the tier tables must precede Upgrades.
    static readonly string[] TierNames = { "Turbinado", "Deluxe", "Supremo", "Lendário" };
    static readonly int[] TierOwned = { 1, 10, 25, 50 };
    static readonly double[] TierCostMult = { 10, 100, 1000, 100000 };

    public static readonly List<UpgradeDef> Upgrades = BuildUpgrades();
    public static readonly Dictionary<string, UpgradeDef> ById = BuildIndex();

    static List<UpgradeDef> BuildUpgrades()
    {
        var list = new List<UpgradeDef>();

        string[] clickNames = { "Patinha Forte", "Patada Dupla", "Pata de Aço", "Pata Dourada", "Pata Cósmica", "Pata Lendária" };
        double[] clickCosts = { 100, 500, 10000, 500000, 50000000, 5000000000 };
        for (int i = 0; i < clickNames.Length; i++)
        {
            list.Add(new UpgradeDef
            {
                Id = "click" + i, Name = clickNames[i], Description = "Cada clique rende o dobro.",
                Cost = clickCosts[i], Kind = UpgradeKind.ClickMultiplier, Icon = PawIcon,
            });
        }

        string[] noseNames = { "Faro Aguçado", "Faro Biônico" };
        double[] noseCosts = { 50000, 5000000 };
        for (int i = 0; i < noseNames.Length; i++)
        {
            list.Add(new UpgradeDef
            {
                Id = "nose" + i, Name = noseNames[i], Description = "Cada clique rende +1% dos petiscos por segundo.",
                Cost = noseCosts[i], Kind = UpgradeKind.ClickPercent, Icon = PawIcon,
            });
        }

        string[] foodNames = { "Ração Premium", "Ração Gourmet", "Ração Divina" };
        double[] foodCosts = { 250000, 25000000, 2500000000 };
        for (int i = 0; i < foodNames.Length; i++)
        {
            list.Add(new UpgradeDef
            {
                Id = "food" + i, Name = foodNames[i], Description = "Toda a produção aumenta em 25%.",
                Cost = foodCosts[i], Kind = UpgradeKind.GlobalMultiplier, Icon = StarIcon,
            });
        }

        for (int b = 0; b < Buildings.Length; b++)
        {
            for (int t = 0; t < TierNames.Length; t++)
            {
                list.Add(new UpgradeDef
                {
                    Id = "b" + b + "_" + t,
                    Name = Buildings[b].Name + " " + TierNames[t],
                    Description = Buildings[b].Name + " produz o dobro.",
                    Cost = Buildings[b].BaseCost * TierCostMult[t],
                    Kind = UpgradeKind.Building,
                    Building = b,
                    RequiredOwned = TierOwned[t],
                    Icon = Buildings[b].Icon,
                });
            }
        }

        list.Sort((a, c) => a.Cost.CompareTo(c.Cost));
        return list;
    }

    static Dictionary<string, UpgradeDef> BuildIndex()
    {
        var map = new Dictionary<string, UpgradeDef>();
        foreach (var u in Upgrades) map[u.Id] = u;
        return map;
    }
}
