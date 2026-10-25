using System.Collections.Generic;
using System.Linq;

public enum UpgradeKind { ClickMultiplier, ClickPercent, GlobalMultiplier, Building }

public class BuildingDef
{
    public string Key;
    public string Name;
    public string Description;
    public double BaseCost;
    public double BaseCps;
    // Procedural icon index, or a PNG under Resources/Icons when IconFile is set.
    public int Icon;
    public string IconFile;
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
    public string IconFile;
    // Short line shown next to the price when the description is only flavor text.
    public string Effect;
}

// Name, flavor text and icon for one tier of a building's upgrades.
public class TierText
{
    public string Building;
    public int Tier;
    public string Name;
    public string Description;
    public string IconFile;
}

public static class GameDefs
{
    public const double CostGrowth = 1.15;

    public static readonly BuildingDef[] Buildings =
    {
        new BuildingDef { Key = "o-inicio", Name = "O Início", Description = "O início do sonho do nosso ambicioso cachorro caramelo brasileiro: The Caramelo Corps.", BaseCost = 10, BaseCps = 0.1, IconFile = "o-inicio" },
        new BuildingDef { Key = "ossinho", Name = "Ossinho de Borracha", Description = "Um brinquedinho que sempre rende uns petiscos.", BaseCost = 15, BaseCps = 0.2, Icon = 0 },
        new BuildingDef { Key = "vira-lata", Name = "Amigo Vira-lata", Description = "Você e seus amigos de rua buscam por moedas e latas de lixo para investir na Caramelo Corp.", BaseCost = 40, BaseCps = 0.301, IconFile = "vira-lata" },
        new BuildingDef { Key = "tigela", Name = "Tigela Cheia", Description = "Sempre sobra um petisco no fundo.", BaseCost = 100, BaseCps = 1, Icon = 1 },
        new BuildingDef { Key = "venda-petiscos", Name = "Venda de Petiscos", Description = "O Caramelo faz petiscos caseiros e vende na rua.", BaseCost = 350, BaseCps = 2.17, IconFile = "venda-petiscos" },
        new BuildingDef { Key = "passeador", Name = "Passeador", Description = "Passeia com o Caramelo e acha petiscos pelo caminho.", BaseCost = 1100, BaseCps = 8, Icon = 2 },
        new BuildingDef { Key = "cachorro-entregador", Name = "Caramelo Entregador", Description = "Cachorros entregadores contratados pela Caramelo Corps.", BaseCost = 3500, BaseCps = 18.0, IconFile = "cachorro-entregador" },
        new BuildingDef { Key = "racao-caramelo", Name = "Ração Caramelo", Description = "Ração caseira misteriosa fabricada pelo Caramelo.", BaseCost = 7000, BaseCps = 32.7, IconFile = "racao-caramelo" },
        new BuildingDef { Key = "petshop", Name = "Pet Shop", Description = "Vende petiscos para toda a vizinhança.", BaseCost = 12000, BaseCps = 47, Icon = 3 },
        new BuildingDef { Key = "drone-entregas", Name = "Drone de Entregas", Description = "Faz a entrega dos petiscos e rações fabricados pelo Caramelo.", BaseCost = 40000, BaseCps = 154.0, IconFile = "drone-entregas" },
        new BuildingDef { Key = "padaria", Name = "Padaria de Biscoitos", Description = "Biscoitinhos quentinhos o dia inteiro.", BaseCost = 130000, BaseCps = 260, Icon = 4 },
        new BuildingDef { Key = "faro-minerador", Name = "Mina de Petiscos", Description = "Foi encontrada uma mina de petiscos caninos super raros! A Caramelo Corp vai cuidar e “preservar” ela com todo o carinho do mundo. Fechada para visitantes.", BaseCost = 450000, BaseCps = 1200, IconFile = "faro-minerador" },
        new BuildingDef { Key = "fabrica-racao", Name = "Fábrica de Ração Pequena", Description = "Produção industrial de petiscos.", BaseCost = 1400000, BaseCps = 1400, Icon = 5 },
        new BuildingDef { Key = "iluminacao-caramelo", Name = "Iluminação Caramelo", Description = "O Caramelo investe na iluminação da cidade para garantir a segurança de seus entregadores e drones durante a noite.", BaseCost = 5000000, BaseCps = 13200.0, IconFile = "iluminacao-caramelo" },
        new BuildingDef { Key = "fabrica-de-petiscos", Name = "Fábrica de Petiscos", Description = "Os investimentos do Caramelo seguem com tudo e a primeira fábrica de petiscos foi criada.", BaseCost = 16000000, BaseCps = 38300.0, IconFile = "fabrica-de-petiscos" },
        new BuildingDef { Key = "fabrica-de-racao", Name = "Fábrica de Ração", Description = "A Ração Caramelo foi um sucesso e por isso abriram a primeira fábrica de ração.", BaseCost = 50000000, BaseCps = 109000.0, IconFile = "fabrica-de-racao" },
        new BuildingDef { Key = "estatua-caramelo", Name = "Estátua do Caramelo", Description = "Todo mundo venera o Caramelo.", BaseCost = 160000000, BaseCps = 317000.0, IconFile = "estatua-caramelo" },
        new BuildingDef { Key = "parque", Name = "Parque dos Vira-latas", Description = "Uma matilha inteira cavando tesouros.", BaseCost = 330000000, BaseCps = 600000, Icon = 7 },
        new BuildingDef { Key = "templo-caramelo", Name = "Templo do Caramelo", Description = "Fiéis deixam oferendas o dia todo.", BaseCost = 1000000000.0, BaseCps = 1640000.0, IconFile = "templo-caramelo" },
        new BuildingDef { Key = "banco-canino", Name = "Banco Canino", Description = "Rende juros em osso e em moeda.", BaseCost = 5000000000.0, BaseCps = 7430000.0, IconFile = "banco-canino" },
        new BuildingDef { Key = "pet-shop-caramelo", Name = "Pet Shop Caramelo", Description = "Ração, brinquedo e banho. Todo vira-lata do bairro passa por aqui.", BaseCost = 25000000000.0, BaseCps = 33800000.0, IconFile = "pet-shop-caramelo" },
        new BuildingDef { Key = "veterinaria-caramelo", Name = "Veterinária Caramelo", Description = "Consulta, vacina e um biscoito no final. Todo vira-lata sai de rabo abanando.", BaseCost = 150000000000.0, BaseCps = 184000000.0, IconFile = "veterinaria-caramelo" },
        new BuildingDef { Key = "base-lunar-caramelo", Name = "Base Lunar Caramelo", Description = "Plantaram uma bandeira na Lua. E um osso.", BaseCost = 900000000000.0, BaseCps = 1010000000.0, IconFile = "base-lunar-caramelo" },
        new BuildingDef { Key = "planta-lunar", Name = "Planta Lunar", Description = "Uma planta misteriosa descoberta na Lua. Dela saem os melhores pudins caninos.", BaseCost = 6000000000000.0, BaseCps = 6090000000.0, IconFile = "planta-lunar" },
        new BuildingDef { Key = "pudim-lunar", Name = "Pudim Lunar Caseiro", Description = "Feito com a planta lunar. Todo cachorro entra na fila por uma colherada.", BaseCost = 40000000000000.0, BaseCps = 36900000000.0, IconFile = "pudim-lunar" },
        new BuildingDef { Key = "fabrica-de-pudim-lunar", Name = "Fábrica de Pudim Lunar", Description = "Os cachorros gostaram tanto do nutritivo pudim do Caramelo que ele montou a própria fábrica na Lua.", BaseCost = 250000000000000.0, BaseCps = 210000000000.0, IconFile = "fabrica-de-pudim-lunar" },
        new BuildingDef { Key = "cometa-caramelo", Name = "Cometa Caramelo", Description = "Passa raspando e deixa uma chuva de ossos e moedas.", BaseCost = 1600000000000000.0, BaseCps = 1220000000000.0, IconFile = "cometa-caramelo" },
    };

    public const int PawIcon = 9;
    public const int StarIcon = 7;

    // Names and flavor text for the tiers of buildings that have their own, keyed by building and tier.
    static readonly TierText[] TierTexts =
    {
        new TierText { Building = "o-inicio", Tier = 0, Name = "Sonho Grande", Description = "Quem sonha pequeno não leva osso pra casa.", IconFile = "o-inicio" },
        new TierText { Building = "o-inicio", Tier = 1, Name = "Primeiro Cartão de Visita", Description = "Papel reciclado, patinha impressa. Já parece empresa de verdade.", IconFile = "o-inicio" },
        new TierText { Building = "o-inicio", Tier = 2, Name = "Garagem de Startup", Description = "Duas caixas de papelão, uma tomada emprestada e muita ambição.", IconFile = "o-inicio" },
        new TierText { Building = "o-inicio", Tier = 3, Name = "Primeiro Investidor", Description = "Um gato rico apostou na ideia. Ninguém sabe por quê.", IconFile = "o-inicio" },
        new TierText { Building = "vira-lata", Tier = 0, Name = "Instinto Malandro", Description = "O Caramelo e seus amigos vira latas aprimoram suas habilidades de coletar moedas.", IconFile = "vira-lata" },
        new TierText { Building = "vira-lata", Tier = 1, Name = "Rodinha da Esquina", Description = "Mais um vira-lata entra na roda. Agora ninguém passa na rua sem pagar o pedágio.", IconFile = "vira-lata" },
        new TierText { Building = "vira-lata", Tier = 2, Name = "Mapa das Lixeiras", Description = "Cada lixeira do bairro marcada com um X. Os melhores achados ficam na zona nobre.", IconFile = "vira-lata" },
        new TierText { Building = "vira-lata", Tier = 3, Name = "Sindicato dos Vira-latas", Description = "Eles se organizaram e exigem a parte deles. Sem advogado, só latido.", IconFile = "vira-lata" },
        new TierText { Building = "venda-petiscos", Tier = 0, Name = "Petisco Crocante", Description = "Crec, crec! Só o barulhinho já faz a fila crescer na barraca.", IconFile = "venda-petiscos-1" },
        new TierText { Building = "venda-petiscos", Tier = 1, Name = "Petisco Gourmet", Description = "Agora com receita de família e guardanapo de pano. Ninguém mais pergunta o preço.", IconFile = "venda-petiscos-2" },
        new TierText { Building = "venda-petiscos", Tier = 2, Name = "Petisco Baconístico", Description = "O cheiro de bacon atravessa o bairro inteiro. Até os gatos entram na fila.", IconFile = "venda-petiscos-3" },
        new TierText { Building = "venda-petiscos", Tier = 3, Name = "Petisco Folheado a Ouro", Description = "Comestível, brilhante e totalmente desnecessário. Por isso mesmo, caríssimo.", IconFile = "venda-petiscos-4" },
        new TierText { Building = "cachorro-entregador", Tier = 0, Name = "Rastreador GPS", Description = "Agora ele só se perde de propósito.", IconFile = "cachorro-entregador" },
        new TierText { Building = "cachorro-entregador", Tier = 1, Name = "Mochila Térmica", Description = "Petisco chega quentinho, osso chega gelado. Como deve ser.", IconFile = "cachorro-entregador" },
        new TierText { Building = "cachorro-entregador", Tier = 2, Name = "Rota do Atalho", Description = "Passa pelo quintal do vizinho. O cachorro do vizinho já foi avisado.", IconFile = "cachorro-entregador" },
        new TierText { Building = "cachorro-entregador", Tier = 3, Name = "Entrega no Mesmo Dia", Description = "Pediu, latiu, chegou. Antes mesmo de o cliente desligar.", IconFile = "cachorro-entregador" },
        new TierText { Building = "racao-caramelo", Tier = 0, Name = "Receita Secreta", Description = "Nem o próprio Caramelo lembra o que tem dentro. Mas vende.", IconFile = "racao-caramelo" },
        new TierText { Building = "racao-caramelo", Tier = 1, Name = "Sabor Churrasco", Description = "Domingo em todo pote. O cheiro faz o quarteirão chorar.", IconFile = "racao-caramelo" },
        new TierText { Building = "racao-caramelo", Tier = 2, Name = "Embalagem Premium", Description = "Saquinho dourado com osso em relevo. Chique até pra gato.", IconFile = "racao-caramelo" },
        new TierText { Building = "racao-caramelo", Tier = 3, Name = "Selo Aprovado pelo Caramelo", Description = "Ele cheirou, lambeu e abanou o rabo. Está certificado.", IconFile = "racao-caramelo" },
        new TierText { Building = "drone-entregas", Tier = 0, Name = "Hélice Reforçada", Description = "Aguenta pássaro, chuva e latido de cachorro bravo.", IconFile = "drone-entregas" },
        new TierText { Building = "drone-entregas", Tier = 1, Name = "Bateria de Longa Duração", Description = "Voa o dia inteiro. Só pousa pra ganhar carinho.", IconFile = "drone-entregas" },
        new TierText { Building = "drone-entregas", Tier = 2, Name = "Câmera Farejadora", Description = "Reconhece petisco a 50 metros. Reconhece gato a 100.", IconFile = "drone-entregas" },
        new TierText { Building = "drone-entregas", Tier = 3, Name = "Enxame de Drones", Description = "Zunindo em formação. O carteiro humano pediu demissão.", IconFile = "drone-entregas" },
        new TierText { Building = "faro-minerador", Tier = 0, Name = "Picareta de Osso", Description = "Ferramenta nova, cheiro antigo. A mina agradece.", IconFile = "faro-minerador" },
        new TierText { Building = "faro-minerador", Tier = 1, Name = "Lanterna de Capacete", Description = "Agora dá pra ver onde os petiscos se escondem.", IconFile = "faro-minerador" },
        new TierText { Building = "faro-minerador", Tier = 2, Name = "Vagonete de Petiscos", Description = "Trilho, carrinho e muito barulho. Sempre passa lotado.", IconFile = "faro-minerador" },
        new TierText { Building = "faro-minerador", Tier = 3, Name = "Veio de Bacon", Description = "Uma camada inteira de bacon fossilizado. O faro não mente.", IconFile = "faro-minerador" },
        new TierText { Building = "iluminacao-caramelo", Tier = 0, Name = "Poste de LED", Description = "O primeiro poste da rua. O entregador finalmente enxerga o número das casas.", IconFile = "iluminacao-caramelo" },
        new TierText { Building = "iluminacao-caramelo", Tier = 1, Name = "Sensor de Presença", Description = "Acende quando o entregador chega. Também acende quando o gato passa.", IconFile = "iluminacao-caramelo" },
        new TierText { Building = "iluminacao-caramelo", Tier = 2, Name = "Rua Iluminada", Description = "Um poste atrás do outro. Nenhum beco fica no escuro.", IconFile = "iluminacao-caramelo" },
        new TierText { Building = "iluminacao-caramelo", Tier = 3, Name = "Farol de Esquina", Description = "Ilumina a esquina inteira. O drone agradece.", IconFile = "iluminacao-caramelo" },
        new TierText { Building = "fabrica-de-petiscos", Tier = 0, Name = "Esteira de Biscoitos", Description = "Os petiscos agora viajam sozinhos. O Caramelo só fiscaliza, de boca aberta.", IconFile = "fabrica-de-petiscos" },
        new TierText { Building = "fabrica-de-petiscos", Tier = 1, Name = "Forno Industrial", Description = "Assa mil petiscos por minuto. O cheiro chega em três bairros.", IconFile = "fabrica-de-petiscos" },
        new TierText { Building = "fabrica-de-petiscos", Tier = 2, Name = "Silo de Petiscos", Description = "Estoque até o teto. O Caramelo já tentou entrar de cabeça.", IconFile = "fabrica-de-petiscos" },
        new TierText { Building = "fabrica-de-petiscos", Tier = 3, Name = "Embalagem Pata Dourada", Description = "Cada pacote leva uma patinha estampada. Marketing é tudo.", IconFile = "fabrica-de-petiscos" },
        new TierText { Building = "fabrica-de-racao", Tier = 0, Name = "Moedor de Grãos", Description = "Tritura tudo em pedacinhos do tamanho de uma mordida.", IconFile = "fabrica-de-racao" },
        new TierText { Building = "fabrica-de-racao", Tier = 1, Name = "Misturador Industrial", Description = "Grãos, proteína e um pouco de amor. Nessa ordem.", IconFile = "fabrica-de-racao" },
        new TierText { Building = "fabrica-de-racao", Tier = 2, Name = "Ensacadeira Automática", Description = "Um saco por segundo. O Caramelo conta, perde a conta e começa de novo.", IconFile = "fabrica-de-racao" },
        new TierText { Building = "fabrica-de-racao", Tier = 3, Name = "Selo de Qualidade", Description = "Aprovado pelo provador oficial: focinho molhado e olhar crítico.", IconFile = "fabrica-de-racao" },
        new TierText { Building = "estatua-caramelo", Tier = 0, Name = "Base de Mármore", Description = "Ficou imponente. Os pombos concordam e pousam.", IconFile = "estatua-caramelo" },
        new TierText { Building = "estatua-caramelo", Tier = 1, Name = "Pose Heroica", Description = "Peito estufado, rabo pra cima. Digno de cartão-postal.", IconFile = "estatua-caramelo" },
        new TierText { Building = "estatua-caramelo", Tier = 2, Name = "Banhada a Ouro", Description = "Brilha de longe. Turista tira foto e deixa moeda.", IconFile = "estatua-caramelo" },
        new TierText { Building = "estatua-caramelo", Tier = 3, Name = "Praça Tombada", Description = "A prefeitura declarou patrimônio. A estátua nem sabia.", IconFile = "estatua-caramelo" },
        new TierText { Building = "templo-caramelo", Tier = 0, Name = "Sino de Osso", Description = "Toca ao meio-dia e o bairro inteiro vem rezar.", IconFile = "templo-caramelo" },
        new TierText { Building = "templo-caramelo", Tier = 1, Name = "Incenso de Petisco", Description = "A fumaça cheira a bacon. A fé só aumenta.", IconFile = "templo-caramelo" },
        new TierText { Building = "templo-caramelo", Tier = 2, Name = "Sacerdote Vira-lata", Description = "Abençoa com patada. Ninguém reclama.", IconFile = "templo-caramelo" },
        new TierText { Building = "templo-caramelo", Tier = 3, Name = "Vitral do Caramelo", Description = "A luz entra e pinta o chão de caramelo. Lindo e rentável.", IconFile = "templo-caramelo" },
        new TierText { Building = "banco-canino", Tier = 0, Name = "Cofre Reforçado", Description = "Porta de aço, cadeado de osso. O gato do vizinho desistiu.", IconFile = "banco-canino" },
        new TierText { Building = "banco-canino", Tier = 1, Name = "Juros Compostos", Description = "O osso enterrado hoje vira dois amanhã. Ninguém explica como.", IconFile = "banco-canino" },
        new TierText { Building = "banco-canino", Tier = 2, Name = "Gerente de Conta", Description = "Sorri, abana o rabo e sugere um investimento. Quase sempre aceitam.", IconFile = "banco-canino" },
        new TierText { Building = "banco-canino", Tier = 3, Name = "Cartão Black de Osso", Description = "Limite ilimitado, desde que tenha petisco de garantia.", IconFile = "banco-canino" },
        new TierText { Building = "pet-shop-caramelo", Tier = 0, Name = "Vitrine de Filhotes", Description = "Todo mundo para na calçada pra olhar. Ninguém vai embora sem sorrir.", IconFile = "pet-shop-caramelo" },
        new TierText { Building = "pet-shop-caramelo", Tier = 1, Name = "Placa de Néon", Description = "Acende de noite e atrai cliente de três bairros.", IconFile = "pet-shop-caramelo" },
        new TierText { Building = "pet-shop-caramelo", Tier = 2, Name = "Corredor de Rações", Description = "Prateleiras até o teto. O Caramelo se perde lá de propósito.", IconFile = "pet-shop-caramelo" },
        new TierText { Building = "pet-shop-caramelo", Tier = 3, Name = "Banho e Tosa", Description = "Sai cheiroso e com laço no pescoço. Volta no mês que vem.", IconFile = "pet-shop-caramelo" },
        new TierText { Building = "veterinaria-caramelo", Tier = 0, Name = "Estetoscópio Canino", Description = "Ouve o coração, a barriga e, de vez em quando, o dono.", IconFile = "veterinaria-caramelo" },
        new TierText { Building = "veterinaria-caramelo", Tier = 1, Name = "Sala de Vacinação", Description = "Fila enorme. Ninguém chora, só o dono.", IconFile = "veterinaria-caramelo" },
        new TierText { Building = "veterinaria-caramelo", Tier = 2, Name = "Raio-X de Ossos", Description = "Descobriu o osso que ele enterrou no quintal em 2019.", IconFile = "veterinaria-caramelo" },
        new TierText { Building = "veterinaria-caramelo", Tier = 3, Name = "Plantão 24 Horas", Description = "Latido às três da manhã? A clínica atende.", IconFile = "veterinaria-caramelo" },
        new TierText { Building = "base-lunar-caramelo", Tier = 0, Name = "Cratera de Escavação", Description = "Cavar é instinto. Na Lua, ninguém reclama do buraco.", IconFile = "base-lunar-caramelo" },
        new TierText { Building = "base-lunar-caramelo", Tier = 1, Name = "Traje Pressurizado", Description = "Capacete de aquário, orelha de fora. Estilo acima de tudo.", IconFile = "base-lunar-caramelo" },
        new TierText { Building = "base-lunar-caramelo", Tier = 2, Name = "Cúpula de Vidro", Description = "Um quintal na Lua, com grama e tudo. Esquisito, mas funciona.", IconFile = "base-lunar-caramelo" },
        new TierText { Building = "base-lunar-caramelo", Tier = 3, Name = "Rover Lunar", Description = "Anda devagar, mas nunca perde um osso de vista.", IconFile = "base-lunar-caramelo" },
        new TierText { Building = "planta-lunar", Tier = 0, Name = "Sementes Lunares", Description = "Vieram da cratera no bolso de um astronauta.", IconFile = "planta-lunar" },
        new TierText { Building = "planta-lunar", Tier = 1, Name = "Orvalho Prateado", Description = "Surge só de madrugada e brilha no escuro.", IconFile = "planta-lunar" },
        new TierText { Building = "planta-lunar", Tier = 2, Name = "Estufa de Cratera", Description = "Um domo de vidro no fundo da cratera. Quentinho e silencioso.", IconFile = "planta-lunar" },
        new TierText { Building = "planta-lunar", Tier = 3, Name = "Pólen Lunar", Description = "Faz o cachorro espirrar estrelas.", IconFile = "planta-lunar" },
        new TierText { Building = "pudim-lunar", Tier = 0, Name = "Receita Secreta Lunar", Description = "Anotada numa folha de planta lunar. Só o Caramelo entende a letra.", IconFile = "pudim-lunar" },
        new TierText { Building = "pudim-lunar", Tier = 1, Name = "Calda de Caramelo Estelar", Description = "Escorre brilhando como um céu de noite.", IconFile = "pudim-lunar" },
        new TierText { Building = "pudim-lunar", Tier = 2, Name = "Forma de Meia-Lua", Description = "Todo pudim sai com uma lua em cima.", IconFile = "pudim-lunar" },
        new TierText { Building = "pudim-lunar", Tier = 3, Name = "Ovos de Galinha Lunar", Description = "Põe um ovo por noite, sempre de gema dourada.", IconFile = "pudim-lunar" },
        new TierText { Building = "fabrica-de-pudim-lunar", Tier = 0, Name = "Linha de Produção Lunar", Description = "As formas de pudim passam sozinhas pela esteira.", IconFile = "fabrica-de-pudim-lunar" },
        new TierText { Building = "fabrica-de-pudim-lunar", Tier = 1, Name = "Caldeirão Industrial", Description = "Mexe dez mil pudins de uma vez, com uma colher gigante.", IconFile = "fabrica-de-pudim-lunar" },
        new TierText { Building = "fabrica-de-pudim-lunar", Tier = 2, Name = "Uniforme de Fábrica", Description = "Touca, bota e rabo para fora pelo buraquinho.", IconFile = "fabrica-de-pudim-lunar" },
        new TierText { Building = "fabrica-de-pudim-lunar", Tier = 3, Name = "Controle de Qualidade", Description = "Cada pudim é provado por um cachorro. Todos são aprovados.", IconFile = "fabrica-de-pudim-lunar" },
        new TierText { Building = "cometa-caramelo", Tier = 0, Name = "Cauda Brilhante", Description = "Cada pedacinho que cai vira osso, e cada osso vira moeda.", IconFile = "cometa-caramelo" },
        new TierText { Building = "cometa-caramelo", Tier = 1, Name = "Rota Orbital", Description = "Passa duas vezes por ano. O Caramelo marca no calendário.", IconFile = "cometa-caramelo" },
        new TierText { Building = "cometa-caramelo", Tier = 2, Name = "Núcleo Caramelo", Description = "Dentro tem um osso enorme. Ninguém sabe de quem era.", IconFile = "cometa-caramelo" },
        new TierText { Building = "cometa-caramelo", Tier = 3, Name = "Chuva de Meteoros", Description = "Chove osso, chove moeda. Leve o guarda-chuva.", IconFile = "cometa-caramelo" },
    };

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
            var building = Buildings[b];
            for (int t = 0; t < TierNames.Length; t++)
            {
                var text = FindTierText(building.Key, t);
                list.Add(new UpgradeDef
                {
                    Id = "b_" + building.Key + "_" + t,
                    Name = text != null ? text.Name : building.Name + " " + TierNames[t],
                    Description = text != null ? text.Description : building.Name + " produz o dobro.",
                    Effect = text != null ? "Produção de " + building.Name + " x2" : null,
                    Cost = building.BaseCost * TierCostMult[t],
                    Kind = UpgradeKind.Building,
                    Building = b,
                    RequiredOwned = TierOwned[t],
                    Icon = building.Icon,
                    IconFile = text != null ? text.IconFile : building.IconFile,
                });
            }
        }

        // Stable sort, so equal prices keep their order; then no two upgrades may share a price.
        var sorted = list.OrderBy(u => u.Cost).ToList();
        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].Cost <= sorted[i - 1].Cost) sorted[i].Cost = NextPrice(sorted[i - 1].Cost);
        }
        return sorted;
    }

    // About 5% above the given price, rounded up to three significant digits.
    static double NextPrice(double price)
    {
        double step = System.Math.Pow(10, System.Math.Floor(System.Math.Log10(price)) - 2);
        return System.Math.Ceiling(price * 1.05 / step) * step;
    }

    static TierText FindTierText(string building, int tier)
    {
        foreach (var text in TierTexts)
            if (text.Building == building && text.Tier == tier) return text;
        return null;
    }

    static Dictionary<string, UpgradeDef> BuildIndex()
    {
        var map = new Dictionary<string, UpgradeDef>();
        foreach (var u in Upgrades) map[u.Id] = u;
        return map;
    }
}
