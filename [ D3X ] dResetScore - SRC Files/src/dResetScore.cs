using CounterStrikeSharp.API.Core; 
 
namespace dResetScore; 
 
public class dResetScore : BasePlugin 
{ 
    public static dResetScore Instance { get; set; } = new dResetScore();
    public override string ModuleName => "[CS2] D3X - [ Reset Score ]";
    public override string ModuleAuthor => "D3X";
    public override string ModuleDescription => "Plugin Umożliwia graczom zresetowanie swoich statystyk.";
    public override string ModuleVersion => "1.0.0";

    public static Dictionary<ulong, DateTime> lastResetScoreUsage = new Dictionary<ulong, DateTime>();

    public override void Load(bool hotReload) {

        Instance = this;
        
        Config.Initialize();
        Command.Load();
    }
} 
