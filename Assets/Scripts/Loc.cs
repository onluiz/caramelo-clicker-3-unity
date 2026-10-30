using System.Collections.Generic;
using UnityEngine;

public enum Lang { Pt, En }

// Localization. Portuguese is the source language of every string in the code; English comes from the
// tables below. UI strings are looked up by their Portuguese text, game data (buildings, tiers,
// blessings, basic upgrades) by id. A missing English entry falls back to the Portuguese text.
public static class Loc
{
    const string PrefKey = "caramelo_lang";

    public static Lang Current { get; private set; } = Lang.En;
    public static bool IsEnglish => Current == Lang.En;

    // Saved choice first; otherwise Portuguese browsers get Portuguese and everyone else English.
    public static void Init()
    {
        int saved = PlayerPrefs.GetInt(PrefKey, -1);
        if (saved == 0) Current = Lang.Pt;
        else if (saved == 1) Current = Lang.En;
        else Current = Application.systemLanguage == SystemLanguage.Portuguese ? Lang.Pt : Lang.En;
    }

    public static void Set(Lang lang)
    {
        Current = lang;
        PlayerPrefs.SetInt(PrefKey, lang == Lang.Pt ? 0 : 1);
    }

    public static string T(string pt)
    {
        if (!IsEnglish || string.IsNullOrEmpty(pt)) return pt;
        return Ui.TryGetValue(pt, out var en) ? en : pt;
    }

    public static string F(string ptFormat, params object[] args)
    {
        return string.Format(T(ptFormat), args);
    }

    // ---------------------------------------------------------------- game data (by id)

    public static string BuildingName(string key, string pt)
    {
        return IsEnglish && Buildings.TryGetValue(key, out var v) ? v.name : pt;
    }

    public static string BuildingDesc(string key, string pt)
    {
        return IsEnglish && Buildings.TryGetValue(key, out var v) ? v.desc : pt;
    }

    public static string TierName(string building, int tier, string pt)
    {
        return IsEnglish && Tiers.TryGetValue(building + ":" + tier, out var v) ? v.name : pt;
    }

    public static string TierDesc(string building, int tier, string pt)
    {
        return IsEnglish && Tiers.TryGetValue(building + ":" + tier, out var v) ? v.desc : pt;
    }

    public static string BlessingName(string id, string pt)
    {
        return IsEnglish && Blessings.TryGetValue(id, out var v) ? v.name : pt;
    }

    public static string BlessingDesc(string id, string pt)
    {
        return IsEnglish && Blessings.TryGetValue(id, out var v) ? v.desc : pt;
    }

    public static string UpgradeName(string id, string pt)
    {
        return IsEnglish && BasicUpgrades.TryGetValue(id, out var v) ? v : pt;
    }

    // ---------------------------------------------------------------- tables

    static readonly Dictionary<string, string> Ui = new Dictionary<string, string>
    {
        { "Construções", "Buildings" },
        { "Melhorias", "Upgrades" },
        { "Melhorias ({0})", "Upgrades ({0})" },
        { "Renascer", "Rebirth" },
        { "Renascer (+{0})", "Rebirth (+{0})" },
        { "Comprar:", "Buy:" },
        { "Máx", "Max" },
        { "Nenhuma melhoria por enquanto.\nContinue juntando petiscos!", "No upgrades yet.\nKeep collecting treats!" },
        { "Configurações", "Settings" },
        { "Fechar", "Close" },
        { "Som: Ligado", "Sound: On" },
        { "Som: Mudo", "Sound: Off" },
        { "Idioma: Português", "Language: English" },
        { "Reiniciar jogo", "Restart game" },
        { "Confirmar reinício!", "Confirm restart!" },
        { "Isso apaga todo o progresso! Clique em \"Confirmar reinício\" para continuar.", "This erases all your progress! Tap \"Confirm restart\" to continue." },
        { "Jogo reiniciado. Boa sorte, Caramelo!", "Game restarted. Good luck, Caramelo!" },
        { "Cliques: {0}   |   Total: {1}", "Clicks: {0}   |   Total: {1}" },
        { "Pata Furiosa! Cliques x{0} ({1}s)", "Furious Paw! Clicks x{0} ({1}s)" },
        { "Frenesi! Produção x{0} ({1}s)", "Frenzy! Production x{0} ({1}s)" },
        { "Pata Furiosa! Seus cliques valem x{0} por 13s!", "Furious Paw! Your clicks are worth x{0} for 13s!" },
        { "Sorte! +{0} petiscos!", "Lucky! +{0} treats!" },
        { "Frenesi! Produção x{0} por 30s!", "Frenzy! Production x{0} for 30s!" },
        { "Bem-vindo de volta! Enquanto você estava fora, o Caramelo juntou {0} petiscos.", "Welcome back! While you were away, Caramelo collected {0} treats." },
        { "Ossos Celestiais: {0}", "Celestial Bones: {0}" },
        { "Cada osso já ganho dá +{0}% de produção (hoje: +{1}%). Gaste os ossos em bênçãos permanentes.", "Each bone earned gives +{0}% production (today: +{1}%). Spend bones on permanent blessings." },
        { "Renascimentos: {0}", "Rebirths: {0}" },
        { "Toque de novo para confirmar!", "Tap again to confirm!" },
        { "Renascer mais poderoso", "Be reborn stronger" },
        { "O Caramelo usa os ossos celestiais para renascer mais poderoso. Reinicia petiscos, construções e melhorias comuns. Você mantém ossos, bênçãos e cliques.", "Caramelo uses the celestial bones to be reborn stronger. Resets treats, buildings and regular upgrades. You keep bones, blessings and clicks." },
        { "+{0} Osso Celestial", "+{0} Celestial Bone" },
        { "+{0} Ossos Celestiais", "+{0} Celestial Bones" },
        { "Fragmentos: {0} de 1 osso · faltam {1} petiscos", "Fragments: {0} of 1 bone · {1} more treats" },
        { "Fragmentos: {0}", "Fragments: {0}" },
        { "Adquirida", "Owned" },
        { "{0} osso", "{0} bone" },
        { "{0} ossos", "{0} bones" },
        { "Junte mais petiscos para desbloquear.", "Collect more treats to unlock." },
        { "+{0}/s cada", "+{0}/s each" },
        { "{0} petiscos", "{0} treats" },
        { "Ainda falta produzir mais petiscos para ganhar um Osso Celestial.", "You still need to produce more treats to earn a Celestial Bone." },
        { "Renascer reinicia petiscos, construções e melhorias. Toque de novo para confirmar!", "Rebirth resets treats, buildings and upgrades. Tap again to confirm!" },
        { "O Caramelo renasceu! +{0} Osso Celestial", "Caramelo was reborn! +{0} Celestial Bone" },
        { "O Caramelo renasceu! +{0} Ossos Celestiais", "Caramelo was reborn! +{0} Celestial Bones" },
        { "Produção de {0} x2", "{0} production x2" },
        { "{0} produz o dobro.", "{0} produces double." },
        { "Cada clique rende o dobro.", "Every click is worth double." },
        { "Cada clique rende +1% dos petiscos por segundo.", "Each click earns +1% of your treats per second." },
        { "Toda a produção aumenta em 25%.", "All production increases by 25%." },
        { "Turbinado", "Turbo" },
        { "Deluxe", "Deluxe" },
        { "Supremo", "Supreme" },
        { "Lendário", "Legendary" },
    };

    static readonly Dictionary<string, string> BasicUpgrades = new Dictionary<string, string>
    {
        { "click0", "Strong Paw" }, { "click1", "Double Smack" }, { "click2", "Steel Paw" },
        { "click3", "Golden Paw" }, { "click4", "Cosmic Paw" }, { "click5", "Legendary Paw" },
        { "nose0", "Sharp Nose" }, { "nose1", "Bionic Nose" },
        { "food0", "Premium Kibble" }, { "food1", "Gourmet Kibble" }, { "food2", "Divine Kibble" },
    };

    static readonly Dictionary<string, (string name, string desc)> Blessings = new Dictionary<string, (string, string)>
    {
        { "mochila", ("Treat Backpack", "Every life starts with 10,000 treats.") },
        { "faro", ("Divine Nose", "Your clicks are worth double.") },
        { "patas", ("Celestial Paws", "All production increases by 25%.") },
        { "pechincha1", ("Celestial Bargain", "Buildings are 5% cheaper.") },
        { "osso", ("Shining Bone", "Golden bones appear 30% faster.") },
        { "sono", ("Deep Sleep", "While offline, Caramelo earns 80% (instead of 50%) for up to 12h.") },
        { "cofre", ("Celestial Vault", "Every life starts with 10 million treats.") },
        { "bencao", ("Caramelo's Blessing", "All production doubles.") },
        { "pechincha2", ("Divine Bargain", "Buildings are 10% cheaper.") },
        { "lenda", ("Canine Legend", "All production triples.") },
    };

    static readonly Dictionary<string, (string name, string desc)> Buildings = new Dictionary<string, (string, string)>
    {
        { "o-inicio", ("The Beginning", "The beginning of the dream of our ambitious Brazilian caramelo dog: The Caramelo Corps.") },
        { "ossinho", ("Rubber Bone", "A little toy that always brings in a few treats.") },
        { "vira-lata", ("Stray Pal", "You and your street friends hunt for coins and trash cans to invest in Caramelo Corp.") },
        { "tigela", ("Full Bowl", "There's always a treat left at the bottom.") },
        { "venda-petiscos", ("Treat Stand", "Caramelo makes homemade treats and sells them on the street.") },
        { "passeador", ("Dog Walker", "Takes Caramelo for walks and finds treats along the way.") },
        { "cachorro-entregador", ("Delivery Caramelo", "Delivery dogs hired by Caramelo Corps.") },
        { "racao-caramelo", ("Caramelo Kibble", "Mysterious homemade kibble made by Caramelo.") },
        { "petshop", ("Pet Shop", "Sells treats to the whole neighborhood.") },
        { "drone-entregas", ("Delivery Drone", "Delivers the treats and kibble made by Caramelo.") },
        { "padaria", ("Pet Bakery", "Biscuits and treats baked fresh!") },
        { "faro-minerador", ("Treat Mine", "A mine of super rare dog treats was found! Caramelo Corp will take care of it and “preserve” it with all the love in the world. Closed to visitors.") },
        { "fabrica-racao", ("Small Kibble Factory", "Industrial treat production.") },
        { "iluminacao-caramelo", ("Caramelo Lighting", "Caramelo invests in city lighting to keep his delivery dogs and drones safe at night.") },
        { "fabrica-de-petiscos", ("Treat Factory", "Caramelo's investments keep going strong and the first treat factory was built.") },
        { "fabrica-de-racao", ("Kibble Factory", "Caramelo Kibble was a hit, so the first kibble factory opened.") },
        { "estatua-caramelo", ("Caramelo Statue", "Everybody worships Caramelo.") },
        { "parque", ("Stray Park", "A whole pack digging for treasure.") },
        { "templo-caramelo", ("Caramelo Temple", "Worshippers leave offerings all day long.") },
        { "banco-canino", ("Canine Bank", "Earns interest in bones and in coins.") },
        { "pet-shop-caramelo", ("Caramelo Pet Shop", "Kibble, toys and a bath. Every stray in the neighborhood stops by.") },
        { "veterinaria-caramelo", ("Caramelo Vet", "Checkup, shots and a biscuit at the end. Every stray leaves with a wagging tail.") },
        { "base-lunar-caramelo", ("Caramelo Moon Base", "They planted a flag on the Moon. And a bone.") },
        { "planta-lunar", ("Moon Plant", "A mysterious plant found on the Moon. The best dog puddings come from it.") },
        { "pudim-lunar", ("Homemade Moon Pudding", "Made with the moon plant. Every dog lines up for a spoonful.") },
        { "fabrica-de-pudim-lunar", ("Moon Pudding Factory", "The dogs liked Caramelo's nutritious pudding so much that he built his own factory on the Moon.") },
        { "cometa-caramelo", ("Caramelo Comet", "Grazes past and leaves a shower of bones and coins.") },
        { "sol-caramelo", ("Caramelo Sun", "For unknown reasons, the Sun took the shape of a caramelo dog. Every dog touched by its light reports having energy to spare.") },
    };

    static readonly Dictionary<string, (string name, string desc)> Tiers = new Dictionary<string, (string, string)>
    {
        { "o-inicio:0", ("Big Dream", "Those who dream small don't take a bone home.") },
        { "o-inicio:1", ("First Business Card", "Recycled paper, printed paw. Already looks like a real company.") },
        { "o-inicio:2", ("Startup Garage", "Two cardboard boxes, a borrowed outlet and lots of ambition.") },
        { "o-inicio:3", ("First Investor", "A rich cat bet on the idea. Nobody knows why.") },
        { "vira-lata:0", ("Street Instinct", "Caramelo and his stray friends sharpen their coin-collecting skills.") },
        { "vira-lata:1", ("Corner Circle", "One more stray joins the circle. Now nobody walks the street without paying the toll.") },
        { "vira-lata:2", ("Trash Can Map", "Every trash can in the neighborhood marked with an X. The best finds are in the fancy part of town.") },
        { "vira-lata:3", ("Strays' Union", "They organized and demand their share. No lawyer, just barking.") },
        { "venda-petiscos:0", ("Crunchy Treat", "Crunch, crunch! The sound alone makes the line at the stand grow.") },
        { "venda-petiscos:1", ("Gourmet Treat", "Now with a family recipe and a cloth napkin. Nobody asks the price anymore.") },
        { "venda-petiscos:2", ("Baconistic Treat", "The smell of bacon crosses the whole neighborhood. Even the cats join the line.") },
        { "venda-petiscos:3", ("Gold-Plated Treat", "Edible, shiny and totally unnecessary. That's exactly why it's so expensive.") },
        { "cachorro-entregador:0", ("GPS Tracker", "Now he only gets lost on purpose.") },
        { "cachorro-entregador:1", ("Thermal Backpack", "Treats arrive warm, bones arrive cold. As it should be.") },
        { "cachorro-entregador:2", ("Shortcut Route", "Cuts through the neighbor's yard. The neighbor's dog has been warned.") },
        { "cachorro-entregador:3", ("Same-Day Delivery", "Ordered, barked, arrived. Before the customer even hangs up.") },
        { "racao-caramelo:0", ("Secret Recipe", "Not even Caramelo remembers what's inside. But it sells.") },
        { "racao-caramelo:1", ("Barbecue Flavor", "Sunday in every bowl. The smell makes the whole block cry.") },
        { "racao-caramelo:2", ("Premium Packaging", "Golden bag with an embossed bone. Fancy even for a cat.") },
        { "racao-caramelo:3", ("Caramelo-Approved Seal", "He sniffed, licked and wagged his tail. It's certified.") },
        { "drone-entregas:0", ("Reinforced Propeller", "Handles birds, rain and the barking of angry dogs.") },
        { "drone-entregas:1", ("Long-Life Battery", "Flies all day. Only lands for a cuddle.") },
        { "drone-entregas:2", ("Sniffer Camera", "Spots a treat from 50 meters. Spots a cat from 100.") },
        { "drone-entregas:3", ("Drone Swarm", "Buzzing in formation. The human mailman quit.") },
        { "faro-minerador:0", ("Bone Pickaxe", "New tool, old smell. The mine says thanks.") },
        { "faro-minerador:1", ("Helmet Lamp", "Now you can see where the treats hide.") },
        { "faro-minerador:2", ("Treat Minecart", "Rails, cart and a lot of noise. Always packed.") },
        { "faro-minerador:3", ("Bacon Vein", "A whole layer of fossilized bacon. The nose never lies.") },
        { "iluminacao-caramelo:0", ("LED Streetlight", "The first lamp on the street. The delivery dog can finally read the house numbers.") },
        { "iluminacao-caramelo:1", ("Motion Sensor", "Turns on when the delivery dog arrives. Also turns on when the cat walks by.") },
        { "iluminacao-caramelo:2", ("Lit Street", "One lamp after another. No alley stays in the dark.") },
        { "iluminacao-caramelo:3", ("Corner Beacon", "Lights up the whole corner. The drone says thanks.") },
        { "fabrica-de-petiscos:0", ("Biscuit Conveyor", "The treats now travel on their own. Caramelo just supervises, mouth open.") },
        { "fabrica-de-petiscos:1", ("Industrial Oven", "Bakes a thousand treats a minute. The smell reaches three neighborhoods.") },
        { "fabrica-de-petiscos:2", ("Treat Silo", "Stock up to the ceiling. Caramelo already tried to dive in headfirst.") },
        { "fabrica-de-petiscos:3", ("Golden Paw Packaging", "Every pack has a paw print. Marketing is everything.") },
        { "fabrica-de-racao:0", ("Grain Grinder", "Crushes everything into bite-sized pieces.") },
        { "fabrica-de-racao:1", ("Industrial Mixer", "Grains, protein and a little love. In that order.") },
        { "fabrica-de-racao:2", ("Automatic Bagger", "One bag per second. Caramelo counts, loses count and starts over.") },
        { "fabrica-de-racao:3", ("Quality Seal", "Approved by the official taster: wet nose and a critical look.") },
        { "estatua-caramelo:0", ("Marble Base", "Looks majestic. The pigeons agree and land on it.") },
        { "estatua-caramelo:1", ("Heroic Pose", "Chest out, tail up. Postcard-worthy.") },
        { "estatua-caramelo:2", ("Gold-Plated", "Shines from afar. Tourists take photos and leave coins.") },
        { "estatua-caramelo:3", ("Protected Square", "The city declared it a landmark. The statue had no idea.") },
        { "templo-caramelo:0", ("Bone Bell", "Rings at noon and the whole neighborhood comes to pray.") },
        { "templo-caramelo:1", ("Treat Incense", "The smoke smells like bacon. Faith only grows.") },
        { "templo-caramelo:2", ("Stray Priest", "Blesses with a paw smack. Nobody complains.") },
        { "templo-caramelo:3", ("Caramelo Stained Glass", "Light comes in and paints the floor caramel. Beautiful and profitable.") },
        { "banco-canino:0", ("Reinforced Vault", "Steel door, bone padlock. The neighbor's cat gave up.") },
        { "banco-canino:1", ("Compound Interest", "The bone buried today becomes two tomorrow. Nobody explains how.") },
        { "banco-canino:2", ("Account Manager", "Smiles, wags his tail and suggests an investment. Almost always accepted.") },
        { "banco-canino:3", ("Bone Black Card", "Unlimited limit, as long as you have a treat as collateral.") },
        { "pet-shop-caramelo:0", ("Puppy Window", "Everybody stops on the sidewalk to look. Nobody leaves without a smile.") },
        { "pet-shop-caramelo:1", ("Neon Sign", "Lights up at night and draws customers from three neighborhoods.") },
        { "pet-shop-caramelo:2", ("Kibble Aisle", "Shelves up to the ceiling. Caramelo gets lost there on purpose.") },
        { "pet-shop-caramelo:3", ("Bath & Grooming", "Leaves smelling nice with a bow on the neck. Back next month.") },
        { "veterinaria-caramelo:0", ("Canine Stethoscope", "Listens to the heart, the tummy and, once in a while, the owner.") },
        { "veterinaria-caramelo:1", ("Vaccination Room", "Huge line. Nobody cries, only the owner.") },
        { "veterinaria-caramelo:2", ("Bone X-Ray", "Found the bone he buried in the backyard in 2019.") },
        { "veterinaria-caramelo:3", ("24-Hour Emergency", "Barking at three in the morning? The clinic is open.") },
        { "base-lunar-caramelo:0", ("Digging Crater", "Digging is instinct. On the Moon, nobody complains about the hole.") },
        { "base-lunar-caramelo:1", ("Pressure Suit", "Fishbowl helmet, ears out. Style above all.") },
        { "base-lunar-caramelo:2", ("Glass Dome", "A backyard on the Moon, with grass and everything. Weird, but it works.") },
        { "base-lunar-caramelo:3", ("Lunar Rover", "Moves slowly, but never loses sight of a bone.") },
        { "planta-lunar:0", ("Moon Seeds", "Came from the crater in an astronaut's pocket.") },
        { "planta-lunar:1", ("Silver Dew", "Shows up only at dawn and glows in the dark.") },
        { "planta-lunar:2", ("Crater Greenhouse", "A glass dome at the bottom of the crater. Warm and quiet.") },
        { "planta-lunar:3", ("Moon Pollen", "Makes the dog sneeze stars.") },
        { "pudim-lunar:0", ("Secret Moon Recipe", "Written on a moon plant leaf. Only Caramelo can read the handwriting.") },
        { "pudim-lunar:1", ("Starry Caramel Sauce", "Drips shining like a night sky.") },
        { "pudim-lunar:2", ("Half-Moon Mold", "Every pudding comes out with a moon on top.") },
        { "pudim-lunar:3", ("Moon Hen Eggs", "Lays one egg a night, always with a golden yolk.") },
        { "fabrica-de-pudim-lunar:0", ("Moon Production Line", "The pudding molds roll by on their own along the conveyor.") },
        { "fabrica-de-pudim-lunar:1", ("Industrial Cauldron", "Stirs ten thousand puddings at once with a giant spoon.") },
        { "fabrica-de-pudim-lunar:2", ("Factory Uniform", "Hat, boots and a tail sticking out through the little hole.") },
        { "fabrica-de-pudim-lunar:3", ("Quality Control", "Every pudding is tasted by a dog. All of them are approved.") },
        { "cometa-caramelo:0", ("Shining Tail", "Every little piece that falls becomes a bone, and every bone becomes a coin.") },
        { "cometa-caramelo:1", ("Orbital Route", "Passes twice a year. Caramelo marks it on the calendar.") },
        { "cometa-caramelo:2", ("Caramelo Core", "There's a huge bone inside. Nobody knows whose it was.") },
        { "cometa-caramelo:3", ("Meteor Shower", "It rains bones, it rains coins. Bring an umbrella.") },
        { "sol-caramelo:0", ("Sunbeam", "A ray crosses the window and Caramelo lies right in the middle. Couldn't be more motivated.") },
        { "sol-caramelo:1", ("Solar Nap", "A nap in the sun, battery full. Wakes up ready for three more deliveries.") },
        { "sol-caramelo:2", ("Caramelo Corona", "The fiery crown sways like a happy tail. You can feel it from far away.") },
        { "sol-caramelo:3", ("Biscuit Eclipse", "The Moon passes in front and, for a moment, the Sun becomes one giant biscuit.") },
    };
}
